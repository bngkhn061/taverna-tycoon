using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Bölüm bölüm otomasyon (v0.9, onaylı taslak 2026-11-10 "oyun yavaş yavaş gelişip otomatiğe geçsin").
    /// Her bölüm ayrı satın alınır; ilki alınınca pahalı "Usta" geliştirmeleri açılır.
    /// Depo otomasyonu mevcut "otomatik sipariş" bayrağıdır (GameState.Data.autoOrder).
    /// </summary>
    public static class Automation
    {
        /// <summary>
        /// Bir otomasyon. v1.9 (oyuncu testi madde 9, 16): iki fiyat — altın (level şartıyla) ya da elmas (gemLevel, daha erken;
        /// erken almak lüks olsun diye pahalı). zone: çalışacağı bölge açılmadan alınamaz (boş = her yer).
        /// </summary>
        public struct Def { public string id, name, desc, icon, zone, zoneName; public int level, gemLevel, gems; public long price; }

        /// v1.9 elmas fiyatları (GAME_DESIGN.md §8.2, ilk saatte ≈ 25 elmas toplanır): Kasa 6 seviye erken 150, Depo 4 seviye erken 30,
        /// Han ve Oyun salonu 4–5 seviye erken 80 / 100 (bölgesi açılınca).
        public static readonly Def[] List =
        {
            new() { id = "kasa", name = Loc.N("Kasa"), desc = Loc.N("Masalardaki para kendiliğinden toplanır"), icon = "coin", level = Balance.KasaLevel, price = Balance.KasaPrice, gemLevel = 16, gems = 150 },
            new() { id = "depo", name = Loc.N("Depo"), desc = Loc.N("Azalan malzeme otomatik sipariş edilir"), icon = "stock", level = Balance.AutoOrderLevel, price = Balance.AutoOrderPrice, gemLevel = 3, gems = 30 },
            new() { id = "han", name = Loc.N("Han resepsiyonu"), desc = Loc.N("Misafir kendi giriş yapar"), icon = "bell", level = 12, price = 6000, gemLevel = 8, gems = 80, zone = "merdiven", zoneName = Loc.N("Han") },
            new() { id = "oyun", name = Loc.N("Oyun salonu"), desc = Loc.N("Oyun masaları kendi döner"), icon = "gift", level = 14, price = 8000, gemLevel = 9, gems = 100, zone = "oyun", zoneName = Loc.N("Oyun Salonu") },
        };

        /// <summary>Usta geliştirmeleri: kademe başı etki ve fiyatlar.</summary>
        public const int UstaMaxLevel = 3;
        /// v1.8: 8.000 / 18.000 / 40.000 → 6.000 / 14.000 / 30.000 (geri ödeme Sv15–29 gelirinde ≈ 3–6 gün; eskisi 5–8 gün).
        public static readonly long[] UstaPrices = { 6000, 14000, 30000 };
        public const float UstaIncomePerLv = 0.10f, UstaGuestPerLv = 0.10f;

        public static event Action Changed;

        public static bool Has(string id) => id == "depo" ? GameState.Data.autoOrder : GameState.Data.autos.Contains(id);
        public static int OwnedCount { get { int n = 0; foreach (var d in List) if (Has(d.id)) n++; return n; } }
        public static bool UstaOpen => OwnedCount > 0;

        /// <summary>Altınla alınabilir mi (seviye <see cref="Def.level"/>).</summary>
        public static Management.BuyResult CanBuy(Def d)
        {
            if (Has(d.id)) return Management.BuyResult.Owned;
            if (GameState.Level < d.level) return Management.BuyResult.LevelLow;
            if (!Management.ZoneOpen(d.zone)) return Management.BuyResult.ZoneLocked;
            return GameState.Coins >= d.price ? Management.BuyResult.Ok : Management.BuyResult.NoMoney;
        }

        /// <summary>v1.9: elmasla alınabilir mi (seviye <see cref="Def.gemLevel"/>, altından erken).</summary>
        public static Management.BuyResult CanBuyWithGems(Def d)
        {
            if (Has(d.id)) return Management.BuyResult.Owned;
            if (GameState.Level < d.gemLevel) return Management.BuyResult.LevelLow;
            if (!Management.ZoneOpen(d.zone)) return Management.BuyResult.ZoneLocked;
            return GameState.Data.gems >= d.gems ? Management.BuyResult.Ok : Management.BuyResult.NoMoney;
        }

        /// <summary>Altınla satın alır.</summary>
        public static Management.BuyResult Buy(Def d)
        {
            var r = CanBuy(d);
            if (r != Management.BuyResult.Ok) return r;
            if (!GameState.TrySpend(d.price)) return Management.BuyResult.NoMoney;
            Grant(d, "altin");
            return Management.BuyResult.Ok;
        }

        /// <summary>v1.9: elmasla satın alır.</summary>
        public static Management.BuyResult BuyWithGems(Def d)
        {
            var r = CanBuyWithGems(d);
            if (r != Management.BuyResult.Ok) return r;
            GameState.Data.gems -= d.gems;
            Grant(d, "elmas");
            return Management.BuyResult.Ok;
        }

        static void Grant(Def d, string currency)
        {
            if (d.id == "depo") GameState.Data.autoOrder = true;
            else GameState.Data.autos.Add(d.id);
            Services.Track("automation_buy", ("otomasyon", d.id), ("para", currency), ("seviye", GameState.Level));
            GameState.Save();
            GameState.NotifyChanged();
            Changed?.Invoke();
        }

        // ---- Usta
        public static int UstaIncomeLv => GameState.Data.ustaGelir;
        public static int UstaGuestLv => GameState.Data.ustaMusteri;
        /// <summary>Kalıcı gelir çarpanı (Boosts.IncomeMultiplier içine girer).</summary>
        public static float IncomeMultiplier => 1f + UstaIncomeLv * UstaIncomePerLv;
        /// <summary>Kalıcı müşteri sıklığı çarpanı (CustomerSpawner).</summary>
        public static float GuestMultiplier => 1f + UstaGuestLv * UstaGuestPerLv;

        public static long UstaPrice(int lv) => lv < UstaMaxLevel ? UstaPrices[lv] : 0;

        /// <summary>which: "gelir" ya da "musteri".</summary>
        public static Management.BuyResult BuyUsta(string which)
        {
            if (!UstaOpen) return Management.BuyResult.LevelLow;
            int lv = which == "gelir" ? UstaIncomeLv : UstaGuestLv;
            if (lv >= UstaMaxLevel) return Management.BuyResult.Owned;
            if (!GameState.TrySpend(UstaPrice(lv))) return Management.BuyResult.NoMoney;
            if (which == "gelir") GameState.Data.ustaGelir++; else GameState.Data.ustaMusteri++;
            GameState.Save();
            GameState.NotifyChanged();
            Changed?.Invoke();
            return Management.BuyResult.Ok;
        }
    }
}
