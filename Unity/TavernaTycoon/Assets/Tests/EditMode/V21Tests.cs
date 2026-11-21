using NUnit.Framework;

namespace Taverna.Tests
{
    /// <summary>
    /// v2.1 Paket 3 (oyuncu testi 2026-11-17 maddeler 10, 25): Barlo'nun yetenek tabloları tekdüze artar ve en üst seviyede durur;
    /// prestij sıfırlar; son ("Krala Davet") yalnızca şube 2 + Sv30 + Saray Hanı ve henüz görülmemişken açılır.
    /// </summary>
    public class V21Tests
    {
        SaveData _saved;

        [SetUp] public void Keep() => _saved = GameState.Data;
        [TearDown] public void Restore() => GameState.DebugReplace(_saved);

        [Test]
        public void BarloTables_HaveFiveLevels_AndStartAtBaseValues()
        {
            foreach (var s in BarloSkills.All)
            {
                Assert.AreEqual(BarloSkills.MaxLevel, BarloSkills.UnlockTable(s).Length, $"{s} açılış tablosu");
                Assert.AreEqual(BarloSkills.MaxLevel, BarloSkills.PriceTable(s).Length, $"{s} fiyat tablosu");
            }
            Assert.AreEqual(BarloSkills.MaxLevel + 1, Balance.BarloSpeedValues.Length);
            Assert.AreEqual(BarloSkills.MaxLevel + 1, Balance.BarloCarryValues.Length);
            Assert.AreEqual(BarloSkills.MaxLevel + 1, Balance.BarloServeValues.Length);
            Assert.AreEqual(BarloSkills.MaxLevel + 1, Balance.BarloCoinValues.Length);
            Assert.AreEqual(Balance.PlayerSpeed, BarloSkills.Value(BarloSkill.Hiz, 0), 1e-5f);
            Assert.AreEqual(Balance.StartCarryCapacity, BarloSkills.Value(BarloSkill.Tasima, 0), 1e-5f);
            Assert.AreEqual(Balance.InteractRadius, BarloSkills.Value(BarloSkill.Servis, 0), 1e-5f);
            Assert.AreEqual(Balance.InteractRadius, BarloSkills.Value(BarloSkill.Para, 0), 1e-5f);
        }

        [Test]
        public void BarloTables_RiseMonotonically()
        {
            foreach (var s in BarloSkills.All)
            {
                for (int n = 2; n <= BarloSkills.MaxLevel; n++)
                {
                    Assert.Greater(BarloSkills.UnlockLevel(s, n), BarloSkills.UnlockLevel(s, n - 1), $"{s} Sv{n} açılış seviyesi");
                    Assert.Greater(BarloSkills.Price(s, n), BarloSkills.Price(s, n - 1), $"{s} Sv{n} fiyatı");
                }
                for (int lv = 1; lv <= BarloSkills.MaxLevel; lv++)
                    Assert.Greater(BarloSkills.Value(s, lv), BarloSkills.Value(s, lv - 1), $"{s} Sv{lv} etkisi");
            }
        }

        [Test]
        public void BarloTables_FitTheEconomy()
        {
            foreach (var s in BarloSkills.All)
            {
                int first = BarloSkills.UnlockLevel(s, 1), last = BarloSkills.UnlockLevel(s, BarloSkills.MaxLevel);
                Assert.That(first, Is.InRange(2, 4), $"{s}: ilk seviye Sv2–4'te alınabilmeli");
                Assert.That(last, Is.InRange(20, Balance.MaxLevel), $"{s}: son seviye geç oyunda");
                // ilk seviye o seviyedeki 3 dk gelirin yarısından ucuz, son seviye en fazla 3 katı (zorunlu değil)
                Assert.Less(BarloSkills.Price(s, 1), Balance.EventIncomeRef(first) * 0.5f, $"{s}: ilk seviye ucuz");
                Assert.Less(BarloSkills.Price(s, BarloSkills.MaxLevel), Balance.EventIncomeRef(last) * 3f, $"{s}: son seviye ulaşılabilir");
            }
        }

        [Test]
        public void BarloLevel_CapsAtMax_AndCannotBuyMore()
        {
            GameState.DebugReplace(new SaveData { level = Balance.MaxLevel, coins = 10_000_000, barloSpeed = 99, barloCarry = BarloSkills.MaxLevel, barloServe = -3 });
            Assert.AreEqual(BarloSkills.MaxLevel, BarloSkills.Level(BarloSkill.Hiz));
            Assert.AreEqual(Management.BuyResult.Owned, BarloSkills.CanBuy(BarloSkill.Hiz));
            Assert.AreEqual(Management.BuyResult.Owned, BarloSkills.CanBuy(BarloSkill.Tasima));
            Assert.AreEqual(0, BarloSkills.Level(BarloSkill.Servis));
            Assert.AreEqual(BarloSkills.Value(BarloSkill.Hiz, BarloSkills.MaxLevel), BarloSkills.Speed, 1e-5f);
            Assert.AreEqual(Balance.BarloCarryValues[BarloSkills.MaxLevel], BarloSkills.Carry);
            Assert.AreEqual(BarloSkills.Value(BarloSkill.Hiz, BarloSkills.MaxLevel), BarloSkills.Value(BarloSkill.Hiz, 42), 1e-5f);
        }

        [Test]
        public void BarloCanBuy_RespectsLevelGateAndGold()
        {
            GameState.DebugReplace(new SaveData { level = 1, coins = 1_000_000 });
            Assert.AreEqual(Management.BuyResult.LevelLow, BarloSkills.CanBuy(BarloSkill.Hiz));
            GameState.DebugReplace(new SaveData { level = BarloSkills.UnlockLevel(BarloSkill.Hiz, 1), coins = 0 });
            Assert.AreEqual(Management.BuyResult.NoMoney, BarloSkills.CanBuy(BarloSkill.Hiz));
            GameState.Data.coins = BarloSkills.Price(BarloSkill.Hiz, 1);
            Assert.AreEqual(Management.BuyResult.Ok, BarloSkills.CanBuy(BarloSkill.Hiz));
            GameState.Data.coins = -50;   // borç
            Assert.AreEqual(Management.BuyResult.NoMoney, BarloSkills.CanBuy(BarloSkill.Hiz));
        }

        [Test]
        public void Prestige_ResetsBarloSkills()
        {
            var d = new SaveData { barloSpeed = 3, barloCarry = 5, barloServe = 1, barloCoin = 2 };
            BarloSkills.ResetAll(d);
            Assert.AreEqual(0, d.barloSpeed + d.barloCarry + d.barloServe + d.barloCoin);
        }

        [Test]
        public void Ending_OpensOnlyOnThirdTavernAtTopTier_Once()
        {
            int top = Tiers.List.Length - 1;
            GameState.DebugReplace(new SaveData { level = Balance.MaxLevel, tier = top, branch = Ending.RequiredBranch });
            Assert.IsTrue(Ending.Available, "şube 2 + Sv30 + Saray Hanı");
            GameState.DebugReplace(new SaveData { level = Balance.MaxLevel, tier = top, branch = 1 });
            Assert.IsFalse(Ending.Available, "2. taverna yetmez");
            GameState.DebugReplace(new SaveData { level = Balance.MaxLevel - 1, tier = top, branch = Ending.RequiredBranch });
            Assert.IsFalse(Ending.Available, "Sv29 yetmez");
            GameState.DebugReplace(new SaveData { level = Balance.MaxLevel, tier = top - 1, branch = Ending.RequiredBranch });
            Assert.IsFalse(Ending.Available, "Saray Hanı gerekli");
            GameState.DebugReplace(new SaveData { level = Balance.MaxLevel, tier = top, branch = Ending.RequiredBranch, endingSeen = true });
            Assert.IsFalse(Ending.Available, "bir kez");
            Assert.IsTrue(Ending.Seen);
            Assert.AreEqual("Dağ Geçidi", Prestige.BranchName(Ending.RequiredBranch));
        }
    }
}
