using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Garson (GAME_DESIGN.md §5): bekleyen bir siparişi üstlenir → istasyondan alır → servis eder.
    /// v1.3 (oyuncu testi "oyun kendi kendine oynuyor"): garson yardım eder ama işi bitirmez —
    /// "Barlo'ya özel" siparişleri almaz ve masadaki parayı toplamaz (o oyuncunun ya da geç açılan Kasa otomasyonunun).
    /// </summary>
    public class Waiter : MonoBehaviour
    {
        public static readonly List<Waiter> All = new();

        public string displayName = "Pip";
        public float speed = 2.4f;
        public Carrier carrier;
        public ToyAnimator anim;
        public Transform idlePoint;

        Customer _target;
        DrinkStation _station;
        CoinPile _coins;
        Vector3 _home;

        StaffBadge _badge;
        int _baseCapacity = -1;

        void OnEnable()
        {
            All.Add(this);
            _home = idlePoint ? idlePoint.position : transform.position;
            _badge = GetComponent<StaffBadge>();
            if (_baseCapacity < 0 && carrier) _baseCapacity = carrier.capacity;
        }
        void OnDisable() { All.Remove(this); Release(); }

        /// <summary>Görevli seviyesiyle (StaffUpgrades) çarpılmış hız.</summary>
        float Speed => _badge && !string.IsNullOrEmpty(_badge.staffId) ? speed * StaffUpgrades.SpeedMultiplier(_badge.staffId, Catalog.Role.Garson) : speed;

        void Update()
        {
            if (carrier && _baseCapacity >= 0)
                carrier.capacity = _baseCapacity + (_badge && !string.IsNullOrEmpty(_badge.staffId) ? StaffUpgrades.CarryBonus(_badge.staffId) : 0);
            if (_target && _target.state != Customer.State.Waiting) Release();

            if (!_target && !_coins) PickJob();

            if (_target)
            {
                if (!carrier.Has(_target.wanted))
                {
                    if (!_station || !_station.isActiveAndEnabled) { Release(); return; }
                    if (MoveTo(_station.ServePos, 0.5f)) _station.TryGiveTo(carrier);
                }
                else if (MoveTo(_target.transform.position, 0.9f))
                {
                    if (carrier.Take(_target.wanted)) { _target.Serve(); Sfx.Play(SfxId.Serve, 1.1f); }
                    Release();
                }
            }
            else if (_coins)
            {
                if (MoveTo(_coins.transform.position, 0.6f)) { _coins.Collect(transform); _coins = null; }
            }
            else MoveTo(_home, 0.2f);
        }

        void PickJob()
        {
            // Önce en sabırsız, kimsenin üstlenmediği siparişi al
            Customer best = null;
            float bestP = float.MaxValue;
            foreach (var c in Customer.All)
            {
                if (c.state != Customer.State.Waiting || c.reservedBy || c.special) continue;
                if (Inn.Upstairs(c.transform.position)) continue;   // teras: garsonlar çıkmaz
                if (PlayerController.I && PlayerController.I.carrier.Has(c.wanted)) continue; // oyuncu zaten getiriyor
                var st = DrinkStation.All.Find(s => s.drink.id == c.wanted);
                if (!st) continue;
                if (c.Patience01 < bestP) { bestP = c.Patience01; best = c; _station = st; }
            }
            if (best)
            {
                _target = best;
                best.reservedBy = this;
                return;
            }
            // v1.3: sipariş yoksa yerine döner (para toplamak oyuncunun işi)
        }

        void Release()
        {
            if (_target && _target.reservedBy == this) _target.reservedBy = null;
            _target = null;
            _station = null;
        }

        bool MoveTo(Vector3 p, float stopDist)
        {
            // Mutfak duvarının öbür yanındaysa önce kapıya (v0.7)
            var via = KitchenLayout.Route(transform.position, p);
            if ((via - p).sqrMagnitude > 0.01f) { MoveStraight(via, 0.15f); return false; }
            return MoveStraight(p, stopDist);
        }

        bool MoveStraight(Vector3 p, float stopDist)
        {
            var to = p - transform.position;
            to.y = 0f;
            if (to.magnitude <= stopDist) { if (anim) anim.SetSpeed(0f); return true; }
            transform.position += to.normalized * Mathf.Min(Speed * Time.deltaTime, to.magnitude);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 600f * Time.deltaTime);
            if (anim) anim.SetSpeed(1f);
            return false;
        }
    }
}
