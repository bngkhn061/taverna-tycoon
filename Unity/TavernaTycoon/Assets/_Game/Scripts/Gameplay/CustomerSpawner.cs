using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Kapıdan müşteri gönderir (GAME_DESIGN.md §7). Açık müşteri tiplerinden ağırlıklı seçer;
    /// yeni açılan tip ilk 3 dakika ×3 ağırlıkla gelir.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        public Transform outside;   // sokağın bir ucu (doğma/kaybolma)
        public Transform outsideB;  // sokağın diğer ucu
        public Transform entrance;  // kapının içi
        public Transform hub;       // salonun ortasındaki toplanma noktası
        public float intervalScale = 1f;

        float _t = 1.5f;
        readonly Dictionary<int, float> _newSince = new();
        int _lastLevel;

        void Start() => _lastLevel = GameState.Level;

        void Update()
        {
            if (GameState.Level != _lastLevel)
            {
                foreach (var c in GameDatabase.I.customers)
                    if (c && c.unlockLevel > _lastLevel && c.unlockLevel <= GameState.Level) _newSince[c.number] = Time.time;
                _lastLevel = GameState.Level;
            }

            // v1.2 rehber: bardak dolana kadar müşteri yok, sonra en fazla 2
            if (!Tutorial.AllowsSpawn) { _t = Mathf.Min(_t, 1.2f); return; }
            _t -= Time.deltaTime;
            if (_t > 0f) return;
            _t = Balance.BaseSpawnInterval * intervalScale * Balance.SpawnLevelScale(GameState.Level) * Random.Range(0.75f, 1.25f)
                 / BoostMultiplier / Balance.SeatSpawnScale(SeatCount());

            var def = PickCustomer();
            if (!def) return;
            var seat = Sailor(def) ? DockSeat() ?? FreeSeat() : FreeSeat();
            if (!seat) return;
            Spawn(def, seat);
        }

        /// <summary>
        /// v1.4: geçici / kazanılan müşteri çarpanlarının ham çarpımı: mutluluk × etkinlik × yoğun saat × Usta.
        /// </summary>
        public static float RawBoost => Happiness.SpawnMultiplier * TavernEvents.SpawnMultiplier
                                        * (RushHour.Active ? Balance.RushSpawnMultiplier : 1f) * Automation.GuestMultiplier;

        /// <summary>Uygulanan müşteri çarpanı: ham çarpım, en fazla ×<see cref="Balance.SpawnBoostCap"/>. Oturak ve seviye ölçeği dışarıda.</summary>
        public static float BoostMultiplier => Balance.CapSpawnBoost(RawBoost);

        /// <summary>Sahnedeki (açılmış) masaların oturak sayısı.</summary>
        static int SeatCount()
        {
            int n = 0;
            foreach (var t in Table.All) if (t) n += t.seats.Length;
            return n;
        }

        float Ambiance01()
        {
            return Mathf.Clamp01(AmbianceSource.Total / 100f);
        }

        Seat FreeSeat()
        {
            var free = new List<Seat>();
            foreach (var t in Table.All)
            {
                // Karanlıkta fenerleri yakılmamış odaya müşteri gelmez (v0.6 gece)
                if (!Night.ZoneLit(Night.ZoneOf(t.transform.position))) continue;
                // Üst kat masası (teras) sadece orada bar varken dolar
                if (Inn.Upstairs(t.transform.position) && !DrinkStation.All.Exists(s => s && Inn.Upstairs(s.transform.position))) continue;
                foreach (var s in t.seats)
                    if (s && s.Free) free.Add(s);
            }
            return free.Count > 0 ? free[Random.Range(0, free.Count)] : null;
        }

        /// <summary>v1.0 İskele: denizciler (Korsan Kaptan, Balıkçı) boş iskele masasını tercih eder.</summary>
        static bool Sailor(CustomerDef d) => d && (d.number == 25 || d.number == 2);

        Seat DockSeat()
        {
            foreach (var t in Table.All)
            {
                if (Night.ZoneOf(t.transform.position) != "iskele" || !Night.ZoneLit("iskele")) continue;
                foreach (var s in t.seats) if (s && s.Free) return s;
            }
            return null;
        }

        CustomerDef PickCustomer()
        {
            var pool = new List<(CustomerDef d, float w)>();
            float total = 0;
            foreach (var c in GameDatabase.I.customers)
            {
                if (!c || !c.modelPrefab || c.unlockLevel > GameState.Level) continue;
                float w = c.spawnWeight;
                // Zengin müşteriler (ödeme ★4+) yıldızlı tavernayı sever
                if (c.payStars >= 4) w *= Mathf.Max(0.3f, Management.Stars / 2.5f) * Tiers.RichWeight * TavernEvents.RichWeight;   // v1.0: kademe zengin müşteriyi sıklaştırır
                w *= TavernEvents.CustomerWeight(c.number);   // v1.5 Kraliyet Ziyareti: Gizli Kral çok sık
                if (_newSince.TryGetValue(c.number, out var since) && Time.time - since < 180f) w *= 3f;
                if (Night.IsNight && Night.IsNightCreature(c)) w *= Night.NightCreatureWeight; // gece fantastik müşteriler
                pool.Add((c, w));
                total += w;
            }
            float r = Random.value * total;
            foreach (var (d, w) in pool) { r -= w; if (r <= 0) return d; }
            return pool.Count > 0 ? pool[^1].d : null;
        }

        void Spawn(CustomerDef def, Seat seat)
        {
            var go = new GameObject($"Customer_{def.displayName}");
            // Sokağın bir ucundan gelir, masasının bulunduğu bölgenin girişinden içeri girer
            var from = Random.value < 0.5f || !outsideB ? outside : outsideB;
            go.transform.position = from.position;
            var t = seat.table;
            var ent = t.entrance ? t.entrance : entrance;
            var h = t.hub ? t.hub : hub;
            var path = new List<Vector3> { new Vector3(ent.position.x, 0f, from.position.z), ent.position, h.position };
            if (seat.approach != null)
                foreach (var a in seat.approach) if (a) path.Add(a.position);
            path.Add(seat.transform.position);
            go.AddComponent<Customer>().Init(def, seat, path, from.position);
        }
    }
}
