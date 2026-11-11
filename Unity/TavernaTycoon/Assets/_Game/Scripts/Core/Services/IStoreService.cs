using System;
using System.Collections.Generic;

namespace Taverna
{
    /// <summary>
    /// Uygulama içi satın alma hizmeti (v1.6 yayın altyapısı). Ürün tanımları ve ödüller <see cref="Premium"/>'da kalır;
    /// bu arayüz yalnızca ödeme köprüsüdür. Gerçek mağaza (Google Play Billing / App Store, örn. Unity IAP ile) bağlanınca
    /// bu arayüzü uygulayan bir sınıf <see cref="Services.Register(IStoreService)"/> ile takılır. Ayrıntı: docs/services-integration.md.
    /// </summary>
    public interface IStoreService
    {
        /// <summary>Test / sahte uygulama mı? Test mağazası yayın derlemesinde hiç kullanılmaz (ürün vermez).</summary>
        bool IsTest { get; }

        /// <summary>Ürün kataloğu yüklendi, satın alma yapılabilir mi?</summary>
        bool IsReady { get; }

        /// <summary>Mağazanın bildiği ürün kimlikleri (Premium.List kimlikleriyle aynı olmalı).</summary>
        IReadOnlyList<string> ProductIds { get; }

        /// <summary>Ürünün mağazadan gelen yerel fiyat metni ("89,99 ₺"); bilinmiyorsa null.</summary>
        string PriceText(string productId);

        /// <summary>
        /// Satın almayı başlatır. Ödeme onaylanınca <paramref name="onPurchased"/> (ürün kimliği), iptal / hata olunca
        /// <paramref name="onFailed"/> (ürün kimliği, neden) çağrılır. Ödülü <see cref="Premium.Grant"/> verir, mağaza vermez.
        /// </summary>
        void Purchase(string productId, Action<string> onPurchased, Action<string, string> onFailed);

        /// <summary>Daha önce alınmış kalıcı ürünleri geri yükler; bulunan ürün kimlikleriyle geri çağırır.</summary>
        void Restore(Action<IReadOnlyList<string>> onRestored);
    }
}
