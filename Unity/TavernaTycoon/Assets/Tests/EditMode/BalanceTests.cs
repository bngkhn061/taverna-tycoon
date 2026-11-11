using NUnit.Framework;

namespace Taverna.Tests
{
    /// <summary>Balance formülleri (GAME_DESIGN.md §3.1, §7, §8): Ün eğrisi, maaş, işe alım, yıldız eşikleri, oturak ölçeği, çarpan sınırı.</summary>
    public class BalanceTests
    {
        const float Eps = 0.0001f;

        [Test]
        public void UnForLevel_FirstLevels_MatchCurve()
        {
            Assert.AreEqual(20, Balance.UnForLevel(1));
            Assert.AreEqual(26, Balance.UnForLevel(2));   // 20 × 1.32 = 26.4
            Assert.AreEqual(35, Balance.UnForLevel(3));   // 20 × 1.32² = 34.85
        }

        [Test]
        public void UnForLevel_IncreasesEveryLevel()
        {
            for (int lv = 2; lv <= Balance.MaxLevel; lv++)
                Assert.Greater(Balance.UnForLevel(lv), Balance.UnForLevel(lv - 1), $"Sv{lv}");
        }

        [Test]
        public void UnForLevel_GrowthRateChangesAtBreaks()
        {
            float early = Balance.UnForLevel(Balance.UnCurveBreak) / (float)Balance.UnForLevel(Balance.UnCurveBreak - 1);
            float mid = Balance.UnForLevel(Balance.UnCurveBreak + 1) / (float)Balance.UnForLevel(Balance.UnCurveBreak);
            float late = Balance.UnForLevel(Balance.UnCurveBreak2 + 1) / (float)Balance.UnForLevel(Balance.UnCurveBreak2);
            Assert.AreEqual(1.32f, early, 0.01f);
            Assert.AreEqual(1.15f, mid, 0.01f);
            Assert.AreEqual(1.08f, late, 0.01f);
        }

        [Test]
        public void Wage_GrowsWithTavernAndStaffLevel()
        {
            Assert.AreEqual(10, Balance.Wage(10, 1, 1, 0));
            Assert.AreEqual(115, Balance.Wage(100, 1, 2, 0));   // ×1.15 / seviye
            Assert.AreEqual(115, Balance.Wage(100, 1, 1, 1));   // +%15 / görevli seviyesi
            Assert.AreEqual(100, Balance.Wage(100, 5, 3, 0));   // işe alım seviyesinin altında artmaz
        }

        [Test]
        public void HireCost_IsMaxOfCatalogAndTwiceWage()
        {
            Assert.AreEqual(500, Balance.HireCost(500, 100));
            Assert.AreEqual(600, Balance.HireCost(500, 300));
            Assert.AreEqual(Balance.HireWageMultiplier * 250L, Balance.HireCost(0, 250));
        }

        [Test]
        public void StarsForAmbiance_Thresholds()
        {
            var t = Balance.StarThresholds;
            Assert.AreEqual(1f, Balance.StarsForAmbiance(0f), Eps);
            for (int i = 1; i < t.Length; i++)
                Assert.AreEqual(i + 1f, Balance.StarsForAmbiance(t[i]), Eps, $"eşik {t[i]}");
            Assert.AreEqual(5f, Balance.StarsForAmbiance(10000f), Eps);
        }

        [Test]
        public void StarsForAmbiance_RoundsDownToHalfStar()
        {
            float half = (Balance.StarThresholds[1] + Balance.StarThresholds[2]) / 2f;
            Assert.AreEqual(2.5f, Balance.StarsForAmbiance(half), Eps);
            Assert.AreEqual(1.5f, Balance.StarsForAmbiance(Balance.StarThresholds[1] - 0.1f), Eps);
        }

        [Test]
        public void AmbianceForStars_IsInverseOnHalfStars()
        {
            for (float s = 1f; s <= 5f; s += 0.5f)
                Assert.AreEqual(s, Balance.StarsForAmbiance(Balance.AmbianceForStars(s)), Eps, $"{s}★");
        }

        [Test]
        public void SeatSpawnScale_ClampedSquareRoot()
        {
            Assert.AreEqual(1f, Balance.SeatSpawnScale(6), Eps);
            Assert.AreEqual(1f, Balance.SeatSpawnScale(12), Eps);
            Assert.AreEqual(1.5f, Balance.SeatSpawnScale(27), Eps);
            Assert.AreEqual(2f, Balance.SeatSpawnScale(48), Eps);
            Assert.AreEqual(2.2f, Balance.SeatSpawnScale(200), Eps);
        }

        [Test]
        public void CapSpawnBoost_LimitsProductToCap()
        {
            Assert.AreEqual(2f, Balance.CapSpawnBoost(2f), Eps);
            Assert.AreEqual(Balance.SpawnBoostCap, Balance.CapSpawnBoost(5f), Eps);
            Assert.AreEqual(Balance.SpawnBoostCap, Balance.CapSpawnBoost(Balance.SpawnBoostCap), Eps);
        }
    }
}
