using System;

namespace Taverna
{
    /// <summary>Ödüllü reklamın beklenen uzunluğu. Test uygulaması sayaç süresini buna göre seçer; gerçek ağ yok sayabilir.</summary>
    public enum AdLength { Short, Long }

    /// <summary>
    /// Ödüllü reklam hizmeti (v1.6 yayın altyapısı). Oyun kodu reklamı yalnızca bu arayüz üzerinden ister
    /// (<see cref="Services.Ads"/>). Gerçek ağ (AdMob, Unity Ads, LevelPlay…) bağlanınca bu arayüzü uygulayan bir sınıf
    /// <see cref="Services.Register(IAdService)"/> ile takılır; çağıran kod değişmez. Ayrıntı: docs/services-integration.md.
    /// </summary>
    public interface IAdService
    {
        /// <summary>Test / sahte uygulama mı? Test uygulaması yayın derlemesinde hiç kullanılmaz (ödül vermez).</summary>
        bool IsTest { get; }

        /// <summary>Şu an gösterilebilecek ödüllü reklam yüklü mü?</summary>
        bool IsRewardedReady { get; }

        /// <summary>
        /// Ödüllü reklamı gösterir. Reklam sonuna kadar izlenirse <paramref name="onRewarded"/>, kapatılır / yüklenemezse
        /// <paramref name="onCancelled"/> (neden metniyle) çağrılır. İkisinden yalnızca biri, en fazla bir kez çağrılır.
        /// </summary>
        /// <param name="placement">Reklam yeri kimliği (ör. "helper", "staff_upgrade"); ölçüm ve ağ panelinde kullanılır.</param>
        /// <param name="reason">Oyuncuya gösterilecek kısa açıklama ("3 dakikalık yardımcı garson").</param>
        /// <param name="length">Kısa ya da uzun reklam.</param>
        void ShowRewarded(string placement, string reason, AdLength length, Action onRewarded, Action<string> onCancelled);
    }
}
