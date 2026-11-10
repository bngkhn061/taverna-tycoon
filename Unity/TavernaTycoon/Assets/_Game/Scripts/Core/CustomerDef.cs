using UnityEngine;

namespace Taverna
{
    /// <summary>Bir müşteri tipinin tasarım verisi (CHARACTERS.md §B).</summary>
    [CreateAssetMenu(menuName = "Taverna/Müşteri")]
    public class CustomerDef : ScriptableObject
    {
        public int number;
        public string displayName;
        public GameObject modelPrefab;
        public DrinkId[] drinks = { DrinkId.Bira };
        [Range(1, 5)] public int patienceStars = 3;
        [Range(0, 5)] public int payStars = 1;
        public int unlockLevel = 1;
        [Tooltip("Aynı oturuşta kaç içecek ister (Cüce: 2).")]
        public int ordersPerVisit = 1;
        public float spawnWeight = 1f;
    }
}
