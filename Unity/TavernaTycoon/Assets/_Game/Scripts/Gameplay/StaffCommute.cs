using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.0 (oyuncu testi madde 15): personel gün bitince bölgesinin kapısından çıkıp sokakta kaybolur,
    /// "Sonraki Güne Geç" ile sabah sokaktan gelip kapıdan girer ve çalıştığı yere yürür.
    /// Kapanışı DayCycle başlatır (ClosingStarted); plan ekranı açılmadan önce yürümeyi bitiremeyen görevli gizlenir.
    /// Sahne kurucu bölge kapılarını (giriş noktalarını) atar. Yürüyüşü her görevlinin <see cref="Commuter"/> bileşeni yapar.
    /// </summary>
    public class StaffCommute : MonoBehaviour
    {
        public Transform doorMeyhane, doorSalon, doorBahce, doorAvlu, doorIskele;
        /// <summary>Sokağın ortası (z) ve kapıdan sokak boyunca yürünen mesafe (m).</summary>
        public float streetZ = -6.5f, streetWalk = 7f;

        static StaffCommute _i;
        static readonly HashSet<GameObject> Away = new();
        static readonly List<Commuter> Walking = new();

        /// <summary>Görevli gece evinde mi (sahnede gizli, sabah gelecek)?</summary>
        public static bool IsAway(GameObject root) => root && Away.Contains(root);
        /// <summary>Kapıya / işine yürüyen görevli var mı?</summary>
        public static bool Busy => Walking.Count > 0;
        /// <summary>Kapanış: tavernanın içinde (henüz kaldırıma çıkmamış) yürüyen görevli var mı?</summary>
        public static bool AnyInside
        {
            get
            {
                foreach (var c in Walking) if (c && !c.OnStreet) return true;
                return false;
            }
        }
        /// <summary>Evdeki görevli sayısı (test günlüğü).</summary>
        public static int AwayCount => Away.Count;
        /// <summary>Yürüyen görevli sayısı (test günlüğü).</summary>
        public static int WalkingCount => Walking.Count;

        void Awake()
        {
            _i = this;
            Away.Clear();
            Walking.Clear();
        }

        void OnEnable() { DayCycle.ClosingStarted += LeaveAll; DayCycle.DayStarted += OnDayStarted; }
        void OnDisable() { DayCycle.ClosingStarted -= LeaveAll; DayCycle.DayStarted -= OnDayStarted; }
        void OnDayStarted(int day) => ArriveAll();

        internal static void SetWalking(Commuter c, bool on)
        {
            if (on) { if (!Walking.Contains(c)) Walking.Add(c); }
            else Walking.Remove(c);
        }

        internal static void SetAway(GameObject root, bool on)
        {
            if (on) Away.Add(root);
            else Away.Remove(root);
        }

        /// <summary>Kapanış: sahnedeki bütün görevliler kapıya yürür.</summary>
        public static void LeaveAll()
        {
            var reg = TavernRegistry.I;
            if (!_i || !reg) return;
            foreach (var e in reg.staff)
            {
                if (!e.root || !e.root.activeInHierarchy) continue;
                if (!e.root.TryGetComponent(out Commuter c)) c = e.root.AddComponent<Commuter>();
                c.Leave(_i.PathOut(e.root.transform.position));
            }
        }

        /// <summary>Plan ekranı açılmadan: hâlâ yürüyen görevliler sokakta kaybolmuş sayılır.</summary>
        public static void FinishClosing()
        {
            foreach (var c in Walking.ToArray()) if (c) c.Vanish();
            Walking.Clear();
        }

        /// <summary>Sabah: evdeki ve hâlâ çalışan görevliler sokaktan gelip yerlerine yürür.</summary>
        public static void ArriveAll()
        {
            var reg = TavernRegistry.I;
            if (!_i || !reg) return;
            foreach (var e in reg.staff)
            {
                if (!e.root || !Away.Contains(e.root)) continue;
                Away.Remove(e.root);
                if (!Management.Working(e.id) || !e.root.TryGetComponent(out Commuter c)) { e.root.SetActive(false); continue; }
                c.Arrive(_i.PathIn(c.Home));
            }
        }

        Vector3 DoorFor(Vector3 p)
        {
            Transform d;
            if (Inn.Upstairs(p)) d = doorSalon;
            else
                d = Night.ZoneOf(p) switch
                {
                    "avlu" => doorAvlu,
                    "iskele" => doorIskele,
                    "bahce" => doorBahce,
                    "salon" => doorSalon,
                    _ => doorMeyhane,
                };
            if (!d) d = doorMeyhane;
            return d ? d.position : new Vector3(0f, 0f, streetZ + 1f);
        }

        /// <summary>Konumdan sokağa: (üst kattaysa merdiven) → bölge kapısı → kaldırım → sokak boyunca uzaklaşma.</summary>
        List<Vector3> PathOut(Vector3 from)
        {
            var l = new List<Vector3>();
            if (Inn.Upstairs(from))
            {
                if (from.z > InnRoomZ && from.x < InnRoomMaxX) l.Add(new Vector3(from.x, 0f, InnCorridorZ));
                l.Add(Inn.UpHole);
                l.Add(Inn.SalonArrive);   // kat değişince ışınlanır (Commuter)
            }
            var door = DoorFor(from);
            door.y = 0f;
            l.Add(door);
            l.Add(new Vector3(door.x, 0f, streetZ));
            l.Add(new Vector3(door.x + StreetDir(door.x) * streetWalk, 0f, streetZ));
            return l;
        }

        /// <summary>Sokaktan işe: sokağın ucu → kaldırım → bölge kapısı → (üst kattaysa merdiven) → çalıştığı yer.</summary>
        List<Vector3> PathIn(Vector3 home)
        {
            var door = DoorFor(home);
            door.y = 0f;
            var l = new List<Vector3>
            {
                new(door.x + StreetDir(door.x) * streetWalk, 0f, streetZ),
                new(door.x, 0f, streetZ),
                door,
            };
            if (Inn.Upstairs(home))
            {
                l.Add(Inn.StairsFoot);
                l.Add(Inn.UpArrive);   // kat değişince ışınlanır
                if (home.z > InnRoomZ && home.x < InnRoomMaxX) l.Add(new Vector3(home.x, 0f, InnCorridorZ));
            }
            l.Add(new Vector3(home.x, 0f, home.z));
            return l;
        }

        /// <summary>Kapı tavernanın solundaysa sokakta sola, sağındaysa sağa yürür.</summary>
        static float StreetDir(float doorX) => doorX < StreetSplitX ? -1f : 1f;
        const float StreetSplitX = 2f;
        /// <summary>Üst kat odalarının ön sınırı, sağ sınırı ve koridor hizası (Inn / BuildInn ile aynı).</summary>
        const float InnRoomZ = 39f, InnRoomMaxX = 6.1f, InnCorridorZ = 38.4f;
    }
}
