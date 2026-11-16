using NUnit.Framework;
using UnityEngine;

namespace Taverna.Tests
{
    /// <summary>Balance formülleri (GAME_DESIGN.md §3.1, §7, §8): Ün eğrisi, maaş, işe alım, yıldız eşikleri, oturak ölçeği, çarpan sınırı.</summary>
    public class BalanceTests
    {
        const float Eps = 0.0001f;

        [Test]
        public void UnForLevel_ReadsTableAndClamps()
        {
            Assert.AreEqual(Balance.UnTable[0], Balance.UnForLevel(1));
            Assert.AreEqual(Balance.UnTable[14], Balance.UnForLevel(15));
            Assert.AreEqual(Balance.UnTable[0], Balance.UnForLevel(0));
            Assert.AreEqual(Balance.UnTable[Balance.UnTable.Length - 1], Balance.UnForLevel(99));
            Assert.AreEqual(Balance.MaxLevel, Balance.UnTable.Length);
        }

        [Test]
        public void UnForLevel_IncreasesEveryLevel()
        {
            for (int lv = 2; lv <= Balance.MaxLevel; lv++)
                Assert.Greater(Balance.UnForLevel(lv), Balance.UnForLevel(lv - 1), $"Sv{lv}");
        }

        [Test]
        public void Wage_GrowsWithTavernAndStaffLevel()
        {
            float k = Balance.WageScale;
            Assert.AreEqual(Mathf.RoundToInt(10 * k), Balance.Wage(10, 1, 1, 0));
            Assert.AreEqual(Mathf.RoundToInt(100 * k * Balance.WageLevelGrowth), Balance.Wage(100, 1, 2, 0));   // ×1.12 / seviye
            Assert.AreEqual(Mathf.RoundToInt(100 * k * 1.15f), Balance.Wage(100, 1, 1, 1));   // +%15 / görevli seviyesi
            Assert.AreEqual(Mathf.RoundToInt(100 * k), Balance.Wage(100, 5, 3, 0));           // işe alım seviyesinin altında artmaz
        }

        /// <summary>v1.8 hedef bandı: günlük maaş / günlük gelir Sv8–30 boyunca %15–35 (iyi oyuncunun görevli seviyesiyle, gelir = 3 dk referans × 420/180).</summary>
        [Test]
        public void WageToIncome_StaysInTargetBand_Sv8To30()
        {
            for (int lv = 8; lv <= Balance.MaxLevel; lv++)
            {
                int staffLv = lv < 12 ? 2 : lv < 18 ? 3 : lv < 26 ? 4 : 5;
                long wages = 0;
                foreach (var s in Catalog.StaffList)
                    if (!s.premium && s.level <= lv) wages += Balance.Wage(s.wage, s.level, lv, staffLv);
                float dayIncome = Balance.EventIncomeRef(lv) * Balance.DayLength / EventMinutesSeconds;
                float ratio = wages / dayIncome;
                Assert.That(ratio, Is.InRange(0.15f, 0.35f), $"Sv{lv}: maaş {wages} / gelir {dayIncome:0} = %{100 * ratio:0.0}");
            }
        }

        const float EventMinutesSeconds = 180f;

        /// <summary>v1.8: etkinlik fiyatları seviyeyle büyür; Kraliyet Ziyareti her seviyede en pahalısı, Tellal en ucuzu.</summary>
        [Test]
        public void EventCost_GrowsWithLevel_RoyalMostExpensive()
        {
            var royal = TavernEvents.Get(TavernEvents.Id.Kraliyet);
            var tellal = TavernEvents.Get(TavernEvents.Id.Tellal);
            foreach (var d in TavernEvents.List)
            {
                Assert.Greater(TavernEvents.CostAt(d, 20), TavernEvents.CostAt(d, 10), d.key);
                if (d.id != TavernEvents.Id.Kraliyet) Assert.Greater(TavernEvents.CostAt(royal, 28), TavernEvents.CostAt(d, 28), d.key);
                if (d.id != TavernEvents.Id.Tellal) Assert.LessOrEqual(TavernEvents.CostAt(tellal, 15), TavernEvents.CostAt(d, 15), d.key);
            }
        }

        /// <summary>v1.8: etkinlik fiyatı, 3 dakikalık referans gelirin payıdır (Kraliyet Sv28 ≈ 5 × 3 dk gelir).</summary>
        [Test]
        public void EventCost_IsShareOfThreeMinuteIncome()
        {
            var royal = TavernEvents.Get(TavernEvents.Id.Kraliyet);
            float expected = royal.share * Balance.EventIncomeRef(28);
            Assert.AreEqual(expected, TavernEvents.CostAt(royal, 28), 10f);
        }

        /// <summary>v1.8: özel sipariş payı erken %10, Sv10+ %7; Sv3 öncesi yok.</summary>
        [Test]
        public void SpecialOrderChance_ByLevel()
        {
            Assert.AreEqual(0f, Balance.SpecialOrderChance(2), Eps);
            Assert.AreEqual(Balance.SpecialChanceEarly, Balance.SpecialOrderChance(5), Eps);
            Assert.AreEqual(Balance.SpecialChanceLate, Balance.SpecialOrderChance(20), Eps);
            Assert.Less(Balance.SpecialChanceLate, Balance.SpecialChanceEarly);
        }

        /// <summary>v1.8: içme süresi seviyeyle uzar ve Sv20'den sonra sabit.</summary>
        [Test]
        public void DrinkDuration_GrowsThenClamps()
        {
            Assert.AreEqual(Balance.DrinkDurationEarly, Balance.DrinkDuration(1), Eps);
            Assert.AreEqual(Balance.DrinkDurationLate, Balance.DrinkDuration(Balance.DrinkDurationLateLevel), Eps);
            Assert.AreEqual(Balance.DrinkDurationLate, Balance.DrinkDuration(Balance.MaxLevel), Eps);
            Assert.Greater(Balance.DrinkDuration(10), Balance.DrinkDuration(5));
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
            Assert.AreEqual(Balance.SeatSpawnMax, Balance.SeatSpawnScale(48), Eps);   // v1.8: üst sınır 1.7
            Assert.AreEqual(Balance.SeatSpawnMax, Balance.SeatSpawnScale(200), Eps);
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
