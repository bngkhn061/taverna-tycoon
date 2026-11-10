using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Yoğun Saat (GAME_DESIGN.md §7): belirli aralıklarla 45 sn boyunca müşteri ×2, bahşiş ×2.
    /// Oyunun zorluk zirvesi; kaçırmanın cezası yok, yakalamanın ödülü büyük.
    /// </summary>
    public class RushHour : MonoBehaviour
    {
        public static bool Active { get; private set; }
        public static float Remaining { get; private set; }
        public float NextIn { get; private set; }
        public static event Action Started, Ended;

        public float firstDelay = 100f;
        public Vector2 interval = new(300f, 420f);
        public float duration = 45f;
        public int minLevel = 2;

        static RushHour _i;

        /// <summary>Mağazadan: Yoğun Saati hemen başlat.</summary>
        public static void ForceStart() { if (_i && !Active) _i.NextIn = 0f; if (_i) _i._forced = true; }
        bool _forced;

        void Start()
        {
            _i = this;
            Active = false;
            NextIn = firstDelay;
        }

        void Update()
        {
            if (!Active)
            {
                if (GameState.Level < minLevel && !_forced) return;
                _forced = false;
                NextIn -= Time.deltaTime;
                if (NextIn > 0f) return;
                Active = true;
                Remaining = duration;
                Sfx.Play(SfxId.Rush);
                GameSettings.Vibrate();
                Started?.Invoke();
            }
            else
            {
                Remaining -= Time.deltaTime;
                if (Remaining > 0f) return;
                Active = false;
                NextIn = UnityEngine.Random.Range(interval.x, interval.y);
                Quests.OnRushCompleted();
                Ended?.Invoke();
            }
        }

        void OnDestroy() => Active = false;
    }
}
