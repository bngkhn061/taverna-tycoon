using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Taverna.Tests
{
    /// <summary>
    /// v1.9 Paket 1 (production/qa/playtests/feedback-2026-11-17.md): yükseltme satırları her seviyede değişir (madde 1),
    /// borç (7), görevli bölgesi (8), elmaslı otomasyon (9, 16), elmas ekonomisi (13, 23), görevin otomatik tamamlanması (24),
    /// rehber kilidi (11).
    /// </summary>
    public class V19Tests
    {
        [TearDown]
        public void Restore() => GameState.DebugReplace(null);

        static List<DrinkDef> AllDrinks()
        {
            var list = new List<DrinkDef>();
            foreach (var guid in AssetDatabase.FindAssets("t:DrinkDef", new[] { "Assets/_Game/Data/Drinks" }))
            {
                var d = AssetDatabase.LoadAssetAtPath<DrinkDef>(AssetDatabase.GUIDToAssetPath(guid));
                if (d) list.Add(d);
            }
            return list;
        }

        /// <summary>Madde 1: yükseltme panelinde hiçbir satır "önce = sonra" göstermez (Hız tek ondalık, Stok, Kalite).</summary>
        [Test]
        public void UpgradeRows_EveryLevelChangesShownValue()
        {
            var drinks = AllDrinks();
            Assert.Greater(drinks.Count, 10, "içecek verisi bulunamadı");
            foreach (var d in drinks)
                for (int lv = 0; lv < Balance.MaxUpgradeLevel; lv++)
                {
                    string a = Balance.ProduceTime(d, lv).ToString("0.0"), b = Balance.ProduceTime(d, lv + 1).ToString("0.0");
                    Assert.AreNotEqual(a, b, $"{d.id} hız Sv{lv}: {a} → {b}");
                    Assert.Greater(Balance.MaxStock(lv + 1), Balance.MaxStock(lv), $"{d.id} stok Sv{lv}");
                    Assert.Greater(Balance.SellPrice(d, lv + 1), Balance.SellPrice(d, lv), $"{d.id} kalite Sv{lv}");
                }
        }

        [Test]
        public void SellPrice_CheapDrinkStillRises_ExpensiveKeepsCurve()
        {
            Assert.AreEqual(3, Balance.SellPrice(3f, 0));
            Assert.AreEqual(4, Balance.SellPrice(3f, 1));   // v1.8: 3 → 3
            Assert.AreEqual(Mathf.RoundToInt(60 * Mathf.Pow(Balance.QualityStep, 5)), Balance.SellPrice(60f, 5));
            Assert.AreEqual(Balance.BaseStock + Balance.MaxUpgradeLevel, Balance.MaxStock(Balance.MaxUpgradeLevel));
        }

        /// <summary>Madde 7: maaş için altın yetmezse bakiye eksiye iner; borçluyken harcama yok.</summary>
        [Test]
        public void ForceSpend_GoesIntoDebt_BlocksSpending()
        {
            GameState.DebugReplace(new SaveData { coins = 100 });
            long unpaid = GameState.ForceSpend(250);
            Assert.AreEqual(150, unpaid);
            Assert.AreEqual(-150, GameState.Coins);
            Assert.IsTrue(GameState.InDebt);
            Assert.IsFalse(GameState.TrySpend(1));
            Assert.IsFalse(GameState.TrySpend(0), "borçluyken bedava ürün de alınmaz (bakiye < 0)");
            GameState.AddCoins(200, Vector3.zero);
            Assert.AreEqual(50, GameState.Coins);
            Assert.IsFalse(GameState.InDebt);
            GameState.DebugReplace(new SaveData { coins = -20 });
            Assert.AreEqual(30, GameState.ForceSpend(30), "zaten borçluyken tamamı borca");
            Assert.AreEqual(-50, GameState.Coins);
        }

        [Test]
        public void FormatNum_WritesNegatives()
        {
            Assert.AreEqual("-150", HudController.FormatNum(-150));
            Assert.AreEqual("-" + HudController.FormatNum(25000), HudController.FormatNum(-25000));
            Assert.AreEqual("0", HudController.FormatNum(0));
        }

        /// <summary>Madde 8: Mina ve Tobi salonda çalışır; salon açılmadan işe alınamaz.</summary>
        [Test]
        public void SalonWaiters_NeedSalon()
        {
            foreach (var id in new[] { "staff_mina", "staff_tobi" })
            {
                var s = System.Array.Find(Catalog.StaffList, x => x.id == id);
                Assert.AreEqual("salon", s.zone, id);
                GameState.DebugReplace(new SaveData { level = 20, coins = 100000 });
                Assert.AreEqual(Management.BuyResult.ZoneLocked, Management.CanHire(s), id);
                GameState.DebugReplace(new SaveData { level = 20, coins = 100000, unlocked = new List<string> { "salon" } });
                Assert.AreEqual(Management.BuyResult.Ok, Management.CanHire(s), id);
            }
        }

        /// <summary>Madde 24: durum görevi koşul zaten tutuyorsa tamamlanmış gelir; sonradan tutarsa RefreshState tamamlar.</summary>
        [Test]
        public void StateQuests_AutoComplete_CountersStartAtZero()
        {
            GameState.DebugReplace(new SaveData { level = 5, unlocked = new List<string> { "staff_nana", "salon" } });
            Assert.IsTrue(Quests.MakeState(QuestType.HireStaff, "staff_nana").Done);
            Assert.IsTrue(Quests.MakeState(QuestType.UnlockZone, "salon").Done);
            Assert.IsTrue(Quests.MakeState(QuestType.OwnMachine, "", DrinkId.Bira).Done);
            var pip = Quests.MakeState(QuestType.HireStaff, "staff_pip");
            Assert.IsFalse(pip.Done);
            var serve = Quests.Make(QuestType.ServeDrink, DrinkId.Bira);
            Assert.AreEqual(0, serve.progress);
            Assert.IsFalse(serve.Done);
            Quests.List.Add(pip);
            Quests.List.Add(serve);
            GameState.Data.unlocked.Add("staff_pip");
            Quests.RefreshState();
            Assert.IsTrue(pip.Done, "işe alınca kendiliğinden tamamlanmalı");
            Assert.IsFalse(serve.Done, "sayaç görevi duruma bakmaz");
        }

        /// <summary>Madde 9, 16: her otomasyon elmasla daha erken (Kasa en az 4 seviye), elmas fiyatı erken almayı lüks yapar.</summary>
        [Test]
        public void Automation_GemPriceEarlier()
        {
            foreach (var d in Automation.List)
            {
                Assert.Less(d.gemLevel, d.level, d.id);
                Assert.Greater(d.gems, 0, d.id);
            }
            var kasa = System.Array.Find(Automation.List, a => a.id == "kasa");
            Assert.That(kasa.level - kasa.gemLevel, Is.InRange(4, 6));
            Assert.GreaterOrEqual(kasa.gems, 100, "Kasa elmasla lüks olmalı (ilk saatte ≈ 25 elmas)");
            GameState.DebugReplace(new SaveData { level = kasa.gemLevel, gems = kasa.gems });
            Assert.AreEqual(Management.BuyResult.Ok, Automation.CanBuyWithGems(kasa));
            Assert.AreEqual(Management.BuyResult.LevelLow, Automation.CanBuy(kasa));
            GameState.DebugReplace(new SaveData { level = kasa.gemLevel, gems = kasa.gems - 1 });
            Assert.AreEqual(Management.BuyResult.NoMoney, Automation.CanBuyWithGems(kasa));
            GameState.DebugReplace(new SaveData { level = kasa.gemLevel - 1, gems = 9999 });
            Assert.AreEqual(Management.BuyResult.LevelLow, Automation.CanBuyWithGems(kasa));
        }

        /// <summary>
        /// Madde 13, 23: hızlandırmalar en az ×2.5 pahalı (v1.8: 10 / 5 / 8 / 15); ilk saatte (≈ Sv11) seviye + görev elması
        /// ×2 geliri en fazla bir kez aldırır.
        /// </summary>
        [Test]
        public void GemEconomy_BoostsExpensive_FirstHourLimited()
        {
            var old = new Dictionary<string, int> { ["x2"] = 10, ["refill"] = 5, ["rush"] = 8, ["coins"] = 15 };
            foreach (var o in Boosts.Offers) Assert.GreaterOrEqual(o.gems, old[o.id] * 2.5f, o.id);
            int levelGems = 0;
            for (int lv = 2; lv <= FirstHourLevel; lv++) levelGems += Boosts.LevelGemReward(lv);
            int questGems = 0;
            for (int i = 0; i < FirstHourQuests; i++) questGems += Quests.QuestGems(1 + i * FirstHourLevel / FirstHourQuests);
            int x2 = System.Array.Find(Boosts.Offers, o => o.id == "x2").gems;
            int total = levelGems + questGems + Boosts.DailyReward(1).gems;
            Assert.Less(total, 2 * x2, $"ilk saat elması {total} (seviye {levelGems} + görev {questGems})");
        }

        /// <summary>İlk saatin sonunda beklenen seviye ve tamamlanan görev sayısı (GAME_DESIGN.md §3.1 hedef süreleri, ≈ 5 dk'da bir görev).</summary>
        const int FirstHourLevel = 11, FirstHourQuests = 12;

        /// <summary>Madde 11: rehber kilidi yalnızca HUD düğmeleri ve dükkân sekmeleri; rehber yokken her şey açık.</summary>
        [Test]
        public void TutorialGate_OnlyHudButtonsAndTabs()
        {
            Assert.IsTrue(Tutorial.IsGatedUi("tab-dec"));
            Assert.IsTrue(Tutorial.IsGatedUi("open-shop"));
            Assert.IsTrue(Tutorial.IsGatedUi("stars"));
            Assert.IsFalse(Tutorial.IsGatedUi("shop-close"));
            Assert.IsFalse(Tutorial.IsGatedUi("open-settings"));
            if (!Tutorial.Active) Assert.IsTrue(Tutorial.AllowsUi("tab-dec"));
        }
    }
}
