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
            GameState.CoinsGainedAt += OnCoins;
            _servedStart = GameState.Data.customersServed;
        }
        void OnDisable() { GameState.CoinsGainedAt -= OnCoins; if (Ended) Time.timeScale = _prevScale; Ended = false; }
        void OnCoins(long a, Vector3 _) => _income += a;

        void Update()
        {
            if (Ended) return;
            _t += Time.deltaTime;
            Progress01 = _t / dayLength;
            RemainingSeconds = dayLength - _t;
            if (_t < dayLength) return;
            _t = dayLength;
            Progress01 = 1f;
            RemainingSeconds = 0f;
            long wages = Management.DailyWages;
            long paid = Math.Min(wages, GameState.Coins);
            if (paid > 0) GameState.TrySpend(paid);
            var r = new Report
            {
                day = GameState.Data.day, income = _income, wages = paid, unpaid = wages - paid,
                served = GameState.Data.customersServed - _servedStart, stars = Management.Stars,
            };
            GameState.Data.day++;
            // Maaş ödenemediyse personel yeni gün küskün çalışır (Balance.SulkySpeed)
            GameState.Data.sulkyDay = wages > paid ? GameState.Data.day : 0;
            _income = 0;
            _servedStart = GameState.Data.customersServed;
            GameState.Save();
            Ended = true;
            _prevScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            DayEnded?.Invoke(r);
        }

        /// <summary>Plan ekranındaki "Sonraki Güne Geç": zaman akar, sabah olur, gece verilen siparişler hemen gelir.</summary>
        public static void NextDay()
        {
            if (!Ended || !_i) return;
            Ended = false;
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
