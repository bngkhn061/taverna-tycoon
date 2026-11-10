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

        void Start()
        {
            foreach (var m in machines) ApplyMachine(m.drink, false);
            foreach (var s in staff) ApplyStaff(s.id, false);
            foreach (var e in GameState.Data.decor)
            {
                var parts = e.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int slot)) PlaceDecor(parts[0], slot, false);
            }
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
            bool on = Management.Hired(id);
            if (on && !e.root.activeSelf)
            {
                e.root.SetActive(true);
                if (animate) e.root.AddComponent<PopIn>();
            }
            else if (!on) e.root.SetActive(false);
        }

        /// <summary>Bu dekor için bölgesi açık, boş ilk yer (-1: yok).</summary>
        public int FreeSlot(string item)
        {
            for (int i = 0; i < decorSlots.Length; i++)
            {
                var s = decorSlots[i];
                if (s.item != item || _usedSlots.Contains(i)) continue;
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
            if (animate) go.AddComponent<PopIn>();
            _usedSlots.Add(slot);
        }
    }
}
