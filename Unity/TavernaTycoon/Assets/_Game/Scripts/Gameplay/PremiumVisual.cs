using UnityEngine;

namespace Taverna
{
    /// <summary>Hazine ürünü alınmışsa hedef nesneyi gösterir (v1.0: Kraliyet tabela çerçevesi).</summary>
    public class PremiumVisual : MonoBehaviour
    {
        public string productId;
        public GameObject target;

        void OnEnable() { Premium.Changed += Refresh; Refresh(); }
        void OnDisable() => Premium.Changed -= Refresh;

        void Refresh()
        {
            if (!target) return;
            bool on = Premium.Owned(productId);
            if (target.activeSelf != on)
            {
                target.SetActive(on);
                if (on && Time.timeSinceLevelLoad > 1f) target.AddComponent<PopIn>();
            }
        }
    }
}
