using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Oyun salonu masası (v0.9): zar, kart, bilek güreşi. Masada oturan müşteri varken "Oyna!" hazır olur;
    /// Barlo yanına gelince (ya da "Oyun salonu" otomasyonu varsa kendiliğinden) bir tur döner,
    /// tur sonunda oyuncu başına oyun ücreti masaya para olarak bırakılır. Sonra kısa bekleme.
    /// </summary>
    [RequireComponent(typeof(Table))]
    public class GameTable : MonoBehaviour
    {
        public enum Kind { Zar, Kart, Bilek }
        public enum State { Idle, Ready, Playing, Cooldown }

        public static readonly List<GameTable> All = new();

        public Kind kind;
        public int feePerPlayer = 6;
        public float roundSeconds = 4f, cooldownSeconds = 10f;
        public Transform[] props;   // turda zıplayan zarlar / kartlar

        public State state { get; private set; }
        public float Progress01 => state == State.Playing ? 1f - _t / roundSeconds : 0f;
        public Vector3 TagPos => transform.position + Vector3.up * 1.25f;
        public string Title => kind switch { Kind.Zar => Loc.T("Zar"), Kind.Kart => Loc.T("Kart"), _ => Loc.T("Bilek güreşi") };

        Table _table;
        float _t, _autoT;
        Vector3[] _propBase;

        void Awake()
        {
            _table = GetComponent<Table>();
            _propBase = new Vector3[props?.Length ?? 0];
            for (int i = 0; i < _propBase.Length; i++) if (props[i]) _propBase[i] = props[i].localPosition;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        int Players
        {
            get
            {
                int n = 0;
                foreach (var s in _table.seats)
                    if (s && s.occupant && (s.occupant.state == Customer.State.Waiting || s.occupant.state == Customer.State.Drinking)) n++;
                return n;
            }
        }

        void Update()
        {
            switch (state)
            {
                case State.Idle:
                    if (Players > 0) { state = State.Ready; _autoT = 1.2f; }
                    break;
                case State.Ready:
                    if (Players == 0) { state = State.Idle; break; }
                    var p = PlayerController.I;
                    bool near = p && DrinkStation.FlatDistance(p.transform.position, transform.position) < BarloSkills.Reach + 0.3f;
                    if (Automation.Has("oyun")) _autoT -= Time.deltaTime;
                    if (near || (Automation.Has("oyun") && _autoT <= 0f)) StartRound(near ? p : null);
                    break;
                case State.Playing:
                    _t -= Time.deltaTime;
                    AnimateProps();
                    if (_t <= 0f) EndRound();
                    break;
                case State.Cooldown:
                    _t -= Time.deltaTime;
                    if (_t <= 0f) state = State.Idle;
                    break;
            }
        }

        void StartRound(PlayerController p)
        {
            state = State.Playing;
            _t = roundSeconds / TavernEvents.GameRoundSpeed;
            Sfx.Play(SfxId.Dice);
            if (p && p.anim) p.anim.Pop();
        }

        void AnimateProps()
        {
            for (int i = 0; i < _propBase.Length; i++)
            {
                if (!props[i]) continue;
                float ph = Time.time * 9f + i * 1.7f;
                props[i].localPosition = _propBase[i] + Vector3.up * Mathf.Abs(Mathf.Sin(ph)) * 0.12f;
                props[i].localRotation = Quaternion.Euler(ph * 40f, ph * 25f, 0f);
            }
        }

        void EndRound()
        {
            for (int i = 0; i < _propBase.Length; i++) if (props[i]) props[i].localPosition = _propBase[i];
            int n = Mathf.Max(1, Players);
            long fee = Mathf.Max(1, Mathf.RoundToInt(feePerPlayer * n * Boosts.IncomeMultiplier * TavernEvents.GameFeeMultiplier));
            CoinPile.Drop(_table.CoinPos, fee);
            GameState.AddUn(1);
            Sfx.Play(SfxId.Claim, 1.1f);
            foreach (var s in _table.seats) if (s && s.occupant) { s.occupant.emote = Customer.Emote.Happy; s.occupant.emoteTime = 1.2f; }
            state = State.Cooldown;
            _t = cooldownSeconds;
        }
    }
}
