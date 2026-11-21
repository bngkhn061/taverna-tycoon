using UnityEngine;

namespace Taverna
{
    /// <summary>v2.1 Barlo'nun yetenekleri (oyuncu testi madde 10).</summary>
    public enum BarloSkill { Hiz, Tasima, Servis, Para }

    /// <summary>
    /// v2.1 Barlo'nun kalıcı yükseltmeleri (GAME_DESIGN.md §15.1): koşu hızı, taşıma kapasitesi, servis menzili, para toplama menzili.
    /// Her biri 5 seviye, altınla; her seviye belirli bir taverna seviyesinde açılır. Sayılar <see cref="Balance"/> tablolarında.
    /// Etkiler: PlayerController (hız, taşıma), Customer / DrinkStation / Table / Guest / GameTable (<see cref="Reach"/>), CoinPile (<see cref="CoinReach"/>).
    /// Prestijte sıfırlanır (Prestige.Apply).
    /// </summary>
    public static class BarloSkills
    {
        /// <summary>En yüksek yetenek seviyesi.</summary>
        public const int MaxLevel = Balance.BarloSkillMaxLevel;

        /// <summary>Dükkân sırası.</summary>
        public static readonly BarloSkill[] All = { BarloSkill.Hiz, BarloSkill.Tasima, BarloSkill.Servis, BarloSkill.Para };

        /// <summary>Kayıttaki seviye (0–5).</summary>
        public static int Level(BarloSkill s) => Mathf.Clamp(Raw(GameState.Data, s), 0, MaxLevel);

        static int Raw(SaveData d, BarloSkill s) => s switch
        {
            BarloSkill.Hiz => d.barloSpeed,
            BarloSkill.Tasima => d.barloCarry,
            BarloSkill.Servis => d.barloServe,
            _ => d.barloCoin,
        };

        static void Set(SaveData d, BarloSkill s, int lv)
        {
            switch (s)
            {
                case BarloSkill.Hiz: d.barloSpeed = lv; break;
                case BarloSkill.Tasima: d.barloCarry = lv; break;
                case BarloSkill.Servis: d.barloServe = lv; break;
                default: d.barloCoin = lv; break;
            }
        }

        /// <summary>Prestij: bütün yetenekler 0'a döner.</summary>
        public static void ResetAll(SaveData d) { d.barloSpeed = d.barloCarry = d.barloServe = d.barloCoin = 0; }

        /// <summary>Yetenek seviyelerinin açıldığı taverna seviyeleri (indeks 0 = yetenek Sv1).</summary>
        public static int[] UnlockTable(BarloSkill s) => s switch
        {
            BarloSkill.Hiz => Balance.BarloSpeedLevels,
            BarloSkill.Tasima => Balance.BarloCarryLevels,
            BarloSkill.Servis => Balance.BarloServeLevels,
            _ => Balance.BarloCoinLevels,
        };

        /// <summary>Yetenek seviyesi bedelleri (indeks 0 = yetenek Sv1).</summary>
        public static long[] PriceTable(BarloSkill s) => s switch
        {
            BarloSkill.Hiz => Balance.BarloSpeedPrices,
            BarloSkill.Tasima => Balance.BarloCarryPrices,
            BarloSkill.Servis => Balance.BarloServePrices,
            _ => Balance.BarloCoinPrices,
        };

        /// <summary>Etki değeri (indeks 0 = yükseltmesiz, <see cref="MaxLevel"/> = en üst); taşıma da float döner.</summary>
        public static float Value(BarloSkill s, int lv)
        {
            lv = Mathf.Clamp(lv, 0, MaxLevel);
            return s switch
            {
                BarloSkill.Hiz => Balance.BarloSpeedValues[lv],
                BarloSkill.Tasima => Balance.BarloCarryValues[lv],
                BarloSkill.Servis => Balance.BarloServeValues[lv],
                _ => Balance.BarloCoinValues[lv],
            };
        }

        /// <summary>Yetenek seviyesi <paramref name="skillLv"/> (1–5) hangi taverna seviyesinde açılır.</summary>
        public static int UnlockLevel(BarloSkill s, int skillLv) => UnlockTable(s)[Mathf.Clamp(skillLv, 1, MaxLevel) - 1];

        /// <summary>Yetenek seviyesi <paramref name="skillLv"/> (1–5) bedeli.</summary>
        public static long Price(BarloSkill s, int skillLv) => PriceTable(s)[Mathf.Clamp(skillLv, 1, MaxLevel) - 1];

        // ---- etkiler (şu anki seviye)
        /// <summary>Barlo'nun koşu hızı (m/sn).</summary>
        public static float Speed => Value(BarloSkill.Hiz, Level(BarloSkill.Hiz));
        /// <summary>Barlo'nun tepsisindeki bardak sayısı.</summary>
        public static int Carry => Mathf.RoundToInt(Value(BarloSkill.Tasima, Level(BarloSkill.Tasima)));
        /// <summary>Servis menzili: müşteriye, makineye, masaya, misafire etkileşim mesafesi (m).</summary>
        public static float Reach => Value(BarloSkill.Servis, Level(BarloSkill.Servis));
        /// <summary>Para yığınlarını çekme mesafesi (m).</summary>
        public static float CoinReach => Value(BarloSkill.Para, Level(BarloSkill.Para));

        /// <summary>Sıradaki seviye alınabilir mi?</summary>
        public static Management.BuyResult CanBuy(BarloSkill s)
        {
            int lv = Level(s);
            if (lv >= MaxLevel) return Management.BuyResult.Owned;
            if (GameState.Level < UnlockLevel(s, lv + 1)) return Management.BuyResult.LevelLow;
            return GameState.Coins >= Price(s, lv + 1) ? Management.BuyResult.Ok : Management.BuyResult.NoMoney;
        }

        /// <summary>Sıradaki seviyeyi altınla alır.</summary>
        public static Management.BuyResult Buy(BarloSkill s)
        {
            var r = CanBuy(s);
            if (r != Management.BuyResult.Ok) return r;
            int next = Level(s) + 1;
            if (!GameState.TrySpend(Price(s, next))) return Management.BuyResult.NoMoney;
            Set(GameState.Data, s, next);
            Services.Track("barlo_skill", ("yetenek", Id(s)), ("seviye", next), ("taverna_seviyesi", GameState.Level));
            Quests.OnUpgrade();
            GameState.Save();
            GameState.NotifyChanged();
            return Management.BuyResult.Ok;
        }

        /// <summary>Kimlik (kayıt anahtarı, ölçüm): hiz, tasima, servis, para.</summary>
        public static string Id(BarloSkill s) => s switch
        {
            BarloSkill.Hiz => "hiz",
            BarloSkill.Tasima => "tasima",
            BarloSkill.Servis => "servis",
            _ => "para",
        };

        /// <summary>"YENİ!" rozeti anahtarı: yetenek ve seviye başına bir tane.</summary>
        public static string Key(BarloSkill s, int skillLv) => $"barlo_{Id(s)}_{skillLv}";

        /// <summary>Dükkân kartındaki ad.</summary>
        public static string Name(BarloSkill s) => s switch
        {
            BarloSkill.Hiz => Loc.T("Koşu hızı"),
            BarloSkill.Tasima => Loc.T("Taşıma kapasitesi"),
            BarloSkill.Servis => Loc.T("Servis menzili"),
            _ => Loc.T("Para toplama menzili"),
        };

        /// <summary>Somut değer metni (en üst seviyede): "Hız 4.9 m/sn", "Tepsi 7 bardak", "Menzil 2.10 m", "Para çekme 4.5 m".</summary>
        public static string ValueText(BarloSkill s, int lv) => s switch
        {
            BarloSkill.Hiz => Loc.F("Hız {0:0.0} m/sn", Value(s, lv)),
            BarloSkill.Tasima => Loc.F("Tepsi {0} bardak", Mathf.RoundToInt(Value(s, lv))),
            BarloSkill.Servis => Loc.F("Menzil {0:0.00} m", Value(s, lv)),
            _ => Loc.F("Para çekme {0:0.00} m", Value(s, lv)),
        };

        /// <summary>Şu anki → sonraki değer: "Hız 3.4 → 3.7 m/sn" (lv = şu anki seviye, lv &lt; MaxLevel).</summary>
        public static string ChangeText(BarloSkill s, int lv) => s switch
        {
            BarloSkill.Hiz => Loc.F("Hız {0:0.0} → {1:0.0} m/sn", Value(s, lv), Value(s, lv + 1)),
            BarloSkill.Tasima => Loc.F("Tepsi {0} → {1} bardak", Mathf.RoundToInt(Value(s, lv)), Mathf.RoundToInt(Value(s, lv + 1))),
            BarloSkill.Servis => Loc.F("Menzil {0:0.00} → {1:0.00} m", Value(s, lv), Value(s, lv + 1)),
            _ => Loc.F("Para çekme {0:0.00} → {1:0.00} m", Value(s, lv), Value(s, lv + 1)),
        };

        /// <summary>Kartın ikinci satırındaki "ne işe yarar" açıklaması.</summary>
        public static string Desc(BarloSkill s) => s switch
        {
            BarloSkill.Hiz => Loc.T("Barlo daha hızlı koşar"),
            BarloSkill.Tasima => Loc.T("Tepside daha çok bardak"),
            BarloSkill.Servis => Loc.T("Müşteriye ve makineye daha uzaktan servis"),
            _ => Loc.T("Paralar uzaktan Barlo'ya uçar"),
        };

        /// <summary>Kit ikonu adı (Resources/Kit/v3i_*).</summary>
        public static string Icon(BarloSkill s) => s switch
        {
            BarloSkill.Hiz => "speed",
            BarloSkill.Tasima => "mug",
            BarloSkill.Servis => "person",
            _ => "coin",
        };
    }
}
