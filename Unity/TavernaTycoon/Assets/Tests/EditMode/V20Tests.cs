using System.Collections.Generic;
using NUnit.Framework;

namespace Taverna.Tests
{
    /// <summary>
    /// v2.0 Paket 2 (production/qa/playtests/feedback-2026-11-17.md): gün sonu son siparişler (madde 15),
    /// han konfor seviyeleri ve misafir gelişi (19), 8 oda (20), bahçe dekoru (21).
    /// </summary>
    public class V20Tests
    {
        [TearDown]
        public void Restore() => GameState.DebugReplace(null);

        /// <summary>Madde 19: her konfor seviyesi ücreti artırır, her seviyenin bir bedeli ve eşya adı var.</summary>
        [Test]
        public void Comfort_FeesRiseEveryLevel_AndTablesAgree()
        {
            Assert.AreEqual(5, Inn.Fees.Length, "konfor 0–4");
            Assert.AreEqual(Inn.Fees.Length - 1, Inn.ComfortCosts.Length, "her yükseltmenin bir bedeli");
            Assert.AreEqual(Inn.Fees.Length, Inn.ComfortNames.Length, "her seviyenin eşya adı");
            for (int i = 1; i < Inn.Fees.Length; i++) Assert.Greater(Inn.Fees[i], Inn.Fees[i - 1], $"gecelik Sv{i}");
            for (int i = 1; i < Inn.ComfortCosts.Length; i++) Assert.Greater(Inn.ComfortCosts[i], Inn.ComfortCosts[i - 1], $"bedel Sv{i}");
        }

        /// <summary>Madde 19: her konfor seviyesi misafir gelişini artırır (açık odaların toplamı).</summary>
        [Test]
        public void Comfort_RaisesGuestArrival()
        {
            Assert.AreEqual(1f, Inn.ArrivalMultiplierFor(0), 1e-5f);
            for (int s = 1; s <= Inn.RoomCount * (Inn.Fees.Length - 1); s++)
                Assert.Greater(Inn.ArrivalMultiplierFor(s), Inn.ArrivalMultiplierFor(s - 1), $"toplam konfor {s}");

            GameState.DebugReplace(new SaveData { roomLvs = new List<int> { 4, 4, 4, 0, 0, 0, 0, 0 } });
            // Oda 3 ödeme karesiyle açılmadı: onun konforu sayılmaz
            Assert.AreEqual(Inn.ArrivalMultiplierFor(8), Inn.ArrivalMultiplier, 1e-5f);
            GameState.Data.unlocked.Add("oda3");
            Assert.AreEqual(Inn.ArrivalMultiplierFor(12), Inn.ArrivalMultiplier, 1e-5f);
        }

        /// <summary>Madde 19: yatak modeli seviyeyle değişir (saman → yün → kuştüyü); ücret seviyeden okunur.</summary>
        [Test]
        public void Comfort_BedAndFeeFollowLevel()
        {
            Assert.AreEqual(0, Room.BedFor(0));
            Assert.AreEqual(1, Room.BedFor(1));
            Assert.AreEqual(1, Room.BedFor(3));
            Assert.AreEqual(2, Room.BedFor(4));
            GameState.DebugReplace(new SaveData { roomLvs = new List<int> { 0, 2, 9 } });
            Assert.AreEqual(Inn.Fees[0], Inn.Fee(0));
            Assert.AreEqual(Inn.Fees[2], Inn.Fee(1));
            Assert.AreEqual(Inn.Fees[4], Inn.Fee(2), "taşan seviye en yükseğe sıkışır");
            Assert.IsFalse(Inn.CanUpgrade(2));
            Assert.AreEqual(Inn.ComfortCosts[2], Inn.UpgradeCost(1));
        }

        /// <summary>Eski kayıt: 3 kademeli konfor (2 = kuştüyü) yeni en yüksek seviyeye taşınır, diğerleri aynı kalır.</summary>
        [Test]
        public void Comfort_OldSaveMigrates()
        {
            Assert.AreEqual(0, Inn.MigrateComfortV2(0));
            Assert.AreEqual(1, Inn.MigrateComfortV2(1));
            Assert.AreEqual(Inn.Fees.Length - 1, Inn.MigrateComfortV2(2));
            Assert.Greater(GameState.CurrentSaveVersion, 2, "göç için kayıt sürümü artmalı");
        }

        /// <summary>Madde 20: 8 oda; ilk ikisi merdivenle, diğerleri ödeme kareleriyle açılır.</summary>
        [Test]
        public void Inn_HasEightRooms()
        {
            Assert.AreEqual(8, Inn.RoomCount);
            GameState.DebugReplace(new SaveData());
            Assert.IsTrue(Inn.RoomOpen(0));
            Assert.IsTrue(Inn.RoomOpen(1));
            Assert.IsFalse(Inn.RoomOpen(7));
            GameState.Data.unlocked.Add("oda8");
            Assert.IsTrue(Inn.RoomOpen(7));
        }

        /// <summary>Madde 15: son siparişler günün son %10'u; kapanış bekleme sınırı makul.</summary>
        [Test]
        public void DayEnd_LastCallIsFinalTenthOfDay()
        {
            Assert.AreEqual(0.9f, Balance.LastCallPhase, 1e-5f);
            Assert.Greater(Balance.LastCallPhase, Night.NightAt, "son siparişler geceye düşer");
            Assert.That(Balance.ClosingMaxSeconds, Is.InRange(5f, 20f));
            Assert.Greater(Balance.CommuteSpeed, 2f);
        }

        /// <summary>Madde 21: bahçe dekoru katalogda, bahçeyle açılır (Sv8+), prefab adı Blender çıktısıyla aynı.</summary>
        [Test]
        public void GardenDecor_InCatalog()
        {
            var yol = Catalog.FindDecor("dec_tasyol");
            var fener = Catalog.FindDecor("dec_bahcefener");
            Assert.AreEqual("PR_Decor_TasYol", yol.prefab);
            Assert.AreEqual("PR_Decor_BahceFeneri", fener.prefab);
            Assert.GreaterOrEqual(yol.level, 8);
            Assert.GreaterOrEqual(fener.level, 8);
            Assert.Greater(fener.ambiance, yol.ambiance);
            Assert.IsFalse(yol.premium || fener.premium);
        }
    }
}
