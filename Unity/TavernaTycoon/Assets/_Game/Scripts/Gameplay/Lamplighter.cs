using UnityEngine;

namespace Taverna
{
    /// <summary>Fenerci (v0.6): akşam olunca açık bölgelerdeki sönük fenerleri sırayla yakar, sonra yerine döner.</summary>
    public class Lamplighter : MonoBehaviour
    {
        public float speed = 2.2f;
        public ToyAnimator anim;
        public Transform idlePoint;
        Lantern _target;
        StaffBadge _badge;

        void Awake() => _badge = GetComponent<StaffBadge>();
        float Speed => _badge ? speed * StaffUpgrades.SpeedMultiplier(_badge.staffId, Catalog.Role.Fenerci) : speed;

        void Update()
        {
            if (_target && (_target.lit || !Night.IsDark)) _target = null;
            if (!_target && Night.IsDark)
            {
                float best = float.MaxValue;
                foreach (var l in Lantern.All)
                {
                    if (l.lit || !l.ZoneOpen || Inn.Upstairs(l.transform.position)) continue;
                    float d = DrinkStation.FlatDistance(transform.position, l.transform.position);
                    if (d < best) { best = d; _target = l; }
                }
            }
            if (_target)
            {
                var stand = _target.transform.position;
                stand.y = 0f;
                if (StaffMove.ToRouted(transform, stand, Speed, 1.0f, anim)) { _target.Ignite(); _target = null; }
            }
            else if (idlePoint) StaffMove.ToRouted(transform, idlePoint.position, Speed, 0.2f, anim);
        }
    }
}
