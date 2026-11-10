using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Gün boyu ışık (v0.6): güneş rengi/yoğunluğu/açısı, ortam ışığı ve kamera arka planı DayCycle'a göre akar.
    /// Gündüz → akşam (turuncu) → gece (koyu mavi) → şafak (son %6). Renkler DESIGN_RULES §5 tonlarından türetildi.
    /// </summary>
    public class DayNightLighting : MonoBehaviour
    {
        public Light sun;
        public Camera cam;

        struct Look { public Color sun, sky, equator, ground, bg; public float intensity, pitch; }

        static readonly Look Day = new()
        {
            sun = new Color32(0xFF, 0xE2, 0xB8, 255), intensity = 1.3f, pitch = 52f,
            sky = new Color32(0xA8, 0x98, 0xB4, 255), equator = new Color32(0x6A, 0x5A, 0x7A, 255), ground = new Color32(0x3A, 0x2A, 0x2A, 255),
            bg = new Color32(0x2A, 0x1F, 0x1A, 255),
        };
        static readonly Look Evening = new()
        {
            sun = new Color32(0xFF, 0x9A, 0x5A, 255), intensity = 0.9f, pitch = 28f,
            sky = new Color32(0x9A, 0x74, 0x8A, 255), equator = new Color32(0x5E, 0x48, 0x60, 255), ground = new Color32(0x30, 0x22, 0x24, 255),
            bg = new Color32(0x2A, 0x1A, 0x22, 255),
        };
        static readonly Look NightLook = new()
        {
            sun = new Color32(0x6F, 0x7F, 0xC8, 255), intensity = 0.35f, pitch = 60f,
            sky = new Color32(0x3A, 0x3F, 0x6A, 255), equator = new Color32(0x2E, 0x2A, 0x4A, 255), ground = new Color32(0x1A, 0x16, 0x20, 255),
            bg = new Color32(0x14, 0x16, 0x26, 255),
        };

        const float DawnSeconds = 2.5f;

        void LateUpdate()
        {
            float p = DayCycle.Progress01;
            Look a, b;
            float t;
            if (p < 0.52f) { a = Day; b = Day; t = 0f; }
            else if (p < Night.EveningAt + 0.04f) { a = Day; b = Evening; t = Mathf.InverseLerp(0.52f, Night.EveningAt + 0.04f, p); }
            else if (p < Night.NightAt + 0.06f) { a = Evening; b = NightLook; t = Mathf.InverseLerp(Night.EveningAt + 0.04f, Night.NightAt + 0.06f, p); }
            else { a = NightLook; b = NightLook; t = 0f; }
            // v0.9: gece plan ekranına kadar sürer; "Sonraki Güne Geç" sonrası 2.5 sn şafak geçişi
            if (DayCycle.SinceDawn < DawnSeconds && p < 0.52f) { a = NightLook; b = Day; t = DayCycle.SinceDawn / DawnSeconds; }
            t = t * t * (3f - 2f * t);
            if (sun)
            {
                sun.color = Color.Lerp(a.sun, b.sun, t);
                sun.intensity = Mathf.Lerp(a.intensity, b.intensity, t);
                sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(a.pitch, b.pitch, t), -30f, 0f);
            }
            RenderSettings.ambientSkyColor = Color.Lerp(a.sky, b.sky, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(a.equator, b.equator, t);
            RenderSettings.ambientGroundColor = Color.Lerp(a.ground, b.ground, t);
            if (cam) cam.backgroundColor = Color.Lerp(a.bg, b.bg, t);
        }
    }
}
