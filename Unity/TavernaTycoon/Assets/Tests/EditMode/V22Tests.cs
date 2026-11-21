using System;
using NUnit.Framework;

namespace Taverna.Tests
{
    /// <summary>
    /// v2.2 Paket 4 (sezonlar ve kostümler, GAME_DESIGN.md §16): takvim aralıkları ve yıl dönümü, kalan gün, sıradaki sezon,
    /// sezon değişince jetonların sona ermesi (taşınmaz), sezon görevleri ve jeton ekonomisi, kostüm sahipliği / giyme kaydı
    /// (prestijte kalır), sezon başlangıç bildirimi kuralları.
    /// </summary>
    public class V22Tests
    {
        SaveData _saved;
        DateTime? _date;

        [SetUp] public void Keep() { _saved = GameState.Data; _date = Seasons.DebugDate; GameState.DebugReplace(new SaveData()); }
        [TearDown] public void Restore() { GameState.DebugReplace(_saved); Seasons.DebugDate = _date; }

        static DateTime D(int y, int m, int d) => new(y, m, d, 12, 0, 0);

        [Test]
        public void Calendar_ThreeSeasons_WithTheirMonths()
        {
            Assert.IsTrue(Seasons.Find(D(2026, 9, 1), out var h, out _, out _)); Assert.AreEqual(SeasonId.Harvest, h.id);
            Assert.IsTrue(Seasons.Find(D(2026, 10, 31), out h, out _, out _)); Assert.AreEqual(SeasonId.Harvest, h.id);
            Assert.IsTrue(Seasons.Find(D(2026, 12, 1), out var w, out _, out _)); Assert.AreEqual(SeasonId.Winter, w.id);
            Assert.IsTrue(Seasons.Find(D(2027, 1, 31), out w, out _, out _)); Assert.AreEqual(SeasonId.Winter, w.id);
            Assert.IsTrue(Seasons.Find(D(2026, 4, 1), out var s, out _, out _)); Assert.AreEqual(SeasonId.Spring, s.id);
            Assert.IsTrue(Seasons.Find(D(2026, 5, 31), out s, out _, out _)); Assert.AreEqual(SeasonId.Spring, s.id);
            foreach (var off in new[] { D(2026, 2, 14), D(2026, 3, 31), D(2026, 6, 1), D(2026, 8, 31), D(2026, 11, 1), D(2026, 11, 30) })
                Assert.IsFalse(Seasons.Find(off, out _, out _, out _), $"{off:MM-dd} sezon dışı olmalı");
        }

        [Test]
        public void Winter_CrossesNewYear_WithOneKey()
        {
            Assert.AreEqual("winter-2026", Seasons.KeyAt(D(2026, 12, 15)));
            Assert.AreEqual("winter-2026", Seasons.KeyAt(D(2027, 1, 20)), "Ocak, Aralık'ta başlayan sezona aittir");
            Assert.AreEqual("harvest-2026", Seasons.KeyAt(D(2026, 9, 20)));
            Assert.AreEqual("", Seasons.KeyAt(D(2026, 11, 22)));
        }

        [Test]
        public void DaysLeft_CountsToday_AndEndsAtOne()
        {
            Seasons.Find(D(2026, 9, 1), out _, out _, out var end);
            Assert.AreEqual(new DateTime(2026, 10, 31), end.Date);
            Assert.AreEqual(61, Seasons.DaysLeft(D(2026, 9, 1), end));
            Assert.AreEqual(1, Seasons.DaysLeft(D(2026, 10, 31), end));
            Seasons.Find(D(2027, 1, 1), out _, out _, out var wend);
            Assert.AreEqual(new DateTime(2027, 1, 31), wend.Date);
        }

        [Test]
        public void NextStart_IsFirstFutureSeason()
        {
            Assert.AreEqual(new DateTime(2026, 12, 1), Seasons.NextStart(D(2026, 11, 22), out var d)); Assert.AreEqual(SeasonId.Winter, d.id);
            Assert.AreEqual(new DateTime(2027, 4, 1), Seasons.NextStart(D(2026, 12, 1), out d)); Assert.AreEqual(SeasonId.Spring, d.id);
            Assert.AreEqual(new DateTime(2026, 9, 1), Seasons.NextStart(D(2026, 6, 10), out d)); Assert.AreEqual(SeasonId.Harvest, d.id);
        }

        [Test]
        public void ForcedDates_FallInsideTheirSeason()
        {
            Assert.AreEqual("harvest-2026", Seasons.KeyAt(Seasons.ForcedDate("harvest", 2026)));
            Assert.AreEqual("winter-2026", Seasons.KeyAt(Seasons.ForcedDate("winter", 2026)));
            Assert.AreEqual("spring-2026", Seasons.KeyAt(Seasons.ForcedDate("spring", 2026)));
            Assert.AreEqual("", Seasons.KeyAt(Seasons.ForcedDate("none", 2026)));
        }

        [Test]
        public void Sync_NewSeason_ExpiresTokens_AndBuildsQuests()
        {
            var d = new SaveData();
            Assert.IsTrue(Seasons.Sync(d, D(2026, 9, 5)));
            Assert.AreEqual("harvest-2026", d.seasonKey);
            Assert.AreEqual(Seasons.QuestDefs.Length, d.seasonQuests.Count);
            d.seasonTokens = 37;
            Assert.IsFalse(Seasons.Sync(d, D(2026, 10, 30)), "aynı sezon: değişiklik yok");
            Assert.AreEqual(37, d.seasonTokens);
            Assert.IsTrue(Seasons.Sync(d, D(2026, 11, 2)), "sezon bitti");
            Assert.AreEqual("", d.seasonKey);
            Assert.AreEqual(0, d.seasonTokens, "jetonlar sonraki sezona taşınmaz");
            Assert.AreEqual(37, d.seasonExpiredTokens);
            Assert.AreEqual("harvest-2026", d.seasonExpiredKey);
            Assert.AreEqual(0, d.seasonQuests.Count);
            Assert.IsTrue(Seasons.Sync(d, D(2026, 12, 1)));
            Assert.AreEqual("winter-2026", d.seasonKey);
            Assert.AreEqual(0, d.seasonTokens);
        }

        [Test]
        public void Sync_DirectJumpBetweenSeasons_AlsoExpires()
        {
            var d = new SaveData();
            Seasons.Sync(d, D(2026, 10, 1));
            d.seasonTokens = 5;
            d.seasonQuests[0].progress = 10;
            Seasons.Sync(d, D(2026, 12, 3));   // oyuncu iki ay sonra döndü
            Assert.AreEqual("winter-2026", d.seasonKey);
            Assert.AreEqual(0, d.seasonTokens);
            Assert.AreEqual(5, d.seasonExpiredTokens);
            Assert.AreEqual(0, d.seasonQuests[0].progress, "görevler yeni sezon için sıfırdan");
        }

        [Test]
        public void Tokens_EarnSpendClaim_OnlyInSeason()
        {
            Seasons.DebugDate = new DateTime(2026, 7, 1);
            Seasons.AddTokens(10, "test");
            Assert.AreEqual(0, Seasons.Tokens, "sezon dışında jeton yok");
            Seasons.DebugDate = new DateTime(2026, 9, 20);
            Seasons.Sync(GameState.Data, Seasons.Now);
            Seasons.AddTokens(10, "test");
            Assert.AreEqual(10, Seasons.Tokens);
            Assert.IsFalse(Seasons.TrySpend(11));
            Assert.IsTrue(Seasons.TrySpend(4));
            Assert.AreEqual(6, Seasons.Tokens);
            var q = Seasons.QuestList[0];
            Assert.AreEqual(0, Seasons.ClaimReward(q), "bitmemiş görev alınamaz");
            q.progress = Seasons.Target(q);
            Assert.AreEqual(Seasons.Reward(q), Seasons.ClaimReward(q));
            Assert.AreEqual(0, Seasons.ClaimReward(q), "ödül bir kez");
        }

        [Test]
        public void Economy_SeasonShopIsReachable_AndQuestsMatter()
        {
            int questTokens = 0;
            foreach (var q in Seasons.QuestDefs) { questTokens += q.reward; Assert.Greater(q.target, 0); }
            Assert.AreEqual(100, questTokens, "dört sezon görevi toplam 100 jeton");
            foreach (var s in Seasons.List)
            {
                Assert.AreEqual(2, s.decor.Length, $"{s.key}: iki kalıcı dekor");
                int shop = Costumes.Find(s.costume).tokens;
                foreach (var id in s.decor)
                {
                    var dd = Catalog.FindDecor(id);
                    Assert.IsTrue(dd.seasonal && dd.premium, $"{id} altınla satılmaz");
                    Assert.Greater(dd.ambiance, 0f);
                    shop += (int)dd.price;
                }
                // sezon dükkânının tamamı: görev jetonları + en fazla ~200 sezon içeceği satışı (birkaç oyun saati, iki aya yayılır)
                Assert.That(shop, Is.InRange(150, questTokens + 200 * Seasons.TokensPerServe), $"{s.key}: sezon dükkânı {shop} jeton");
                Assert.AreEqual(s.id, Costumes.Find(s.costume).season);
            }
        }

        [Test]
        public void Playable_NeedsLevelAndFinishedTutorial()
        {
            Assert.IsFalse(Seasons.PlayableFor(1, false));
            Assert.IsFalse(Seasons.PlayableFor(5, true));
            Assert.IsTrue(Seasons.PlayableFor(Seasons.MinLevel, false));
        }

        [Test]
        public void SeasonDrinks_AreOnlyTheThree()
        {
            int n = 0;
            foreach (DrinkId id in Enum.GetValues(typeof(DrinkId))) if (Seasons.IsSeasonDrink(id)) n++;
            Assert.AreEqual(3, n);
            foreach (var s in Seasons.List) Assert.IsTrue(Seasons.IsSeasonDrink(s.drink));
            Assert.AreEqual(22, (int)DrinkId.CicekSerbeti, "yeni içecekler enum sonuna eklendi (kayıt uyumu)");
        }

        // ------------------------------------------------------------------ kostümler
        [Test]
        public void Costumes_Catalog_CoversBarloAndEveryRole()
        {
            int barlo = 0;
            foreach (var c in Costumes.List) if (c.owner == "barlo") barlo++;
            Assert.GreaterOrEqual(barlo, 4);
            foreach (var owner in new[] { "staff_pip", "staff_gus", "staff_ayla", "staff_nana" })
                Assert.IsTrue(Array.Exists(Costumes.List, c => c.owner == owner), owner);
            Assert.IsTrue(Array.Exists(Costumes.List, c => c.source == Costumes.Source.Store));
            Assert.IsTrue(Array.Exists(Costumes.List, c => c.source == Costumes.Source.Season));
            Assert.IsTrue(Array.Exists(Costumes.List, c => c.source == Costumes.Source.Reward));
            foreach (var c in Costumes.List)
                if (c.source == Costumes.Source.Store)
                {
                    var p = Premium.Find(c.product);
                    Assert.AreEqual(Premium.Kind.Costume, p.kind, c.id);
                    Assert.AreEqual(c.id, p.grant, c.id);
                }
        }

        [Test]
        public void Costumes_EquipNeedsOwnership_OnePerOwner()
        {
            var d = new SaveData();
            Assert.IsFalse(Costumes.Equip(d, "barlo_korsan"), "sahip olunmadan giyilmez");
            Costumes.GrantTo(d, "barlo_korsan");
            Costumes.GrantTo(d, "barlo_hasat");
            Costumes.GrantTo(d, "pip_yelek");
            Assert.IsTrue(Costumes.Equip(d, "barlo_korsan"));
            Assert.IsTrue(Costumes.Equip(d, "pip_yelek"));
            Assert.IsTrue(Costumes.Equip(d, "barlo_hasat"));
            Assert.AreEqual("barlo_hasat", Costumes.Worn(d, "barlo"), "sahip başına tek kostüm");
            Assert.AreEqual("pip_yelek", Costumes.Worn(d, "staff_pip"));
            Assert.AreEqual(2, d.wornCostumes.Count);
            Assert.IsTrue(Costumes.Unequip(d, "barlo"));
            Assert.AreEqual("", Costumes.Worn(d, "barlo"));
            Assert.AreEqual("pip_yelek", Costumes.Worn(d, "staff_pip"));
        }

        [Test]
        public void Costumes_RewardUnlocksWithEnding()
        {
            var d = new SaveData();
            var kral = Costumes.Find("barlo_kral");
            Assert.IsFalse(Costumes.Owned(d, kral));
            d.endingSeen = true;
            Assert.IsTrue(Costumes.Owned(d, kral));
            Assert.IsTrue(Costumes.Equip(d, "barlo_kral"));
        }

        [Test]
        public void Costumes_SurviveSaveRoundTrip()
        {
            var d = new SaveData();
            Costumes.GrantTo(d, "gus_usta");
            Costumes.Equip(d, "gus_usta");
            var back = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(d));
            Assert.AreEqual("gus_usta", Costumes.Worn(back, "staff_gus"));
            Assert.IsTrue(back.costumes.Contains("gus_usta"));
            // eski kayıt (alanlar yok) → boş listeler, kendi kıyafeti
            var old = UnityEngine.JsonUtility.FromJson<SaveData>("{\"level\":7}");
            Assert.AreEqual("", Costumes.Worn(old, "barlo"));
            Assert.AreEqual(0, old.seasonTokens);
            Assert.AreEqual("", old.seasonKey);
        }

        [Test]
        public void SeasonCostume_OnlyInItsSeason()
        {
            GameState.Data.level = 5;
            GameState.Data.tutorialDone = true;
            Seasons.DebugDate = new DateTime(2026, 9, 20);
            Seasons.Sync(GameState.Data, Seasons.Now);
            var kis = Costumes.Find("barlo_kis");
            var hasat = Costumes.Find("barlo_hasat");
            Assert.AreEqual(Seasons.Buy.OutOfSeason, Costumes.CanBuyWithTokens(kis));
            Assert.AreNotEqual(Seasons.Buy.OutOfSeason, Costumes.CanBuyWithTokens(hasat));
        }

        // ------------------------------------------------------------------ bildirim
        [Test]
        public void Notification_SeasonStart_RespectsRules()
        {
            var now = new DateTime(2026, 11, 29, 20, 0, 0);
            var start = new DateTime(2026, 12, 1, Seasons.NotifyHour, 0, 0);
            var plan = Notifications.Plan(now, 240, start, "Kış Festivali");
            var season = plan.Find(p => p.kind == Notifications.Kind.SeasonStart);
            Assert.AreEqual(start, season.fireLocal);
            var days = new System.Collections.Generic.HashSet<DateTime>();
            foreach (var p in plan)
            {
                Assert.IsTrue(days.Add(p.fireLocal.Date), "günde en fazla bir bildirim");
                Assert.IsTrue(p.fireLocal.Hour >= Notifications.QuietEndHour && p.fireLocal.Hour < Notifications.QuietStartHour, "gece yok");
                Assert.Greater(p.fireLocal, now);
            }
            // sezon günü diğer bildirimlerin önüne geçer (günlük ödül de 1 Aralık'a düşerdi: 30 Kasım 20:00 + 1 gün)
            var plan2 = Notifications.Plan(new DateTime(2026, 11, 30, 11, 0, 0), 240, start, "Kış Festivali");
            Assert.IsTrue(plan2.Exists(p => p.kind == Notifications.Kind.SeasonStart));
            Assert.IsFalse(plan2.Exists(p => p.kind != Notifications.Kind.SeasonStart && p.fireLocal.Date == start.Date));
        }
    }
}
