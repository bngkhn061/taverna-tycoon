using UnityEngine;

namespace Taverna
{
    /// <summary>Bir içeceğin tüm tasarım verisi (GAME_DESIGN.md §4).</summary>
    [CreateAssetMenu(menuName = "Taverna/İçecek")]
    public class DrinkDef : ScriptableObject
    {
        public DrinkId id;
        public string displayName;
        public int basePrice = 3;
        public float baseTime = 2f;
        public int unlockLevel = 1;
        public Sprite icon;
        public GameObject cupPrefab;
        [Tooltip("Yükseltme taban fiyatları pahalı içeceklerde çarpılır (×1, ×4, ×10).")]
        public float upgradeCostMultiplier = 1f;
        [Tooltip("Yemek mi? (mutfak makinesinde pişer, Aşçı hızlandırır, müşteri ara sıra ister)")]
        public bool isFood;
    }
}
