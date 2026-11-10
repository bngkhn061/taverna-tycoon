using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Han misafiri (v0.8): akşam sokaktan resepsiyona yürür, Barlo yanına gelince gecelik ücreti öder,
    /// merdivenden odasına çıkar ve yatakta uyur; sabah ayrılır, oda dağınık kalır.
    /// Resepsiyonda çok beklerse kızgın gider (hafif ceza, Management.OnAngryCustomer).
    /// </summary>
    public class Guest : MonoBehaviour
    {
        public enum State { WalkIn, Waiting, ToStairs, Sleeping, Leaving }

        public static readonly List<Guest> All = new();

        public State state { get; private set; }
        public Room room;
        public float patience, patienceMax;
        public float Patience01 => patienceMax > 0f ? patience / patienceMax : 0f;
        public Vector3 HeadPos => transform.position + Vector3.up * 1.55f;

        ToyAnimator _anim;
        readonly List<Vector3> _path = new();
        int _pathIdx;
        const float Speed = 1.7f;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Odasını ayırıp sokağın ucunda bir misafir yaratır (rastgele açık müşteri modeli).</summary>
        public static Guest Spawn(Room room)
        {
            var db = GameDatabase.I;
            if (!db || !room) return null;
            var pool = new List<CustomerDef>();
            foreach (var c in db.customers) if (c && c.modelPrefab && c.unlockLevel <= GameState.Level) pool.Add(c);
            if (pool.Count == 0) return null;
            var def = pool[Random.Range(0, pool.Count)];
            var go = new GameObject($"Guest_{def.displayName}");
            go.transform.position = Inn.ArrivalPath[0];
            var m = Instantiate(def.modelPrefab, go.transform);
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.identity;
            var g = go.AddComponent<Guest>();
            g._anim = m.AddComponent<ToyAnimator>();
            g.room = room;
            room.Reserve();
            g.patienceMax = g.patience = Inn.GuestPatience * Management.PatienceMultiplier;
            g.SetPath(Inn.ArrivalPath, 1);
            g.state = State.WalkIn;
            return g;
        }

        void SetPath(Vector3[] pts, int start)
        {
            _path.Clear();
            for (int i = start; i < pts.Length; i++) _path.Add(pts[i]);
            _pathIdx = 0;
        }

        bool Follow()
        {
            if (_pathIdx >= _path.Count) { _anim.SetSpeed(0f); return true; }
            var to = _path[_pathIdx] - transform.position;
            to.y = 0f;
            float step = Speed * Time.deltaTime;
            if (to.magnitude <= step) { transform.position = _path[_pathIdx]; _pathIdx++; }
            else
            {
                transform.position += to.normalized * step;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), Time.deltaTime * 10f);
            }
            _anim.SetSpeed(1f);
            return false;
        }

        void Update()
        {
            switch (state)
            {
                case State.WalkIn:
                    if (Follow())
                    {
                        state = State.Waiting;
                        transform.rotation = Quaternion.LookRotation(Vector3.right); // tezgâha dönük
                    }
                    if (!Night.IsDark) Leave(false);
                    break;
                case State.Waiting:
                    patience -= Time.deltaTime;
                    // v0.9 Han resepsiyonu otomasyonu: misafir kısa bekleyince kendi giriş yapar
                    if (Automation.Has("han") && patienceMax - patience > Balance.AutoCheckInDelay) { CheckIn(); break; }
                    var p = PlayerController.I;
                    if (p && DrinkStation.FlatDistance(p.transform.position, transform.position) < Balance.InteractRadius) CheckIn();
                    else if (patience <= 0f) Leave(true);
                    else if (!Night.IsDark) Leave(false);
                    break;
                case State.ToStairs:
                    if (Follow()) GoToBed();
                    break;
                case State.Sleeping:
                    _anim.SetSpeed(0f);
                    if (!Night.IsDark) Checkout();
                    break;
                case State.Leaving:
                    if (Follow()) Destroy(gameObject);
                    break;
            }
        }

        void CheckIn()
        {
            int fee = room ? room.Fee : Inn.Fees[0];
            GameState.AddCoins(Mathf.RoundToInt(fee * Boosts.IncomeMultiplier), transform.position + Vector3.up);
            Sfx.Play(SfxId.Buy);
            Inn.CheckIns++;
            _anim.Hop();
            SetPath(Inn.ToStairsPath, 0);
            state = State.ToStairs;
        }

        void GoToBed()
        {
            if (!room || !room.bedPoint) { Destroy(gameObject); return; }
            room.Occupy();
            // Sırtüstü yatar: yüzü yukarı, başı yatağın başına (+z)
            transform.SetPositionAndRotation(room.bedPoint.position, Quaternion.LookRotation(Vector3.up, room.bedPoint.forward));
            state = State.Sleeping;
        }

        void Checkout()
        {
            if (room) room.MakeDirty();
            Destroy(gameObject);
        }

        void Leave(bool angry)
        {
            if (angry) { Inn.AngryGuests++; Management.OnAngryCustomer(); Sfx.Play(SfxId.Angry); }
            if (room) room.Release();
            room = null;
            var back = new List<Vector3>();
            for (int i = Inn.ArrivalPath.Length - 2; i >= 0; i--) back.Add(Inn.ArrivalPath[i]);
            _path.Clear();
            _path.AddRange(back);
            _pathIdx = 0;
            state = State.Leaving;
        }
    }
}
