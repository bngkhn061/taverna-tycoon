using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Taverna
{
    /// <summary>Yerel ölçüm sayaçları (analytics_counters.json). Yalnızca bu cihazdaki oturumları sayar.</summary>
    [Serializable]
    public class AnalyticsCounters
    {
        /// <summary>Olay adları ve sayıları (paralel listeler; JsonUtility sözlük yazmaz).</summary>
        public List<string> names = new();
        public List<int> counts = new();
        /// <summary>Rehberi başlatan / bitiren / atlayan oturum sayısı.</summary>
        public int tutorialStartedSessions, tutorialCompletedSessions, tutorialSkippedSessions;
        /// <summary>Rehber adımı başına toplam süre (sn) ve örnek sayısı; adım başına atlanma sayısı (indeks = adım − 1).</summary>
        public float[] stepSeconds = new float[Tutorial.StepCount];
        public int[] stepSamples = new int[Tutorial.StepCount];
        public int[] skipAtStep = new int[Tutorial.StepCount];
        /// <summary>Aynı oturumun iki kez sayılmaması için son sayılan oturum kimlikleri.</summary>
        public string lastStartSession = "", lastCompleteSession = "", lastSkipSession = "";

        /// <summary>Olay sayacını bir artırır.</summary>
        public void Bump(string name)
        {
            int i = names.IndexOf(name);
            if (i < 0) { names.Add(name); counts.Add(1); }
            else counts[i]++;
        }

        /// <summary>Olayın sayısı (hiç yoksa 0).</summary>
        public int Count(string name)
        {
            int i = names.IndexOf(name);
            return i < 0 ? 0 : counts[i];
        }

        /// <summary>Ortalama süresi en uzun rehber adımı (1 tabanlı; veri yoksa 0) ve ortalaması.</summary>
        public (int step, float avg) SlowestStep()
        {
            int best = 0; float bestAvg = 0f;
            for (int i = 0; i < stepSamples.Length && i < stepSeconds.Length; i++)
            {
                if (stepSamples[i] <= 0) continue;
                float a = stepSeconds[i] / stepSamples[i];
                if (a > bestAvg) { bestAvg = a; best = i + 1; }
            }
            return (best, bestAvg);
        }

        /// <summary>En çok atlanan rehber adımı (1 tabanlı; atlama yoksa 0).</summary>
        public int MostSkippedStep()
        {
            int best = 0, n = 0;
            for (int i = 0; i < skipAtStep.Length; i++) if (skipAtStep[i] > n) { n = skipAtStep[i]; best = i + 1; }
            return best;
        }
    }

    /// <summary>
    /// Ölçümün test / yerel uygulaması (v1.6): olayları <c>persistentDataPath/analytics.jsonl</c> dosyasına satır satır JSON
    /// olarak yazar (dosya ~1 MB'ı geçince baştan başlar) ve <see cref="AnalyticsCounters"/> tutar. Veri cihazdan çıkmaz;
    /// "kaç oyuncu bitirdi" gibi gerçek sayılar ancak gerçek hizmet bağlanınca toplanır.
    /// Satırlar bellekte biriktirilir, diske yazma arka planda yapılır (kapanışta eşzamanlı).
    /// </summary>
    public class LocalAnalyticsService : IAnalyticsService
    {
        /// <summary>Olay dosyasının üst sınırı (bayt); aşılınca dosya baştan başlar.</summary>
        public const long MaxFileBytes = 1024 * 1024;
        public const string FileName = "analytics.jsonl", CountersFileName = "analytics_counters.json";

        readonly string _path, _countersPath;
        readonly List<string> _buffer = new();
        readonly object _fileLock = new();
        bool _countersDirty;

        /// <summary>Sayaçlar (oturumlar arası kalıcı).</summary>
        public AnalyticsCounters Counters { get; private set; } = new();
        /// <summary>Bu süreçte gönderilen olay sayısı.</summary>
        public int SentThisProcess { get; private set; }
        /// <summary>Olay dosyasının tam yolu.</summary>
        public string FilePath => _path;

        /// <summary>Dosyaları <paramref name="directory"/> altında tutar (oyunda Application.persistentDataPath).</summary>
        public LocalAnalyticsService(string directory)
        {
            _path = Path.Combine(directory, FileName);
            _countersPath = Path.Combine(directory, CountersFileName);
            try
            {
                if (File.Exists(_countersPath))
                    Counters = JsonUtility.FromJson<AnalyticsCounters>(File.ReadAllText(_countersPath)) ?? new AnalyticsCounters();
            }
            catch (Exception e) { Debug.LogWarning("[Ölçüm] sayaçlar okunamadı: " + e.Message); Counters = new AnalyticsCounters(); }
            FixArrays(Counters);
        }

        static void FixArrays(AnalyticsCounters c)
        {
            if (c.stepSeconds == null || c.stepSeconds.Length != Tutorial.StepCount) Array.Resize(ref c.stepSeconds, Tutorial.StepCount);
            if (c.stepSamples == null || c.stepSamples.Length != Tutorial.StepCount) Array.Resize(ref c.stepSamples, Tutorial.StepCount);
            if (c.skipAtStep == null || c.skipAtStep.Length != Tutorial.StepCount) Array.Resize(ref c.skipAtStep, Tutorial.StepCount);
            c.names ??= new List<string>();
            c.counts ??= new List<int>();
        }

        /// <inheritdoc/>
        public bool IsTest => true;

        /// <inheritdoc/>
        public void Track(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            string line = ToJsonLine(DateTime.UtcNow, Services.SessionId, eventName, parameters);
            lock (_fileLock) _buffer.Add(line);
            SentThisProcess++;
            Count(eventName, parameters);
        }

        /// <summary>Sayaçları günceller (rehber hunisi ve olay sayıları).</summary>
        void Count(string eventName, IReadOnlyDictionary<string, object> p)
        {
            var c = Counters;
            string session = Services.SessionId;
            c.Bump(eventName);
            switch (eventName)
            {
                case "tutorial_start":
                    if (c.lastStartSession != session) { c.lastStartSession = session; c.tutorialStartedSessions++; }
                    break;
                case "tutorial_complete":
                    if (c.lastCompleteSession != session) { c.lastCompleteSession = session; c.tutorialCompletedSessions++; }
                    break;
                case "tutorial_skip":
                    if (c.lastSkipSession != session) { c.lastSkipSession = session; c.tutorialSkippedSessions++; }
                    if (TryInt(p, "adim", out int sk) && sk >= 1 && sk <= c.skipAtStep.Length) c.skipAtStep[sk - 1]++;
                    break;
                case "tutorial_step":
                    if (TryInt(p, "adim", out int st) && st >= 1 && st <= c.stepSeconds.Length && TryFloat(p, "sure_sn", out float sec))
                    {
                        c.stepSeconds[st - 1] += sec;
                        c.stepSamples[st - 1]++;
                    }
                    break;
            }
            _countersDirty = true;
        }

        static bool TryInt(IReadOnlyDictionary<string, object> p, string k, out int v)
        {
            v = 0;
            if (p == null || !p.TryGetValue(k, out var o) || o == null) return false;
            try { v = Convert.ToInt32(o, CultureInfo.InvariantCulture); return true; } catch { return false; }
        }

        static bool TryFloat(IReadOnlyDictionary<string, object> p, string k, out float v)
        {
            v = 0f;
            if (p == null || !p.TryGetValue(k, out var o) || o == null) return false;
            try { v = Convert.ToSingle(o, CultureInfo.InvariantCulture); return true; } catch { return false; }
        }

        /// <inheritdoc/>
        public void Flush(bool blocking)
        {
            string[] lines;
            lock (_fileLock)
            {
                if (_buffer.Count == 0 && !_countersDirty) return;
                lines = _buffer.ToArray();
                _buffer.Clear();
            }
            string counters = _countersDirty ? JsonUtility.ToJson(Counters) : null;   // JSON ana iş parçacığında
            _countersDirty = false;
            if (blocking) Write(lines, counters);
            else Task.Run(() => Write(lines, counters));
        }

        void Write(string[] lines, string counters)
        {
            lock (_fileLock)
            {
                try
                {
                    if (lines.Length > 0)
                    {
                        var sb = new StringBuilder();
                        foreach (var l in lines) sb.Append(l).Append('\n');
                        var text = sb.ToString();
                        long size = File.Exists(_path) ? new FileInfo(_path).Length : 0;
                        if (size + Encoding.UTF8.GetByteCount(text) > MaxFileBytes) File.WriteAllText(_path, text);   // dolunca baştan
                        else File.AppendAllText(_path, text);
                    }
                    if (counters != null) File.WriteAllText(_countersPath, counters);
                }
                catch (Exception e) { Debug.LogWarning("[Ölçüm] dosyaya yazılamadı: " + e.Message); }
            }
        }

        /// <summary>Ayarlar ekranındaki geliştirici özet satırı.</summary>
        public string SummaryText()
        {
            var c = Counters;
            var (slow, avg) = c.SlowestStep();
            int skip = c.MostSkippedStep();
            return $"Ölçüm özeti (bu cihaz): rehber başlatan {c.tutorialStartedSessions} · bitiren {c.tutorialCompletedSessions} · atlayan {c.tutorialSkippedSessions} oturum" +
                   (slow > 0 ? $" · en çok takılınan adım {slow} (ort. {avg:0} sn)" : "") +
                   (skip > 0 ? $" · en çok atlanan adım {skip}" : "");
        }

        // ------------------------------------------------------------------ JSON satırı
        /// <summary>Tek olay satırı: {"zaman":…,"oturum":…,"olay":…,"p":{…}}.</summary>
        public static string ToJsonLine(DateTime utc, string session, string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            var sb = new StringBuilder(96);
            sb.Append("{\"zaman\":\"").Append(utc.ToString("o", CultureInfo.InvariantCulture)).Append("\",\"oturum\":");
            AppendString(sb, session);
            sb.Append(",\"olay\":");
            AppendString(sb, eventName);
            sb.Append(",\"p\":{");
            bool first = true;
            if (parameters != null)
                foreach (var kv in parameters)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    AppendString(sb, kv.Key);
                    sb.Append(':');
                    AppendValue(sb, kv.Value);
                }
            sb.Append("}}");
            return sb.ToString();
        }

        static void AppendValue(StringBuilder sb, object v)
        {
            switch (v)
            {
                case null: sb.Append("null"); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int or long or short or byte: sb.Append(Convert.ToString(v, CultureInfo.InvariantCulture)); break;
                case float f: sb.Append(float.IsFinite(f) ? f.ToString("0.###", CultureInfo.InvariantCulture) : "null"); break;
                case double d: sb.Append(double.IsFinite(d) ? d.ToString("0.###", CultureInfo.InvariantCulture) : "null"); break;
                default: AppendString(sb, Convert.ToString(v, CultureInfo.InvariantCulture)); break;
            }
        }

        static void AppendString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char ch in s ?? "")
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4"));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
