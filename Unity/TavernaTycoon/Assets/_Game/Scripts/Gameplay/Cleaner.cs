using UnityEngine;

namespace Taverna
{
    /// <summary>Temizlikçi: kirli masaya gider, temizler, sonrakine geçer.</summary>
    public class Cleaner : MonoBehaviour
    {
        public float speed = 2.0f;
        public ToyAnimator anim;
        public Transform idlePoint;
        Table _target;
        float _cleanT;
        StaffBadge _badge;

        void Awake() => _badge = GetComponent<StaffBadge>();
        float Speed => _badge ? speed * StaffUpgrades.SpeedMultiplier(_badge.staffId, Catalog.Role.Temizlikci) : speed;

        void Update()
        {
            if (_target && !_target.Dirty) _target = null;
            if (!_target)
            {
                float best = float.MaxValue;
                foreach (var t in Table.All)
                {
                    if (!t.Dirty || t.claimedByCleaner || Inn.Upstairs(t.transform.position)) continue;
                    float d = DrinkStation.FlatDistance(transform.position, t.transform.position);
                    if (d < best) { best = d; _target = t; }
                }
                if (_target) _target.claimedByCleaner = true;
            }
            if (_target)
            {
                if (StaffMove.ToRouted(transform, _target.transform.position, Speed, 0.9f, anim))
                {
                    _cleanT += Time.deltaTime;
                    if (_cleanT > 0.8f) { _target.Clean(); _target = null; _cleanT = 0f; }
                }
            }
            else if (idlePoint) StaffMove.ToRouted(transform, idlePoint.position, Speed, 0.2f, anim);
        }

        void OnDisable() { if (_target) _target.claimedByCleaner = false; }
    }
}
