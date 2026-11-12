using System;
using NUnit.Framework;

namespace Taverna.Tests
{
    /// <summary>v1.7 bildirim planı (saf işlev): ne zaman, hangi tür; sessiz saatler ve günde en fazla bir bildirim.</summary>
    public class NotificationTests
    {
        static readonly int Cap = Offline.CapMinutes;

        [TearDown]
        public void Restore() => Loc.SetTableForTests(Loc.Source, null);

        [Test]
        public void QuietHours_ShiftToNineAm()
        {
            var d = new DateTime(2026, 11, 11);
            Assert.AreEqual(d.AddDays(1).AddHours(9), Notifications.OutOfQuietHours(d.AddHours(22)));
            Assert.AreEqual(d.AddDays(1).AddHours(9), Notifications.OutOfQuietHours(d.AddHours(23).AddMinutes(59)));
            Assert.AreEqual(d.AddHours(9), Notifications.OutOfQuietHours(d.AddHours(3)));
            Assert.AreEqual(d.AddHours(9), Notifications.OutOfQuietHours(d.AddHours(9)));
            Assert.AreEqual(d.AddHours(21).AddMinutes(59), Notifications.OutOfQuietHours(d.AddHours(21).AddMinutes(59)));
        }

        [Test]
        public void Daytime_PlansOfflineDailyAndReminder()
        {
            var now = new DateTime(2026, 11, 11, 10, 0, 0);
            var p = Notifications.Plan(now, Cap);
            Assert.AreEqual(3, p.Count);
            Assert.AreEqual(Notifications.Kind.OfflineFull, p[0].kind);
            Assert.AreEqual(now.AddMinutes(Cap), p[0].fireLocal, "çevrimdışı üst sınır dolunca");
            Assert.AreEqual(Notifications.Kind.DailyReady, p[1].kind);
            Assert.AreEqual(now.AddDays(1), p[1].fireLocal, "ertesi gün");
            Assert.AreEqual(Notifications.Kind.Reminder, p[2].kind);
            Assert.AreEqual(now.AddDays(Notifications.ReminderDays), p[2].fireLocal, "3 gün sonra");
            StringAssert.Contains((Cap / 60).ToString(), p[0].text);
        }

        [Test]
        public void Evening_OfflineShiftsToMorning_DailyDroppedSameDay()
        {
            // 21:00 → kasa 23:00'te dolar → sessiz saat → ertesi gün 09:00; günlük ödül ertesi gün 21:00 → aynı gün, düşer
            var now = new DateTime(2026, 11, 11, 21, 0, 0);
            var p = Notifications.Plan(now, Cap);
            Assert.AreEqual(2, p.Count);
            Assert.AreEqual(Notifications.Kind.OfflineFull, p[0].kind);
            Assert.AreEqual(new DateTime(2026, 11, 12, 9, 0, 0), p[0].fireLocal);
            Assert.AreEqual(Notifications.Kind.Reminder, p[1].kind);
            Assert.AreEqual(new DateTime(2026, 11, 14, 21, 0, 0), p[1].fireLocal);
        }

        [Test]
        public void Plan_NeverInQuietHours_AtMostOnePerDay_NeverInPast()
        {
            var day = new DateTime(2026, 11, 11);
            for (int m = 0; m < 24 * 60; m += 7)
            {
                var now = day.AddMinutes(m);
                var p = Notifications.Plan(now, Cap);
                Assert.IsNotEmpty(p);
                var days = new System.Collections.Generic.HashSet<DateTime>();
                foreach (var n in p)
                {
                    Assert.IsTrue(n.fireLocal.Hour >= Notifications.QuietEndHour && n.fireLocal.Hour < Notifications.QuietStartHour, $"{now:HH:mm} → {n.fireLocal:HH:mm}");
                    Assert.IsTrue(days.Add(n.fireLocal.Date), $"{now:HH:mm}: aynı güne iki bildirim");
                    Assert.Greater(n.fireLocal, now);
                    Assert.IsFalse(string.IsNullOrEmpty(n.title));
                    Assert.IsFalse(string.IsNullOrEmpty(n.text));
                }
            }
        }

        [Test]
        public void Texts_UseActiveLanguage()
        {
            Loc.SetTableForTests("en", new System.Collections.Generic.Dictionary<string, string> { ["Kasa doldu!"] = "Till is full!" });
            var p = Notifications.Plan(new DateTime(2026, 11, 11, 10, 0, 0), Cap);
            Assert.AreEqual("Till is full!", p[0].title);
        }
    }
}
