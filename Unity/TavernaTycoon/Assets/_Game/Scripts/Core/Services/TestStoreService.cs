using System;
using System.Collections.Generic;

namespace Taverna
{
    /// <summary>
    /// Test mağazası (v1.0 Hazine test onayı, v1.6'da <see cref="IStoreService"/> uygulaması): satın alma isteğinde HUD bir
    /// "TEST · gerçek ödeme alınmaz" onay ekranı açar (<see cref="Confirmer"/>); ONAYLA ödemeyi başarılı sayar.
    /// Fiyat metinleri Premium.List'teki örnek fiyatlardır. Yalnızca geliştirme derlemesinde çalışır.
    /// </summary>
    public class TestStoreService : IStoreService
    {
        /// <summary>Test onay ekranını açan HUD işlevi: (ürün kimliği, onaylandı, vazgeçildi). HUD kaydeder.</summary>
        public static Action<string, Action, Action> Confirmer;

        readonly List<string> _ids = new();

        /// <summary>Ürün listesini Premium kataloğundan kurar.</summary>
        public TestStoreService()
        {
            foreach (var p in Premium.List) _ids.Add(p.id);
        }

        /// <inheritdoc/>
        public bool IsTest => true;

        /// <inheritdoc/>
        public bool IsReady => Services.TestProvidersAllowed;

        /// <inheritdoc/>
        public IReadOnlyList<string> ProductIds => _ids;

        /// <inheritdoc/>
        public string PriceText(string productId)
        {
            var p = Premium.Find(productId);
            return string.IsNullOrEmpty(p.id) ? null : p.price;
        }

        /// <inheritdoc/>
        public void Purchase(string productId, Action<string> onPurchased, Action<string, string> onFailed)
        {
            if (!Services.TestProvidersAllowed) { onFailed?.Invoke(productId, Loc.T("test mağazası yayın derlemesinde kapalı")); return; }
            if (!_ids.Contains(productId)) { onFailed?.Invoke(productId, Loc.T("bilinmeyen ürün")); return; }
            if (Confirmer == null) { onFailed?.Invoke(productId, Loc.T("onay ekranı yok")); return; }
            Confirmer(productId, () => onPurchased?.Invoke(productId), () => onFailed?.Invoke(productId, Loc.T("vazgeçildi")));
        }

        /// <inheritdoc/>
        public void Restore(Action<IReadOnlyList<string>> onRestored)
        {
            // Test mağazasında makbuz yok: kayıtta zaten alınmış görünenler geri yüklenmiş sayılır.
            var owned = new List<string>();
            if (Services.TestProvidersAllowed)
                foreach (var id in _ids) if (Premium.Owned(id)) owned.Add(id);
            onRestored?.Invoke(owned);
        }
    }
}
