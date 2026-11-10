using UnityEngine;

namespace Taverna
{
    /// <summary>Hizmetçi (v0.8): üst katta dağınık odaları sırayla toplar, sonra koridorda bekler.</summary>
    public class Housekeeper : MonoBehaviour
    {
        public float speed = 2.0f;
        public ToyAnimator anim;
        public Transform idlePoint;
        Room _target;
        Table _table;
        float _cleanT;
        StaffBadge _badge;

        void Awake() => _badge = GetComponent<StaffBadge>();
        float Speed => _badge ? speed * StaffUpgrades.SpeedMultiplier(_badge.staffId, Catalog.Role.Hizmetci) : speed;

        void Update()
        {
            if (_target && _target.state != Room.State.Dirty) _target = null;
            if (!_target)
            {
                float best = float.MaxValue;
                foreach (var r in Room.All)
                {
                    if (r.state != Room.State.Dirty) continue;
                    float d = DrinkStation.FlatDistance(transform.position, r.bedPoint.position);
                    if (d < best) { best = d; _target = r; }
                }
            }
            if (!_target)
            {
                if (_table && (!_table.Dirty || _table.Busy)) _table = null;
                if (!_table)
                    foreach (var t in Table.All)
                        if (t.Dirty && !t.Busy && Inn.Upstairs(t.transform.position)) { _table = t; break; }
                if (_table)
                {
                    var tp = _table.transform.position;
                    bool inRoom = transform.position.z > 39.0f && transform.position.x < 6.1f;
                    var goal = inRoom ? new Vector3(transform.position.x, 0f, 38.4f) : new Vector3(tp.x, 0f, tp.z - 0.8f);
                    if (StaffMove.To(transform, goal, Speed, 0.2f, anim) && !inRoom)
                    {
                        _cleanT += Time.deltaTime;
                        if (_cleanT > 0.8f) { _table.Clean(); _table = null; _cleanT = 0f; }
                    }
                    return;
                }
            }
            if (_target)
            {
                // Odaya kapıdan girer: önce kapı hizası (koridor), sonra yatak
                var door = new Vector3(_target.bedPoint.position.x + 0.5f, 0f, 38.4f);
                bool inRoom = transform.position.z > 39.0f;
                var goal = inRoom || Mathf.Abs(transform.position.x - door.x) < 0.2f ? _target.bedPoint.position : door;
                goal.y = 0f;
                if (StaffMove.To(transform, goal, Speed, goal == door ? 0.15f : 1.0f, anim) && goal != door)
                {
                    _cleanT += Time.deltaTime;
                    if (_cleanT > 1.0f) { _target.Clean(); _target = null; _cleanT = 0f; }
                }
            }
            else if (idlePoint)
            {
                bool inRoom = transform.position.z > 39.0f;
                if (inRoom) StaffMove.To(transform, new Vector3(transform.position.x, 0f, 38.4f), Speed, 0.15f, anim);
                else StaffMove.To(transform, idlePoint.position, Speed, 0.2f, anim);
            }
        }
    }
}
