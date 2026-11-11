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
            if (on && !e.root.activeSelf)
            {
                e.root.SetActive(true);
                if (animate) e.root.AddComponent<PopIn>();
            }
            else if (!on) e.root.SetActive(false);
        }

        bool _concert;

        /// <summary>v1.2 Ozan Konseri: ozan işe alınmamış olsa da konser süresince sahneye çıkar.</summary>
        void Update()
        {
            bool c = TavernEvents.Is(TavernEvents.Id.Konser);
            if (c == _concert) return;
            _concert = c;
            var e = Array.Find(staff, x => x.id == "staff_lir");
            if (e == null || !e.root) return;
            if (c && !e.root.activeSelf) { e.root.SetActive(true); e.root.AddComponent<PopIn>(); }
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
            if (item == "dec_avize") NightLight.Attach(go, 0.95f, 5.5f, new Color(1f, 0.72f, 0.4f));
            else if (item == "dec_kristal") NightLight.Attach(go, 0.95f, 7f, new Color(1f, 0.85f, 0.65f), 3.2f);
            if (animate) go.AddComponent<PopIn>();
            _usedSlots.Add(slot);
        }
    }
}
