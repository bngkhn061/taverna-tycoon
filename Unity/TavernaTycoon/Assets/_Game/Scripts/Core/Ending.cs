using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.1 "Gerçek bir son" (oyuncu testi madde 25, GAME_DESIGN.md §15.2): 3. taverna (şube 2, Dağ Geçidi) Sv30'a ulaşıp
    /// Saray Hanı kademesini alınca "Tavernayı Büyüt" karesi prestij kartının yerine "Krala Davet" kartını açar. Onaylanınca
    /// son sahne (HudController.HudV21 + EndingScene) ve jenerik oynar; sonra serbest oyun: prestij yine açık, "Efsanevi Han" rozeti kalıcı.
    /// Son sahne bir kez kendiliğinden oynar; Ayarlar'dan yeniden izlenebilir.
    /// </summary>
    public static class Ending
    {
        /// <summary>Son için gereken şube (0 = ilk taverna; 2 = üçüncü taverna, Dağ Geçidi).</summary>
        public const int RequiredBranch = 2;
        /// <summary>Son için gereken seviye.</summary>
        public const int RequiredLevel = Balance.MaxLevel;

        /// <summary>Son sahne görüldü mü (kayıtta).</summary>
        public static bool Seen => GameState.Data.endingSeen;

        /// <summary>Şartlar tamam ve son henüz görülmedi: hedef kutusu ve büyüt karesi "Krala Davet" gösterir.</summary>
        public static bool Available => !Seen && Prestige.Branch >= RequiredBranch && GameState.Level >= RequiredLevel && Tiers.Next == null;

        /// <summary>Son sahne ya da jenerik oynuyor (gün akmaz, müşteri gelmez, sabır azalmaz, joystick kapalı).</summary>
        public static bool Playing { get; private set; }

        /// <summary>Barlo büyüt karesine bastı ve son açık (HUD Krala Davet kartını açar).</summary>
        public static event Action ZoneEntered;

        public static void RaiseZoneEntered() => ZoneEntered?.Invoke();

        /// <summary>Son sahne başlar / biter (HUD çağırır).</summary>
        public static void SetPlaying(bool on) => Playing = on;

        /// <summary>
        /// İlk kez görülünce: kayda işlenir, ölçüm olayı <c>game_complete</c> (toplam oyun süresi, toplam gün, şube) gönderilir.
        /// Yeniden izlemede bir şey yapmaz. Dönen değer: ilk kez mi.
        /// </summary>
        public static bool MarkSeen()
        {
            var d = GameState.Data;
            if (d.endingSeen) return false;
            d.endingSeen = true;
            Services.Track("game_complete", ("sure_sn", Mathf.RoundToInt(d.playSeconds)), ("gun", d.daysTotal), ("sube", Prestige.Branch),
                           ("servis", d.customersServed));
            GameState.Save();
            GameState.NotifyChanged();
            return true;
        }
    }
}
