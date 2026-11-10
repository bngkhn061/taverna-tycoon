using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Müşteri döngüsü (GAME_DESIGN.md §2, §7):
    /// içeri yürü → otur → sipariş (sabır halkası) → servis → iç → parayı masaya bırak → (tekrar sipariş) → çık.
    /// </summary>
    public class Customer : MonoBehaviour
    {
        public enum State { WalkingIn, Waiting, Drinking, Leaving }
        public enum Emote { None, Happy, Angry }

        public static readonly List<Customer> All = new();

        public CustomerDef def;
        public State state;
        public DrinkId wanted;
        public float patience, patienceMax;
        public Emote emote;
        public float emoteTime;
        public float walkSpeed = 1.9f;
        /// <summary>v1.3 "Barlo'ya özel" sipariş: garsonlar almaz, oyuncu servis edince ×2.5 öder.</summary>
        public bool special;
        /// <summary>Test günlüğü: açılan ve servis edilen özel sipariş sayısı.</summary>
        public static int SpecialOrders, SpecialServed;
        [System.NonSerialized] public Waiter reservedBy;

        Seat _seat;
        List<Vector3> _path;
        int _pathIdx;
        int _ordersLeft;
        float _drinkT;
        GameObject _cupGo;
        ToyAnimator _anim;
        Vector3 _exit;

        public float Patience01 => patienceMax > 0 ? patience / patienceMax : 0f;
        public Vector3 HeadPos => transform.position + Vector3.up * (def && def.modelPrefab ? 1.55f : 1.4f);

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void Init(CustomerDef d, Seat seat, List<Vector3> pathIn, Vector3 exit)
        {
            def = d;
            _seat = seat;
            seat.occupant = this;
            _path = pathIn;
            _pathIdx = 0;
            _exit = exit;
            _ordersLeft = Mathf.Max(1, d.ordersPerVisit) + (Random.value < Balance.ExtraOrderChance(GameState.Level) ? 1 : 0);
            state = State.WalkingIn;
            if (d.modelPrefab)
            {
                var m = Instantiate(d.modelPrefab, transform);
                m.transform.localPosition = Vector3.zero;
                if (!m.TryGetComponent(out _anim)) _anim = m.AddComponent<ToyAnimator>();
            }
            GameState.MarkSeen(d.number);
        }

        void Update()
        {
            switch (state)
            {
                case State.WalkingIn:
                    if (FollowPath()) { FaceTable(); NewOrder(); }
                    break;
                case State.Waiting:
                    // Karanlık odada (yakılmamış fenerler) sabır hızlı biter
                    patience -= Time.deltaTime * (Night.IsDark && !Night.ZoneLit(Night.ZoneOf(transform.position)) ? Night.DarkPatienceDrain : 1f);
                    TryGetServed();
                    if (patience <= 0f) Leave(Emote.Angry);
                    break;
                case State.Drinking:
                    _drinkT -= Time.deltaTime;
                    if (_drinkT <= 0f) FinishDrink();
                    break;
                case State.Leaving:
                    if (FollowPath()) Destroy(gameObject);
                    break;
            }
            if (emoteTime > 0f) { emoteTime -= Time.deltaTime; if (emoteTime <= 0f) emote = Emote.None; }
        }

        bool FollowPath()
        {
            if (_path == null || _pathIdx >= _path.Count) { if (_anim) _anim.SetSpeed(0); return true; }
            var target = _path[_pathIdx];
            target.y = transform.position.y;
            // v1.0 teras: merdiven ayağına varınca öbür kata geçer (üst kat dünyada ayrı yerde)
            if (Inn.Upstairs(target) != Inn.Upstairs(transform.position)) { transform.position = target; _pathIdx++; return false; }
            var to = target - transform.position;
            float step = walkSpeed * Time.deltaTime;
            if (to.magnitude <= step) { transform.position = target; _pathIdx++; }
            else
            {
                transform.position += to.normalized * step;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 540f * Time.deltaTime);
            }
            if (_anim) _anim.SetSpeed(1f);
            return false;
        }

        void FaceTable()
        {
            var to = _seat.table.transform.position - transform.position;
            to.y = 0;
            if (to.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(to);
        }

        void NewOrder()
        {
            // Sadece tavernada istasyonu olan içecekleri ister
            var options = new List<DrinkId>();
            // v1.0: müşteri bulunduğu kattaki makinelerden ister (teras müşterisi teras barından)
            bool up = Inn.Upstairs(transform.position);
            bool SameFloor(DrinkStation s) => s && s.drink && Inn.Upstairs(s.transform.position) == up;
            foreach (var d in def.drinks)
                if (DrinkStation.All.Exists(s => SameFloor(s) && s.drink.id == d)) options.Add(d);
            if (options.Count == 0)
                foreach (var s in DrinkStation.All) if (SameFloor(s) && !s.drink.isFood) options.Add(s.drink.id);
            wanted = options.Count > 0 ? options[Random.Range(0, options.Count)] : DrinkId.Bira;
            // v0.7: mutfak açıksa müşteriler ara sıra yemek ister (her siparişte %35)
            var foods = up ? new List<DrinkStation>() : DrinkStation.All.FindAll(s => s.drink && s.drink.isFood);
            if (foods.Count > 0 && Random.value < Balance.FoodOrderChance) wanted = foods[Random.Range(0, foods.Count)].drink.id;
            // v1.3: üst kattaki (teras) siparişler zaten oyuncunun; özel sipariş sadece zemin katta
            special = !up && Random.value < Balance.SpecialOrderChance(GameState.Level);
            if (special) SpecialOrders++;
            patienceMax = patience = Balance.PatienceSeconds(def.patienceStars) * Balance.PatienceLevelScale(GameState.Level) * Management.PatienceMultiplier
                                     * (special ? Balance.SpecialPatienceMultiplier : 1f);
            state = State.Waiting;
            if (Random.value < 0.3f) Sfx.Voice(def, VoiceKind.Order, transform.position);
        }

        void TryGetServed()
        {
            var p = PlayerController.I;
            if (!p || !p.carrier) return;
            if (DrinkStation.FlatDistance(p.transform.position, transform.position) > Balance.InteractRadius) return;
            if (!p.carrier.Take(wanted)) return;
            if (p.anim) p.anim.Pop();
            Sfx.Play(SfxId.Serve);
            Serve();
        }

        /// <summary>Oyuncu ya da garson servis ettiğinde.</summary>
        public void Serve()
        {
            reservedBy = null;
            Happiness.OnServed(Patience01, HeadPos);
            if (special) { Happiness.Bonus(Balance.SpecialHappiness, HeadPos); SpecialServed++; }
            float tip = Balance.TipMultiplier(Patience01) * (RushHour.Active ? 2f : 1f) * Management.TipFromStars * TavernEvents.TipMultiplier;
            GameState.RaiseServed(wanted);
            var station = DrinkStation.All.Find(s => s.drink.id == wanted);
            int price = station ? station.Price : 1;
            _pendingPay = Mathf.Max(1, Mathf.CeilToInt(price * Balance.PayMultiplier(def.payStars) * tip * Boosts.IncomeMultiplier
                                                       * Night.PayMultiplier(def, transform.position)
                                                       * (special ? Balance.SpecialPayMultiplier : 1f)));
            special = false;
            _drinkT = Balance.DrinkDuration;
            state = State.Drinking;
            emote = tip > 1f ? Emote.Happy : Emote.None;
            emoteTime = 1.2f;

            var drinkDef = GameDatabase.I ? GameDatabase.I.Drink(wanted) : null;
            if (drinkDef && drinkDef.cupPrefab)
            {
                _cupGo = Instantiate(drinkDef.cupPrefab);
                _cupGo.transform.position = Vector3.Lerp(transform.position, _seat.table.transform.position, 0.55f) + Vector3.up * 0.64f;
                _cupGo.transform.localScale = Vector3.one * 1.2f;
            }
            if (_anim) _anim.Pop();
            if (Random.value < (tip > 1f ? 0.45f : 0.2f)) Sfx.Voice(def, VoiceKind.Happy, transform.position, 0.9f);
        }

        int _pendingPay;

        void FinishDrink()
        {
            if (_cupGo) { _seat.table.AddDirt(_cupGo); _cupGo = null; }
            CoinPile.Drop(_seat.table.CoinPos, _pendingPay);
            GameState.Data.customersServed++;
            GameState.AddUn(def.payStars >= 4 ? Balance.RichUn : 1);
            _ordersLeft--;
            if (_ordersLeft > 0) NewOrder();
            else Leave(Emote.Happy);
        }

        void Leave(Emote e)
        {
            reservedBy = null;
            if (e == Emote.Angry) { Sfx.Voice(def, VoiceKind.Grumble, transform.position, 1.1f); Management.OnAngryCustomer(); Happiness.OnAngry(HeadPos); }
            emote = e;
            emoteTime = 1.5f;
            state = State.Leaving;
            if (_seat) _seat.occupant = null;
            // Gelinen yolun tersinden çık
            var back = new List<Vector3>();
            for (int i = _path.Count - 2; i >= 0; i--) back.Add(_path[i]);
            back.Add(_exit);
            _path = back;
            _pathIdx = 0;
            if (_cupGo) Destroy(_cupGo);
        }

        void OnDestroy() { if (_seat && _seat.occupant == this) _seat.occupant = null; }
    }
}
