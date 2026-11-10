using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Elmasla alınan hızlandırmalar (GAME_DESIGN.md §8.2 — Mağaza) ve günlük ödül.
    /// Elmas ilerlemek için gerekmez; sadece rahatlık ve hız sağlar.
    /// </summary>
    public static class Boosts
    {
        static float _incomeUntil;
        public static float IncomeMultiplier => (Time.unscaledTime < _incomeUntil ? 2f : 1f) * Automation.IncomeMultiplier * Premium.IncomeMultiplier;
        public static float IncomeRemaining => Mathf.Max(0f, _incomeUntil - Time.unscaledTime);

        public struct Offer
        {
            public string id, name, icon;
            public int gems;
            public bool hot;
        }

        public static readonly Offer[] Offers =
        {
            new() { id = "x2", name = "10 dk ×2 Gelir", icon = "ParaYigini", gems = 10, hot = true },
            new() { id = "refill", name = "Anında Dolum", icon = "Bira", gems = 5 },
            new() { id = "rush", name = "Yoğun Saati Başlat", icon = "EjderAtesi", gems = 8 },
            new() { id = "coins", name = "Altın Kesesi", icon = "Para", gems = 15 },
        };

        /// <summary>v1.1 (oyuncu testi "15 elmasa çok altın veriyor"): erken seviyede küçük, ilerledikçe hızlanır (Sv1 160, Sv5 770, Sv10 2000).</summary>
        public static long CoinPackAmount => 100L + (long)(60f * Mathf.Pow(GameState.Level, 1.5f));

        public static bool Buy(string id)
        {
            var o = Array.Find(Offers, x => x.id == id);
            if (GameState.Data.gems < o.gems) return false;
            GameState.Data.gems -= o.gems;
            var at = PlayerController.I ? PlayerController.I.transform.position : Vector3.zero;
            switch (id)
            {
                case "x2": _incomeUntil = Mathf.Max(_incomeUntil, Time.unscaledTime) + 600f; break;
                case "refill": foreach (var s in DrinkStation.All) s.FillToMax(); break;
                case "rush": RushHour.ForceStart(); break;
                case "coins": GameState.AddCoins(CoinPackAmount, at); break;
            }
            GameState.Save();
            GameState.NotifyChanged();
            return true;
        }

        // ------------------------------------------------------------------ günlük ödül
        static string Today => DateTime.Now.ToString("yyyy-MM-dd");
        public static bool DailyAvailable => GameState.Data.lastDaily != Today;

        /// <summary>Seri: dün de alındıysa +1, yoksa 1'e döner. 7. günde en büyük ödül.</summary>
        public static int NextStreak
        {
            get
            {
                string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                return GameState.Data.lastDaily == yesterday ? Mathf.Min(GameState.Data.dailyStreak + 1, 7) : 1;
            }
        }

        public static (long coins, int gems) DailyReward(int streak) =>
            (40L * streak * Mathf.Max(1, GameState.Level), streak >= 7 ? 10 : 1 + streak / 2);

        public static (long coins, int gems) ClaimDaily()
        {
            if (!DailyAvailable) return (0, 0);
            int s = NextStreak;
            var r = DailyReward(s);
            GameState.Data.dailyStreak = s;
            GameState.Data.lastDaily = Today;
            GameState.Data.coins += r.coins;
            GameState.Data.gems += r.gems;
            GameState.Save();
            GameState.NotifyChanged();
            return r;
        }

        /// <summary>Seviye ödülü: her seviyede elmas (seviye atladın kartında gösterilir).</summary>
        public static int LevelGemReward(int level) => 2 + level / 5;
    }
}
