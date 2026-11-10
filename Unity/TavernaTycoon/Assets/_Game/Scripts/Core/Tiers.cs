using System;
using System.Collections.Generic;

namespace Taverna
{
    /// <summary>
    /// "Tavernayı Büyüt" kademeleri (v1.0, onaylı mockup design/ui/ui_v11_buyut.html, 2026-11-10):
    /// Ahşap Meyhane → Taş Han (Sv15) → Saray Hanı (Sv22). Her kademe satış fiyatlarını çarpar, taverna puanı ekler,
    /// zengin müşterileri sıklaştırır ve sahnedeki yapı parçalarını değiştirir (TierSet). Oyuncunun zemin/duvar teması korunur.
    /// </summary>
    public static class Tiers
    {
        public class Tier
        {
            public int index;
            public string name, looks, guests;
            public int level;
            public long cost;
            public float price, ambiance;
            /// <summary>Kademe için açık olması gereken alanlar (kimlik, görünen ad).</summary>
            public (string id, string name)[] zones;
        }

        public static readonly Tier[] List =
        {
            new() { index = 0, name = "Ahşap Meyhane", level = 1, cost = 0, price = 1f, ambiance = 0f, zones = new (string, string)[0],
                    looks = "Ahşap direkler, mum avizeler", guests = "Köy ve kasaba müşterileri" },
            new() { index = 1, name = "Taş Han", level = 15, cost = 35000, price = 1.5f, ambiance = 30f,
                    zones = new[] { ("salon", "Ana Salon"), ("bahce", "Bahçe"), ("mutfak", "Mutfak"), ("merdiven", "Han"), ("oyun", "Oyun Salonu") },
                    looks = "Taş sütun ve kemerler, demir avizeler", guests = "Şövalye, tüccar, maceracı daha sık" },
            // Saray: yeni bölümler (şarap mahzeni, çatı terası) sıradaki partide gelir; o zamana kadar şart tutmaz
            new() { index = 2, name = "Saray Hanı", level = 22, cost = 120000, price = 2.25f, ambiance = 60f,
                    zones = new[] { ("mahzen", "Şarap Mahzeni"), ("teras", "Çatı Terası") },
                    looks = "Mermer, altın süsler, kadife perdeler", guests = "Soylular, büyücüler, Gizli Kral" },
        };

        public static event Action Changed;
        /// <summary>Barlo "Tavernayı Büyüt" alanına bastı (HUD kademe kartını açar).</summary>
        public static event Action ZoneEntered;

        public static int CurrentIndex => Math.Clamp(GameState.Data.tier, 0, List.Length - 1);
        public static Tier Current => List[CurrentIndex];
        public static Tier Next => CurrentIndex + 1 < List.Length ? List[CurrentIndex + 1] : null;

        public static float PriceMultiplier => Current.price;
        public static float Ambiance => Current.ambiance;
        /// <summary>Zengin (ödeme ★4+) müşterilerin ağırlık çarpanı.</summary>
        public static float RichWeight => 1f + 0.5f * CurrentIndex;

        /// <summary>Hedef alanı görünür mü? (sıradaki kademenin seviyesine gelindi)</summary>
        public static bool Visible => Next != null && GameState.Level >= Next.level;

        public static List<(string label, bool ok)> Requirements(Tier t)
        {
            var r = new List<(string, bool)> { ($"Seviye {t.level}", GameState.Level >= t.level) };
            bool all = true;
            foreach (var z in t.zones) if (!GameState.IsUnlocked(z.id)) all = false;
            if (t.zones.Length > 0)
            {
                if (all) r.Add(("Tüm alanlar açık", true));
                else foreach (var z in t.zones) if (!GameState.IsUnlocked(z.id)) r.Add(($"{z.name} açık değil", false));
            }
            r.Add(($"Altın {HudController.FormatNum(Math.Min(GameState.Coins, t.cost))} / {HudController.FormatNum(t.cost)}", GameState.Coins >= t.cost));
            return r;
        }

        public static bool CanUpgrade
        {
            get
            {
                var n = Next;
                if (n == null) return false;
                foreach (var (_, ok) in Requirements(n)) if (!ok) return false;
                return true;
            }
        }

        public static void RaiseZoneEntered() => ZoneEntered?.Invoke();

        /// <summary>Parayı düşer ve kademeyi yükseltir; görünüm değişimini HUD'daki dönüşüm animasyonu tetikler (Apply).</summary>
        public static bool Upgrade()
        {
            if (!CanUpgrade || !GameState.TrySpend(Next.cost)) return false;
            GameState.Data.tier = CurrentIndex + 1;
            GameState.Save();
            return true;
        }

        /// <summary>Sahnedeki kademe parçalarını yeni kademeye geçirir (dönüşüm animasyonunun ortasında çağrılır).</summary>
        public static void Apply()
        {
            Changed?.Invoke();
            GameState.NotifyChanged();
        }
    }
}
