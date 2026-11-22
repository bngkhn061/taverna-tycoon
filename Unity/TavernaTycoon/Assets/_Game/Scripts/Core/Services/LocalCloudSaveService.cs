using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Geliştirme ve test için sahte bulut (v2.3): kaydı cihazda ayrı bir klasöre yazar
    /// (<c>persistentDataPath/cloud_fake/cloud.dat</c> + <c>cloud.meta.json</c> + <c>last_sync.txt</c>). Hep "girişli".
    /// Yalnızca geliştirme derlemesinde çalışır (<see cref="Services.TestProvidersAllowed"/>); yayında Ayarlar "Yakında" der.
    /// Çakışma ekranını denemek için klasördeki dosya başka bir kurulumun kaydıyla değiştirilebilir (test botu böyle yapar).
    /// </summary>
    public class LocalCloudSaveService : ICloudSaveService
    {
        /// <summary>Sahte bulut klasörünün adı (persistentDataPath altında).</summary>
        public const string FolderName = "cloud_fake";
        const string DataFile = "cloud.dat", MetaFile = "cloud.meta.json", SyncFile = "last_sync.txt";

        readonly string _dir;

        /// <param name="root">Kök klasör (oyunda persistentDataPath; testte geçici klasör).</param>
        public LocalCloudSaveService(string root) { _dir = Path.Combine(root, FolderName); }

        /// <summary>Sahte bulutun klasörü.</summary>
        public string Folder => _dir;

        /// <inheritdoc/>
        public bool IsTest => true;
        /// <inheritdoc/>
        public bool IsAvailable => Services.TestProvidersAllowed;
        /// <inheritdoc/>
        public bool SignedIn => IsAvailable;
        /// <inheritdoc/>
        public string ProviderName => Loc.T("Yerel test bulutu");

        /// <inheritdoc/>
        public DateTime? LastSyncUtc
        {
            get
            {
                try
                {
                    var p = Path.Combine(_dir, SyncFile);
                    if (File.Exists(p) && DateTime.TryParse(File.ReadAllText(p), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t)) return t;
                }
                catch (Exception) { }
                return null;
            }
        }

        /// <inheritdoc/>
        public void SignIn(Action<bool> done) => done?.Invoke(IsAvailable);

        /// <inheritdoc/>
        public void Upload(byte[] data, CloudSaveMeta meta, Action<bool, string> done)
        {
            if (!IsAvailable) { done?.Invoke(false, Loc.T("Bulut kaydı yakında")); return; }
            try
            {
                Directory.CreateDirectory(_dir);
                var tmp = Path.Combine(_dir, DataFile + ".tmp");
                File.WriteAllBytes(tmp, data ?? Array.Empty<byte>());
                var target = Path.Combine(_dir, DataFile);
                if (File.Exists(target)) File.Delete(target);
                File.Move(tmp, target);
                File.WriteAllText(Path.Combine(_dir, MetaFile), JsonUtility.ToJson(meta ?? new CloudSaveMeta()));
                MarkSync();
                done?.Invoke(true, null);
            }
            catch (Exception e) { done?.Invoke(false, e.Message); }
        }

        /// <inheritdoc/>
        public void Download(Action<CloudDownload> done)
        {
            if (!IsAvailable) { done?.Invoke(new CloudDownload { error = Loc.T("Bulut kaydı yakında") }); return; }
            try
            {
                var p = Path.Combine(_dir, DataFile);
                if (!File.Exists(p)) { done?.Invoke(default); return; }
                var r = new CloudDownload { data = File.ReadAllBytes(p) };
                var mp = Path.Combine(_dir, MetaFile);
                if (File.Exists(mp)) r.meta = JsonUtility.FromJson<CloudSaveMeta>(File.ReadAllText(mp));
                MarkSync();
                done?.Invoke(r);
            }
            catch (Exception e) { done?.Invoke(new CloudDownload { error = e.Message }); }
        }

        /// <summary>Test: sahte bulutu boşaltır (dosyaları siler, klasör kalır).</summary>
        public void Clear()
        {
            foreach (var f in new[] { DataFile, MetaFile, SyncFile })
                try { var p = Path.Combine(_dir, f); if (File.Exists(p)) File.Delete(p); } catch (Exception) { }
        }

        /// <summary>Test: bulut dosyasını doğrudan yazar (başka cihazın kaydı ya da bozulmuş içerik denemesi).</summary>
        public void DebugWriteRaw(byte[] data)
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllBytes(Path.Combine(_dir, DataFile), data);
        }

        void MarkSync()
        {
            try { File.WriteAllText(Path.Combine(_dir, SyncFile), DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)); } catch (Exception) { }
        }
    }
}
