using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Bir grup zemin/duvar parçasının tema varyantları: her tema için hazır bir alt grup,
    /// seçili tema (Themes.Current) görünür, diğerleri kapalı. Sahne kurucu ekler.
    /// Ayrıca seçili temaların ambiyansını bir AmbianceSource üzerinden verir (sadece birincil set).
    /// </summary>
    public class ThemeSet : MonoBehaviour
    {
        public Themes.Kind kind;
        public string[] ids;
        public GameObject[] variants;
        /// <summary>Ambiyansı bu set mi bildirsin (sahnede bir tane).</summary>
        public AmbianceSource ambiance;

        void OnEnable()
        {
            Themes.Changed += Apply;
            Apply();
        }

        void OnDisable() => Themes.Changed -= Apply;

        public void Apply()
        {
            string cur = Themes.Current(kind);
            for (int i = 0; i < variants.Length; i++)
                if (variants[i]) variants[i].SetActive(ids[i] == cur);
            if (ambiance) ambiance.value = Themes.Ambiance;
        }
    }
}
