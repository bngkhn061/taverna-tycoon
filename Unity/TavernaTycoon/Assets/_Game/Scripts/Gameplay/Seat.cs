using UnityEngine;

namespace Taverna
{
    /// <summary>Tek bir oturma yeri. approach: masaların etrafından dolaşmak için sıralı ara noktalar.</summary>
    public class Seat : MonoBehaviour
    {
        public Table table;
        public Transform[] approach;
        [System.NonSerialized] public Customer occupant;
        public bool Free => occupant == null && table && table.isActiveAndEnabled && !table.Dirty;
    }
}
