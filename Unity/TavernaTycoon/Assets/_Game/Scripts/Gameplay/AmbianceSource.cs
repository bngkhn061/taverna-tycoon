using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>Dekorun Ambiyans puanı (GAME_DESIGN.md §6.3): müşteri gelme hızını artırır.</summary>
    public class AmbianceSource : MonoBehaviour
    {
        public static readonly List<AmbianceSource> All = new();
        public float value = 5f;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static float Total
        {
            get
            {
                float a = 0;
                foreach (var s in All) a += s.value;
                foreach (var t in Table.All) a += t.ambiance;
                return a + Premium.Ambiance + Tiers.Ambiance;
            }
        }
    }
}
