using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Taverna
{
    /// <summary>Açılışta kaydın nereden geldiği.</summary>
    public enum SaveLoadSource
    {
        /// <summary>Hiç kayıt yok (yeni oyun).</summary>
        None,
        /// <summary>save.dat imzası tuttu.</summary>
        Main,
        /// <summary>save.dat bozuktu, save.bak açıldı.</summary>
        Backup,
        /// <summary>Eski sürümün PlayerPrefs kaydı taşındı.</summary>
        Legacy,
        /// <summary>İkisi de bozuk: dosyalar kenara alındı, yeni oyun.</summary>
        Corrupt,
    }

    /// <summary>
    /// Kayıt dosyaları (v1.6): <c>persistentDataPath/save.dat</c> (imzalı, <see cref="SaveCodec"/>), bir önceki sürüm
    /// <c>save.bak</c>. Yazım önce <c>save.tmp</c>'ye yapılır, sonra yer değiştirilir (yarım dosya kalmaz). Olağan kayıtlar
    /// arka plan iş parçacığında yazılır (ana iş parçacığı takılmasın); kapanış / arka plana geçişte eşzamanlı yazılır.
    /// Bütün dosya işlemleri tek kilitle sıralanır. Yollar ve anahtar <see cref="Init"/> ile ana iş parçacığında verilir.
    /// </summary>
    public static class SaveStore
    {
        public const string MainFile = "save.dat", BackupFile = "save.bak", TempFile = "save.tmp", CorruptPrefix = "save.corrupt-";

        static string _dir;
        static byte[] _key;
        static readonly object _queueLock = new(), _ioLock = new();
        static string _queued;
        static bool _running;
        static Task _worker;

        /// <summary>Kayıt klasörü.</summary>
        public static string Directory => _dir;
        public static string MainPath => Path.Combine(_dir, MainFile);
        public static string BackupPath => Path.Combine(_dir, BackupFile);
        static string TempPath => Path.Combine(_dir, TempFile);

        /// <summary>Hazır mı (Init çağrıldı)?</summary>
        public static bool Ready => _dir != null && _key != null;
        /// <summary>Toplam diske yazım sayısı (arka plan + eşzamanlı).</summary>
        public static int WriteCount { get; private set; }
        /// <summary>Son yazımın süresi (ms; imza + dosya + yer değiştirme).</summary>
        public static double LastWriteMs { get; private set; }
        /// <summary>En uzun yazım (ms).</summary>
        public static double MaxWriteMs { get; private set; }
        /// <summary>Son yazım hatası (yoksa null).</summary>
        public static string LastError { get; private set; }
        /// <summary>Son kenara alınan bozuk dosyanın adı (yoksa null).</summary>
        public static string LastCorruptName { get; private set; }

        /// <summary>Kayıt klasörünü ve imza anahtarını ayarlar (ana iş parçacığında, açılışta).</summary>
        public static void Init(string directory, byte[] key)
        {
            Flush();
            _dir = directory;
            _key = key;
            System.IO.Directory.CreateDirectory(_dir);
        }

        /// <summary>
        /// Kaydı okur: önce save.dat, imza tutmazsa save.bak. İkisi de yok → <see cref="SaveLoadSource.None"/>.
        /// Var ama ikisi de bozuk → dosyalar <c>save.corrupt-*</c> adıyla kenara alınır, <see cref="SaveLoadSource.Corrupt"/>.
        /// Yedekten açılırsa bozuk ana dosya da kenara alınır (bir sonraki yazım sağlam yedeği ezmesin).
        /// </summary>
        public static SaveLoadSource Read(out string json)
        {
            json = null;
            Flush();
            lock (_ioLock)
            {
                bool hasMain = File.Exists(MainPath), hasBak = File.Exists(BackupPath);
                if (!hasMain && !hasBak) return SaveLoadSource.None;
                if (hasMain && TryReadFile(MainPath, out json)) return SaveLoadSource.Main;
                if (hasBak && TryReadFile(BackupPath, out json))
                {
                    if (hasMain) MoveAside(MainPath, ".dat");
                    return SaveLoadSource.Backup;
                }
                json = null;
                if (hasMain) MoveAside(MainPath, ".dat");
                if (hasBak) MoveAside(BackupPath, ".bak");
                return SaveLoadSource.Corrupt;
            }
        }

        static bool TryReadFile(string path, out string json)
        {
            json = null;
            try { return SaveCodec.TryDecode(File.ReadAllText(path, Encoding.UTF8), _key, out json); }
            catch (Exception) { return false; }
        }

        static void MoveAside(string path, string ext)
        {
            try
            {
                string name = CorruptPrefix + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ext;
                string target = Path.Combine(_dir, name);
                for (int i = 1; File.Exists(target); i++) target = Path.Combine(_dir, $"{CorruptPrefix}{DateTime.UtcNow:yyyyMMdd-HHmmss}-{i}{ext}");
                File.Move(path, target);
                LastCorruptName = Path.GetFileName(target);
            }
            catch (Exception e) { LastError = "kenara alınamadı: " + e.Message; }
        }

        /// <summary>JSON'u arka planda yazar (sıraya alır; yazım sürerken gelen yeni kayıt bir öncekinin yerine geçer).</summary>
        public static void WriteAsync(string json)
        {
            if (!Ready) return;
            lock (_queueLock)
            {
                _queued = json;
                if (_running) return;
                _running = true;
                _worker = Task.Run(WorkerLoop);
            }
        }

        static void WorkerLoop()
        {
            while (true)
            {
                string json;
                lock (_queueLock)
                {
                    json = _queued;
                    _queued = null;
                    if (json == null) { _running = false; return; }
                }
                WriteNow(json);
            }
        }

        /// <summary>Bekleyen arka plan yazımını bitirir, sonra JSON'u hemen (eşzamanlı) yazar. Kapanışta kullanılır.</summary>
        public static void WriteSync(string json)
        {
            if (!Ready) return;
            lock (_queueLock) _queued = null;   // daha yeni içerik bu: kuyruktakine gerek yok
            Flush();
            WriteNow(json);
        }

        /// <summary>Arka plandaki yazım bitene kadar bekler.</summary>
        public static void Flush()
        {
            while (true)
            {
                Task t;
                lock (_queueLock) { if (!_running) return; t = _worker; }
                try { t?.Wait(); } catch (AggregateException) { }
            }
        }

        /// <summary>Tek yazım: imzala → save.tmp → (save.dat → save.bak) → save.tmp → save.dat.</summary>
        static void WriteNow(string json)
        {
            var sw = Stopwatch.StartNew();
            lock (_ioLock)
            {
                try
                {
                    var content = SaveCodec.Encode(json, _key);
                    using (var fs = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var bytes = Encoding.UTF8.GetBytes(content);
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Flush(true);
                    }
                    if (File.Exists(MainPath))
                    {
                        if (File.Exists(BackupPath)) File.Delete(BackupPath);
                        File.Move(MainPath, BackupPath);
                    }
                    File.Move(TempPath, MainPath);
                    LastError = null;
                }
                catch (Exception e) { LastError = e.Message; UnityEngine.Debug.LogWarning("[Kayıt] yazılamadı: " + e.Message); }
                WriteCount++;
            }
            LastWriteMs = sw.Elapsed.TotalMilliseconds;
            if (LastWriteMs > MaxWriteMs) MaxWriteMs = LastWriteMs;
        }

        /// <summary>Kayıt dosyalarını siler (Ayarlar › Kaydı Sıfırla). Kenara alınmış bozuk dosyalara dokunmaz.</summary>
        public static void DeleteAll()
        {
            if (!Ready) return;
            lock (_queueLock) _queued = null;
            Flush();
            lock (_ioLock)
            {
                foreach (var p in new[] { MainPath, BackupPath, TempPath })
                    try { if (File.Exists(p)) File.Delete(p); } catch (Exception e) { LastError = e.Message; }
            }
        }
    }
}
