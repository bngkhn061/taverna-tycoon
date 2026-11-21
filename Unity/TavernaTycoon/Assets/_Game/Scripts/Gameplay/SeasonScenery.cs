using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.2 sezon sahnesi: sezonun süslemesini (kabak ve başak / kar ve çam / çiçek) takvim sürdükçe kendiliğinden gösterir,
    /// sezon tezgâhını (geçici makine, sezon içeceği) sezon oynanabilirken (Sv2, rehber bitince) açar. Sezon dışında hepsi kapalı.
    /// Dizilerin sırası SeasonId − 1: Hasat, Kış, Bahar. Sahne kurucu doldurur.
    /// </summary>
    public class SeasonScenery : MonoBehaviour
    {
        public GameObject[] decorSets = new GameObject[3];
        public GameObject[] stalls = new GameObject[3];

        float _t;
        /// <summary>Durum kaç saniyede bir yeniden okunur.</summary>
        const float CheckSeconds = 1f;

        void Start() => Apply(false);

        void Update()
        {
            _t -= Time.unscaledDeltaTime;
            if (_t > 0f) return;
            _t = CheckSeconds;
            Apply(true);
        }

        void Apply(bool animate)
        {
            var cur = Seasons.Active ? Seasons.Current.id : SeasonId.None;
            bool playable = Seasons.Playable;
            for (int i = 0; i < 3; i++)
            {
                var id = (SeasonId)(i + 1);
                Set(i < decorSets.Length ? decorSets[i] : null, cur == id, animate);
                Set(i < stalls.Length ? stalls[i] : null, cur == id && playable, animate);
            }
        }

        static void Set(GameObject go, bool on, bool animate)
        {
            if (!go || go.activeSelf == on) return;
            go.SetActive(on);
            if (on && animate) go.AddComponent<PopIn>();
        }
    }
}
