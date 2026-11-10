using UnityEngine;

namespace Taverna
{
    /// <summary>Ateş ve meşale ışığı titremesi (Perlin gürültüsü ile yumuşak, rastgele).</summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        /// <summary>Taban yoğunluğun yüzde kaçı kadar oynasın (0.25 = ±%25).</summary>
        public float amount = 0.25f;
        public float speed = 6f;

        Light _light;
        float _base, _seed;

        void Awake()
        {
            _light = GetComponent<Light>();
            _base = _light.intensity;
            _seed = Random.value * 100f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(_seed, Time.time * speed) * 2f - 1f;
            _light.intensity = _base * (1f + n * amount);
        }
    }
}
