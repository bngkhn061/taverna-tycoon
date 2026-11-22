using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Taverna
{
    /// <summary>Yerel kayıt ile bulut kaydı karşılaştırmasının sonucu (<see cref="CloudSave.Resolve"/>).</summary>
    public enum CloudChoice
    {
        /// <summary>İki kayıt aynı ilerlemede.</summary>
        Same,
        /// <summary>Bu cihazın ilerlemesi açıkça büyük.</summary>
        UseLocal,
        /// <summary>Bulutun ilerlemesi açıkça büyük.</summary>
        UseCloud,
        /// <summary>Belirsiz: oyuncuya iki kayıt karşılaştırmalı gösterilir, oyuncu seçer.</summary>
        Ask,
    }

    /// <summary>
    /// Bulut kaydı biçimi (v2.3): <c>TAVERNA-CLOUD-1</c> başlığı, HMAC-SHA256 imzası, üst bilgi JSON'u ve kayıt JSON'u (her biri bir satır).
    /// İmza cihazdan bağımsız bir anahtarla atılır (bulut kaydı başka cihazda da doğrulanabilsin); anahtar <see cref="SaveCodec.DeriveKey"/>
    /// ile gömülü sabitten türetilir. İmzası tutmayan, kesilmiş ya da imzasız içerik reddedilir. Yerel kayıttaki gibi bu, sıradan dosya
    /// düzenlemesine ve aktarım bozulmasına karşı bir korumadır; kararlı bir hileciyi durdurmaz.
    /// </summary>
    public static class CloudSaveCodec
    {
        /// <summary>İlk satır (biçim sürümü).</summary>
        public const string Header = "TAVERNA-CLOUD-1";
        /// <summary>Bulut anahtarının "cihaz kimliği" yerine geçen sabit (değişirse eski bulut kayıtları reddedilir).</summary>
        const string KeySalt = "cloud-save|v1";

        static byte[] _key;
        static byte[] Key => _key ??= SaveCodec.DeriveKey(KeySalt);

        /// <summary>Bulut baytları: başlık + imza + üst bilgi + kayıt (UTF-8).</summary>
        public static byte[] Encode(CloudSaveMeta meta, string saveJson)
        {
            string metaJson = JsonUtility.ToJson(meta ?? new CloudSaveMeta());
            string body = metaJson + "\n" + (saveJson ?? "");
            return Encoding.UTF8.GetBytes(Header + "\n" + SaveCodec.Sign(body, Key) + "\n" + body);
        }

        /// <summary>İçeriği çözer; başlık doğru ve imza tutuyorsa true (üst bilgi ve kayıt JSON'u). Aksi hâlde false: reddedilir.</summary>
        public static bool TryDecode(byte[] data, out CloudSaveMeta meta, out string saveJson)
        {
            meta = null;
            saveJson = null;
            if (data == null || data.Length == 0) return false;
            string content;
            try { content = Encoding.UTF8.GetString(data); } catch (Exception) { return false; }
            int a = content.IndexOf('\n');
            if (a < 0 || content.Substring(0, a).TrimEnd('\r') != Header) return false;
            int b = content.IndexOf('\n', a + 1);
            if (b < 0) return false;
            string sig = content.Substring(a + 1, b - a - 1).TrimEnd('\r');
            string body = content.Substring(b + 1);
            if (sig.Length != 64 || !string.Equals(sig, SaveCodec.Sign(body, Key), StringComparison.OrdinalIgnoreCase)) return false;
            int c = body.IndexOf('\n');
            if (c < 0) return false;
            try { meta = JsonUtility.FromJson<CloudSaveMeta>(body.Substring(0, c)); }
            catch (Exception) { return false; }
            saveJson = body.Substring(c + 1);
            return meta != null && saveJson.Length > 0;
        }
    }

    /// <summary>
    /// v2.3 bulut kaydı (GAME_DESIGN.md §17.6): yedekleme, geri yükleme, çakışma kuralı, otomatik eşitleme.
    /// Sağlayıcı <see cref="Services.Cloud"/>; yokken (yayında gerçek sağlayıcı bağlanmadan) her işlem "Yakında" der, hiçbir şey yüklenmez.
    /// <para>Çakışma kuralı (<see cref="Resolve"/>): ilerlemesi açıkça büyük olan kazanır — önce prestij sayısı, sonra seviye farkı
    /// (≥ <see cref="ClearLevelGap"/>), sonra toplam kazanç oranı (≥ <see cref="ClearEarnRatio"/>, seviye ters yönde değilse).
    /// Belirsizse oyuncuya "Bu cihaz: Sv12, 3 prestij — Bulut: Sv14, 3 prestij" gösterilir, oyuncu seçer.</para>
    /// </summary>
    public static class CloudSave
    {
        /// <summary>Otomatik yedekleme aralığı (gerçek saniye): 10 dakika. Ayrıca uygulama arka plana geçerken.</summary>
        public const float AutoSyncSeconds = 600f;
        /// <summary>Aynı prestijde bu kadar seviye fark "açıkça büyük" sayılır.</summary>
        public const int ClearLevelGap = 3;
        /// <summary>Seviyeler yakınsa toplam kazanç bu oranda büyükse "açıkça büyük" sayılır.</summary>
        public const float ClearEarnRatio = 1.5f;
        /// <summary>Rastgele kurulum kimliğinin PlayerPrefs anahtarı (kayıtla birlikte buluta gitmez, cihazda kalır).</summary>
        public const string DeviceIdPrefsKey = "taverna_cloud_device_id";

        /// <summary>Bir işlemin sonucu.</summary>
        public enum Outcome { Uploaded, Restored, UpToDate, NeedsChoice, NoCloudSave, Rejected, Failed, Unavailable, Skipped }

        /// <summary>İşlem sonucu ve (seçim gerekiyorsa) iki kaydın üst bilgisi.</summary>
        public struct Result
        {
            public Outcome outcome;
            public CloudSaveMeta local, cloud;
            /// <summary>Seçim gerekiyorsa buluttaki kayıt JSON'u ("Bulutu yükle" bununla yapılır).</summary>
            public string cloudJson;
            public string error;
        }

        /// <summary>Durum değişti (Ayarlar yenilensin).</summary>
        public static event Action Changed;
        /// <summary>Bir işlem sürüyor.</summary>
        public static bool Busy { get; private set; }
        /// <summary>Otomatik eşitleme bulutta daha ileri (ya da belirsiz) bir kayıt buldu; yüklemedi. Ayarlar'da not çıkar.</summary>
        public static bool ConflictPending { get; private set; }
        /// <summary>Test: otomatik yedekleme sayısı.</summary>
        public static int AutoBackups { get; private set; }

        static string _deviceId;
        static float _autoT;

        /// <summary>Sağlayıcı kullanılabilir mi (gerçek, ya da geliştirmede sahte)? Değilse Ayarlar "Yakında" der.</summary>
        public static bool Enabled => Services.CloudEnabled;

        /// <summary>Bu kurulumun rastgele kimliği (ilk istekte üretilir; kişisel veri değil).</summary>
        public static string DeviceId
        {
            get
            {
                if (!string.IsNullOrEmpty(_deviceId)) return _deviceId;
                _deviceId = PlayerPrefs.GetString(DeviceIdPrefsKey, "");
                if (string.IsNullOrEmpty(_deviceId))
                {
                    _deviceId = Guid.NewGuid().ToString("N");
                    PlayerPrefs.SetString(DeviceIdPrefsKey, _deviceId);
                    PlayerPrefs.Save();
                }
                return _deviceId;
            }
        }

        /// <summary>Kayıttan üst bilgi.</summary>
        public static CloudSaveMeta BuildMeta(SaveData d, string deviceId, DateTime utc) => new()
        {
            savedUtc = utc.ToString("o", CultureInfo.InvariantCulture),
            level = d?.level ?? 1,
            branch = d?.branch ?? 0,
            totalEarned = d?.totalEarned ?? 0,
            deviceId = deviceId ?? "",
            saveVersion = d?.saveVersion ?? 0,
        };

        /// <summary>Çakışma kuralı (saf işlev; birim testli). Bulut yoksa <see cref="CloudChoice.UseLocal"/>.</summary>
        public static CloudChoice Resolve(CloudSaveMeta local, CloudSaveMeta cloud)
        {
            if (cloud == null) return CloudChoice.UseLocal;
            if (local == null) return CloudChoice.UseCloud;
            if (local.branch != cloud.branch) return local.branch > cloud.branch ? CloudChoice.UseLocal : CloudChoice.UseCloud;
            int dl = local.level - cloud.level;
            if (dl >= ClearLevelGap) return CloudChoice.UseLocal;
            if (-dl >= ClearLevelGap) return CloudChoice.UseCloud;
            if (dl == 0 && local.totalEarned == cloud.totalEarned) return CloudChoice.Same;
            if (dl >= 0 && local.totalEarned > 0 && local.totalEarned >= cloud.totalEarned * ClearEarnRatio) return CloudChoice.UseLocal;
            if (dl <= 0 && cloud.totalEarned > 0 && cloud.totalEarned >= local.totalEarned * ClearEarnRatio) return CloudChoice.UseCloud;
            // Aynı kurulumun eski yedeği: bu cihaz o yedekten ileride ya da eşitse belirsizlik yok
            if (!string.IsNullOrEmpty(local.deviceId) && local.deviceId == cloud.deviceId && dl >= 0 && local.totalEarned >= cloud.totalEarned)
                return CloudChoice.UseLocal;
            return CloudChoice.Ask;
        }

        /// <summary>"Sv12, 3 prestij".</summary>
        public static string Summary(CloudSaveMeta m) => m == null ? Loc.T("kayıt yok") : Loc.F("Sv{0}, {1} prestij", m.level, m.branch);

        /// <summary>"Bu cihaz: Sv12, 3 prestij — Bulut: Sv14, 3 prestij".</summary>
        public static string CompareLine(CloudSaveMeta local, CloudSaveMeta cloud) => Loc.F("Bu cihaz: {0} — Bulut: {1}", Summary(local), Summary(cloud));

        /// <summary>Kaydın yazıldığı yerel zaman ("21.11 14:02"); okunamazsa "-".</summary>
        public static string When(string isoUtc) =>
            DateTime.TryParse(isoUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t) ? t.ToLocalTime().ToString("dd.MM HH:mm", CultureInfo.InvariantCulture) : "-";

        /// <summary>Bu cihazın şimdiki kaydının üst bilgisi.</summary>
        public static CloudSaveMeta LocalMeta() => BuildMeta(GameState.Data, DeviceId, DateTime.UtcNow);

        // ------------------------------------------------------------------ işlemler
        /// <summary>
        /// Yedekle: önce buluttakine bakılır. Bulut boş, bozuk ya da geride / aynıysa yüklenir. Bulut açıkça ileride ya da belirsizse:
        /// elle yedeklemede <see cref="Outcome.NeedsChoice"/> (oyuncu seçer), otomatikte yüklenmez (<see cref="ConflictPending"/>).
        /// </summary>
        public static void Backup(bool auto, Action<Result> done)
        {
            if (!Enabled) { Finish(done, new Result { outcome = Outcome.Unavailable }); return; }
            if (Busy) { Finish(done, new Result { outcome = Outcome.Skipped }); return; }
            if (auto && !GameState.IsLoaded) { Finish(done, new Result { outcome = Outcome.Skipped }); return; }   // yüklenmemiş (boş) kayıt buluta gitmesin
            Busy = true;
            var local = LocalMeta();
            Services.Cloud.Download(dl =>
            {
                if (dl.error != null) { Busy = false; Track("cloud_backup", auto, "hata"); Finish(done, new Result { outcome = Outcome.Failed, error = dl.error, local = local }); return; }
                CloudSaveMeta cloud = null;
                string cloudJson = null;
                bool valid = dl.data != null && CloudSaveCodec.TryDecode(dl.data, out cloud, out cloudJson);
                var choice = valid ? Resolve(local, cloud) : CloudChoice.UseLocal;
                if (choice is CloudChoice.UseCloud or CloudChoice.Ask)
                {
                    Busy = false;
                    if (auto) { ConflictPending = true; Track("cloud_backup", true, "bulut_ileride"); Finish(done, new Result { outcome = Outcome.Skipped, local = local, cloud = cloud }); return; }
                    Services.Track("cloud_conflict", ("yer", "yedekle"), ("kural", choice.ToString()), ("cihaz_sv", local.level), ("bulut_sv", cloud.level));
                    Finish(done, new Result { outcome = Outcome.NeedsChoice, local = local, cloud = cloud, cloudJson = cloudJson });
                    return;
                }
                UploadNow(local, auto, done);
            });
        }

        /// <summary>"Bu cihazı tut": bulut kaydının üstüne bu cihazın kaydı yazılır.</summary>
        public static void ForceUpload(Action<Result> done)
        {
            if (!Enabled) { Finish(done, new Result { outcome = Outcome.Unavailable }); return; }
            Busy = true;
            UploadNow(LocalMeta(), false, done);
        }

        static void UploadNow(CloudSaveMeta local, bool auto, Action<Result> done)
        {
            var bytes = CloudSaveCodec.Encode(local, GameState.SnapshotJson());
            Services.Cloud.Upload(bytes, local, (ok, err) =>
            {
                Busy = false;
                if (ok) { ConflictPending = false; if (auto) AutoBackups++; }
                Track("cloud_backup", auto, ok ? "tamam" : "hata");
                Finish(done, new Result { outcome = ok ? Outcome.Uploaded : Outcome.Failed, error = err, local = local });
            });
        }

        /// <summary>
        /// Buluttan yükle: kayıt yoksa <see cref="Outcome.NoCloudSave"/>, imzası tutmuyorsa <see cref="Outcome.Rejected"/> (hiçbir şey değişmez).
        /// Bulut açıkça ilerideyse yüklenir; aynıysa "zaten güncel"; bu cihaz ileride ya da belirsizse oyuncu seçer.
        /// </summary>
        public static void Restore(Action<Result> done)
        {
            if (!Enabled) { Finish(done, new Result { outcome = Outcome.Unavailable }); return; }
            if (Busy) { Finish(done, new Result { outcome = Outcome.Skipped }); return; }
            Busy = true;
            var local = LocalMeta();
            Services.Cloud.Download(dl =>
            {
                Busy = false;
                if (dl.error != null) { Track("cloud_restore", false, "hata"); Finish(done, new Result { outcome = Outcome.Failed, error = dl.error, local = local }); return; }
                if (dl.data == null) { Track("cloud_restore", false, "bulut_bos"); Finish(done, new Result { outcome = Outcome.NoCloudSave, local = local }); return; }
                if (!CloudSaveCodec.TryDecode(dl.data, out var cloud, out var json) || GameState.FromJson(json) == null)
                {
                    Track("cloud_restore", false, "imza_red");
                    Debug.LogWarning("[Bulut] indirilen kaydın imzası tutmadı ya da okunamadı; reddedildi.");
                    Finish(done, new Result { outcome = Outcome.Rejected, local = local });
                    return;
                }
                var choice = Resolve(local, cloud);
                if (choice == CloudChoice.Same) { Track("cloud_restore", false, "guncel"); Finish(done, new Result { outcome = Outcome.UpToDate, local = local, cloud = cloud }); return; }
                if (choice == CloudChoice.UseCloud)
                {
                    bool ok = ApplyCloud(json);
                    Finish(done, new Result { outcome = ok ? Outcome.Restored : Outcome.Rejected, local = local, cloud = cloud });
                    return;
                }
                Services.Track("cloud_conflict", ("yer", "yukle"), ("kural", choice.ToString()), ("cihaz_sv", local.level), ("bulut_sv", cloud.level));
                Finish(done, new Result { outcome = Outcome.NeedsChoice, local = local, cloud = cloud, cloudJson = json });
            });
        }

        /// <summary>Çakışma ekranındaki seçimi ölçüme yazar ("cihaz", "bulut", "vazgec").</summary>
        public static void TrackChoice(string choice) => Services.Track("cloud_conflict", ("secim", choice));

        /// <summary>
        /// Bulut kaydını bu cihaza uygular: kayıt yükseltilir, bellekteki kaydın yerine geçer ve hemen (bu cihazın anahtarıyla) imzalanıp yazılır.
        /// Oyun sahnesindeyse çağıran sahneyi yeniden yükler.
        /// </summary>
        public static bool ApplyCloud(string json)
        {
            var d = GameState.FromJson(json);
            if (d == null) { Track("cloud_restore", false, "okunamadi"); return false; }
            GameState.ReplaceData(d);
            GameState.SaveSync();
            ConflictPending = false;
            Track("cloud_restore", false, "tamam");
            return true;
        }

        // ------------------------------------------------------------------ otomatik eşitleme
        /// <summary>Her kare (gerçek zaman): <see cref="AutoSyncSeconds"/>'de bir otomatik yedek (sağlayıcı varsa).</summary>
        public static void Tick(float unscaledDt)
        {
            if (!Enabled || !Services.Cloud.SignedIn) return;
            _autoT += unscaledDt;
            if (_autoT < AutoSyncSeconds) return;
            _autoT = 0f;
            Backup(true, null);
        }

        /// <summary>Uygulama arka plana geçerken: otomatik yedek (sağlayıcı varsa).</summary>
        public static void OnPause()
        {
            if (!Enabled || !Services.Cloud.SignedIn) return;
            _autoT = 0f;
            Backup(true, null);
        }

        static void Track(string ev, bool auto, string result) => Services.Track(ev, ("otomatik", auto), ("sonuc", result));

        static void Finish(Action<Result> done, Result r)
        {
            done?.Invoke(r);
            Changed?.Invoke();
        }
    }
}
