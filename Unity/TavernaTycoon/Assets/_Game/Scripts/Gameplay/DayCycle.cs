using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Gün döngüsü (GAME_DESIGN.md §3.2.4): 1 gün = 7 dk (v0.9). Gece bitince zaman durur, "Gün Bitti + Yarını Planla"
    /// ekranı açılır (maaş ödenir, rapor); oyuncu "Sonraki Güne Geç" deyince sabah olur (şafak geçişi, gece siparişleri gelir).
    /// </summary>
    public class DayCycle : MonoBehaviour
    {
        public struct Report { public int day; public long income, wages, unpaid; public int served; public float stars; }

        public static event Action<Report> DayEnded;
        public static float Progress01 { get; private set; }
        /// <summary>Günün bitmesine kalan süre (saniye); HUD'daki gün saati için.</summary>
        public static float RemainingSeconds { get; private set; }
        public float dayLength = Balance.DayLength;
        /// <summary>Gün bitti, plan ekranı açık (zaman durdu).</summary>
        public static bool Ended { get; private set; }
        public static event Action<int> DayStarted;
        /// <summary>Son "sonraki gün"den bu yana geçen gerçek süre (şafak ışığı geçişi için).</summary>
        public static float SinceDawn => Time.unscaledTime - _dawnAt;
        static float _dawnAt = -100f;
        static DayCycle _i;
        float _prevScale = 1f;

        float _t;
        long _income;
        int _servedStart;

        void OnEnable()
        {
            _i = this;
            dayLength = Balance.DayLength;
            Ended = false;
            Closing = false;
            GameState.CoinsGainedAt += OnCoins;
            _servedStart = GameState.Data.customersServed;
        }
        void OnDisable() { GameState.CoinsGainedAt -= OnCoins; if (Ended) Time.timeScale = _prevScale; Ended = false; Closing = false; }
        void OnCoins(long a, Vector3 _) => _income += a;

        /// <summary>v2.0 (madde 15): günün son kısmı — yeni müşteri gelmez, oturanlar yeni sipariş vermez.</summary>
        public static bool LastCall => Closing || Ended || Progress01 >= Balance.LastCallPhase;
        /// <summary>v2.0: gün süresi doldu, müşteriler ve personel çıkıyor (plan ekranı henüz açılmadı).</summary>
        public static bool Closing { get; private set; }
        /// <summary>Kapanış başladı: müşteriler hesabı kapatıp çıkar, personel kapıya yürür (StaffCommute).</summary>
        public static event Action ClosingStarted;
        float _closeT;

        /// <summary>Tavernanın içinde (henüz sokağa çıkmamış) müşteri var mı? Kapanış bunu bekler.</summary>
        static bool CustomersInside()
        {
            foreach (var c in Customer.All)
                if (c && (c.state != Customer.State.Leaving || c.transform.position.z > OutsideZ)) return true;
            return false;
        }
        /// <summary>Bu z'nin önü sokak (ön cephe z = −5, kaldırım −6..−7).</summary>
        const float OutsideZ = -5.6f;

        void Update()
        {
            if (Ended) return;
            if (Closing)
            {
                _closeT += Time.deltaTime;
                if ((!CustomersInside() && !StaffCommute.AnyInside) || _closeT >= Balance.ClosingMaxSeconds) EndDay();
                return;
            }
            if (Tutorial.HoldsTime) { RemainingSeconds = dayLength - _t; Progress01 = _t / dayLength; return; }   // v1.2: rehber sürerken gün akmaz
            _t += Time.deltaTime;
            Progress01 = _t / dayLength;
            RemainingSeconds = dayLength - _t;
            if (_t < dayLength) return;
            _t = dayLength;
            Progress01 = 1f;
            RemainingSeconds = 0f;
            // v2.0: önce kapanış — oturan müşteriler hesabı öder (bekleyenler kızmadan) çıkar, personel evine gider
            Closing = true;
            _closeT = 0f;
            foreach (var c in Customer.All.ToArray()) if (c) c.CloseUp();
            ClosingStarted?.Invoke();
        }

        void EndDay()
        {
            Closing = false;
            // Sokakta kalan müşteriler kaybolur; plan ekranında tavernada kimse yok
            foreach (var c in Customer.All.ToArray()) if (c) Destroy(c.gameObject);
            StaffCommute.FinishClosing();
            // v1.9 (oyuncu testi madde 7): maaş her durumda düşer; altın yetmezse kalanı borç olur (bakiye eksiye iner)
            long wages = Management.DailyWages;
            long unpaid = GameState.ForceSpend(wages);
            long paid = wages - unpaid;
            var r = new Report
            {
                day = GameState.Data.day, income = _income, wages = paid, unpaid = wages - paid,
                served = GameState.Data.customersServed - _servedStart, stars = Management.Stars,
            };
            Services.Track("day_end", ("gun", r.day), ("gelir", r.income), ("maas", r.wages), ("odenmeyen", r.unpaid),
                           ("servis", r.served), ("mutluluk", Mathf.RoundToInt(Happiness.Value)), ("seviye", GameState.Level));
            GameState.Data.day++;
            // Maaş ödenemediyse personel yeni gün küskün çalışır (Balance.SulkySpeed)
            GameState.Data.sulkyDay = wages > paid ? GameState.Data.day : 0;
            _income = 0;
            _servedStart = GameState.Data.customersServed;
            GameState.Save();
            Ended = true;
            TavernEvents.StopAll();
            _prevScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            DayEnded?.Invoke(r);
        }

        /// <summary>Test botu (v1.9 borç senaryosu): günü verilen ilerlemeye (0–1) atlatır.</summary>
        public static void DebugSetProgress(float p) { if (_i) _i._t = Mathf.Clamp01(p) * _i.dayLength; }

        /// <summary>Plan ekranındaki "Sonraki Güne Geç": zaman akar, sabah olur, gece verilen siparişler hemen gelir.</summary>
        public static void NextDay()
        {
            if (!Ended || !_i) return;
            Ended = false;
            Closing = false;
            _i._t = 0f;
            Progress01 = 0f;
            RemainingSeconds = _i.dayLength;
            Time.timeScale = _i._prevScale;
            _dawnAt = Time.unscaledTime;
            Depot.CompleteAll();
            DayStarted?.Invoke(GameState.Data.day);
        }
    }
}
