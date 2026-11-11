using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Etkinlikler (v1.2, mockup design/ui/ui_v12_akis.html E; oyuncu testi: "gün içinde para harcayarak konser getirelim,
    /// daha çok müşteri gelsin; başka etkinlik daha çok para getirsin"). Altınla başlar, 3 dk sürer, her biri günde bir kez,
    /// aynı anda tek etkinlik. Bölgesi açık değilse (kervan: Ana Salon, turnuva: Oyun Salonu) başlatılamaz.
    /// Etkiler çarpan olarak okunur: müşteri gelişi, satış fiyatı, zengin müşteri ve bahşiş, oyun masası ücreti.
    /// v1.4: Tellal (Sv2, 40 + 30·Sv, müşteri ×1.3) eski parayla "Tellal tut" satın alımının yerini aldı.
    /// v1.5: Kraliyet Ziyareti (Sv28, 2.000 + 500·Sv, Ana Salon): fiyat ×2, bahşiş ×2, Gizli Kral ağırlığı ×40.
    /// </summary>
    public static class TavernEvents
    {
        public enum Id { Konser, Festival, Kervan, Turnuva, Tellal, Kraliyet }

        /// <summary>v1.4 Tellal etkinliği: müşteri gelişi çarpanı (eski parayla "Tellal tut" satın alımının yerine).</summary>
        public const float TellalSpawn = 1.3f;

        /// <summary>v1.5 Kraliyet Ziyareti (Sv28): satış fiyatı ve bahşiş çarpanı.</summary>
        public const float RoyalPrice = 2f, RoyalTip = 2f;
        /// <summary>v1.5 Kraliyet Ziyareti: Gizli Kral'ın (müşteri 30) gelme ağırlığı çarpanı (taban ağırlık 0.05 → 2).</summary>
        public const float RoyalKingWeight = 40f;
        /// <summary>Gizli Kral müşterisinin numarası (CHARACTERS.md §B).</summary>
        public const int KingCustomerNumber = 30;

        public struct Def
        {
            public Id id;
            public string key, name, desc, icon, zone, zoneName;
            public int level;
            public long baseCost, perLevel;
        }

        public const float Seconds = 180f;

        public static readonly Def[] List =
        {
            // v1.4: Tellal ilk etkinlik (Sv2); sokakta zil çalan tellal görünür (Campaigns)
            new() { id = Id.Tellal, key = "tellal", name = "Tellal", desc = "Müşteri gelişi ×1.3 · sokakta tellal bağırır", icon = "Cust_15", level = 2, baseCost = 40, perLevel = 30 },
            new() { id = Id.Konser, key = "konser", name = "Ozan Konseri", desc = "Müşteri gelişi ×1.6 · ozan çalar", icon = "Staff_OzanSahne", level = 4, baseCost = 120, perLevel = 60 },
            new() { id = Id.Festival, key = "festival", name = "Bira Festivali", desc = "Tüm satışlar ×1.5 fiyat", icon = "Bira", level = 5, baseCost = 150, perLevel = 80 },
            new() { id = Id.Kervan, key = "kervan", name = "Tüccar Kervanı", desc = "Zengin müşteriler gelir · bahşiş ×2", icon = "Cust_09", level = 6, zone = "salon", zoneName = "Ana Salon", baseCost = 200, perLevel = 100 },
            new() { id = Id.Turnuva, key = "turnuva", name = "Zar Turnuvası", desc = "Oyun masaları ×2.5 ücret, hızlı tur", icon = "ZarMasasi", level = 13, zone = "oyun", zoneName = "Oyun Salonu", baseCost = 250, perLevel = 120 },
            // v1.5: oyun sonu etkinliği, en pahalısı (Sv28: 2.000 + 500×28 = 16.000)
            new() { id = Id.Kraliyet, key = "kraliyet", name = "Kraliyet Ziyareti", desc = "Satışlar ×2 · bahşiş ×2 · Gizli Kral gelir", icon = "Cust_30", level = 28, zone = "salon", zoneName = "Ana Salon", baseCost = 2000, perLevel = 500 },
        };

        public static event Action Changed;

        static Id? _active;
        static float _until;

        public static Id? Active => _active.HasValue && Time.unscaledTime < _until ? _active : null;
        public static float Remaining => Active.HasValue ? _until - Time.unscaledTime : 0f;
        public static bool Is(Id id) => Active == id;
        public static Def Get(Id id) => Array.Find(List, d => d.id == id);

        public static long Cost(Def d) => d.baseCost + d.perLevel * GameState.Level;

        public enum State { Ready, Running, Busy, UsedToday, LevelLow, ZoneLocked, NoMoney }

        public static bool UsedToday(Def d) => GameState.Data.eventDay == GameState.Data.day && GameState.Data.eventsUsed.Contains(d.key);

        public static State StateOf(Def d)
        {
            if (Is(d.id)) return State.Running;
            if (GameState.Level < d.level) return State.LevelLow;
            if (!Management.ZoneOpen(d.zone)) return State.ZoneLocked;
            if (UsedToday(d)) return State.UsedToday;
            if (Active.HasValue) return State.Busy;
            return GameState.Coins >= Cost(d) ? State.Ready : State.NoMoney;
        }

        /// <summary>Etkinliği başlatır (para düşer). Başlamazsa nedeni döner.</summary>
        public static State Start(Def d)
        {
            var s = StateOf(d);
            if (s != State.Ready) return s;
            if (!GameState.TrySpend(Cost(d))) return State.NoMoney;
            if (GameState.Data.eventDay != GameState.Data.day) { GameState.Data.eventDay = GameState.Data.day; GameState.Data.eventsUsed.Clear(); }
            GameState.Data.eventsUsed.Add(d.key);
            _active = d.id;
            _until = Time.unscaledTime + Seconds;
            GameState.Save();
            Changed?.Invoke();
            return State.Running;
        }

        /// <summary>Gün bitince süren etkinlik de biter.</summary>
        public static void StopAll() { _active = null; Changed?.Invoke(); }

        // ---- çarpanlar
        public static float SpawnMultiplier => Is(Id.Konser) ? 1.6f : Is(Id.Kervan) ? 1.2f : Is(Id.Tellal) ? TellalSpawn : 1f;
        public static float PriceMultiplier => Is(Id.Festival) ? 1.5f : Is(Id.Kraliyet) ? RoyalPrice : 1f;
        public static float TipMultiplier => Is(Id.Kervan) ? 2f : Is(Id.Kraliyet) ? RoyalTip : 1f;
        public static float RichWeight => Is(Id.Kervan) ? 5f : 1f;
        /// <summary>v1.5: müşteriye özel ağırlık (Kraliyet Ziyareti'nde Gizli Kral ×40). Aynı anda tek etkinlik olduğu için çarpanlar karışmaz.</summary>
        public static float CustomerWeight(int number) => Is(Id.Kraliyet) && number == KingCustomerNumber ? RoyalKingWeight : 1f;
        public static float GameFeeMultiplier => Is(Id.Turnuva) ? 2.5f : 1f;
        public static float GameRoundSpeed => Is(Id.Turnuva) ? 1.4f : 1f;

        public static string Effect(Id id) => id switch
        {
            Id.Konser => "müşteri ×1.6",
            Id.Festival => "fiyat ×1.5",
            Id.Kervan => "bahşiş ×2",
            Id.Tellal => $"müşteri ×{TellalSpawn:0.0}",
            Id.Kraliyet => $"fiyat ×{RoyalPrice:0} · bahşiş ×{RoyalTip:0}",
            _ => "oyun ücreti ×2.5",
        };
    }
}
