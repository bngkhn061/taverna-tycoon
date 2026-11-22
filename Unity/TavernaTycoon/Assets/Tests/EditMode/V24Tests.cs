using NUnit.Framework;

namespace Taverna.Tests
{
    /// <summary>
    /// v2.4 Paket 6 (production/qa/playtests/feedback-2026-11-22.md, GAME_DESIGN.md §18): günlük ödülün altın / elmas değerleri,
    /// giriş sinemasının yalnızca ilk yeni oyunda oynaması, kralın hediyesinin (Kraliyet Fermanı, +%5 gelir) prestijte kalması.
    /// </summary>
    public class V24Tests
    {
        // ------------------------------------------------------------------ günlük ödül (madde 1)
        [Test]
        public void DailyReward_GemsOnlyOnDaySeven_Small()
        {
            for (int s = 1; s <= 6; s++) Assert.AreEqual(0, Boosts.DailyReward(s, 10).gems, $"{s}. gün elmas vermemeli");
            Assert.AreEqual(5, Boosts.DailyReward(7, 10).gems, "7. gün 5 elmas");
            Assert.AreEqual(5, Boosts.DailyWeekGems);
            int week = 0;
            for (int s = 1; s <= 7; s++) week += Boosts.DailyReward(s, 1).gems;
            Assert.AreEqual(5, week, "haftalık elmas (v1.9–v2.3: 23)");
            int cheapest = int.MaxValue;
            foreach (var o in Boosts.Offers) cheapest = System.Math.Min(cheapest, o.gems);
            Assert.Less(week, cheapest, "bir haftalık günlük ödül en ucuz elmas ürününe bile yetmez");
        }

        [Test]
        public void DailyReward_CoinsScaleWithDayAndLevel()
        {
            Assert.AreEqual(50L, Boosts.DailyReward(1, 1).coins);
            Assert.AreEqual(50L * 3 * 10, Boosts.DailyReward(3, 10).coins);
            Assert.AreEqual(50L * 6 * 20, Boosts.DailyReward(6, 20).coins);
            Assert.AreEqual((long)(50L * 7 * 10 * 1.5f), Boosts.DailyReward(7, 10).coins, "7. gün altın ×1.5");
            Assert.AreEqual(Boosts.DailyReward(7, 10), Boosts.DailyReward(12, 10), "seri 7'de kalır");
            Assert.AreEqual(Boosts.DailyReward(1, 0).coins, Boosts.DailyReward(1, 1).coins, "seviye en az 1 sayılır");
            for (int s = 1; s < 7; s++) Assert.Less(Boosts.DailyReward(s, 5).coins, Boosts.DailyReward(s + 1, 5).coins);
        }

        // ------------------------------------------------------------------ giriş sineması (madde 3)
        [Test]
        public void Opening_PlaysOnlyOnFirstNewGame()
        {
            var fresh = new SaveData();
            Assert.IsTrue(Opening.ShouldAutoPlay(fresh, true, false), "ilk yeni oyun");
            Assert.IsFalse(Opening.ShouldAutoPlay(fresh, false, false), "menüden yeni oyun değil (devam eden oyun)");
            Assert.IsFalse(Opening.ShouldAutoPlay(fresh, true, true), "cihazda daha önce oynadı (kayıt sıfırlandı)");
            Assert.IsFalse(Opening.ShouldAutoPlay(new SaveData { openingSeen = true }, true, false), "kayıtta görüldü");
            Assert.IsFalse(Opening.ShouldAutoPlay(new SaveData { branch = 1 }, true, false), "prestij / şube açılışı");
            Assert.IsFalse(Opening.ShouldAutoPlay(new SaveData { customersServed = 3 }, true, false), "oynanmış kayıt");
            Assert.IsFalse(Opening.ShouldAutoPlay(null, true, false));
        }

        [Test]
        public void Opening_SeenFlagSurvivesPrestige()
        {
            var d = new SaveData { level = 30, coins = 500000, openingSeen = true };
            Prestige.ResetForBranch(d, 1);
            Assert.AreEqual(1, d.branch);
            Assert.AreEqual(1, d.level);
            Assert.IsTrue(d.openingSeen);
            Assert.IsFalse(Opening.ShouldAutoPlay(d, true, false), "yeni şubede giriş oynamaz");
        }

        [Test]
        public void Opening_TotalLengthIn20To35Seconds()
        {
            Assert.That(OpeningScene.TotalSeconds, Is.InRange(20f, 35f));
            Assert.AreEqual(5, OpeningScene.Durations.Length, "yolculuk, harabe, saray, ferman, tabela");
        }

        // ------------------------------------------------------------------ kralın hediyesi (madde 3)
        [Test]
        public void RoyalGift_BonusFivePercent_SurvivesPrestige()
        {
            var d = new SaveData { level = 30, coins = 500000, branch = 2, tier = 2 };
            Assert.AreEqual(1f, RoyalGift.IncomeMultiplierFor(d), 1e-6f);
            Assert.IsTrue(RoyalGift.Grant(d));
            Assert.IsFalse(RoyalGift.Grant(d), "ikinci kez verilmez");
            Assert.AreEqual(1.05f, RoyalGift.IncomeMultiplierFor(d), 1e-6f);
            Prestige.ResetForBranch(d, 3);
            Assert.AreEqual(3, d.branch);
            Assert.AreEqual(0, d.tier, "kademe sıfırlanır");
            Assert.IsTrue(d.royalDecree, "ferman yeni şubede de duvarda");
            Assert.AreEqual(1.05f, RoyalGift.IncomeMultiplierFor(d), 1e-6f, "+%5 gelir prestijte kalır");
        }

        [Test]
        public void RoyalGift_OldSaveThatSawEnding_GetsGift()
        {
            var json = UnityEngine.JsonUtility.ToJson(new SaveData { endingSeen = true, level = 30, saveVersion = 4 });
            var d = GameState.FromJson(json);
            Assert.IsTrue(d.royalDecree, "v2.4 öncesi sonu görmüş kayıt hediyeyi alır");
            var d2 = GameState.FromJson(UnityEngine.JsonUtility.ToJson(new SaveData { level = 30 }));
            Assert.IsFalse(d2.royalDecree);
        }

        // ------------------------------------------------------------------ ses (madde 2)
        [Test]
        public void Audio_MixConstantsSafe()
        {
            Assert.LessOrEqual(Sfx.RoomNight + Sfx.MurmurMax, Sfx.AmbientCap + 0.1f);
            Assert.LessOrEqual(1f + Sfx.CoinStreakStep * Sfx.CoinStreakMax, 1.13f, "para serisi en çok +%12 tizleşir");
        }
    }
}
