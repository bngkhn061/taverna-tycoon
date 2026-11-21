using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.0 (madde 15): bir görevlinin işe geliş / evine gidiş yürüyüşü. Yürürken iş davranışları (garson, aşçı,
    /// temizlikçi, fenerci, hizmetçi, ozan) kapalıdır; varınca açılır. Kat değiştiren adımda (merdiven) ışınlanır.
    /// <see cref="StaffCommute"/> yönetir.
    /// </summary>
    public class Commuter : MonoBehaviour
    {
        enum Mode { None, Out, In }

        Mode _mode;
        readonly List<Vector3> _pts = new();
        readonly List<Behaviour> _off = new();
        int _idx;
        float _shrink = -1f;
        Vector3 _scale = Vector3.one;
        Quaternion _homeRot;
        ToyAnimator _anim;

        /// <summary>Kapanışta kapıdan çıkıp kaldırıma vardı mı (artık tavernada sayılmaz)?</summary>
        public bool OnStreet => _mode == Mode.Out && _idx >= _pts.Count - 1;

        /// <summary>İşteki yeri (evine gitmeden önce durduğu konum); sabah buraya döner.</summary>
        public Vector3 Home { get; private set; }

        /// <summary>Kapanışta: işi bırakır, verilen yoldan sokağa yürür.</summary>
        public void Leave(List<Vector3> path)
        {
            var p = transform.position;
            Home = p;
            transform.position = new Vector3(p.x, 0f, p.z);   // sahnedeki ozan yere iner
            _homeRot = transform.rotation;
            _scale = transform.localScale;
            SetWork(false);
            Begin(Mode.Out, path);
        }

        /// <summary>Sabah: yolun başında (sokakta) belirir ve işine yürür.</summary>
        public void Arrive(List<Vector3> path)
        {
            transform.localScale = _scale;
            if (path.Count > 0) transform.position = path[0];
            gameObject.SetActive(true);
            SetWork(false);
            Begin(Mode.In, path);
        }

        /// <summary>Kapanış süresi doldu: sokakta kaybolmuş sayılır (gizlenir).</summary>
        public void Vanish()
        {
            if (_mode != Mode.Out) return;
            Finish();
        }

        void Begin(Mode m, List<Vector3> path)
        {
            _mode = m;
            _pts.Clear();
            _pts.AddRange(path);
            _idx = m == Mode.In ? 1 : 0;
            _shrink = -1f;
            if (!_anim) _anim = GetComponentInChildren<ToyAnimator>();
            StaffCommute.SetWalking(this, true);
        }

        void SetWork(bool on)
        {
            if (!on)
            {
                // Liste temizlenmez: sabah çağrısında işler zaten kapalı, akşam kapatılanlar sabah açılsın
                foreach (var b in GetComponents<Behaviour>())
                    if (b.enabled && !_off.Contains(b) && (b is Waiter || b is StaffWork || b is Cleaner || b is Lamplighter || b is Housekeeper || b is StaffIdle))
                    {
                        b.enabled = false;
                        _off.Add(b);
                    }
            }
            else
            {
                foreach (var b in _off) if (b) b.enabled = true;
                _off.Clear();
            }
        }

        void Update()
        {
            if (_mode == Mode.None) return;
            if (_shrink >= 0f)
            {
                _shrink += Time.deltaTime / ShrinkSeconds;
                transform.localScale = _scale * Mathf.Max(0.01f, 1f - _shrink);
                if (_shrink >= 1f) Finish();
                return;
            }
            if (_idx >= _pts.Count) { Arrived(); return; }
            var target = _pts[_idx];
            // Merdiven: üst kat dünyada ayrı yerde, kat değişen adımda ışınlan
            if (Inn.Upstairs(target) != Inn.Upstairs(transform.position)) { transform.position = target; _idx++; return; }
            if (StaffMove.ToRouted(transform, target, Balance.CommuteSpeed, StopDistance, _anim)) _idx++;
        }

        void Arrived()
        {
            if (_mode == Mode.Out) { _shrink = 0f; return; }   // sokağın ucunda küçülüp kaybolur
            transform.rotation = _homeRot;
            if (_anim) _anim.SetSpeed(0f);
            SetWork(true);
            _mode = Mode.None;
            StaffCommute.SetWalking(this, false);
        }

        void Finish()
        {
            _mode = Mode.None;
            _shrink = -1f;
            transform.localScale = _scale;
            StaffCommute.SetWalking(this, false);
            StaffCommute.SetAway(gameObject, true);
            gameObject.SetActive(false);
        }

        void OnDisable() { if (_mode != Mode.None) StaffCommute.SetWalking(this, false); }

        const float ShrinkSeconds = 0.35f, StopDistance = 0.15f;
    }
}
