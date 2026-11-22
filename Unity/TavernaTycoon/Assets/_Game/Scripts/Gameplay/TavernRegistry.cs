using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Sahnedeki satın alınabilir şeylerin listesi (sahne kurucu doldurur):
    /// makine kökleri, personel kökleri, dekor yerleri ve dekor prefabları.
    /// Oyun açılınca kayıttaki satın alımları sahneye uygular.
    /// </summary>
    public class TavernRegistry : MonoBehaviour
    {
        public static TavernRegistry I { get; private set; }

        [Serializable] public class MachineEntry { public DrinkId drink; public GameObject root; }
        [Serializable] public class StaffEntry { public string id; public GameObject root; }
        [Serializable] public class DecorSlot { public string item; public string zone; public Vector3 pos; public float rotY; }
        [Serializable] public class DecorPrefab { public string item; public GameObject prefab; }

        public MachineEntry[] machines;
        public StaffEntry[] staff;
        public DecorSlot[] decorSlots;
        public DecorPrefab[] decorPrefabs;
        public Transform decorRoot;

        readonly System.Collections.Generic.HashSet<int> _usedSlots = new();
        readonly System.Collections.Generic.Dictionary<int, GameObject> _placed = new();

        /// <summary>v2.3: dekor sahneye kondu (yuva, nesne); düzenleme modu kayıtlı yerleşimi uygular. Büyüme efektinden önce çağrılır.</summary>
        public static event Action<int, GameObject> DecorPlaced;

        /// <summary>v2.3: yuvadaki dekor nesnesi (yoksa null).</summary>
        public GameObject Placed(int slot) => _placed.TryGetValue(slot, out var go) ? go : null;

        /// <summary>v2.3: sahnedeki bütün dekorlar (yuva, nesne).</summary>
        public System.Collections.Generic.IReadOnlyDictionary<int, GameObject> AllPlaced => _placed;

        void Awake() => I = this;

        void OnEnable() => GameState.ZoneUnlocked += OnZoneUnlocked;
        void OnDisable() => GameState.ZoneUnlocked -= OnZoneUnlocked;

        /// <summary>v1.1: bölge açılınca o bölgede çalışacak (önceden alınmış) personel işe başlar.</summary>
        void OnZoneUnlocked(string zone)
        {
            foreach (var s in staff) ApplyStaff(s.id, true);
            PlaceSavedDecor(true);   // v1.5: yeni şubede satın alınmış dekor bölgesi açılınca geri gelir
        }

        void Start()
        {
            foreach (var m in machines) ApplyMachine(m.drink, false);
            foreach (var s in staff) ApplyStaff(s.id, false);
            PlaceSavedDecor(false);
        }

        /// <summary>Kayıttaki dekorları yerleştirir; v1.5: bölgesi kapalı olan yer (prestij sonrası) açılana kadar bekler.</summary>
        void PlaceSavedDecor(bool animate)
        {
            foreach (var e in GameState.Data.decor)
            {
                var parts = e.Split(':');
                if (parts.Length != 2 || !int.TryParse(parts[1], out int slot) || slot < 0 || slot >= decorSlots.Length) continue;
                if (!Management.ZoneOpen(decorSlots[slot].zone)) continue;
                PlaceDecor(parts[0], slot, animate);
            }
        }

        /// <summary>Yer kayıtta bir dekora ait mi (yerleşmemiş olsa da)?</summary>
        static bool SavedSlot(int slot)
        {
            string suffix = ":" + slot;
            foreach (var e in GameState.Data.decor) if (e.EndsWith(suffix)) return true;
            return false;
        }

        public void ApplyMachine(DrinkId d, bool animate)
        {
            var e = Array.Find(machines, x => x.drink == d);
            if (e == null || !e.root) return;
            bool on = Management.OwnsMachine(d);
            if (on && !e.root.activeSelf)
            {
                e.root.SetActive(true);
                if (animate) e.root.AddComponent<PopIn>();
            }
            else if (!on) e.root.SetActive(false);
        }

        public void ApplyStaff(string id, bool animate)
        {
            var e = Array.Find(staff, x => x.id == id);
            if (e == null || !e.root) return;
            bool on = Management.Working(id);
            if (on && StaffCommute.IsAway(e.root)) return;   // v2.0: gece evinde; sabah kapıdan gelir
            if (on && !e.root.activeSelf)
            {
                e.root.SetActive(true);
                if (animate) e.root.AddComponent<PopIn>();
            }
            else if (!on) e.root.SetActive(false);
        }

        /// <summary>v2.0: avize ışığının dekor yuvasına göre yüksekliği (m) ve bahçe fenerinin cam hizası.</summary>
        const float AvizeLightHeight = 1.45f, GardenLampLightHeight = 1.45f;
        /// <summary>v2.2: kabak fenerinin ışık hizası (direkli fener).</summary>
        const float KabakLampHeight = 0.95f;

        bool _concert;

        /// <summary>v1.2 Ozan Konseri: ozan işe alınmamış olsa da konser süresince sahneye çıkar.</summary>
        void Update()
        {
            bool c = TavernEvents.Is(TavernEvents.Id.Konser);
            if (c == _concert) return;
            _concert = c;
            var e = Array.Find(staff, x => x.id == "staff_lir");
            if (e == null || !e.root) return;
            if (c && !e.root.activeSelf && !StaffCommute.IsAway(e.root)) { e.root.SetActive(true); e.root.AddComponent<PopIn>(); }
            else if (!c) ApplyStaff("staff_lir", false);
        }

        /// <summary>Bu dekor için bölgesi açık, boş ilk yer (-1: yok).</summary>
        public int FreeSlot(string item)
        {
            for (int i = 0; i < decorSlots.Length; i++)
            {
                var s = decorSlots[i];
                if (s.item != item || _usedSlots.Contains(i) || SavedSlot(i)) continue;
                if (!Management.ZoneOpen(s.zone)) continue;
                return i;
            }
            return -1;
        }

        public void PlaceDecor(string item, int slot, bool animate)
        {
            if (slot < 0 || slot >= decorSlots.Length || _usedSlots.Contains(slot)) return;
            var pf = Array.Find(decorPrefabs, p => p.item == item);
            if (pf == null || !pf.prefab) return;
            var s = decorSlots[slot];
            var go = Instantiate(pf.prefab, s.pos, Quaternion.Euler(0f, s.rotY, 0f), decorRoot);
            go.AddComponent<AmbianceSource>().value = Catalog.FindDecor(item).ambiance;
            // v1.1: avizeler gerçekten aydınlatsın (mumların hizasında sıcak ışık, akşam güçlenir)
            // v2.0 (madde 14): avizeler küçülüp yükseldi (gövde yuvanın 1.5–1.9 m üstü); ışık gövdenin hizasında, menzil büyüdü
            if (item == "dec_avize") NightLight.Attach(go, AvizeLightHeight, 6.5f, new Color(1f, 0.72f, 0.4f), 2.8f);
            else if (item == "dec_kristal") NightLight.Attach(go, AvizeLightHeight, 8f, new Color(1f, 0.85f, 0.65f), 3.6f);
            // v2.0 (madde 21): bahçe feneri gece kendiliğinden yanar (Lantern değil: yakılması gerekmez, bölge fener sayımına girmez)
            else if (item == "dec_kabakfener") NightLight.Attach(go, KabakLampHeight, 4.5f, new Color(1f, 0.62f, 0.25f), 2.6f).dayIntensity = 0f;   // v2.2
            else if (item == "dec_bahcefener") NightLight.Attach(go, GardenLampLightHeight, 6f, new Color(1f, 0.74f, 0.42f), 3.4f).dayIntensity = 0f;
            _usedSlots.Add(slot);
            _placed[slot] = go;
            DecorPlaced?.Invoke(slot, go);   // v2.3 düzenleme modu (ölçüler tam boyutta okunsun diye büyüme efektinden önce)
            if (animate) go.AddComponent<PopIn>();
        }
    }
}
