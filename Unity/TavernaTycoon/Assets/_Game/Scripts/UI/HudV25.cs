using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v2.5 cila (GAME_DESIGN.md §19, oyuncu testi 2026-11-23 "ekran çok karışık"): sade HUD.
    /// <list type="bullet">
    /// <item>Sol üst üç satır: para + elmas, mutluluk yüzü + yüzde ve gün saati, küçük araç kutuları (yardımcı, etkinlik, sezon, düzenle;
    ///       simge + kısa yazı). Düzen Hud.uxml + Theme.uss "v2.5" bölümünde.</item>
    /// <item>Aynı anda en fazla bir "YENİ!" rozeti (etkinlik önce, sonra düzenle); rozet ilk dokunuşta kalkar.</item>
    /// <item>Sahnedeki satın alma etiketleri yalnızca sıradaki hedefte ve Barlo'nun <see cref="ZoneNearMeters"/> m yakınında tam;
    ///       uzaktakiler küçük, soluk para simgesi (alınabiliyorsa sarı).</item>
    /// <item>İpucu şeridi kısa sürer ve solar; isteğe bağlı ipuçları yalnızca sakin bir anda (<see cref="CalmForTip"/>).</item>
    /// <item>Son sahnede tabelanın üstünde tavernanın adı görünür (ad yazısı son sahne katmanına taşınır); tabela kameranın önünden
    ///       kaldırıldıysa (<see cref="CinemaOccluders"/>) ad da gizlenir.</item>
    /// </list>
    /// </summary>
    public partial class HudController
    {
        /// <summary>Satın alma etiketi Barlo bu kadar yakınken (m, yatay) tam görünür.</summary>
        public const float ZoneNearMeters = 2.8f;   // ödeme karesine yaklaşınca (meyhane ≈ 8 m: 4.5 m'de hepsi tam kalıyordu)
        /// <summary>İpucu şeridinin solma süresi (sn).</summary>
        const float ToastFadeSeconds = 0.35f;
        /// <summary>Mutluluk bu yüzdenin altındaysa sayı kırmızı.</summary>
        const float HappyLowPercent = 25f;

        Label[] _newBadges;
        bool _endSignOnly;

        /// <summary>Test botu: görünen "YENİ!" rozeti sayısı.</summary>
        public int DebugNewBadgeCount
        {
            get
            {
                int n = 0;
                if (_newBadges != null) foreach (var b in _newBadges) if (b != null && !b.ClassListContains("hidden") && b.parent != null && !b.parent.ClassListContains("hidden")) n++;
                return n;
            }
        }

        /// <summary>Test botu: tam ve küçük (uzak) görünen satın alma etiketi sayısı.</summary>
        public (int full, int far) DebugZoneTagCounts
        {
            get
            {
                int f = 0, s = 0;
                foreach (var kv in _zoneTags)
                {
                    if (kv.Value.box.style.display == DisplayStyle.None) continue;
                    if (kv.Value.box.ClassListContains("zt-far")) s++; else f++;
                }
                return (f, s);
            }
        }

        /// <summary>Test botu: sol üst blok ve araç kutularının panel ölçüsü (genişlik × yükseklik, px).</summary>
        public string DebugLeftLayout
        {
            get
            {
                var tl = _root.Q(className: "hud-top-left");
                var tools = _root.Q("tool-row");
                var parts = new List<string>();
                if (tools != null) foreach (var c in tools.Children()) if (!c.ClassListContains("hidden") && !c.ClassListContains("tut-hidden")) parts.Add($"{c.name} {c.layout.width:0}×{c.layout.height:0}");
                return tl != null ? $"sol blok {tl.layout.width:0}×{tl.layout.height:0} · kutular [{string.Join(", ", parts)}]" : "-";
            }
        }

        /// <summary>Test botu: son sahnenin gizlediği (kameranın önüne giren) nesneler.</summary>
        public string EndingOccluders => _endScene ? _endScene.OccluderInfo : "-";
        /// <summary>Test botu: son sahneyi "yeniden izle" gibi başlatır (her kademede ve şubede çekim denetimi).</summary>
        public void DebugReplayEnding() => StartEnding(true);
        /// <summary>Test botu: ETKİNLİK düğmesine basar (etkinlik paneli açılır, "YENİ!" kalkar).</summary>
        public void DebugEventPress() => OpenEvents();

        void InitV25()
        {
            _newBadges = new[] { _eventNew, _editNew };
        }

        /// <summary>İsteğe bağlı ipucu (yeni özellik, sezon) gösterilebilir mi: açık pencere, şerit, bildirim, yoğun saat yok.</summary>
        bool CalmForTip => !ModalOpen && !AnyPanelOpen && !LevelCardOpen && _toastT <= 0f && _bannerT <= 0f && !RushHour.Active;

        /// <summary>Satın alma etiketi tam mı görünsün: sıradaki hedef ya da Barlo'nun yakınında (aynı katta).</summary>
        bool ZoneTagFull(PayZone z)
        {
            if (z == PayZone.NextGoal()) return true;
            var p = PlayerController.I;
            if (!p) return true;
            var a = p.transform.position;
            var b = z.transform.position;
            if (Inn.Upstairs(a) != Inn.Upstairs(b)) return false;
            a.y = b.y = 0f;
            return (a - b).sqrMagnitude <= ZoneNearMeters * ZoneNearMeters;
        }

        void UpdateV25()
        {
            // Tek "YENİ!": öncelik sırasıyla ilk görünür olan kalır
            bool shown = false;
            foreach (var b in _newBadges)
            {
                if (b == null) continue;
                bool vis = !b.ClassListContains("hidden") && b.parent != null && !b.parent.ClassListContains("hidden");
                if (vis && shown) b.AddToClassList("hidden");
                shown |= vis;
            }
            _happyNum.EnableInClassList("low", Happiness.Value < HappyLowPercent);

            // Son sahne: tabelada tavernanın adı. HUD kökü sahne boyunca kapalı kalır; ad yazısı son sahne katmanının en altına
            // (siyah şeritlerin altına) taşınır, bitince dünya katmanına döner.
            if (_signLabel == null) return;
            bool ending = Ending.EndingPlaying && _endRoot != null;
            if (ending)
            {
                if (_signLabel.parent != _endRoot) { _endRoot.Insert(0, _signLabel); _endSignOnly = true; }
                _signLabel.visible = SignTier.BoardVisible;
            }
            else if (_endSignOnly)
            {
                _endSignOnly = false;
                _world.Add(_signLabel);
                _signLabel.visible = true;
            }
        }
    }
}
