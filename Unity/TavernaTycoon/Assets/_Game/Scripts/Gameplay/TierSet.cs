using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Kademe görünümü (v1.0): variants[i] = i. kademenin parçaları (0 = Ahşap, genelde boş).
    /// Sadece geçerli kademenin grubu görünür; değişince yeni parçalar zıplayarak gelir (PopIn).
    /// </summary>
    public class TierSet : MonoBehaviour
    {
        public GameObject[] variants;
        int _shown = -1;

        void OnEnable() { Tiers.Changed += OnChanged; Show(false); }
        void OnDisable() => Tiers.Changed -= OnChanged;
        void OnChanged() => Show(true);

        void Show(bool animate)
        {
            int t = Tiers.CurrentIndex;
            if (t == _shown) return;
            _shown = t;
            for (int i = 0; i < variants.Length; i++)
            {
                if (!variants[i]) continue;
                bool on = i == t;
                variants[i].SetActive(on);
                if (on && animate)
                    foreach (Transform c in variants[i].transform) c.gameObject.AddComponent<PopIn>();
            }
        }
    }
}
