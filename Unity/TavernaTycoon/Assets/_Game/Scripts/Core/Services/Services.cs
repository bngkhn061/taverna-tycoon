using System;
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Reklam, mağaza ve ölçüm hizmetlerinin tek erişim noktası (v1.6). Açılışta test uygulamaları kurulur
    /// (<see cref="TestAdService"/>, <see cref="TestStoreService"/>, <see cref="LocalAnalyticsService"/>).
    /// Gerçek sağlayıcı tek satırla takılır: <c>Services.Register(new BenimReklamım());</c> (docs/services-integration.md).
    /// <para>
    /// Yayın güvenliği: test reklamı / test mağazası yalnızca geliştirme derlemesinde (<see cref="Debug.isDebugBuild"/>) çalışır.
    /// Yayın derlemesinde gerçek sağlayıcı kayıtlı değilse <see cref="RewardedAdsEnabled"/> ve <see cref="StoreEnabled"/> false olur:
    /// reklam düğmeleri gizlenir, Hazine "Yakında" der, hiçbir ödül / ürün verilmez.
    /// </para>
    /// </summary>
    public static class Services
    {
        /// <summary>Kayıtlı reklam hizmeti (varsayılan: test).</summary>
        public static IAdService Ads { get; private set; }
        /// <summary>Kayıtlı mağaza hizmeti (varsayılan: test).</summary>
        public static IStoreService Store { get; private set; }
        /// <summary>Kayıtlı ölçüm hizmeti (varsayılan: yerel dosya).</summary>
        public static IAnalyticsService Analytics { get; private set; }

        /// <summary>Bu süreçteki oturum kimliği (ölçüm satırlarında).</summary>
        public static string SessionId { get; private set; } = NewSessionId();

        /// <summary>Oturumun başladığı gerçek zaman (Time.realtimeSinceStartup).</summary>
        public static float SessionStart { get; private set; }

        /// <summary>
        /// Test bayrağı "-autotest-release-sim": geliştirme derlemesinde yayın derlemesi gibi davran
        /// (test reklamı ve test mağazası kapalı). Yalnızca otomatik testte açılır.
        /// </summary>
        public static bool ReleaseSimulation { get; set; }

        /// <summary>Test uygulamalarının çalışmasına izin var mı? (geliştirme derlemesi ve yayın taklidi kapalı)</summary>
        public static bool TestProvidersAllowed => Debug.isDebugBuild && !ReleaseSimulation;

        /// <summary>Reklamla ödül veren düğmeler gösterilebilir mi? (gerçek ağ ya da izinli test)</summary>
        public static bool RewardedAdsEnabled => Ads != null && (!Ads.IsTest || TestProvidersAllowed);

        /// <summary>Hazine ürünleri satın alınabilir mi? (gerçek mağaza ya da izinli test)</summary>
        public static bool StoreEnabled => Store != null && (!Store.IsTest || TestProvidersAllowed);

        static bool _installed;

        /// <summary>Test uygulamalarını kurar (bir kez). Sahne yüklenmeden önce kendiliğinden çağrılır.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InstallDefaults()
        {
            if (_installed) return;
            _installed = true;
            if (Ads == null) Ads = new TestAdService();
            if (Store == null) Store = new TestStoreService();
            if (Analytics == null) Analytics = new LocalAnalyticsService(Application.persistentDataPath);
            if (Application.isPlaying) ServicesRunner.Ensure();
        }

        /// <summary>Gerçek reklam ağını takar (test uygulamasının yerine geçer).</summary>
        public static void Register(IAdService ads) => Ads = ads ?? throw new ArgumentNullException(nameof(ads));
        /// <summary>Gerçek mağazayı takar (test mağazasının yerine geçer).</summary>
        public static void Register(IStoreService store) => Store = store ?? throw new ArgumentNullException(nameof(store));
        /// <summary>Gerçek ölçüm hizmetini takar (yerel dosyanın yerine geçer; eski hizmetin kuyruğu önce yazılır).</summary>
        public static void Register(IAnalyticsService analytics)
        {
            if (analytics == null) throw new ArgumentNullException(nameof(analytics));
            Analytics?.Flush(false);
            Analytics = analytics;
        }

        /// <summary>Yerel test ölçüm hizmeti (yalnızca o kayıtlıysa; özet satırı için).</summary>
        public static LocalAnalyticsService LocalAnalytics => Analytics as LocalAnalyticsService;

        /// <summary>
        /// Ölçüm olayı gönderir: <c>Services.Track("level_up", ("seviye", 5))</c>. Hizmet yoksa sessizce geçer;
        /// ölçüm hatası oyunu asla durdurmaz.
        /// </summary>
        public static void Track(string eventName, params (string key, object value)[] parameters)
        {
            if (Analytics == null) InstallDefaults();
            if (Analytics == null) return;
            var dict = new Dictionary<string, object>(parameters?.Length ?? 0);
            if (parameters != null)
                foreach (var (k, v) in parameters) dict[k] = v;
            try { Analytics.Track(eventName, dict); }
            catch (Exception e) { Debug.LogWarning($"[Ölçüm] {eventName} gönderilemedi: {e.Message}"); }
        }

        /// <summary>Yeni oturum başlat (açılış ya da uzun aradan sonra uygulamaya dönüş).</summary>
        internal static void BeginSession()
        {
            SessionId = NewSessionId();
            SessionStart = Time.realtimeSinceStartup;
            Track("session_start", ("platform", Application.platform.ToString()), ("surum", Application.version), ("gelistirme", Debug.isDebugBuild));
        }

        /// <summary>Oturumu bitir (arka plana geçiş ya da kapanış).</summary>
        internal static void EndSession(bool blocking)
        {
            Track("session_end", ("sure_sn", Mathf.RoundToInt(Time.realtimeSinceStartup - SessionStart)));
            Analytics?.Flush(blocking);
        }

        static string NewSessionId() => Guid.NewGuid().ToString("N").Substring(0, 12);
    }
}
