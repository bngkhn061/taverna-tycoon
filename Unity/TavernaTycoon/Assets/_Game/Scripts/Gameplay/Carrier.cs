using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>Tepside taşınan içecekler (oyuncu ve ileride garsonlar).</summary>
    public class Carrier : MonoBehaviour
    {
        public int capacity = Balance.StartCarryCapacity;
        public Transform stackPoint;
        public float spacing = 0.2f;
        [Tooltip("Sadece bir şey taşınırken görünen tepsi.")]
        public GameObject trayVisual;

        readonly List<(DrinkDef def, GameObject go)> _items = new();

        public int Count => _items.Count;
        public bool HasSpace => _items.Count < capacity;

        // v1.8: döngü (lambda + FindAll her kare liste ve kapanış ayırıyordu; garsonlar ve makineler sık çağırır)
        public bool Has(DrinkId id) => CountOf(id) > 0;
        public int CountOf(DrinkId id)
        {
            int n = 0;
            for (int i = 0; i < _items.Count; i++) if (_items[i].def.id == id) n++;
            return n;
        }

        public void Add(DrinkDef def)
        {
            GameObject go = null;
            if (def.cupPrefab && stackPoint)
            {
                go = Instantiate(def.cupPrefab, stackPoint);
                go.transform.localScale = Vector3.one * 1.25f;
            }
            _items.Add((def, go));
            Layout();
            var a = GetComponentInChildren<ToyAnimator>(); if (a) a.Pop();
        }

        public bool Take(DrinkId id)
        {
            int idx = _items.FindIndex(i => i.def.id == id);
            if (idx < 0) return false;
            if (_items[idx].go) Destroy(_items[idx].go);
            _items.RemoveAt(idx);
            Layout();
            return true;
        }

        void Start() => Layout();

        void Layout()
        {
            if (trayVisual) trayVisual.SetActive(_items.Count > 0);
            // Tepsideki bardaklar yan yana 2'li sıralar, sonra üst üste
            for (int i = 0; i < _items.Count; i++)
            {
                if (!_items[i].go) continue;
                int row = i / 2, col = i % 2;
                _items[i].go.transform.localPosition = new Vector3((col - 0.5f) * spacing, row * spacing * 1.1f, 0f);
                _items[i].go.transform.localRotation = Quaternion.identity;
            }
        }
    }
}
