using System;

namespace Taverna
{
    /// <summary>
    /// Gerçek bulut sağlayıcısı bağlanmamışken (yayın derlemesi) kullanılan boş uygulama: kullanılamaz, hiçbir şey yüklemez.
    /// Ayarlar › Bulut kaydı "Yakında" gösterir; otomatik eşitleme çalışmaz.
    /// </summary>
    public class NoneCloudSaveService : ICloudSaveService
    {
        /// <inheritdoc/>
        public bool IsTest => false;
        /// <inheritdoc/>
        public bool IsAvailable => false;
        /// <inheritdoc/>
        public bool SignedIn => false;
        /// <inheritdoc/>
        public string ProviderName => Loc.T("Yakında");
        /// <inheritdoc/>
        public DateTime? LastSyncUtc => null;
        /// <inheritdoc/>
        public void SignIn(Action<bool> done) => done?.Invoke(false);
        /// <inheritdoc/>
        public void Upload(byte[] data, CloudSaveMeta meta, Action<bool, string> done) => done?.Invoke(false, Loc.T("Bulut kaydı yakında"));
        /// <inheritdoc/>
        public void Download(Action<CloudDownload> done) => done?.Invoke(new CloudDownload { error = Loc.T("Bulut kaydı yakında") });
    }
}
