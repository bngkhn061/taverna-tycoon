#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.4 ses teşhisi ve sessiz test (production/qa/playtests/feedback-2026-11-22.md madde 2).
    /// "-autotest-hidden" koşularında sahne yüklenmeden kurulur:
    /// <list type="bullet">
    /// <item>Sessiz test: ana ses düzeyi (<see cref="Sfx.MasterVolume"/>) −60 dB'e iner; gizli test sahibin hoparlöründen duyulmaz.</item>
    /// <item>Kayıt: Unity'nin son karışımı (ana çıkış, <see cref="AudioListener.GetOutputData"/>) her karede okunur ve ses saatine
    ///       (<see cref="AudioSettings.dspTime"/>) göre yalnızca yeni örnekler eklenerek kesintisiz dikilir. Dinleyici bileşeni olsun
    ///       olmasın çalışır (v2.3'te oyun sahnesinde AudioListener yoktu). Kısık düzey yazarken geri çarpılır.</item>
    /// </list>
    /// WAV 32 bit kayan noktalı yazılır (tools/audio/analyze_capture.py çözümler). Yalnızca geliştirme derlemesinde.
    /// </summary>
    public class AudioCapture : MonoBehaviour
    {
        public static AudioCapture I { get; private set; }
        /// <summary>Gizli testte ana ses düzeyi (−60 dB; 1e-4'te Unity çıkışı tamamen susturuyor, kayıt okunamıyor).</summary>
        public const float HiddenVolume = 1e-3f;
        /// <summary>Her karede okunan çıkış penceresi (örnek; 2'nin kuvveti). 48 kHz'de 170 ms: 6 kare/sn'ye kadar boşluksuz.</summary>
        const int Window = 8192;

        readonly float[] _l = new float[Window], _r = new float[Window];
        float[] _buf;
        int _len, _rate;
        long _lastPos = -1;
        bool _recording;
        /// <summary>Kayıt sürüyor mu.</summary>
        public bool Recording => _recording;
        /// <summary>Son kaydın süresi (sn).</summary>
        public float Seconds => _rate > 0 ? _len / (float)(_rate * 2) : 0f;
        /// <summary>Teşhis: ses saati okunurken ilerlediği için tekrar okunan kare, pencereden uzun boşluk (örnek kaybı) sayısı.</summary>
        public int Retries { get; private set; }
        public int Gaps { get; private set; }
        /// <summary>Son kayıtta kısık düzeyden geri çarpıldı mı (GetOutputData ana düzeyden sonra mı okuyor).</summary>
        public bool Rescaled { get; private set; }
        public float RawPeak { get; private set; }

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (!AutoTest.Enabled || Array.IndexOf(Environment.GetCommandLineArgs(), "-autotest-hidden") < 0) return;
            var go = new GameObject("AudioCapture");
            DontDestroyOnLoad(go);
            I = go.AddComponent<AudioCapture>();
            Sfx.MasterVolume = HiddenVolume;
            AudioListener.volume = HiddenVolume;
            Debug.Log("[Playtest] AudioCapture: gizli test sessiz (ana düzey −60 dB), çıkış kaydı hazır");
        }
#endif

        /// <summary>En fazla <paramref name="maxSeconds"/> saniyelik yeni kayıt başlatır.</summary>
        public void Begin(float maxSeconds)
        {
            lock (_lock) { _recording = false; }
            _rate = AudioSettings.outputSampleRate;
            _buf = new float[Mathf.CeilToInt(maxSeconds * _rate) * 2];
            _len = 0;
            _lastPos = -1;
            Retries = Gaps = 0;
            _recording = true;
        }

        /// <summary>Kaydı durdurur ve WAV'a yazar; dönen değer dosya yolu.</summary>
        public string End(string file)
        {
            lock (_lock) _recording = false;
            float peak = 0f;
            for (int i = 0; i < _len; i++) peak = Mathf.Max(peak, Mathf.Abs(_buf[i]));
            RawPeak = peak;
            // Okunan veri ana düzeyden sonraysa (kısık) geri çarp; değilse olduğu gibi
            float vol = AudioListener.volume;
            Rescaled = !FilterMode && vol < 0.01f && peak > 0f && peak < vol * 50f;
            if (Rescaled) for (int i = 0; i < _len; i++) _buf[i] /= vol;
            var path = Path.Combine(AutoTest.CaptureDir, file);
            WriteWav(path, _buf, _len, 2, _rate);
            return path;
        }

        /// <summary>
        /// Dinleyici süzgeci kipi (<see cref="ListenerTap"/>): kayıt dinleyicinin OnAudioFilterRead'inden tam ölçekte beslenir ve
        /// çıkış orada sıfırlanır (ana düzey 1 kalır; kısık ana düzeyde FMOD sessiz kaynakları sanal sese çevirip susturuyor).
        /// </summary>
        public bool FilterMode { get; private set; }
        readonly object _lock = new();

        /// <summary>Kayıt dinleyici süzgecinden gelsin: ana düzey 1, çıkış süzgeçte sıfırlanır.</summary>
        public void UseListener(AudioListener l)
        {
            if (!l) return;
            if (!l.TryGetComponent<ListenerTap>(out _)) l.gameObject.AddComponent<ListenerTap>();
            FilterMode = true;
            Sfx.MasterVolume = 1f;
            AudioListener.volume = 1f;
        }

        /// <summary>Ses iş parçacığından (ListenerTap): örnekleri kayda ekle.</summary>
        public void Feed(float[] data)
        {
            lock (_lock)
            {
                if (!_recording || _buf == null) return;
                int m = Math.Min(data.Length, _buf.Length - _len);
                if (m > 0) { Array.Copy(data, 0, _buf, _len, m); _len += m; }
                if (_len >= _buf.Length) _recording = false;
            }
        }

        void LateUpdate()
        {
            if (!_recording || _buf == null || FilterMode) return;
            double t0 = 0, t1 = 1;
            for (int k = 0; k < 3 && t0 != t1; k++)
            {
                t0 = AudioSettings.dspTime;
                AudioListener.GetOutputData(_l, 0);
                AudioListener.GetOutputData(_r, 1);
                t1 = AudioSettings.dspTime;
                if (t0 != t1) Retries++;
            }
            if (t0 != t1) return;   // bu kare atlanır; sonraki karede pencere boşluğu kapatır
            long pos = (long)Math.Round(t1 * _rate);
            if (_lastPos < 0) { _lastPos = pos; return; }
            long nw = pos - _lastPos;
            _lastPos = pos;
            if (nw <= 0) return;
            if (nw > Window) { Gaps++; nw = Window; }
            for (int i = Window - (int)nw; i < Window && _len + 1 < _buf.Length; i++)
            {
                _buf[_len++] = _l[i];
                _buf[_len++] = _r[i];
            }
            if (_len + 1 >= _buf.Length) _recording = false;
        }

        /// <summary>IEEE float (biçim 3) WAV yazar.</summary>
        static void WriteWav(string path, float[] s, int len, int ch, int rate)
        {
            using var f = new BinaryWriter(File.Create(path));
            int bytes = len * 4;
            f.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            f.Write(36 + bytes);
            f.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E', (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            f.Write(16);
            f.Write((short)3);
            f.Write((short)ch);
            f.Write(rate);
            f.Write(rate * ch * 4);
            f.Write((short)(ch * 4));
            f.Write((short)32);
            f.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            f.Write(bytes);
            for (int i = 0; i < len; i++) f.Write(s[i]);
        }
    }
}
#endif
