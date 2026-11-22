using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v1.9 arayüz eklemeleri (oyuncu testi 2026-11-17, production/qa/playtests/feedback-2026-11-17.md Paket 1):
    /// zorunlu rehberde hedef dışı düğme kilidi (madde 11), yıldız / ambiyans açıklama kartı (22), dükkânda "YENİ!" rozetleri ve
    /// seviye kartında o seviyede açılan her şey (6, 12), otomasyon kartında altın + elmas fiyatı (9, 16), ad değiştirmede elmas.
    /// </summary>
    public partial class HudController
    {
        VisualElement _starCard;
        Label _starCardText;
        float _starCardT, _gateToastT;
        /// <summary>Dükkân bu açılışta "YENİ!" gösterdiği ürünler (açık kaldıkça rozet durur; kapanınca temizlenir).</summary>
        readonly HashSet<string> _newSession = new();
        readonly Dictionary<string, Label> _tabNew = new();
        bool _seenDirty;
        Button _renameGems;

        /// <summary>Test botu: rehber kilidinin geri çevirdiği dokunuş sayısı.</summary>
        public static int DebugGateBlocked;

        static readonly string[] ShopTabs = { "mac", "staff", "dec", "auto" };
        /// <summary>Yıldız kartının yenilenme aralığı (sn).</summary>
        const float StarCardRefreshSeconds = 0.5f;
        /// <summary>Seviye kartında en fazla bu kadar açılan gösterilir (fazlası "+N").</summary>
        const int MaxUnlockCards = 10;
        /// <summary>Bu sayıdan fazla açılan varsa kartlar küçülür (iki satır).</summary>
        const int CompactUnlockCards = 4;

        void InitV19()
        {
            // --- zorunlu rehber: hedef dışındaki HUD düğmeleri ve dükkân sekmeleri dokunuşu almaz
            _root.RegisterCallback<PointerDownEvent>(Gate, TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerUpEvent>(Gate, TrickleDown.TrickleDown);
            _root.RegisterCallback<ClickEvent>(Gate, TrickleDown.TrickleDown);
            _root.RegisterCallback<NavigationSubmitEvent>(Gate, TrickleDown.TrickleDown);

            // --- yıldızlar: dokununca ambiyans kartı
            _starsRow.AddManipulator(new Clickable(ToggleStarCard));
            _starCard = new VisualElement { pickingMode = PickingMode.Ignore };
            _starCard.AddToClassList("happy-card");
            _starCard.AddToClassList("v3-panel");
            _starCard.AddToClassList("star-card");
            _starCard.AddToClassList("hidden");
            var title = new Label(Loc.T("Ambiyans ve yıldızlar")) { pickingMode = PickingMode.Ignore };
            title.AddToClassList("happy-card-title");
            _starCardText = new Label { pickingMode = PickingMode.Ignore };
            _starCardText.AddToClassList("happy-card-text");
            _starCard.Add(title);
            _starCard.Add(_starCardText);
            _root.Q("hud-root").Add(_starCard);

            // --- dükkân sekmelerinde "YENİ!"
            foreach (var id in ShopTabs)
            {
                var tab = _root.Q<Button>("tab-" + id);
                if (tab == null) continue;
                var l = new Label(Loc.T("YENİ!")) { pickingMode = PickingMode.Ignore };
                l.AddToClassList("tab-new");
                l.AddToClassList("hidden");
                tab.Add(l);
                _tabNew[id] = l;
            }

            // --- ad değiştir: elmasla üçüncü seçenek
            var coinsBtn = _root.Q<Button>("rename-coins");
            _renameGems = new Button { name = "rename-gems" };
            _renameGems.AddToClassList("btn");
            _renameGems.AddToClassList("btn-teal");
            var gi = new VisualElement();
            gi.AddToClassList("btn-icon");
            gi.style.backgroundImage = new StyleBackground(GemIcon);
            _renameGems.Add(gi);
            _renameGems.Add(new Label(Balance.RenameGems.ToString()));
            coinsBtn.parent.Insert(coinsBtn.parent.IndexOf(coinsBtn) + 1, _renameGems);
            _renameGems.clicked += () =>
            {
                if (!ValidName(out var n)) return;
                if (GameState.Data.gems < Balance.RenameGems) { Toast(Loc.T("Yeterli elmas yok")); return; }
                GameState.Data.gems -= Balance.RenameGems;
                Services.Track("rename", ("para", "elmas"), ("bedel", Balance.RenameGems));
                ApplyName(n);
            };
        }

        void UpdateV19()
        {
            // Rehber kilidi görünür olsun: kilitli düğmeler soluk
            bool tut = Tutorial.Active;
            foreach (var n in new[] { "open-quests", "open-shop", "open-staff", "open-depot", "open-shop-plus", "happy-pill", "helper-btn", "event-btn", "upgrade-cta" })
                _root.Q(n)?.EnableInClassList("tut-locked", tut && !Tutorial.AllowsUi(n));
            foreach (var id in ShopTabs)
                _root.Q("tab-" + id)?.EnableInClassList("tut-locked", tut && !Tutorial.AllowsUi("tab-" + id));
            _root.Q("tab-prem")?.EnableInClassList("tut-locked", tut);
            _root.Q("tab-boost")?.EnableInClassList("tut-locked", tut);
            _starsRow.EnableInClassList("tut-locked", tut);

            if (!_starCard.ClassListContains("hidden"))
            {
                _starCardT -= Time.unscaledDeltaTime;
                if (_starCardT <= 0f) { _starCardT = StarCardRefreshSeconds; RefreshStarCard(); }
            }
            if (_seenDirty) { _seenDirty = false; GameState.Save(); }
        }

        // ------------------------------------------------------------------ rehber kilidi (madde 11)
        void Gate(EventBase e)
        {
            if (!Tutorial.Active) return;
            for (var v = e.target as VisualElement; v != null && v != _root; v = v.parent)
            {
                if (string.IsNullOrEmpty(v.name)) continue;
                if (!Tutorial.IsGatedUi(v.name)) { if (v is Button) return; continue; }
                if (Tutorial.AllowsUi(v.name)) return;
                e.StopImmediatePropagation();
                if (e is PointerDownEvent or NavigationSubmitEvent)
                {
                    DebugGateBlocked++;
                    if (Time.unscaledTime > _gateToastT)
                    {
                        _gateToastT = Time.unscaledTime + 1.5f;
                        Toast(Loc.T("Önce rehberdeki adımı yap"));
                        Sfx.Play(SfxId.Error);
                    }
                }
                return;
            }
        }

        /// <summary>
        /// Test botu: düğmeye gerçek bir "gönder" olayı yollar (rehber kilidinden geçer). Dönen değer: kilit izin veriyor muydu.
        /// </summary>
        public bool DebugClick(string name)
        {
            var b = _root.Q<Button>(name);
            if (b == null) return false;
            bool allowed = Tutorial.AllowsUi(name);
            using (var e = NavigationSubmitEvent.GetPooled())
            {
                e.target = b;
                b.SendEvent(e);
            }
            return allowed;
        }

        /// <summary>Test botu: Hazine / başlangıç paketi kartı açık mı; kapat.</summary>
        public bool DebugModalOpen => PremModalOpen;
        public void DebugClosePremModal() { if (PremModalOpen) _premModal.RemoveFromHierarchy(); }

        /// <summary>Test botu: açık dükkân sekmesi (boş = dükkân kapalı).</summary>
        public string DebugShopTab => _shopRoot.ClassListContains("hidden") ? "" : _shopTab;
        /// <summary>Test botu: görev / depo paneli açık mı?</summary>
        public bool DebugQuestsOpen => !_questsPanel.ClassListContains("hidden");
        public bool DebugDepotOpen => !_depotPanel.ClassListContains("hidden");
        /// <summary>Test botu: seviye kartındaki açılan kartlarının etiketleri ve adları.</summary>
        public string DebugUnlocks
        {
            get
            {
                var parts = new List<string>();
                foreach (var c in _unlocks.Children())
                {
                    var labels = c.Query<Label>().ToList();
                    parts.Add(string.Join("/", labels.ConvertAll(l => l.text)));
                }
                return string.Join(" | ", parts);
            }
        }

        /// <summary>Rehberde halkanın gösterdiği düğme: dükkân açıkken hedef yan düğme kapalı kalır, dükkân içindeki karşılığı gösterilir.</summary>
        string TutorialFocus()
        {
            var t = Tutorial.UiTarget;
            if (t == null || _shopRoot.ClassListContains("hidden")) return t;
            if (Tutorial.Step == Tutorial.StepStaff) return "tab-staff";
            return "shop-close";
        }

        // ------------------------------------------------------------------ yıldız kartı (madde 22)
        void ToggleStarCard()
        {
            Sfx.Play(SfxId.Pop);
            _starCard.ToggleInClassList("hidden");
            if (!_starCard.ClassListContains("hidden")) { _happyCard.AddToClassList("hidden"); RefreshStarCard(); }
        }

        /// <summary>Test botu: yıldız kartını aç / kapat.</summary>
        public void DebugStarCard(bool on)
        {
            _starCard.EnableInClassList("hidden", !on);
            if (on) RefreshStarCard();
        }

        void RefreshStarCard()
        {
            string next = Management.NextStar == 0
                ? Loc.T("En yüksek yıldızdasın (★5)")
                : Loc.F("★{0} için {1} ambiyans daha (Dükkân · Dekor)", Management.NextStar, Management.AmbianceToNextStar);
            _starCardText.text =
                Loc.F("Ambiyans {0:0} · şu an ★{1:0.#}\n", AmbianceSource.Total, Management.Stars) +
                Loc.T("Ambiyans masalardan, dekordan, zemin / duvar temasından ve ozan sahnesinden gelir.\n") +
                Loc.F("★4 ve üstü zengin müşteri getirir: daha çok öder, servis başına +{0} Ün\n", Balance.RichUn) +
                Loc.F("Bahşiş: yıldız başına +%{0} · şu an ×{1:0.0#}\n", Mathf.RoundToInt(Balance.TipPerStar * 100f), Management.TipFromStars) +
                next;
        }

        // ------------------------------------------------------------------ "YENİ!" rozetleri (madde 12)
        /// <summary>Ürün görülmemiş ve seviyesi yeni açılmış mı?</summary>
        static bool Unseen(string key, int level) =>
            GameState.Level >= level && level > GameState.Data.seenLevel && !GameState.Data.seenItems.Contains(key);

        /// <summary>Kart kurulurken çağrılır: görülmemişse görüldü işaretler; bu açılışta "YENİ!" gösterilecek mi?</summary>
        bool NewBadge(string key, int level)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (Unseen(key, level))
            {
                GameState.Data.seenItems.Add(key);
                _newSession.Add(key);
                _seenDirty = true;
            }
            return _newSession.Contains(key);
        }

        static IEnumerable<(string key, int level)> TabItems(string tab)
        {
            switch (tab)
            {
                case "mac":
                    foreach (var m in Catalog.Machines) if (m.drink != DrinkId.Bira) yield return (Catalog.MachineKey(m.drink), Management.MachineLevel(m.drink));
                    break;
                case "staff":
                    foreach (var s in Catalog.StaffList) if (!s.premium && !Management.Hired(s.id)) yield return (s.id, s.level);   // işe alınan görevli yeni sayılmaz
                    // v2.1: Barlo'nun yetenek seviyeleri (alınmış olanlar yeni sayılmaz)
                    foreach (var b in BarloSkills.All)
                        for (int n = BarloSkills.Level(b) + 1; n <= BarloSkills.MaxLevel; n++) yield return (BarloSkills.Key(b, n), BarloSkills.UnlockLevel(b, n));
                    break;
                case "dec":
                    foreach (var d in Catalog.DecorList) if (!d.premium) yield return (d.id, d.level);
                    foreach (var t in Themes.List) if (!t.premium) yield return (t.id, t.level);
                    break;
                case "auto":
                    foreach (var a in Automation.List) yield return ("auto_" + a.id, Mathf.Min(a.level, a.gemLevel));
                    break;
            }
        }

        static bool TabHasNew(string tab)
        {
            foreach (var (k, lv) in TabItems(tab)) if (Unseen(k, lv)) return true;
            return false;
        }

        /// <summary>Sekme rozetleri (BuildShop sonunda): açık sekmenin ürünleri görüldü sayıldığı için onun rozeti kalkar.</summary>
        void RefreshTabBadges()
        {
            foreach (var id in ShopTabs)
                if (_tabNew.TryGetValue(id, out var l)) l.EnableInClassList("hidden", !TabHasNew(id));
        }

        static void AddNewBadge(VisualElement item)
        {
            var l = new Label(Loc.T("YENİ!")) { pickingMode = PickingMode.Ignore };
            l.AddToClassList("item-new");
            item.Add(l);
        }

        // ------------------------------------------------------------------ otomasyon kartı: altın ya da elmas (madde 9, 16)
        void AddAutomationItem(Automation.Def d)
        {
            var item = new VisualElement();
            item.AddToClassList("item");
            bool owned = Automation.Has(d.id);
            item.EnableInClassList("owned", owned);
            var img = new VisualElement();
            img.AddToClassList("item-img");
            img.style.backgroundImage = new StyleBackground(KitIcon(d.icon));
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var n = new Label(Loc.T(d.name));
            n.AddToClassList("item-name");
            string sub = owned ? Loc.F("Açık · {0}", Loc.T(d.desc))
                : !Management.ZoneOpen(d.zone) ? Loc.F("{0} · önce {1} açılmalı", Loc.T(d.desc), Loc.T(d.zoneName))
                : Loc.F("{0} · altınla Sv {1}, elmasla Sv {2}", Loc.T(d.desc), d.level, d.gemLevel);
            var s = new Label(sub);
            s.AddToClassList("item-sub");
            col.Add(n); col.Add(s);
            item.Add(img); item.Add(col);
            var btns = new VisualElement();
            btns.AddToClassList("staff-btns");
            if (owned)
            {
                var b = PriceButton("btn-teal", KitIcon("check"), out var t);
                b.AddToClassList("owned");
                t.text = Loc.T("VAR");
                b.SetEnabled(false);
                btns.Add(b);
            }
            else
            {
                btns.Add(AutoButton(d, false));
                btns.Add(AutoButton(d, true));
            }
            item.Add(btns);
            if (NewBadge("auto_" + d.id, Mathf.Min(d.level, d.gemLevel))) AddNewBadge(item);
            _shopScroll.Add(item);
        }

        Button AutoButton(Automation.Def d, bool gems)
        {
            var state = gems ? Automation.CanBuyWithGems(d) : Automation.CanBuy(d);
            int lv = gems ? d.gemLevel : d.level;
            bool locked = state is Management.BuyResult.LevelLow or Management.BuyResult.ZoneLocked;
            var b = PriceButton(locked ? "btn-paper" : gems ? "btn-plum" : "btn-teal", locked ? KitIcon("lock") : gems ? GemIcon : CoinIcon, out var t);
            b.AddToClassList("btn-sm");
            t.text = state == Management.BuyResult.LevelLow ? Loc.F("SV {0}", lv) : state == Management.BuyResult.ZoneLocked ? Loc.T("KİLİTLİ")
                   : gems ? d.gems.ToString() : FormatNum(d.price);
            b.EnableInClassList("locked", state != Management.BuyResult.Ok);
            b.clicked += () =>
            {
                var r = gems ? Automation.BuyWithGems(d) : Automation.Buy(d);
                if (r == Management.BuyResult.Ok) { Sfx.Play(SfxId.Buy); Confetti(16); GameSettings.Vibrate(); }
                else Toast(gems && r == Management.BuyResult.NoMoney ? Loc.T("Yeterli elmas yok") : Management.BuyMessage(r, lv, Loc.T(d.zoneName)));
                BuildShop();
                RefreshTop();
            };
            return b;
        }

        // ------------------------------------------------------------------ seviye kartı: açılanlar (madde 6, 12)
        /// <summary>Bu seviyede açılan her şey: (etiket, ikon, ad). Ödül en başta.</summary>
        List<(string tag, Sprite icon, string name)> LevelUnlocks(int level)
        {
            var list = new List<(string, Sprite, string)> { (Loc.T("ödül"), GemIcon, Loc.F("+{0} Elmas", Boosts.LevelGemReward(level))) };
            foreach (var m in Catalog.Machines)
            {
                if (m.drink == DrinkId.Bira || Management.MachineLevel(m.drink) != level) continue;
                var def = GameDatabase.I.Drink(m.drink);
                if (def) list.Add((Loc.T("yeni makine"), def.icon, Loc.T(def.displayName)));
            }
            foreach (var st in Catalog.StaffList)
                if (!st.premium && st.level == level) list.Add((Loc.T("işe alınabilir"), LoadIcon(st.icon), st.name));
            // v2.1: bu seviyede açılan Barlo yetenek seviyeleri
            foreach (var b in BarloSkills.All)
                for (int n = 1; n <= BarloSkills.MaxLevel; n++)
                    if (BarloSkills.UnlockLevel(b, n) == level) list.Add((Loc.T("Barlo yeteneği"), KitIcon(BarloSkills.Icon(b)), $"{BarloSkills.Name(b)} {n}"));
            // Alanlar: aynı adlı kareler tek kartta (ör. "Yeni Masa ×3")
            var zones = new List<(string label, Sprite icon, int n)>();
            foreach (var z in PayZone.All)
            {
                if (!z || z.requiredLevel != level || GameState.IsUnlocked(z.id)) continue;
                string lab = z.DisplayLabel;
                int i = zones.FindIndex(x => x.label == lab);
                if (i >= 0) zones[i] = (lab, zones[i].icon, zones[i].n + 1);
                else zones.Add((lab, ZoneIcon(z), 1));
            }
            foreach (var (lab, icon, n) in zones) list.Add((Loc.T("yeni alan"), icon, n > 1 ? $"{lab} ×{n}" : lab));
            foreach (var d in Catalog.DecorList)
                if (!d.premium && d.level == level) list.Add((Loc.T("yeni dekor"), LoadIcon(d.icon), Loc.T(d.name)));
            foreach (var t in Themes.List)
                if (!t.premium && t.level == level) list.Add((Loc.T("yeni tema"), KitIcon("brush"), Loc.T(t.name)));
            foreach (var e in TavernEvents.List)
                if (e.level == level) list.Add((Loc.T("yeni etkinlik"), LoadIcon(e.icon), Loc.T(e.name)));
            foreach (var a in Automation.List)
            {
                if (a.gemLevel == level) list.Add((Loc.T("otomasyon · elmas"), KitIcon(a.icon), Loc.T(a.name)));
                if (a.level == level) list.Add((Loc.T("otomasyon · altın"), KitIcon(a.icon), Loc.T(a.name)));
            }
            foreach (var c in GameDatabase.I.customers)
                if (c && c.unlockLevel == level) list.Add((Loc.T("yeni müşteri"), LoadIcon($"Cust_{c.number:00}"), Loc.T(c.displayName)));
            foreach (var t in Tiers.List)
                if (t.index > 0 && t.level == level) list.Add((Loc.T("kademe şartı"), KitIcon("home"), Loc.T(t.name)));
            if (level == Prestige.RequiredLevel) list.Add((Loc.T("prestij şartı"), KitIcon("shield"), Loc.T("Yeni Şube")));
            if (level == Layout.UnlockLevel) list.Add((Loc.T("yeni özellik"), KitIcon("hammer"), Loc.T("Düzenleme modu")));   // v2.3
            return list;
        }

        void FillUnlocks(int level)
        {
            _unlocks.Clear();
            var list = LevelUnlocks(level);
            bool compact = list.Count > CompactUnlockCards;
            _unlocks.EnableInClassList("compact", compact);
            int shown = Mathf.Min(list.Count, list.Count > MaxUnlockCards ? MaxUnlockCards - 1 : MaxUnlockCards);
            for (int i = 0; i < shown; i++) AddUnlock(list[i].tag, list[i].icon, list[i].name);
            if (shown < list.Count) AddUnlock(Loc.T("ve daha"), KitIcon("plus"), "+" + (list.Count - shown));
        }
    }
}
