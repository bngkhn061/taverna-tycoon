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
        /// <summary>v2.0: sabah ilk müşteriye kadar en fazla bu kadar saniye (personel kapıdan girerken).</summary>
        const float FirstMorningSpawn = 3f;
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
            // v2.0 (madde 15): günün son %10'unda ve kapanışta yeni müşteri gelmez (sabah ilk müşteri kısa sürede gelir)
            if (DayCycle.LastCall || Ending.Playing) { _t = Mathf.Min(_t, FirstMorningSpawn); return; }   // v2.1: son sahnede de yeni müşteri gelmez
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
            // v2.2: sezon boyunca gelenlerin bir kısmı sezon müşterisi (sezon içeceğini ister)
            if (Seasons.Playable && Random.value < Seasons.CustomerChance)
            {
                var sc = GameDatabase.I.SeasonCustomer(Seasons.Current.id);
                if (sc && sc.modelPrefab) return sc;
            }
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
            var path = new List<Vector3> { new Vector3(ent.position.x, 0f, from.position.z), ent.position };
            path.AddRange(InnerPath(seat));
            var cut = Shortcut(path, 1, seat);
            go.AddComponent<Customer>().Init(def, seat, cut, from.position);
            Spawned++;
            // düzenleme modunda taşınan masanın toplanma noktası kapının kendisidir: o zaman uğranacak ayrı nokta yok
            bool viaHub = path.Count > 3 && (path[2] - path[1]).sqrMagnitude > 0.01f && cut.Contains(path[2]);
            if (!viaHub) HubSkipped++;
            if (AutoTest.Enabled)
            {
                Clear(path[1], path[^1], seat);
                Debug.Log($"[Taverna] müşteri yolu {t.name}/{seat.name}: {path.Count - 1} → {cut.Count - 1} adım · toplanma noktası {(viaHub ? "VAR" : "yok")} ({HubSkipped}/{Spawned} kestirme){(viaHub ? " · kapıdan engel: " + LastBlocker : "")}");
            }
        }

        /// <summary>v2.5.1 ölçüm: gelen müşteri ve toplanma noktasına uğramadan yürüyen müşteri sayısı (oturum boyunca).</summary>
        public static int Spawned, HubSkipped;

        /// <summary>Kestirme denemesinde müşterinin genişliği (yarıçap) ve yoklama yüksekliği (masa ve tezgâh bu boyda).</summary>
        public const float ClearRadius = 0.24f, ClearY = 0.5f;
        static readonly RaycastHit[] _hits = new RaycastHit[24];
        /// <summary>Hedef oturağın kendi masası, yolun yalnız bu son kısmında engel sayılmaz (oturak masanın 0.2–0.4 m yanında).</summary>
        public const float OwnTableSlack = 0.6f;
        /// <summary>Kendi masasının köşesini sıyırmaya izin: yolun ortası masaya bundan yakın geçmezse engel değil.</summary>
        public const float OwnTableThin = 0.12f;

        /// <summary>a→b yolunun ilk <paramref name="upTo"/> metresinde, yolun ortası <paramref name="c"/>'ye <see cref="OwnTableThin"/>'den yakın geçiyor mu?</summary>
        static bool Pierces(Collider c, Vector3 a, Vector3 b, float upTo)
        {
            var from = new Vector3(a.x, ClearY, a.z);
            var dir = new Vector3(b.x - a.x, 0f, b.z - a.z).normalized;
            for (float s = 0f; s <= upTo; s += 0.05f)
            {
                var p = from + dir * s;
                var q = c.ClosestPoint(p);
                q.y = p.y;
                if ((q - p).sqrMagnitude < OwnTableThin * OwnTableThin) return true;
            }
            return false;
        }
        /// <summary>Son <see cref="Clear"/> yoklamasını kesen nesnenin adı (ölçüm günlüğü için; boşsa yol açıktı).</summary>
        public static string LastBlocker { get; private set; } = "";

        /// <summary>
        /// v2.5.1 (oyuncu testi 2026-11-24: "müşteriler önce fıçıya gidiyor sonra masaya oturuyor"): yol, <paramref name="from"/>
        /// noktasından sonra kestirilir. Her noktadan, arası boş olan en uzak noktaya doğrudan gidilir; böylece önü açık
        /// masaya giden müşteri bira fıçısının önündeki toplanma noktasına uğramaz. Arası kapalıysa eski yol aynen kalır.
        /// </summary>
        public static List<Vector3> Shortcut(List<Vector3> path, int from, Seat seat)
        {
            if (path == null || from < 0 || path.Count - from < 3) return path;
            var res = new List<Vector3>(path.Count);
            for (int k = 0; k <= from; k++) res.Add(path[k]);
            int i = from, last = path.Count - 1;
            while (i < last)
            {
                int next = i + 1;
                for (int j = last; j > i + 1; j--)
                    if (Clear(path[i], path[j], j == last ? seat : null)) { next = j; break; }
                // kapıdan yalnız toplanma noktası görünüyorsa önce aradaki masanın yanından kısa bir sapma dene
                if (i == from && next == from + 1)
                    for (int j = last; j > from + 1; j--)
                        if (Detour(path[i], path[j], j == last ? seat : null, out var dp)) { res.Add(dp); next = j; break; }
                res.Add(path[next]);
                i = next;
            }
            return res;
        }

        static readonly float[] DetourAlong = { 0.25f, 0.4f, 0.55f, 0.7f, 0.85f };
        static readonly float[] DetourSide = { 0.4f, -0.4f, 0.8f, -0.8f, 1.2f, -1.2f, 1.6f, -1.6f, 2.0f, -2.0f };

        /// <summary>
        /// a'dan b'ye düz yol kapalıysa, aradaki engelin yanından tek bir sapma noktası arar (a→sapma ve sapma→b açık olmalı);
        /// birden çok varsa toplam yolu en kısa olanı.
        /// </summary>
        public static bool Detour(Vector3 a, Vector3 b, Seat seat, out Vector3 point)
        {
            point = default;
            var v = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float len = v.magnitude;
            if (len < 0.5f || Inn.Upstairs(a) != Inn.Upstairs(b)) return false;
            var side = new Vector3(v.z, 0f, -v.x) / len;
            float best = float.MaxValue;
            foreach (var f in DetourAlong)
                foreach (var s in DetourSide)
                {
                    var d = a + v * f + side * s;
                    d.y = 0f;
                    float cost = Vector3.Distance(a, d) + Vector3.Distance(d, b);
                    if (cost >= best) continue;
                    if (!Clear(a, d, null) || !Clear(d, b, seat)) continue;
                    best = cost;
                    point = d;
                }
            return best < float.MaxValue;
        }

        /// <summary>
        /// İki nokta arasında müşteri genişliğinde engel (masa, tezgâh, makine, duvar) yok mu? Tetikleyiciler, karakterler ve
        /// ön görünmez sınır sayılmaz; hedef oturaksa kendi masası yalnız son <see cref="OwnTableSlack"/> metrede sayılmaz
        /// (oturak masaya değer, ama müşteri kendi masasının içinden de geçmez). Kat değişiyorsa (teras merdiveni) kestirme yok.
        /// </summary>
        public static bool Clear(Vector3 a, Vector3 b, Seat seat)
        {
            LastBlocker = "";
            if (Inn.Upstairs(a) != Inn.Upstairs(b)) { LastBlocker = "kat"; return false; }
            var d = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float len = d.magnitude;
            if (len < 0.01f) return true;
            int n = Physics.SphereCastNonAlloc(new Vector3(a.x, ClearY, a.z), ClearRadius, d / len, _hits, len, ~0, QueryTriggerInteraction.Ignore);
            var own = seat && seat.table ? seat.table.transform : null;
            for (int k = 0; k < n; k++)
            {
                var c = _hits[k].collider;
                if (!c || _hits[k].distance <= 0f) continue;   // başlangıçta zaten içinde olunan (ör. kapı eşiği) sayılmaz
                if (c is CharacterController || c.name.EndsWith("Barrier") || c.GetComponentInParent<Customer>()) continue;
                if (own && c.transform.IsChildOf(own) && (_hits[k].distance >= len - OwnTableSlack || !Pierces(c, a, b, len - OwnTableSlack))) continue;
                LastBlocker = c.name;
                return false;
            }
            return true;
        }

        static CustomerSpawner _i;
        void Awake() => _i = this;

        /// <summary>
        /// Kapıdan sonraki yol: toplanma noktası, oturağın yaklaşma noktaları, oturak. v2.3: düzenleme modu taşınan masanın
        /// yaklaşma noktalarını ızgara yolundan yeniden kurar (o masanın toplanma noktası kapının kendisi olur).
        /// </summary>
        public static List<Vector3> InnerPath(Seat seat)
        {
            var t = seat.table;
            var h = t.hub ? t.hub : _i ? _i.hub : null;
            var path = new List<Vector3>();
            if (h) path.Add(h.position);
            if (seat.approach != null)
                foreach (var a in seat.approach) if (a) path.Add(a.position);
            path.Add(seat.transform.position);
            return path;
        }
    }
}
