using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Ödeme karesi (GAME_DESIGN.md §2, §6.1): oyuncu üstünde durunca para akar;
    /// tamamlanınca hedef nesneler (yeni istasyon, masa, duvar yıkımı…) açılır.
    /// </summary>
    public class PayZone : MonoBehaviour
    {
        public static readonly List<PayZone> All = new();

        public string id = "zone";
        public string label = "Yeni Masa";
        public long cost = 50;
        public int requiredLevel = 1;
        [Tooltip("Bu alan, önce şu alan açılınca görünür (ör. 'salon').")]
        public string requires = "";
        public GameObject[] unlockTargets;
        public GameObject[] hideOnUnlock;
        public float radius = 0.7f;
        public float drainDuration = 1.6f;

        public long Paid { get; private set; }

        /// <summary>Sıradaki hedef: görünür alanlardan en düşük seviyeli, sonra en ucuz olanı (HUD ve test botu ortak).</summary>
        public static PayZone NextGoal()
        {
            PayZone best = null;
            foreach (var z in All)
            {
                if (!z.enabled || !z.Visible) continue;
                if (!best || z.requiredLevel < best.requiredLevel || (z.requiredLevel == best.requiredLevel && z.cost < best.cost)) best = z;
            }
            return best;
        }
        public long Remaining => cost - Paid;
        public bool Visible => !GameState.IsUnlocked(id) && (string.IsNullOrEmpty(requires) || GameState.IsUnlocked(requires));
        public bool LevelOk => GameState.Level >= requiredLevel;
        public bool Available => Visible && LevelOk;

        float _carry, _tick;
        GameObject _visual;

        void Awake()
        {
            _visual = transform.childCount > 0 ? transform.GetChild(0).gameObject : null;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Start()
        {
            Paid = GameState.ZonePaid(id);
            if (GameState.IsUnlocked(id)) ApplyUnlocked(false);
        }

        void Update()
        {
            bool avail = Available;
            if (_visual) _visual.SetActive(Visible);
            if (!avail) return;

            var p = PlayerController.I;
            if (!p || DrinkStation.FlatDistance(p.transform.position, transform.position) > radius) { _carry = 0; return; }

            // Toplam bedel drainDuration sürede akar; para yetmezse olduğu kadar akar ve kaldığı yeri hatırlar
            _carry += cost / drainDuration * Time.deltaTime;
            long step = (long)_carry;
            if (step <= 0) return;
            step = System.Math.Min(step, System.Math.Min(Remaining, GameState.Coins));
            if (step <= 0) { _carry = 0; return; }
            _carry -= step;
            GameState.TrySpend(step);
            Paid += step;
            _tick -= Time.deltaTime;
            if (_tick <= 0f) { _tick = 0.07f; Sfx.Play(SfxId.Tick, 0.8f + 0.6f * (Paid / (float)cost)); }
            GameState.SetZonePaid(id, Paid);
            if (Paid >= cost)
            {
                GameState.Unlock(id);
                ApplyUnlocked(true);
                Sfx.Play(SfxId.Buy);
                GameSettings.Vibrate();
            }
        }

        void ApplyUnlocked(bool animate)
        {
            foreach (var t in unlockTargets)
            {
                if (!t) continue;
                t.SetActive(true);
                if (animate) t.AddComponent<PopIn>();
            }
            foreach (var h in hideOnUnlock) if (h) h.SetActive(false);
            if (_visual) _visual.SetActive(false);
            enabled = false;
        }
    }
}
