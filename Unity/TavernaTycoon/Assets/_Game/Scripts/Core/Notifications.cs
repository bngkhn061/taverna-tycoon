using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using Unity.Notifications;
#endif

namespace Taverna
{
    /// <summary>
    /// v1.7 yerel bildirimler (com.unity.mobile.notifications, birleşik API).
    /// Ayarlar'daki Bildirim düğmesi açıkken uygulama arka plana geçince planlanır, ön plana dönünce hepsi iptal edilir;
    /// düğme kapalıyken hiçbiri planlanmaz. İzin (Android 13+ POST_NOTIFICATIONS, iOS yetkilendirme) yalnızca oyuncu
    /// düğmeyi açınca istenir. Düğme varsayılan kapalı (<see cref="GameSettings.Notifications"/>).
    /// Ne zaman / hangi metin: saf <see cref="Plan"/> işlevi (birim testli). Editörde ve masaüstünde paket çağrıları derlenmez.
    /// </summary>
    public static class Notifications
    {
        /// <summary>Android bildirim kanalı kimliği.</summary>
        public const string ChannelId = "taverna_main";
        /// <summary>Sessiz saatlerin başladığı saat (yerel, dahil): bu saatten sonra bildirim yok.</summary>
        public const int QuietStartHour = 22;
        /// <summary>Sessiz saatlerin bittiği saat (yerel): bildirimler en erken bu saatte.</summary>
        public const int QuietEndHour = 9;
        /// <summary>Oyuncu bu kadar gün gelmezse hatırlatma.</summary>
        public const int ReminderDays = 3;
        /// <summary>Günlük ödül bildirimi kaç gün sonra (ertesi gün).</summary>
        public const int DailyDays = 1;

        /// <summary>Bildirim türü (kimlik ve test için).</summary>
        public enum Kind { OfflineFull = 1, DailyReady = 2, Reminder = 3, SeasonStart = 4 }   // v2.2: sezon başladı

        /// <summary>Planlanmış tek bildirim (yerel saat; metinler etkin dilde).</summary>
        public struct Planned
        {
            public Kind kind;
            public DateTime fireLocal;
            public string title, text;
        }

        /// <summary>Tarih ve saati sessiz saatlerin dışına iter: 22:00–09:00 arası → 09:00 (gerekirse ertesi gün).</summary>
        public static DateTime OutOfQuietHours(DateTime t)
        {
            if (t.Hour >= QuietStartHour) return t.Date.AddDays(1).AddHours(QuietEndHour);
            if (t.Hour < QuietEndHour) return t.Date.AddHours(QuietEndHour);
            return t;
        }

        /// <summary>
        /// Arka plana geçerken planlanacak bildirimler (saf işlev). Adaylar: çevrimdışı kazanç doldu (şimdi + <paramref name="offlineCapMinutes"/>),
        /// günlük ödül hazır (ertesi gün aynı saat), <see cref="ReminderDays"/> gün gelmeyince hatırlatma. Her biri sessiz saatlerden
        /// çıkarılır; zaman sırasıyla, aynı takvim gününe ikinci bildirim düşmez (günde en fazla bir). Geçmişe düşen olmaz.
        /// </summary>
        public static List<Planned> Plan(DateTime nowLocal, int offlineCapMinutes) => Plan(nowLocal, offlineCapMinutes, null, null);

        /// <summary>
        /// v2.2: <paramref name="seasonStartLocal"/> verilirse sezon başlangıç bildirimi de aday olur (o günün diğer bildirimlerinin
        /// önüne geçer; sessiz saat ve günde en fazla bir kuralı aynen geçerli). <paramref name="seasonName"/> çevrilmiş sezon adı.
        /// </summary>
        public static List<Planned> Plan(DateTime nowLocal, int offlineCapMinutes, DateTime? seasonStartLocal, string seasonName)
        {
            var cands = new List<Planned>
            {
                new() { kind = Kind.OfflineFull, fireLocal = OutOfQuietHours(nowLocal.AddMinutes(offlineCapMinutes)),
                        title = Loc.T("Kasa doldu!"), text = Loc.F("Sen yokken taverna {0} saat çalıştı. Kazancını topla!", offlineCapMinutes / 60) },
                new() { kind = Kind.DailyReady, fireLocal = OutOfQuietHours(nowLocal.AddDays(DailyDays)),
                        title = Loc.T("Günlük ödül hazır"), text = Loc.T("Barlo bugünün ödülünü ayırdı. Seriyi bozma!") },
                new() { kind = Kind.Reminder, fireLocal = OutOfQuietHours(nowLocal.AddDays(ReminderDays)),
                        title = Loc.T("Barlo seni özledi"), text = Loc.T("Müşteriler kapıda bekliyor. Tavernaya bir uğra!") },
            };
            if (seasonStartLocal.HasValue)
                cands.Add(new Planned { kind = Kind.SeasonStart, fireLocal = OutOfQuietHours(seasonStartLocal.Value),
                                        title = Loc.F("{0} başladı!", seasonName), text = Loc.T("Sezon menüsü, sezon müşterileri ve süsler tavernada. Jeton topla!") });
            cands.Sort((a, b) => a.fireLocal.CompareTo(b.fireLocal));
            var days = new HashSet<DateTime>();
            var seasonDay = cands.Find(c => c.kind == Kind.SeasonStart).fireLocal.Date;   // sezon günü diğer bildirimlere kapalı
            var plan = new List<Planned>();
            foreach (var c in cands)
            {
                if (c.fireLocal <= nowLocal) continue;
                if (seasonStartLocal.HasValue && c.kind != Kind.SeasonStart && c.fireLocal.Date == seasonDay) continue;
                if (!days.Add(c.fireLocal.Date)) continue;   // günde en fazla bir
                plan.Add(c);
            }
            return plan;
        }

        // ------------------------------------------------------------------ çalışma anı
        /// <summary>Teşhis: son arka plan geçişinde planlananlar (editörde ve masaüstünde de hesaplanır, gönderilmez).</summary>
        public static List<Planned> LastPlan { get; private set; } = new();

        /// <summary>Ayarlar düğmesi: açınca izin istenir (yalnızca o an), kapatınca planlananların hepsi iptal edilir.</summary>
        public static void SetEnabled(bool on)
        {
            GameSettings.Notifications = on;
            if (on) NotificationsRunner.Ensure().RequestPermission();
            else CancelAll();
        }

        /// <summary>Uygulama arka plana geçti / kapanıyor: düğme açıksa planla.</summary>
        public static void OnBackground()
        {
            CancelAll();
            if (!GameSettings.Notifications) return;
            // v2.2: sıradaki sezonun başlangıcı (takvim; zorlanmış test tarihiyle değil, cihaz tarihiyle)
            var start = Seasons.NextNotifyLocal(DateTime.Now, out var season);
            LastPlan = Plan(DateTime.Now, Offline.CapMinutes, start, Loc.T(season.name));
            if (Debug.isDebugBuild)
                foreach (var p in LastPlan) Debug.Log($"[Bildirim] plan {p.kind} {p.fireLocal:yyyy-MM-dd HH:mm} · {p.title} · {p.text}");
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (!InitCenter()) return;
            foreach (var p in LastPlan)
            {
                var n = new Notification { Identifier = (int)p.kind, Title = p.title, Text = p.text };
                NotificationCenter.ScheduleNotification(n, new NotificationDateTimeSchedule(p.fireLocal));
            }
#endif
        }

        /// <summary>Ön plana dönüldü: planlananlar ve bildirim merkezindekiler temizlenir.</summary>
        public static void OnForeground() => CancelAll();

        static void CancelAll()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (!InitCenter()) return;
            NotificationCenter.CancelAllScheduledNotifications();
            NotificationCenter.CancelAllDeliveredNotifications();
#endif
        }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        static bool _init;
        /// <summary>Bildirim merkezini bir kez kurar (Android kanalı adı etkin dilde).</summary>
        internal static bool InitCenter()
        {
            if (_init) return true;
            try
            {
                var args = NotificationCenterArgs.Default;
                args.AndroidChannelId = ChannelId;
                args.AndroidChannelName = Loc.T("Taverna haberleri");
                args.AndroidChannelDescription = Loc.T("Kasa, günlük ödül ve hatırlatmalar");
                NotificationCenter.Initialize(args);
                _init = true;
            }
            catch (Exception e) { Debug.LogWarning("[Bildirim] başlatılamadı: " + e.Message); }
            return _init;
        }
#endif
    }

    /// <summary>Uygulama ön/arka plan geçişlerini <see cref="Notifications"/>'a iletir; izin isteğini bekler (sahneler arası yaşar).</summary>
    public class NotificationsRunner : MonoBehaviour
    {
        static NotificationsRunner _i;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() => Ensure();

        /// <summary>Tek örneği döndürür (yoksa kurar).</summary>
        public static NotificationsRunner Ensure()
        {
            if (_i) return _i;
            var go = new GameObject("Notifications") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _i = go.AddComponent<NotificationsRunner>();
            return _i;
        }

        void OnApplicationPause(bool paused) { if (paused) Notifications.OnBackground(); else Notifications.OnForeground(); }
        void OnApplicationQuit() => Notifications.OnBackground();

        /// <summary>İzni ister (yalnızca oyuncu düğmeyi açınca). Reddedilirse düğme kapanır.</summary>
        public void RequestPermission() => StartCoroutine(RequestRoutine());

        IEnumerator RequestRoutine()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (!Notifications.InitCenter()) yield break;
            var req = Unity.Notifications.NotificationCenter.RequestPermission();
            yield return req;
            if (req.Status == Unity.Notifications.NotificationsPermissionStatus.Denied) GameSettings.Notifications = false;
#else
            yield break;
#endif
        }
    }
}
