using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v1.1 (oyuncu testi: "avizeden ışık gelmiyor"): avize gibi dekorlara sıcak nokta ışığı.
    /// Gündüz hafif, akşam (Night.IsDark) güçlü yanar; gölge yok (mobilde ucuz).
    /// </summary>
    public class NightLight : MonoBehaviour
    {
        public float dayIntensity = 0.5f, nightIntensity = 2.4f;
        Light _light;

        /// <summary>Verilen dekorun içine (yerel yükseklikte) ışık ekler.</summary>
        public static NightLight Attach(GameObject decor, float height, float range, Color color, float night = 2.4f)
        {
            var go = new GameObject("NightLight");
            go.transform.SetParent(decor.transform, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = range;
            l.color = color;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.Auto;   // çok avize olunca URP en önemlilerini piksel ışığı yapar
            var n = go.AddComponent<NightLight>();
            n.nightIntensity = night;
            return n;
        }

        void OnEnable() => _light = GetComponent<Light>();

        void Update()
        {
            if (!_light) return;
            float target = Night.IsDark ? nightIntensity : dayIntensity;
            _light.intensity = Mathf.MoveTowards(_light.intensity, target, Time.deltaTime * 2f);
        }
    }
}
