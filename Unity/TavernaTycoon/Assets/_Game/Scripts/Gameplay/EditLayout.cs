using System;
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.3 masa ve dekor düzenleme modu (GAME_DESIGN.md §17). Meyhane, Ana Salon ve Bahçe'deki masalar ile yer dekorları
    /// (saksı, flama, fıçı yığını, bahçe feneri, taş yol, masa altında olmayan halı) dokun-sürükle ile taşınır, 90° döner,
    /// konum <see cref="Layout.GridStep"/> m'lik ızgaraya oturur. Geçersiz yerleşim kırmızı görünür ve bırakılamaz (<see cref="LayoutRules"/>).
    /// <para>
    /// Projede NavMesh yok: müşteri kapıdan masanın toplanma noktasına ve oturağın yaklaşma noktalarına düz çizgilerle yürür.
    /// Taşınan masanın (ve yolu taşınan bir eşyanın içinden geçen masanın) yaklaşma noktaları bölge ızgarasında A* ile yeniden kurulur;
    /// garsonlar ve personel zaten müşteriye / masaya düz yürür (KitchenLayout kapı rotası aynı). Oturaklar, para noktası, kirli bardaklar,
    /// masa üstü mum ve masa altı halı masanın çocuğudur, onunla taşınır; kademe örtüsü ve dolgu ışığı <see cref="TableFollower"/> ile.
    /// </para>
    /// Tezgâh, makineler, mutfak, han, oyun salonu, mahzen, teras, avlu ve iskele taşınmaz.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class EditLayout : MonoBehaviour
    {
        public static EditLayout I { get; private set; }
        /// <summary>Düzenleme modu açık mı (zaman durur, HUD düzenleme çubuğunu gösterir)?</summary>
        public static bool Active => I && I._active;
        /// <summary>Mod açıldı / kapandı.</summary>
        public static event Action<bool> ActiveChanged;
        /// <summary>Son kapanış "Bitti" miydi (HUD bildirimi)?</summary>
        public static bool LastCloseSaved { get; private set; }
        /// <summary>Oyuncuya kısa mesaj (metin, hata mı).</summary>
        public static event Action<string, bool> Message;

        [Tooltip("Taşınabilir nesnenin izi (sarı), seçili (açık), geçersiz (kırmızı), bölge ızgarası. Sahne kurucu atar.")]
        public Material okMat, selMat, badMat, gridMat;

        /// <summary>Düzenlenebilen bölge: kimlik (Management.ZoneOpen) ve iç sınır (dünya XZ; duvarların içi).</summary>
        public struct ZoneDef { public string id; public RectXZ bounds; }

        /// <summary>Bölgeler (TavernaSceneBuilder: Meyhane x −4.1..4.1, Salon 4.1..15.1, Bahçe −15.1..−4.1; ön cephe z −5, arka duvar 5.1).</summary>
        public static readonly ZoneDef[] Zones =
        {
            new() { id = "", bounds = new RectXZ(-3.95f, -4.95f, 3.95f, 4.95f) },
            new() { id = "salon", bounds = new RectXZ(4.25f, -4.95f, 14.95f, 4.95f) },
            new() { id = "bahce", bounds = new RectXZ(-14.95f, -4.95f, -4.25f, 4.95f) },
        };

        /// <summary>Taşınabilir yer dekorları.</summary>
        public static readonly string[] MovableDecor = { "dec_saksi", "dec_flama", "dec_fici", "dec_bahcefener", "dec_tasyol", "dec_hali" };
        /// <summary>Üstünden yürünen düz dekorlar (yalnızca sınır, sabit engel ve birbirleriyle çakışma denetlenir).</summary>
        public static readonly string[] FlatDecor = { "dec_tasyol", "dec_hali" };
        /// <summary>Masanın üstünde / altında duran, masayla birlikte taşınan dekorlar (mum, halı).</summary>
        public static readonly string[] TableDecor = { "dec_mum", "dec_hali" };

        /// <summary>Oturak çevresinin ize eklenen yarıçapı (m) ve iz payı.</summary>
        const float SeatRadius = 0.22f, FootPad = 0.04f;
        /// <summary>Açık tutulan alanlar (yarı boyutlar, m): ön kapı ağzı genişliği / derinliği, toplanma noktası, makine önü, ödeme karesi, iç kapı, sütun.</summary>
        const float DoorHalfWidth = 0.7f, DoorDepth = 1.1f, HubHalf = 0.45f, StationPad = 0.45f, PayZoneHalf = 0.7f, InnerDoorHalf = 0.6f, PillarHalf = 0.3f;
        /// <summary>Müşterinin bölgeye girdiği nokta: ön cepheden içeri (m).</summary>
        const float DoorInside = 0.3f;
        /// <summary>Izgara çizgisi aralığı (m, görsel).</summary>
        const float GridLineStep = 0.5f;
        /// <summary>Kamera odağının düzenlemede kayabildiği alan.</summary>
        static readonly RectXZ CameraArea = new(-15f, -5f, 15f, 5f);
        /// <summary>Geçersiz dönüşün kırmızı yanıp sönme süresi (gerçek sn).</summary>
        const float FlashSeconds = 0.6f;

        /// <summary>Merdiven ağzı (salon sağ arka köşesi; Inn.StairsFoot ve ToStairsPath).</summary>
        static readonly RectXZ StairsArea = new(12.3f, 2.1f, 14.95f, 4.95f);
        /// <summary>İç kapıların bu yandaki ağzı (mutfak, oyun salonu, mahzen, avlu, iskele).</summary>
        static readonly Vector2[] InnerDoors =
        {
            new(KitchenLayout.DoorX, KitchenLayout.WallZ - 0.5f), new(KitchenLayout.GameDoorX, KitchenLayout.WallZ - 0.5f),
            new(KitchenLayout.CellarDoorX, KitchenLayout.WallZ - 0.5f), new(KitchenLayout.SideWallX - 0.5f, KitchenLayout.YardDoorZ),
            new(-KitchenLayout.SideWallX + 0.5f, KitchenLayout.DockDoorZ), new(Inn.StairsFoot.x, Inn.StairsFoot.z),
        };

        class Item
        {
            public string key;
            public int zone;
            public Transform tr;
            public Table table;
            public bool flat;
            public Vector3 defPos;
            public float defYaw;
            public Vector2 localCenter, half;
            public Layout.Pose pose, openPose, lastValid;
            public Transform defHub;
            public Transform[][] defApproach;
            public Vector3[] seatLocal;
            public readonly List<GameObject> custom = new();
            public GameObject quad;
            public bool Alive => tr && tr.gameObject.activeInHierarchy;
            public Layout.Pose DefaultPose => new(defPos.x, defPos.z, 0);
            public bool IsDefault => pose.Same(DefaultPose);
        }

        class ZoneState
        {
            public LayoutGrid grid;
            public readonly List<RectXZ> keepClear = new();
            public readonly List<Vector2> starts = new(), targets = new();
            public GameObject gridQuad;
        }

        readonly List<Item> _items = new();
        readonly ZoneState[] _zones = new ZoneState[Zones.Length];
        readonly Collider[] _buf = new Collider[16];
        readonly HashSet<Collider> _movableColliders = new();
        readonly List<UnityEngine.Object> _gridAssets = new();
        bool _active, _started, _resetAll, _drag, _pan;
        Item _sel;
        PlaceResult _selResult = PlaceResult.Ok;
        float _prevScale = 1f, _flashT;
        Vector2 _grab, _panAnchor;

        /// <summary>Test botu: son denetimin sonucu ve bırakmada geri alınan sayısı.</summary>
        public PlaceResult LastResult { get; private set; } = PlaceResult.Ok;
        public int Rejected { get; private set; }

        void Awake()
        {
            I = this;
            foreach (var t in FindObjectsByType<Table>(FindObjectsInactive.Include)) AddTable(t);
        }

        void OnEnable()
        {
            Table.Activated += OnTableActivated;
            TavernRegistry.DecorPlaced += OnDecorPlaced;
        }

        void OnDisable()
        {
            Table.Activated -= OnTableActivated;
            TavernRegistry.DecorPlaced -= OnDecorPlaced;
            if (_active) { Time.timeScale = _prevScale; CameraFollow.EditView = false; _active = false; }
        }

        void Start()
        {
            // Kayıtlı yerleşim: açık bölgelerdeki etkin masalar (dekorlar sahneye konarken OnDecorPlaced uygular)
            foreach (var it in _items)
                if (it.table && it.Alive && Layout.TryGet(GameState.Data, it.key, out var p)) ApplyPose(it, p);
            _started = true;
            for (int z = 0; z < Zones.Length; z++) RebuildPaths(z);
        }

        // ------------------------------------------------------------------ nesneler
        static int ZoneIndexOf(Vector3 p)
        {
            for (int i = 0; i < Zones.Length; i++) if (Zones[i].bounds.Contains(new Vector2(p.x, p.z))) return i;
            return -1;
        }

        void AddTable(Table t)
        {
            var pos = t.transform.position;
            int zone = ZoneIndexOf(pos);
            if (zone < 0 || Inn.Upstairs(pos)) return;
            var b = MeshBounds(t.transform, out bool any);
            var r = any ? new RectXZ(b.min.x, b.min.z, b.max.x, b.max.z) : RectXZ.Center(pos.x, pos.z, 0.5f, 0.5f);
            var it = new Item
            {
                key = Layout.TableKey(t.name), zone = zone, tr = t.transform, table = t, defPos = pos, defYaw = t.transform.eulerAngles.y,
                defHub = t.hub, defApproach = new Transform[t.seats.Length][], seatLocal = new Vector3[t.seats.Length],
            };
            for (int i = 0; i < t.seats.Length; i++)
            {
                var s = t.seats[i];
                if (!s) continue;
                it.defApproach[i] = s.approach != null ? (Transform[])s.approach.Clone() : new Transform[0];
                it.seatLocal[i] = t.transform.InverseTransformPoint(s.transform.position);
                var sp = s.transform.position;
                r = new RectXZ(Mathf.Min(r.minX, sp.x - SeatRadius), Mathf.Min(r.minZ, sp.z - SeatRadius), Mathf.Max(r.maxX, sp.x + SeatRadius), Mathf.Max(r.maxZ, sp.z + SeatRadius));
            }
            r = r.Expand(FootPad);
            it.localCenter = r.Mid - new Vector2(pos.x, pos.z);
            it.half = new Vector2((r.maxX - r.minX) * 0.5f, (r.maxZ - r.minZ) * 0.5f);
            it.pose = it.openPose = it.lastValid = it.DefaultPose;
            foreach (var c in t.GetComponentsInChildren<Collider>(true)) _movableColliders.Add(c);
            _items.Add(it);
        }

        /// <summary>Görünmeyen (kapalı) nesnede de çalışan dünya sınırı: mesh sınırlarının köşeleri dünyaya çevrilir.</summary>
        static Bounds MeshBounds(Transform root, out bool any)
        {
            any = false;
            var b = new Bounds(root.position, Vector3.zero);
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.sharedMesh) continue;
                var mb = mf.sharedMesh.bounds;
                var m = mf.transform.localToWorldMatrix;
                for (int k = 0; k < 8; k++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1));
                    var w = m.MultiplyPoint3x4(c);
                    if (!any) { b = new Bounds(w, Vector3.zero); any = true; }
                    else b.Encapsulate(w);
                }
            }
            return b;
        }

        Item Find(string key) => _items.Find(x => x.key == key);
        Item TableItem(Table t) => _items.Find(x => x.table == t);

        void OnTableActivated(Table t)
        {
            var it = TableItem(t);
            if (it == null || !_started) return;
            // Yeni alınan masa varsayılan yuvasına gelir; kayıtta yerleşimi varsa (prestij sonrası, bölge yeniden açıldı) oraya
            if (Layout.TryGet(GameState.Data, it.key, out var p)) ApplyPose(it, p);
            RebuildPaths(it.zone);
        }

        void OnDecorPlaced(int slot, GameObject go)
        {
            var reg = TavernRegistry.I;
            if (!reg || slot < 0 || slot >= reg.decorSlots.Length || !go) return;
            var s = reg.decorSlots[slot];
            // Masa üstü mum / masa altı halı: varsayılan yeri bir masanın üstündeyse masaya bağlanır (masa taşınmışsa onunla gelir)
            if (Array.IndexOf(TableDecor, s.item) >= 0)
            {
                foreach (var t in _items)
                {
                    if (!t.table || !t.Alive) continue;
                    var top = RectXZ.Center(t.defPos.x + t.localCenter.x, t.defPos.z + t.localCenter.y, t.half.x - SeatRadius, t.half.y - SeatRadius);
                    if (!top.Contains(new Vector2(s.pos.x, s.pos.z))) continue;
                    var local = s.pos - t.defPos;
                    go.transform.SetParent(t.tr, true);
                    go.transform.SetPositionAndRotation(t.tr.TransformPoint(local), t.tr.rotation * Quaternion.Euler(0f, s.rotY, 0f));
                    return;
                }
            }
            if (Array.IndexOf(MovableDecor, s.item) < 0) return;
            int zone = ZoneIndexOf(s.pos);
            if (zone < 0) return;
            var b = MeshBounds(go.transform, out bool any);
            var r = (any ? new RectXZ(b.min.x, b.min.z, b.max.x, b.max.z) : RectXZ.Center(s.pos.x, s.pos.z, 0.3f, 0.3f)).Expand(FootPad);
            var it = new Item
            {
                key = Layout.DecorKey(slot), zone = zone, tr = go.transform, flat = Array.IndexOf(FlatDecor, s.item) >= 0,
                defPos = s.pos, defYaw = s.rotY, localCenter = r.Mid - new Vector2(s.pos.x, s.pos.z),
                half = new Vector2((r.maxX - r.minX) * 0.5f, (r.maxZ - r.minZ) * 0.5f),
            };
            it.pose = it.openPose = it.lastValid = it.DefaultPose;
            _items.RemoveAll(x => x.key == it.key);
            _items.Add(it);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) _movableColliders.Add(c);
            if (Layout.TryGet(GameState.Data, it.key, out var p)) ApplyPose(it, p);
            if (_active) { it.openPose = it.lastValid = it.pose; MakeQuad(it); }
            if (_started) RebuildPaths(zone);
        }

        // ------------------------------------------------------------------ yerleşim
        static Vector2 Rot(Vector2 v, int q)
        {
            var r = Quaternion.Euler(0f, 90f * q, 0f) * new Vector3(v.x, 0f, v.y);
            return new Vector2(r.x, r.z);
        }

        static RectXZ RectOf(Item it, Layout.Pose p)
        {
            var c = p.Pos + Rot(it.localCenter, p.quarter);
            return p.quarter % 2 == 0 ? RectXZ.Center(c.x, c.y, it.half.x, it.half.y) : RectXZ.Center(c.x, c.y, it.half.y, it.half.x);
        }

        void ApplyPose(Item it, Layout.Pose p)
        {
            if (!it.tr) return;
            Vector3 oldCoin = it.table ? it.table.CoinPos : Vector3.zero;
            var piles = new List<CoinPile>();
            if (it.table)
                foreach (var cp in CoinPile.All)
                    if (cp && DrinkStation.FlatDistance(cp.transform.position, oldCoin) < 0.35f) piles.Add(cp);
            it.tr.SetPositionAndRotation(new Vector3(p.x, it.defPos.y, p.z), Quaternion.Euler(0f, it.defYaw + 90f * p.quarter, 0f));
            it.pose = p;
            if (!it.table) return;
            foreach (var cp in piles) cp.transform.position = it.table.CoinPos;
            foreach (var c in Customer.All)
                if (c && c.Seat && c.Seat.table == it.table) c.OnSeatMoved();
        }

        ZoneState BuildZone(int z)
        {
            var zs = _zones[z] ?? (_zones[z] = new ZoneState());
            var bounds = Zones[z].bounds;
            zs.grid = new LayoutGrid(bounds);
            zs.keepClear.Clear(); zs.starts.Clear(); zs.targets.Clear();
            Physics.SyncTransforms();
            var half = new Vector3(LayoutGrid.Cell * 0.4f, 0.55f, LayoutGrid.Cell * 0.4f);
            zs.grid.FillStatic(p =>
            {
                int n = Physics.OverlapBoxNonAlloc(new Vector3(p.x, 0.75f, p.y), half, _buf, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                for (int k = 0; k < n; k++)
                {
                    var c = _buf[k];
                    if (!c || _movableColliders.Contains(c) || c is CharacterController) continue;
                    if (c.GetComponentInParent<Table>(true)) continue;
                    return true;
                }
                return false;
            });
            var area = bounds.Expand(0.5f);
            void Keep(RectXZ r) { if (r.Overlaps(area)) zs.keepClear.Add(r); }
            // ön kapılar (müşteri girişi) ve toplanma noktaları
            var doors = new HashSet<Transform>();
            var hubs = new HashSet<Transform>();
            foreach (var it in _items)
            {
                if (it.zone != z || !it.table) continue;
                if (it.table.entrance) doors.Add(it.table.entrance);
                if (it.defHub && it.defHub != it.table.entrance) hubs.Add(it.defHub);
            }
            foreach (var d in doors)
            {
                Keep(new RectXZ(d.position.x - DoorHalfWidth, bounds.minZ - 0.1f, d.position.x + DoorHalfWidth, bounds.minZ + DoorDepth));
                zs.starts.Add(new Vector2(d.position.x, bounds.minZ + DoorInside));
            }
            foreach (var h in hubs) { Keep(RectXZ.Center(h.position.x, h.position.z, HubHalf, HubHalf)); if (bounds.Contains(new Vector2(h.position.x, h.position.z))) zs.targets.Add(new Vector2(h.position.x, h.position.z)); }
            foreach (var d in InnerDoors)
            {
                Keep(RectXZ.Center(d.x, d.y, InnerDoorHalf, InnerDoorHalf));
                if (bounds.Contains(d)) zs.targets.Add(d);
            }
            Keep(StairsArea);
            // makineler (alınmamışlar ve sezon tezgâhı dahil) ve önlerindeki servis noktası
            foreach (var st in FindObjectsByType<DrinkStation>(FindObjectsInactive.Include))
            {
                var a = st.transform.position;
                var sp = st.servePoint ? st.servePoint.position : a;
                Keep(new RectXZ(Mathf.Min(a.x, sp.x) - StationPad, Mathf.Min(a.z, sp.z) - StationPad, Mathf.Max(a.x, sp.x) + StationPad, Mathf.Max(a.z, sp.z) + StationPad));
                if (st.isActiveAndEnabled && bounds.Contains(new Vector2(sp.x, sp.z))) zs.targets.Add(new Vector2(sp.x, sp.z));
            }
            // alınmamış ödeme kareleri, "Tavernayı Büyüt" karesi, kademe sütunları
            foreach (var pz in FindObjectsByType<PayZone>(FindObjectsInactive.Include))
                if (!GameState.IsUnlocked(pz.id)) Keep(RectXZ.Center(pz.transform.position.x, pz.transform.position.z, PayZoneHalf, PayZoneHalf));
            foreach (var tz in FindObjectsByType<TierZone>(FindObjectsInactive.Include))
                Keep(RectXZ.Center(tz.transform.position.x, tz.transform.position.z, PayZoneHalf, PayZoneHalf));
            foreach (var ts in FindObjectsByType<TierSet>(FindObjectsInactive.Include))
                foreach (var v in ts.variants)
                    if (v) foreach (Transform ch in v.transform) if (ch.name.Contains("Sutun")) Keep(RectXZ.Center(ch.position.x, ch.position.z, PillarHalf, PillarHalf));
            return zs;
        }

        LayoutRules.Input InputFor(Item it, Layout.Pose p, ZoneState zs)
        {
            var c = new LayoutRules.Input { grid = zs.grid, candidate = RectOf(it, p), flat = it.flat };
            c.keepClear.AddRange(zs.keepClear);
            c.starts.AddRange(zs.starts);
            c.targets.AddRange(zs.targets);
            foreach (var o in _items)
            {
                if (o == it || o.zone != it.zone) continue;
                if (!o.Alive) { if (o.table) c.reserved.Add(RectOf(o, o.DefaultPose)); continue; }
                (o.flat ? c.otherFlats : c.others).Add(RectOf(o, o.pose));
                if (o.table) foreach (var s in o.table.seats) if (s) c.targets.Add(new Vector2(s.transform.position.x, s.transform.position.z));
            }
            if (it.table)
                foreach (var l in it.seatLocal)
                {
                    var w = p.Pos + Rot(new Vector2(l.x, l.z), p.quarter);
                    c.ownTargets.Add(w);
                }
            return c;
        }

        PlaceResult Validate(Item it, Layout.Pose p)
        {
            if (p.Same(it.DefaultPose)) return PlaceResult.Ok;
            var zs = _zones[it.zone] ?? BuildZone(it.zone);
            return LayoutRules.Check(InputFor(it, p, zs));
        }

        /// <summary>
        /// Bölgedeki masaların müşteri yolları: taşınan masa ve yolu taşınmış bir eşyanın içinden geçen masa için kapıdan oturağa
        /// ızgara yolu (A*) kurulur; diğerleri sahne kurucunun yazdığı yaklaşma noktalarını kullanır.
        /// </summary>
        void RebuildPaths(int z)
        {
            var zs = BuildZone(z);
            var foot = new List<RectXZ>();
            var moved = new List<(Item it, RectXZ r)>();
            foreach (var o in _items)
            {
                if (o.zone != z || !o.Alive || o.flat) continue;
                var r = RectOf(o, o.pose);
                foot.Add(r);
                if (!o.IsDefault) moved.Add((o, r.Expand(-0.05f)));
            }
            var blocked = zs.grid.Blocked(foot);
            foreach (var it in _items)
            {
                if (it.zone != z || !it.table) continue;
                var t = it.table;
                foreach (var g in it.custom) if (g) Destroy(g);
                it.custom.Clear();
                t.hub = it.defHub;
                for (int i = 0; i < t.seats.Length; i++) if (t.seats[i]) t.seats[i].approach = it.defApproach[i];
                if (!it.Alive || !t.entrance) continue;
                bool custom = !it.IsDefault;
                if (!custom)
                    foreach (var s in t.seats)
                    {
                        if (!s) continue;
                        var pts = new List<Vector2> { Flat(t.entrance.position) };
                        if (t.hub) pts.Add(Flat(t.hub.position));
                        foreach (var a in s.approach) if (a) pts.Add(Flat(a.position));
                        pts.Add(Flat(s.transform.position));
                        foreach (var (o, r) in moved) if (o != it && LayoutGrid.PolylineCrosses(pts, r)) { custom = true; break; }
                        if (custom) break;
                    }
                if (!custom) continue;
                var start = new Vector2(t.entrance.position.x, Zones[z].bounds.minZ + DoorInside);
                bool ok = true;
                var paths = new List<List<Vector2>>();
                foreach (var s in t.seats)
                {
                    var path = s ? zs.grid.FindPath(blocked, start, Flat(s.transform.position)) : null;
                    if (path == null) { ok = false; break; }
                    paths.Add(path);
                }
                if (!ok) { Debug.LogWarning($"[Düzenle] {t.name}: ızgara yolu bulunamadı, varsayılan yol kullanılıyor."); continue; }
                t.hub = t.entrance;
                for (int i = 0; i < t.seats.Length; i++)
                {
                    var arr = new Transform[paths[i].Count];
                    for (int k = 0; k < arr.Length; k++)
                    {
                        var go = new GameObject($"EditApproach{k}");
                        go.transform.SetParent(t.seats[i].transform, false);
                        go.transform.position = new Vector3(paths[i][k].x, 0f, paths[i][k].y);
                        arr[k] = go.transform;
                        it.custom.Add(go);
                    }
                    t.seats[i].approach = arr;
                }
            }
            foreach (var c in Customer.All)
                if (c && c.Seat && c.Seat.table && ZoneIndexOf(c.Seat.table.transform.position) == z) c.OnSeatMoved();
        }

        static Vector2 Flat(Vector3 p) => new(p.x, p.z);

        // ------------------------------------------------------------------ oturum
        /// <summary>Düzenleme modu açılabilir mi (Sv5, rehber bitti, gün sürüyor, son sahne yok)?</summary>
        public static bool CanOpen => I && !Active && GameState.Level >= Layout.UnlockLevel && !Tutorial.Active && !DayCycle.Ended && !DayCycle.Closing && !Ending.Playing;

        /// <summary>Düzenleme modunu açar: zaman durur, kamera üstten bakar, taşınabilir nesnelerin izi görünür.</summary>
        public void Open()
        {
            if (!CanOpen) return;
            _prevScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            _active = true;
            _resetAll = false;
            _sel = null;
            PlayerController.JoystickInput = Vector2.zero;
            var pl = PlayerController.I;
            var f = pl ? pl.transform.position : Vector3.zero;
            CameraFollow.EditFocus = ClampFocus(Inn.Upstairs(f) ? new Vector3(9f, 0f, 0f) : f);
            CameraFollow.EditView = true;
            int n = 0;
            for (int z = 0; z < Zones.Length; z++)
            {
                if (!Management.ZoneOpen(Zones[z].id)) continue;
                var zs = BuildZone(z);
                zs.gridQuad = GridQuad(Zones[z].bounds);
            }
            foreach (var it in _items)
            {
                it.openPose = it.lastValid = it.pose;
                if (it.Alive && Management.ZoneOpen(Zones[it.zone].id)) { MakeQuad(it); n++; }
            }
            Services.Track("edit_mode_open", ("tasinabilir", n), ("tasinmis", GameState.Data.layout.Count), ("seviye", GameState.Level));
            ActiveChanged?.Invoke(true);
        }

        /// <summary>Kapatır. <paramref name="save"/>: "Bitti" (yerleşim kaydedilir) ya da "İptal" (açılıştaki hâline döner).</summary>
        public void Close(bool save)
        {
            if (!_active) return;
            _drag = _pan = false;
            if (!save) foreach (var it in _items) { if (it.Alive && !it.pose.Same(it.openPose)) ApplyPose(it, it.openPose); }
            else
            {
                var d = GameState.Data;
                if (_resetAll) Layout.Clear(d);
                int moved = 0;
                foreach (var it in _items)
                {
                    if (!it.Alive) continue;
                    if (it.IsDefault) Layout.Remove(d, it.key);
                    else { Layout.Set(d, it.key, it.pose); moved++; }
                }
                d.layoutVersion = Layout.Version;
                GameState.Save();
                Services.Track("edit_mode_save", ("tasinmis", moved), ("sifirlama", _resetAll), ("seviye", GameState.Level));
            }
            for (int z = 0; z < Zones.Length; z++) RebuildPaths(z);
            Unstick();
            if (PlayerController.I) PlayerController.I.EnsureFree();   // v2.4.1: varsayılana dönen eşya da Barlo'nun üstüne gelebilir
            foreach (var it in _items) if (it.quad) { Destroy(it.quad); it.quad = null; }
            foreach (var zs in _zones) if (zs?.gridQuad) { Destroy(zs.gridQuad); zs.gridQuad = null; }
            foreach (var a in _gridAssets) if (a) Destroy(a);
            _gridAssets.Clear();
            _sel = null;
            _active = false;
            LastCloseSaved = save;
            CameraFollow.EditView = false;
            Time.timeScale = _prevScale;
            ActiveChanged?.Invoke(false);
        }

        /// <summary>"Varsayılana dön": açık bölgelerdeki her şey varsayılan yuvasına; "Bitti" ile kaydedilir (kapalı bölgelerin satırları da silinir).</summary>
        public void ResetDefaults()
        {
            if (!_active) return;
            foreach (var it in _items) if (it.Alive) { ApplyPose(it, it.DefaultPose); it.lastValid = it.pose; }
            _resetAll = true;
            _selResult = PlaceResult.Ok;
            Message?.Invoke(Loc.T("Varsayılan yerleşim: kaydetmek için BİTTİ"), false);
        }

        /// <summary>Seçili nesneyi 90° döndürür; yeni yerleşim geçersizse kırmızı yanıp söner ve geri döner.</summary>
        public void Rotate()
        {
            if (!_active || _sel == null) { Message?.Invoke(Loc.T("Önce bir masa ya da dekor seç"), false); return; }
            var p = new Layout.Pose(_sel.pose.x, _sel.pose.z, _sel.pose.quarter + 1);
            var r = Validate(_sel, p);
            LastResult = r;
            if (r == PlaceResult.Ok) { ApplyPose(_sel, p); _sel.lastValid = p; _selResult = r; }
            else { _flashT = FlashSeconds; Rejected++; Message?.Invoke(Reason(r), true); }
        }

        /// <summary>Geçersiz yerleşimin nedeni (oyuncuya).</summary>
        public static string Reason(PlaceResult r) => r switch
        {
            PlaceResult.OutOfBounds => Loc.T("Bölge dışına konamaz"),
            PlaceResult.Blocked => Loc.T("Duvar, tezgâh, makine, kapı ya da yürüme yolu: buraya konamaz"),
            PlaceResult.Overlap => Loc.T("Başka bir eşyayla çakışıyor"),
            PlaceResult.Unreachable => Loc.T("Sandalyeler ya da servis noktaları erişilemez kalır"),
            _ => "",
        };

        /// <summary>Seçili nesne var mı ve adı (HUD).</summary>
        public bool HasSelection => _sel != null;
        /// <summary>Seçili nesnenin şimdiki yerleşimi geçerli mi?</summary>
        public bool SelectionValid => _sel == null || _selResult == PlaceResult.Ok;

        // ------------------------------------------------------------------ dokunuş (HUD ekran koordinatı verir)
        /// <summary>Parmak / fare basıldı: altında taşınabilir nesne varsa seçilir ve sürüklenir, yoksa kamera kaydırılır.</summary>
        public void PointerDown(Vector2 screen)
        {
            if (!_active || !Ground(screen, out var p)) return;
            var hit = Pick(p);
            if (hit != null)
            {
                _sel = hit;
                _selResult = PlaceResult.Ok;
                _drag = true;
                _grab = hit.pose.Pos - p;
                Sfx.Play(SfxId.Pop);
            }
            else { _pan = true; _panAnchor = p; }
        }

        /// <summary>Sürükleme: seçili nesne ızgaraya oturarak taşınır (geçersizse kırmızı) ya da kamera kayar.</summary>
        public void PointerMove(Vector2 screen)
        {
            if (!_active || !Ground(screen, out var p)) return;
            if (_drag && _sel != null)
            {
                var np = new Layout.Pose(Layout.Snap(p.x + _grab.x), Layout.Snap(p.y + _grab.y), _sel.pose.quarter);
                if (np.Same(_sel.pose)) return;
                ApplyPose(_sel, np);
                _selResult = Validate(_sel, np);
                LastResult = _selResult;
            }
            else if (_pan)
            {
                var d = _panAnchor - p;
                CameraFollow.EditFocus = ClampFocus(CameraFollow.EditFocus + new Vector3(d.x, 0f, d.y));
            }
        }

        /// <summary>Bırakma: geçersiz yerleşim bırakılamaz, nesne son geçerli yerine döner.</summary>
        public void PointerUp()
        {
            if (_drag && _sel != null)
            {
                if (_selResult != PlaceResult.Ok)
                {
                    Rejected++;
                    Message?.Invoke(Reason(_selResult), true);
                    ApplyPose(_sel, _sel.lastValid);
                    _selResult = PlaceResult.Ok;
                }
                else _sel.lastValid = _sel.pose;
            }
            _drag = _pan = false;
        }

        static Vector3 ClampFocus(Vector3 f) => new(Mathf.Clamp(f.x, CameraArea.minX, CameraArea.maxX), 0.4f, Mathf.Clamp(f.z, CameraArea.minZ, CameraArea.maxZ));

        static bool Ground(Vector2 screen, out Vector2 p)
        {
            p = default;
            var cam = Camera.main;
            if (!cam) return false;
            var ray = cam.ScreenPointToRay(screen);
            if (Mathf.Abs(ray.direction.y) < 1e-4f) return false;
            float t = -ray.origin.y / ray.direction.y;
            if (t < 0f) return false;
            var w = ray.origin + ray.direction * t;
            p = new Vector2(w.x, w.z);
            return true;
        }

        Item Pick(Vector2 p)
        {
            Item best = null;
            float bestScore = float.MaxValue;
            foreach (var it in _items)
            {
                if (!it.Alive || !it.quad) continue;
                var r = RectOf(it, it.pose).Expand(0.1f);
                if (!r.Contains(p)) continue;
                float score = r.Area + (it.flat ? 100f : 0f) - (it == _sel ? 0.5f : 0f);   // düz eşya en son, küçük önce
                if (score < bestScore) { bestScore = score; best = it; }
            }
            return best;
        }

        /// <summary>Kapanışta Barlo taşınan bir eşyanın içinde kaldıysa en yakın boş kareye alınır.</summary>
        void Unstick()
        {
            var pl = PlayerController.I;
            if (!pl || Inn.Upstairs(pl.transform.position)) return;
            var pp = Flat(pl.transform.position);
            int z = ZoneIndexOf(pl.transform.position);
            if (z < 0) return;
            var foot = new List<RectXZ>();
            bool inside = false;
            foreach (var o in _items)
            {
                if (o.zone != z || !o.Alive || o.flat) continue;
                var r = RectOf(o, o.pose);
                foot.Add(r);
                if (!o.IsDefault && r.Contains(pp)) inside = true;
            }
            if (!inside) return;
            var zs = _zones[z] ?? BuildZone(z);
            var reach = zs.grid.Flood(zs.grid.Blocked(foot), zs.starts);
            float bestD = float.MaxValue;
            Vector2 best = pp;
            for (int j = 0; j < zs.grid.h; j++)
                for (int i = 0; i < zs.grid.w; i++)
                {
                    if (!reach[j * zs.grid.w + i]) continue;
                    var c = zs.grid.CenterOf(i, j);
                    float d = (c - pp).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = c; }
                }
            pl.Teleport(new Vector3(best.x, pl.transform.position.y, best.y));
        }

        // ------------------------------------------------------------------ görsel: iz ve ızgara
        void MakeQuad(Item it)
        {
            if (it.quad) return;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "EditFootprint_" + it.key;
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var rd = q.GetComponent<MeshRenderer>();
            rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rd.receiveShadows = false;
            rd.sharedMaterial = okMat;
            it.quad = q;
        }

        GameObject GridQuad(RectXZ b)
        {
            if (!gridMat) return null;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "EditGrid";
            q.transform.SetPositionAndRotation(new Vector3(b.Mid.x, 0.052f, b.Mid.y), Quaternion.Euler(90f, 0f, 0f));
            q.transform.localScale = new Vector3(b.maxX - b.minX, b.maxZ - b.minZ, 1f);
            var rd = q.GetComponent<MeshRenderer>();
            rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rd.receiveShadows = false;
            var m = new Material(gridMat);
            // Parçacık gölgelendiricisi doku döşemesi (tiling) uygulamıyor: bölge boyunda tek doku, çizgiler içinde
            var tex = MakeGridTexture(b);
            m.SetTexture("_BaseMap", tex);
            _gridAssets.Add(tex);
            _gridAssets.Add(m);
            rd.material = m;
            return q;
        }

        /// <summary>Izgara dokusunun çözünürlüğü (piksel / m) ve çizgi kalınlığı (m).</summary>
        const int GridPixelsPerMeter = 32;
        const float GridLineWidth = 0.035f;

        /// <summary>Bölge boyunda ızgara dokusu: <see cref="GridLineStep"/> m'de bir beyaz çizgi (dünya ızgarasına hizalı).</summary>
        static Texture2D MakeGridTexture(RectXZ b)
        {
            int w = Mathf.CeilToInt((b.maxX - b.minX) * GridPixelsPerMeter), h = Mathf.CeilToInt((b.maxZ - b.minZ) * GridPixelsPerMeter);
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "EditGridTex" };
            var px = new Color32[w * h];
            bool Line(float world)
            {
                float f = Mathf.Repeat(world, GridLineStep);
                return f < GridLineWidth * 0.5f || f > GridLineStep - GridLineWidth * 0.5f;
            }
            for (int y = 0; y < h; y++)
            {
                bool ly = Line(b.minZ + (y + 0.5f) / GridPixelsPerMeter);
                for (int x = 0; x < w; x++)
                    px[y * w + x] = ly || Line(b.minX + (x + 0.5f) / GridPixelsPerMeter) ? new Color32(255, 255, 255, 120) : new Color32(255, 255, 255, 0);
            }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        void Update()
        {
            if (!_active) return;
            if (_flashT > 0f) _flashT -= Time.unscaledDeltaTime;
            foreach (var it in _items)
            {
                if (!it.quad) continue;
                bool show = it.Alive;
                if (it.quad.activeSelf != show) it.quad.SetActive(show);
                if (!show) continue;
                var r = RectOf(it, it.pose);
                it.quad.transform.position = new Vector3(r.Mid.x, it.flat ? 0.075f : 0.065f, r.Mid.y);
                it.quad.transform.localScale = new Vector3(r.maxX - r.minX, r.maxZ - r.minZ, 1f);
                bool sel = it == _sel;
                bool bad = sel && (_selResult != PlaceResult.Ok || _flashT > 0f);
                var mat = bad ? badMat : sel ? selMat : okMat;
                var rd = it.quad.GetComponent<MeshRenderer>();
                if (rd.sharedMaterial != mat) rd.sharedMaterial = mat;
            }
        }

        // ------------------------------------------------------------------ test botu kancaları
        /// <summary>Test: anahtarın şimdiki yerleşimi (yoksa false).</summary>
        public bool DebugPose(string key, out Layout.Pose pose)
        {
            var it = Find(key);
            pose = it != null ? it.pose : default;
            return it != null;
        }

        /// <summary>Test: anahtar varsayılan yerinde mi?</summary>
        public bool DebugIsDefault(string key) => Find(key)?.IsDefault ?? true;

        /// <summary>Test: taşınabilir (sahnede etkin) nesnelerin anahtarları.</summary>
        public List<string> DebugKeys(bool tables)
        {
            var l = new List<string>();
            foreach (var it in _items) if (it.Alive && (it.table != null) == tables && Management.ZoneOpen(Zones[it.zone].id)) l.Add(it.key);
            return l;
        }

        /// <summary>Test: nesnenin masası (dekorsa null).</summary>
        public Table DebugTable(string key) => Find(key)?.table;

        /// <summary>Test: nesnenin bölge indeksi.</summary>
        public int DebugZone(string key) => Find(key)?.zone ?? -1;

        /// <summary>Test: nesneyi seçip yerleşimi dener (sürükleme gibi); <paramref name="release"/> ise bırakır (geçersizse geri döner).</summary>
        public PlaceResult DebugTry(string key, Vector2 pos, int quarter, bool release)
        {
            var it = Find(key);
            if (it == null || !_active) return PlaceResult.OutOfBounds;
            _sel = it;
            _drag = true;
            var p = new Layout.Pose(pos.x, pos.y, quarter);
            ApplyPose(it, p);
            _selResult = Validate(it, p);
            LastResult = _selResult;
            var r = _selResult;
            if (release) PointerUp();
            return r;
        }

        /// <summary>Test: varsayılandan en az <paramref name="minDist"/> uzakta, geçerli ilk yeri arar (ızgarada, varsayılana yakından uzağa).</summary>
        public bool DebugFindValid(string key, float minDist, out Vector2 pos, int quarter = 0)
        {
            pos = default;
            var it = Find(key);
            if (it == null) return false;
            var b = Zones[it.zone].bounds;
            var cands = new List<Vector2>();
            for (float x = Layout.Snap(b.minX); x <= b.maxX; x += 0.5f)
                for (float z = Layout.Snap(b.minZ); z <= b.maxZ; z += 0.5f)
                {
                    var c = new Vector2(x, z);
                    if ((c - it.DefaultPose.Pos).magnitude >= minDist) cands.Add(c);
                }
            cands.Sort((a, c) => (a - it.DefaultPose.Pos).sqrMagnitude.CompareTo((c - it.DefaultPose.Pos).sqrMagnitude));
            foreach (var c in cands)
                if (Validate(it, new Layout.Pose(c.x, c.y, quarter)) == PlaceResult.Ok) { pos = c; return true; }
            return false;
        }

        /// <summary>Test: bölgenin ızgara özeti ve aday yerlerin sonuç dağılımı (neden geçersiz?).</summary>
        public string DebugDiagnose(string key)
        {
            var it = Find(key);
            if (it == null) return "yok";
            var zs = BuildZone(it.zone);
            int stat = 0;
            var names = new HashSet<string>();
            var half = new Vector3(LayoutGrid.Cell * 0.4f, 0.55f, LayoutGrid.Cell * 0.4f);
            for (int j = 0; j < zs.grid.h; j++)
                for (int i = 0; i < zs.grid.w; i++)
                {
                    if (!zs.grid.IsStatic(i, j)) continue;
                    stat++;
                    if (names.Count >= 8) continue;
                    var p = zs.grid.CenterOf(i, j);
                    int n = Physics.OverlapBoxNonAlloc(new Vector3(p.x, 0.75f, p.y), half, _buf, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                    for (int k = 0; k < n; k++) if (_buf[k] && !_movableColliders.Contains(_buf[k])) names.Add(_buf[k].name + "@" + _buf[k].bounds.size.ToString("0.0"));
                }
            var hist = new Dictionary<PlaceResult, int>();
            var b = Zones[it.zone].bounds;
            for (float x = Layout.Snap(b.minX); x <= b.maxX; x += 0.5f)
                for (float z = Layout.Snap(b.minZ); z <= b.maxZ; z += 0.5f)
                {
                    var r = Validate(it, new Layout.Pose(x, z, 0));
                    hist[r] = hist.TryGetValue(r, out var c) ? c + 1 : 1;
                }
            var hs = new List<string>();
            foreach (var kv in hist) hs.Add($"{kv.Key}={kv.Value}");
            return $"bölge {it.zone} ızgara {zs.grid.w}x{zs.grid.h} sabit {stat} · açık alan {zs.keepClear.Count} · giriş {zs.starts.Count} · hedef {zs.targets.Count} · iz {RectOf(it, it.pose)} · sonuç [{string.Join(" ", hs)}] · engel [{string.Join(", ", names)}]";
        }

        /// <summary>Test: kamera odağını bir noktaya alır (görüntü için).</summary>
        public static void DebugFocus(Vector2 p) => CameraFollow.EditFocus = ClampFocus(new Vector3(p.x, 0f, p.y));
    }
}
