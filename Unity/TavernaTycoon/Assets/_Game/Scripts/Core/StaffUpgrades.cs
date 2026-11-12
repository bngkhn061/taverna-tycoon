using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Görevli gelişimi (v0.6, oyuncu testi: "görevlilerin upgradeleri olsun, hem reklam hem para ile").
    /// Her işe alınmış görevli 5 seviyeye kadar gelişir; seviye parayla alınır ya da görevli başına
    /// 10 dakikada bir reklam izleyerek bedava verilir (v1.1: görevli başına en fazla 2 kez; en yüksek seviye uzun reklam). Etkiler rolüne göre (Balance sabitleri).
    /// </summary>
    public static class StaffUpgrades
    {
        public const int MaxLevel = 5;
        public const float AdCooldownSeconds = 600f;
        /// <summary>v1.1 (oyuncu testi): görevli başına reklamla en fazla 2 seviye; gerisi parayla.</summary>
        public const int MaxAdLevels = 2;
        /// <summary>En yüksek seviyeyi reklamla almak uzun reklam ister (sn, sahte reklam ekranı).</summary>
        public const float LongAdSeconds = 15f, ShortAdSeconds = 5f;
        static readonly Dictionary<string, float> _adReadyAt = new();

        public static int Level(string staffId)
        {
            int i = GameState.Data.staffIds.IndexOf(staffId);
            return i >= 0 ? GameState.Data.staffLvs[i] : 0;
        }

        static void SetLevel(string staffId, int lv)
        {
            int i = GameState.Data.staffIds.IndexOf(staffId);
            if (i >= 0) GameState.Data.staffLvs[i] = lv;
            else { GameState.Data.staffIds.Add(staffId); GameState.Data.staffLvs.Add(lv); }
        }

        /// <summary>v1.3 kovulan görevli yeniden işe alınınca baştan başlar.</summary>
        public static void ResetLevel(string staffId)
        {
            int i = GameState.Data.staffIds.IndexOf(staffId);
            if (i >= 0) GameState.Data.staffLvs[i] = 0;
        }

        public static int Cost(Catalog.Staff s, int lv) => Mathf.RoundToInt(s.hire * 0.6f * Mathf.Pow(1.6f, lv));

        public static bool CanLevel(Catalog.Staff s) => Management.Hired(s.id) && Level(s.id) < MaxLevel;

        public static bool TryBuy(Catalog.Staff s)
        {
            if (!CanLevel(s)) return false;
            if (!GameState.TrySpend(Cost(s, Level(s.id)))) return false;
            Grant(s.id);
            return true;
        }

        public static float AdWait(string staffId) =>
            _adReadyAt.TryGetValue(staffId, out var t) ? Mathf.Max(0f, t - Time.unscaledTime) : 0f;

        public static int AdsUsed(string staffId) => GameState.Data.staffAds.FindAll(x => x == staffId).Count;
        public static int AdsLeft(string staffId) => Mathf.Max(0, MaxAdLevels - AdsUsed(staffId));
        public static bool AdAvailable(Catalog.Staff s) => CanLevel(s) && AdsLeft(s.id) > 0 && AdWait(s.id) <= 0f;
        /// <summary>Bu reklam görevliyi en yüksek seviyeye çıkaracaksa uzun reklam.</summary>
        public static bool AdIsLong(Catalog.Staff s) => Level(s.id) + 1 >= MaxLevel;

        /// <summary>Reklam ödülü: bir seviye bedava, sonra bekleme süresi başlar.</summary>
        public static void GrantByAd(string staffId)
        {
            _adReadyAt[staffId] = Time.unscaledTime + AdCooldownSeconds;
            GameState.Data.staffAds.Add(staffId);
            Grant(staffId);
        }

        static void Grant(string staffId)
        {
            SetLevel(staffId, Mathf.Min(MaxLevel, Level(staffId) + 1));
            Quests.OnUpgrade();
            GameState.Save();
            GameState.NotifyChanged();
        }

        // ---- etkiler
        /// <summary>Garson: seviye başına %8 hız. Temizlikçi: seviye başına %10.</summary>
        public static float SpeedMultiplier(string staffId, Catalog.Role role) =>
            (1f + Level(staffId) * (role == Catalog.Role.Temizlikci || role == Catalog.Role.Hizmetci ? 0.10f : 0.08f)) * Management.MoraleMultiplier;

        /// <summary>Garson: 2. ve 4. seviyede +1 bardak.</summary>
        public static int CarryBonus(string staffId)
        {
            int lv = Level(staffId);
            return (lv >= 2 ? 1 : 0) + (lv >= 4 ? 1 : 0);
        }

        /// <summary>Barmen makine hızı: %40 + seviye başına %5.</summary>
        public static float BarmenBonus(string staffId) => 0.40f + 0.05f * Level(staffId);

        /// <summary>Ozan sabır bonusu: %25 + seviye başına %5.</summary>
        public static float BardBonus(string staffId) => 0.25f + 0.05f * Level(staffId);

        /// <summary>Kartta gösterilen etki metni.</summary>
        public static string EffectText(Catalog.Staff s, int lv) => s.role switch
        {
            Catalog.Role.Garson => Loc.F("Hız +%{0} · taşıma +{1}", 8 * lv, (lv >= 2 ? 1 : 0) + (lv >= 4 ? 1 : 0)),
            Catalog.Role.Barmen => Loc.F("Makineler +%{0} hız", 40 + 5 * lv),
            Catalog.Role.Temizlikci => Loc.F("Hız +%{0}", 10 * lv),
            Catalog.Role.Fenerci => Loc.F("Hız +%{0}", 8 * lv),
            Catalog.Role.Asci => Loc.F("Yemekler +%{0} hız", 40 + 5 * lv),
            Catalog.Role.Hizmetci => Loc.F("Hız +%{0}", 10 * lv),
            _ => Loc.F("Sabır +%{0}", 25 + 5 * lv),
        };
    }
}
