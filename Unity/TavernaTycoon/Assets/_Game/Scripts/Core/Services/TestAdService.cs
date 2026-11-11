using System;

namespace Taverna
{
    /// <summary>
    /// Test reklamı (v0.6 sahte reklam ekranı, v1.6'da <see cref="IAdService"/> uygulaması): gerçek reklam yerine
    /// kısa reklamda 5, uzun reklamda 15 saniye sayan bir bekleme ekranı. Ekranı HUD çizer (<see cref="Presenter"/>).
    /// Yalnızca geliştirme derlemesinde ödül verir; yayın derlemesinde <see cref="Services.RewardedAdsEnabled"/> false olur.
    /// </summary>
    public class TestAdService : IAdService
    {
        /// <summary>Kısa / uzun test reklamının sayaç süresi (gerçek saniye).</summary>
        public const float ShortSeconds = StaffUpgrades.ShortAdSeconds, LongSeconds = StaffUpgrades.LongAdSeconds;

        /// <summary>
        /// Bekleme ekranını çizen HUD işlevi: (neden, saniye, bitince). HUD açılınca kaydeder, kapanınca siler.
        /// Kayıtlı değilse reklam "hazır değil" sayılır.
        /// </summary>
        public static Action<string, float, Action> Presenter;

        /// <inheritdoc/>
        public bool IsTest => true;

        /// <inheritdoc/>
        public bool IsRewardedReady => Presenter != null && Services.TestProvidersAllowed;

        /// <summary>Uzunluğa göre sayaç süresi.</summary>
        public static float SecondsFor(AdLength length) => length == AdLength.Long ? LongSeconds : ShortSeconds;

        /// <inheritdoc/>
        public void ShowRewarded(string placement, string reason, AdLength length, Action onRewarded, Action<string> onCancelled)
        {
            if (!Services.TestProvidersAllowed) { onCancelled?.Invoke("test reklamı yayın derlemesinde kapalı"); return; }
            if (Presenter == null) { onCancelled?.Invoke("reklam ekranı yok"); return; }
            Presenter(reason, SecondsFor(length), () => onRewarded?.Invoke());
        }
    }
}
