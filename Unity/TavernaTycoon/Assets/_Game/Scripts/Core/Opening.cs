using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.4 giriş sineması (GAME_DESIGN.md §18.2): yeni oyunda taverna adı seçildikten sonra bir kez oynar — Barlo yükleriyle şehre
    /// taşınır, harap bir han bulur, Kral'dan izin ister, Kral fermanı verir (fermanda oyuncunun seçtiği ad), Barlo tabelayı asar.
    /// Yalnızca ilk yeni oyunda kendiliğinden oynar (kayıtta ve cihazda işaretlenir); prestij / şube açılışında ve kayıt sıfırlanınca
    /// yeniden oynamaz. Ayarlar › "Girişi yeniden izle". Dünya kısmı <see cref="OpeningScene"/>, arayüz HudV24.
    /// </summary>
    public static class Opening
    {
        /// <summary>Cihazda bir kez görüldü işareti (kayıt sıfırlansa da kalır).</summary>
        public const string PrefsKey = "opening_seen";

        /// <summary>Sinema oynuyor mu (gün akmaz, müşteri gelmez, joystick kapalı; <see cref="Ending.Playing"/> bunu da kapsar).</summary>
        public static bool Playing { get; private set; }

        public static void SetPlaying(bool on) => Playing = on;

        /// <summary>Bu cihazda giriş sineması daha önce oynadı mı.</summary>
        public static bool DeviceSeen => PlayerPrefs.GetInt(PrefsKey, 0) == 1;

        /// <summary>
        /// Kendiliğinden oynasın mı: ana menüden yeni oyun (ad yeni seçildi), kayıtta ve cihazda daha önce görülmemiş, ilk taverna
        /// (prestij şubesi değil) ve henüz kimseye servis yapılmamış.
        /// </summary>
        public static bool ShouldAutoPlay(SaveData d, bool newGameFromMenu, bool deviceSeen) =>
            d != null && newGameFromMenu && !deviceSeen && !d.openingSeen && d.branch == 0 && d.customersServed == 0;

        /// <summary>Kayda ve cihaza "görüldü" yazar.</summary>
        public static void MarkSeen(SaveData d)
        {
            if (d != null) d.openingSeen = true;
            if (AutoTest.Enabled) return;   // otomatik test cihaz işaretini bırakmaz (sahibin Windows kaydı etkilenmesin)
            PlayerPrefs.SetInt(PrefsKey, 1);
            PlayerPrefs.Save();
        }
    }
}
