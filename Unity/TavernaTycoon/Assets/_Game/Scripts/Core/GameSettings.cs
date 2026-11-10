using UnityEngine;

namespace Taverna
{
    /// <summary>Oyuncu ayarları (GAME_DESIGN.md §10 — Ayarlar ekranı). PlayerPrefs'te saklanır.</summary>
    public static class GameSettings
    {
        public static float Music { get => PlayerPrefs.GetFloat("set_music", 0.7f); set { PlayerPrefs.SetFloat("set_music", value); Apply(); } }
        public static float Sfx { get => PlayerPrefs.GetFloat("set_sfx", 0.85f); set { PlayerPrefs.SetFloat("set_sfx", value); Apply(); } }
        public static bool Vibration { get => PlayerPrefs.GetInt("set_vib", 1) == 1; set => PlayerPrefs.SetInt("set_vib", value ? 1 : 0); }
        public static bool Notifications { get => PlayerPrefs.GetInt("set_notif", 0) == 1; set => PlayerPrefs.SetInt("set_notif", value ? 1 : 0); }
        public static string Language { get => PlayerPrefs.GetString("set_lang", "TR"); set => PlayerPrefs.SetString("set_lang", value); }
        /// <summary>0 Düşük, 1 Orta, 2 Yüksek</summary>
        public static int Graphics { get => PlayerPrefs.GetInt("set_gfx", 1); set { PlayerPrefs.SetInt("set_gfx", value); Apply(); } }

        public static void Apply()
        {
            int count = QualitySettings.names.Length;
            if (count > 0)
            {
                // Proje kalite seviyelerini 3 gruba eşle (Düşük/Orta/Yüksek)
                int target = Mathf.Clamp(Mathf.RoundToInt(Graphics / 2f * (count - 1)), 0, count - 1);
                QualitySettings.SetQualityLevel(target, true);
            }
            Application.targetFrameRate = 60;
        }

        public static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Vibration) Handheld.Vibrate();
#endif
        }
    }
}
