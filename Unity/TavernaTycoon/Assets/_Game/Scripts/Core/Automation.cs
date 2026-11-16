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
        public struct Def { public string id, name, desc, icon; public int level; public long price; }

        public static readonly Def[] List =
        {
            new() { id = "kasa", name = Loc.N("Kasa"), desc = Loc.N("Masalardaki para kendiliğinden toplanır"), icon = "coin", level = Balance.KasaLevel, price = Balance.KasaPrice },
            new() { id = "depo", name = Loc.N("Depo"), desc = Loc.N("Azalan malzeme otomatik sipariş edilir"), icon = "stock", level = Balance.AutoOrderLevel, price = Balance.AutoOrderPrice },
            new() { id = "han", name = Loc.N("Han resepsiyonu"), desc = Loc.N("Misafir kendi giriş yapar"), icon = "bell", level = 12, price = 6000 },
            new() { id = "oyun", name = Loc.N("Oyun salonu"), desc = Loc.N("Oyun masaları kendi döner"), icon = "gift", level = 14, price = 8000 },
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

        public static Management.BuyResult CanBuy(Def d)
        {
            if (Has(d.id)) return Management.BuyResult.Owned;
            if (GameState.Level < d.level) return Management.BuyResult.LevelLow;
            return GameState.Coins >= d.price ? Management.BuyResult.Ok : Management.BuyResult.NoMoney;
        }

        public static Management.BuyResult Buy(Def d)
        {
            var r = CanBuy(d);
            if (r != Management.BuyResult.Ok) return r;
            if (d.id == "depo") { if (!Depot.BuyAutoOrder()) return Management.BuyResult.NoMoney; }
            else
            {
                if (!GameState.TrySpend(d.price)) return Management.BuyResult.NoMoney;
                GameState.Data.autos.Add(d.id);
                GameState.Save();
                GameState.NotifyChanged();
            }
            Changed?.Invoke();
            return Management.BuyResult.Ok;
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
