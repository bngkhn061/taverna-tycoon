using System;

namespace Taverna
{
    /// <summary>
    /// Bulut kaydının üst bilgisi (v2.3): çakışma çözümü yalnızca bunlara bakar. Kişisel veri yoktur; cihaz kimliği
    /// oyunun ilk açılışta ürettiği rastgele bir GUID'dir (<see cref="CloudSave.DeviceId"/>), donanım kimliği değildir.
    /// </summary>
    [Serializable]
    public class CloudSaveMeta
    {
        /// <summary>Kaydın yazıldığı an (UTC, ISO 8601).</summary>
        public string savedUtc = "";
        /// <summary>Taverna seviyesi.</summary>
        public int level;
        /// <summary>Prestij (açılan şube) sayısı.</summary>
        public int branch;
        /// <summary>Oyun boyunca kazanılan toplam altın (prestijte sıfırlanmaz).</summary>
        public long totalEarned;
        /// <summary>Kaydı yükleyen kurulumun rastgele kimliği.</summary>
        public string deviceId = "";
        /// <summary>Kayıt biçimi sürümü (<see cref="GameState.CurrentSaveVersion"/>).</summary>
        public int saveVersion;
    }

    /// <summary>İndirme sonucu: <see cref="data"/> null ise <see cref="error"/> nedenini söyler (bulutta kayıt yoksa ikisi de null).</summary>
    public struct CloudDownload
    {
        public byte[] data;
        public CloudSaveMeta meta;
        public string error;
    }

    /// <summary>
    /// Bulut kaydı hizmeti (v2.3 altyapı; GAME_DESIGN.md §17.6, docs/services-integration.md). Oyun kodu buluta yalnızca bu arayüzle
    /// (<see cref="Services.Cloud"/>) ve <see cref="CloudSave"/> üzerinden erişir. Gerçek sağlayıcı (Google Play Games Saved Games,
    /// iCloud, Unity Cloud Save) bu arayüzü uygulayan bir sınıfla <see cref="Services.Register(ICloudSaveService)"/> ile takılır.
    /// Bayt içeriği zaten imzalı bulut kaydıdır (<see cref="CloudSaveCodec"/>); hizmet içeriğe bakmaz, saklar ve geri verir.
    /// Geri çağrılar ana iş parçacığında, en fazla bir kez çağrılır.
    /// </summary>
    public interface ICloudSaveService
    {
        /// <summary>Test / sahte uygulama mı? Test uygulaması yayın derlemesinde kullanılmaz ("Yakında").</summary>
        bool IsTest { get; }

        /// <summary>Sağlayıcı bu cihazda kullanılabilir mi (bağlı, destekleniyor)?</summary>
        bool IsAvailable { get; }

        /// <summary>Oyuncu sağlayıcıya giriş yapmış mı?</summary>
        bool SignedIn { get; }

        /// <summary>Ayarlar'da gösterilen sağlayıcı adı (çevrilmiş).</summary>
        string ProviderName { get; }

        /// <summary>Son başarılı yükleme ya da indirmenin zamanı (UTC; hiç olmadıysa null).</summary>
        DateTime? LastSyncUtc { get; }

        /// <summary>Girişi başlatır (gerekiyorsa oyuncuya sağlayıcının ekranı açılır); sonuç geri çağrılır.</summary>
        void SignIn(Action<bool> done);

        /// <summary>Kaydı yükler (öncekinin yerine geçer). <paramref name="done"/>: başarılı mı, değilse neden.</summary>
        void Upload(byte[] data, CloudSaveMeta meta, Action<bool, string> done);

        /// <summary>Buluttaki kaydı indirir.</summary>
        void Download(Action<CloudDownload> done);
    }
}
