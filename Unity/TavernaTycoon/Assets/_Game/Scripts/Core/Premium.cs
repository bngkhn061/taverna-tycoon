using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Hazine (v1.0, onaylı mockup design/ui/ui_v10_hazine.html, 2026-11-10): sadece gerçek parayla alınan ürünler.
    /// v1.6: ödeme <see cref="IStoreService"/> üzerinden (<see cref="Services.Store"/>). <see cref="Request"/> mağazadan
    /// satın alma ister; ödeme onaylanınca <see cref="Grant"/> ürünü verir. Varsayılan <see cref="TestStoreService"/>
    /// geliştirme sürümünde HUD test onay ekranı açar; yayın derlemesinde gerçek mağaza kayıtlı değilse ürünler "Yakında" olur
    /// ve hiçbir ürün verilmez. Ürün tanımları ve ödüller burada kalır. Fiyat metinleri örnektir (gerçek mağaza kendi metnini verir).
    /// </summary>
    public static class Premium
    {
        public enum Kind { VipMonth, VipLife, Starter, Staff, Decor, Theme, Frame, Gems }

        public struct Product
        {
            public string id, name, desc, price, icon;
            public Kind kind;
            /// <summary>Ödülün kimliği: personel / dekor kimliği ya da elmas miktarı.</summary>
            public string grant;
            public int gems;
        }

        public static readonly Product[] List =
        {
            new() { id = "vip_month", name = "VIP Üyelik", desc = "Reklamsız · kalıcı ×2 gelir · günde 10 elmas", price = "89,99 ₺/ay", kind = Kind.VipMonth, icon = "Kit:star" },
            new() { id = "vip_life", name = "Ömür boyu VIP", desc = "VIP ayrıcalıkları süresiz", price = "449,99 ₺", kind = Kind.VipLife, icon = "Kit:star" },
            new() { id = "starter", name = "Başlangıç Paketi", desc = "5.000 altın · 100 elmas · Altın Fıçı süsü", price = "39,99 ₺", kind = Kind.Starter, icon = "DEC_AltinFici" },
            new() { id = "staff_lirael", name = "Lirael · Elf Garson", desc = "Çok hızlı · 4 bardak taşır · maaş yok", price = "79,99 ₺", kind = Kind.Staff, grant = "staff_lirael", icon = "Staff_Lirael" },
            new() { id = "staff_ignis", name = "Ignis · Ejder Aşçı", desc = "Yemekler ×2 hızlı pişer · maaş yok", price = "99,99 ₺", kind = Kind.Staff, grant = "staff_ignis", icon = "Staff_Ignis" },
            new() { id = "thm_altin", name = "Altın Tema", desc = "Altın mermer zemin + altın duvar · +40 puan", price = "49,99 ₺", kind = Kind.Theme, icon = "Prem_AltinTema" },
            new() { id = "dec_kristal", name = "Kristal Avize", desc = "+25 puan", price = "39,99 ₺", kind = Kind.Decor, grant = "dec_kristal", icon = "DEC_KristalAvize" },
            new() { id = "dec_ejder", name = "Ejder Heykeli", desc = "+30 puan", price = "59,99 ₺", kind = Kind.Decor, grant = "dec_ejder", icon = "DEC_EjderHeykeli" },
            new() { id = "frame_kraliyet", name = "Kraliyet Tabela Çerçevesi", desc = "Altın tabela · +10 puan", price = "29,99 ₺", kind = Kind.Frame, icon = "Prem_Cerceve" },
            new() { id = "gems_100", name = "100 Elmas", desc = "", price = "29,99 ₺", kind = Kind.Gems, gems = 100, icon = "Kit:gem" },
            new() { id = "gems_550", name = "550 Elmas", desc = "+%10", price = "129,99 ₺", kind = Kind.Gems, gems = 550, icon = "Kit:gem" },
            new() { id = "gems_1200", name = "1.200 Elmas", desc = "+%20 · EN İYİ", price = "249,99 ₺", kind = Kind.Gems, gems = 1200, icon = "Kit:gem" },
            new() { id = "gems_3000", name = "3.000 Elmas", desc = "+%25", price = "549,99 ₺", kind = Kind.Gems, gems = 3000, icon = "Kit:gem" },
        };

        public const int VipDailyGems = 10;
        public const float VipIncome = 2f;
        public const int StarterLevel = 3;
        public const double StarterHours = 72;
        public const float FrameAmbiance = 10f;

        /// <summary>v1.6: mağaza kapalıyken (yayın derlemesi, gerçek mağaza yok) satın alma istendi; HUD "Yakında" gösterir.</summary>
        public static event Action<Product> Unavailable;
        /// <summary>v1.6: ödeme tamamlanmadı (iptal / hata): ürün, neden.</summary>
        public static event Action<Product, string> Failed;
        public static event Action<Product> Granted;
        public static event Action Changed;

        /// <summary>Hazine satın alınabilir mi? (gerçek mağaza ya da geliştirme derlemesinde test mağazası)</summary>
        public static bool Available => Services.StoreEnabled;

        /// <summary>Test mağazası mı kullanılıyor? (Hazine notu "TEST SÜRÜMÜ" der)</summary>
        public static bool TestStore => Services.Store != null && Services.Store.IsTest && Services.StoreEnabled;

        /// <summary>Gösterilecek fiyat: mağazanın yerel metni, yoksa katalogdaki örnek fiyat.</summary>
        public static string PriceOf(Product p) => Services.Store?.PriceText(p.id) ?? p.price;

        public static Product Find(string id) => Array.Find(List, p => p.id == id);
        static DateTime Now => DateTime.UtcNow;

        // ---- sahiplik
        public static bool Owned(string id) => GameState.Data.premium.Contains(id);

        public static bool IsVip
        {
            get
            {
                if (Owned("vip_life")) return true;
                return DateTime.TryParse(GameState.Data.vipUntilUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var until) && until > Now;
            }
        }

        /// <summary>Tek seferlik ürünler alındıysa düğme "ALINDI" olur; VIP aylık ve elmas tekrar alınabilir.</summary>
        public static bool CanBuy(Product p) => p.kind switch
        {
            Kind.Gems => true,
            Kind.VipMonth => !Owned("vip_life"),
            Kind.VipLife => !Owned("vip_life"),
            Kind.Starter => StarterAvailable,
            _ => !Owned(p.id),
        };

        /// <summary>Başlangıç paketi: Sv3'te bir kez teklif edilir, 72 saat (gerçek zaman) Hazine'de durur.</summary>
        public static bool StarterAvailable
        {
            get
            {
                if (Owned("starter") || GameState.Level < StarterLevel) return false;
                if (string.IsNullOrEmpty(GameState.Data.starterShownUtc)) return true;
                return DateTime.TryParse(GameState.Data.starterShownUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var t)
                       && (Now - t).TotalHours < StarterHours;
            }
        }

        public static TimeSpan StarterLeft
        {
            get
            {
                if (!DateTime.TryParse(GameState.Data.starterShownUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var t)) return TimeSpan.FromHours(StarterHours);
                var left = t.AddHours(StarterHours) - Now;
                return left > TimeSpan.Zero ? left : TimeSpan.Zero;
            }
        }

        /// <summary>Teklif ekranı bir kez açılacak mı? (Sv3'e ulaşıldı, hiç gösterilmedi)</summary>
        public static bool ShouldOfferStarter => !Owned("starter") && GameState.Level >= StarterLevel && string.IsNullOrEmpty(GameState.Data.starterShownUtc);

        public static void MarkStarterShown()
        {
            GameState.Data.starterShownUtc = Now.ToString("o");
            GameState.Save();
        }

        // ---- etkiler
        public static float IncomeMultiplier => IsVip ? VipIncome : 1f;
        /// <summary>Tabela çerçevesinin puanı (Altın tema puanı Themes'ten, premium süslerinki kendi kaynaklarından gelir).</summary>
        public static float Ambiance => Owned("frame_kraliyet") ? FrameAmbiance : 0f;

        /// <summary>VIP günlük elması (gerçek gün başına bir kez). GameBootstrap çağırır.</summary>
        public static void TickDaily()
        {
            if (!IsVip) return;
            var today = Now.ToString("yyyy-MM-dd");
            if (GameState.Data.vipGemDate == today) return;
            GameState.Data.vipGemDate = today;
            GameState.Data.gems += VipDailyGems;
            GameState.Save();
            GameState.NotifyChanged();
        }

        // ---- satın alma
        /// <summary>
        /// Satın alma ister (mağaza üzerinden). Ölçüm: store_request; ödeme onaylanınca store_purchase ve <see cref="Grant"/>.
        /// Mağaza kapalıysa (<see cref="Available"/> false) yalnızca <see cref="Unavailable"/> yayınlanır, ürün verilmez.
        /// </summary>
        public static void Request(string id)
        {
            var p = Find(id);
            if (string.IsNullOrEmpty(p.id) || !CanBuy(p)) return;
            Services.Track("store_request", ("urun", p.id), ("acik", Available));
            if (!Available) { Unavailable?.Invoke(p); return; }
            Services.Store.Purchase(p.id,
                ok => { Services.Track("store_purchase", ("urun", ok)); Grant(ok); },
                (pid, why) => { Services.Track("store_fail", ("urun", pid), ("neden", why)); Failed?.Invoke(p, why); });
        }

        /// <summary>
        /// Ödeme onaylandı: ürünü ver (mağaza makbuzu ya da geliştirme derlemesinde test onayı).
        /// v1.6 güvenlik: mağaza kapalıyken hiçbir yolla ürün verilmez.
        /// </summary>
        public static void Grant(string id)
        {
            var p = Find(id);
            if (string.IsNullOrEmpty(p.id)) return;
            if (!Available) { Debug.LogWarning($"[Hazine] mağaza kapalı, ürün verilmedi: {id}"); return; }
            var d = GameState.Data;
            switch (p.kind)
            {
                case Kind.VipMonth:
                    var from = DateTime.TryParse(d.vipUntilUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var u) && u > Now ? u : Now;
                    d.vipUntilUtc = from.AddDays(30).ToString("o");
                    break;
                case Kind.Gems:
                    d.gems += p.gems;
                    break;
                case Kind.Starter:
                    d.coins += 5000;
                    d.gems += 100;
                    GiveDecor("dec_altinfici");
                    break;
                case Kind.Staff:
                    if (!d.unlocked.Contains(p.grant)) d.unlocked.Add(p.grant);   // Unlock() değil: "yeni alan açıldı" kutlaması çıkmasın
                    if (TavernRegistry.I) TavernRegistry.I.ApplyStaff(p.grant, true);
                    break;
                case Kind.Decor:
                    GiveDecor(p.grant);
                    break;
                case Kind.Theme:
                    foreach (var tid in new[] { "zemin_altin", "duvar_altin" })
                    {
                        if (!d.unlocked.Contains("thm_" + tid)) d.unlocked.Add("thm_" + tid);
                        Themes.BuyOrSelect(Themes.Find(tid));
                    }
                    break;
            }
            if (p.kind != Kind.Gems && p.kind != Kind.VipMonth && !d.premium.Contains(p.id)) d.premium.Add(p.id);
            GameState.Save();
            GameState.NotifyChanged();
            Granted?.Invoke(p);
            Changed?.Invoke();
        }

        static void GiveDecor(string decorId)
        {
            int slot = Management.FreeDecorSlot(decorId);
            if (slot < 0) return;
            GameState.Data.decor.Add($"{decorId}:{slot}");
            if (TavernRegistry.I) TavernRegistry.I.PlaceDecor(decorId, slot, true);
        }

        /// <summary>
        /// Satın almaları geri yükle: mağazanın bildirdiği kalıcı ürünlerden kayıtta olmayanları verir; bulunan ürün sayısıyla
        /// geri çağırır. Mağaza kapalıysa -1 ile geri çağırır.
        /// </summary>
        public static void Restore(Action<int> done)
        {
            if (!Available) { done?.Invoke(-1); return; }
            Services.Store.Restore(ids =>
            {
                foreach (var id in ids)
                {
                    var p = Find(id);
                    if (!string.IsNullOrEmpty(p.id) && p.kind != Kind.Gems && p.kind != Kind.VipMonth && !Owned(id)) Grant(id);
                }
                done?.Invoke(ids.Count);
            });
        }
    }
}
