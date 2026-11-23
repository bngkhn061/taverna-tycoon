using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v2.2 Paket 4 arayüzü (GAME_DESIGN.md §16): HUD'da sezon rozeti (ad, kalan gün, toplanacak görev), Etkinlikler panelinde
    /// "Sezon" sekmesi (jeton, sona erme uyarısı, sezon görevleri, kalıcı sezon dekoru, sezon kostümü), sezon başlangıç duyurusu,
    /// sona eren jetonların bildirimi; Dükkân › Hazine içinde Gardırop (önizleme, giy / çıkar, edinim yolu).
    /// </summary>
    public partial class HudController
    {
        Button _seasonBtn;
        Label _seasonBtnText, _seasonBtnBadge;
        VisualElement _seasonBtnIc;
        string _eventsTab = "events";
        ScrollView _evScroll;
        static Sprite _tokenIcon;
        static Sprite TokenIcon => _tokenIcon ? _tokenIcon : (_tokenIcon = LoadIcon("SeasonToken"));

        /// <summary>Sezon duyurusu en erken bu kadar sn sonra (giriş animasyonu bitsin).</summary>
        const float SeasonIntroDelay = 4f;
        /// <summary>Sona eren jeton penceresi en erken bu kadar sn sonra (oyun zamanı).</summary>
        const float ExpiredNoticeDelay = 6f;
        /// <summary>Yeniden kurulan sezon sekmesinde kaydırma konumunun geri verildiği gecikme (ms; yerleşim bitsin).</summary>
        const long ScrollRestoreMs = 40;

        /// <summary>Test botu: sezon rozeti görünüyor mu ve metni.</summary>
        public bool SeasonButtonVisible => _seasonBtn != null && !_seasonBtn.ClassListContains("hidden");
        public string SeasonButtonText => _seasonBtnText != null ? _seasonBtnText.text : "";
        /// <summary>Test botu: etkinlik panelinin açık sekmesi ("" = kapalı).</summary>
        public string DebugEventsTab => _eventsPanel.ClassListContains("hidden") ? "" : _eventsTab;

        void InitV22()
        {
            _seasonBtn = new Button(() => OpenEvents("season"));
            _seasonBtn.AddToClassList("btn"); _seasonBtn.AddToClassList("btn-sm"); _seasonBtn.AddToClassList("helper-btn");
            _seasonBtn.AddToClassList("season-btn"); _seasonBtn.AddToClassList("tool-btn"); _seasonBtn.AddToClassList("hidden");   // v2.5 araç kutusu
            _seasonBtnIc = new VisualElement { pickingMode = PickingMode.Ignore };
            _seasonBtnIc.AddToClassList("price-icon");
            _seasonBtnText = new Label { pickingMode = PickingMode.Ignore };
            _seasonBtnBadge = new Label("!") { pickingMode = PickingMode.Ignore };
            _seasonBtnBadge.AddToClassList("event-new");
            _seasonBtnBadge.AddToClassList("hidden");
            _seasonBtnBadge.style.color = (Color)new Color32(251, 243, 226, 255);
            _seasonBtn.Add(_seasonBtnIc); _seasonBtn.Add(_seasonBtnText); _seasonBtn.Add(_seasonBtnBadge);
            _eventBtn.parent.Add(_seasonBtn);
            Seasons.Changed += OnSeasonChanged;
            Costumes.Changed += OnCostumeChanged;
        }

        void DisableV22()
        {
            Seasons.Changed -= OnSeasonChanged;
            Costumes.Changed -= OnCostumeChanged;
        }

        void OnSeasonChanged()
        {
            if (!_eventsPanel.ClassListContains("hidden") && _eventsTab == "season") BuildEvents();
        }

        void OnCostumeChanged()
        {
            if (!_shopRoot.ClassListContains("hidden") && _shopTab == "prem") BuildShop();
        }

        void UpdateV22()
        {
            bool show = Seasons.Playable;   // v2.5: mutluluk kartı araç kutularının altında, sezon kutusu gizlenmez
            _seasonBtn.EnableInClassList("hidden", !show);
            var d = GameState.Data;
            if (show)
            {
                var s = Seasons.Current;
                _seasonBtnText.text = Loc.Upper(Loc.T(s.name));   // v2.5: araç kutusunda yalnızca sezonun adı; kalan gün ipucunda ve sezon sekmesinde
                _seasonBtn.tooltip = Loc.F("{0} · {1} gün", Loc.Upper(Loc.T(s.name)), Seasons.DaysLeftNow);
                _seasonBtn.style.unityBackgroundImageTintColor = s.color;
                _seasonBtnIc.style.backgroundImage = new StyleBackground(LoadIcon("Season_" + s.set));
                _seasonBtnBadge.EnableInClassList("hidden", Seasons.ReadyCount == 0);
                // sezon başlangıç duyurusu (sezon başına bir kez, sakin bir anda)
                if (d.seasonKey != "" && d.seasonSeenKey != d.seasonKey && !ModalOpen && !AnyPanelOpen && !LevelCardOpen
                    && !DayCycle.Ended && Time.timeSinceLevelLoad > SeasonIntroDelay)
                {
                    d.seasonSeenKey = d.seasonKey;
                    ShowBanner(Loc.Upper(Loc.F("{0} başladı!", Loc.T(s.name))), "star", true, 2.8f);
                    Toast(Loc.F("Sezon tezgâhı açıldı: {0} sat, sezon jetonu topla!", Seasons.DrinkName(s.drink)));
                    Confetti(22);
                    Sfx.Play(SfxId.Unlock);
                    GameState.Save();
                }
            }
            // biten sezonun kullanılmamış jetonları: pencereyle açıkça söylenir (taşınmaz)
            if (d.seasonExpiredTokens > 0 && !ModalOpen && !AnyPanelOpen && !LevelCardOpen && !DayCycle.Ended && Time.timeSinceLevelLoad > ExpiredNoticeDelay)
            {
                var old = System.Array.Find(Seasons.List, x => d.seasonExpiredKey.StartsWith(x.key + "-"));
                var o = PremCard(Loc.Upper(Loc.T(old.name ?? "")), 520f, out var card);
                var t = new Label(Loc.F("{0} bitti: {1} sezon jetonu sona erdi. Jetonlar sonraki sezona taşınmaz.", Loc.T(old.name ?? ""), d.seasonExpiredTokens));
                t.AddToClassList("ending-text");
                card.Add(t);
                card.Add(Note(Loc.T("Aldığın sezon dekoru ve kostümleri kalıcıdır.")));
                var ok = ModalButton(Loc.T("TAMAM"), "btn-paper", () => o.RemoveFromHierarchy());
                ok.style.marginTop = 12;
                card.Add(ok);
                d.seasonExpiredTokens = 0;
                GameState.Save();
            }
        }

        // ------------------------------------------------------------------ Etkinlikler › Sezon sekmesi
        /// <summary>
        /// Etkinlik panelinin başı: sezon sürüyorsa iki sekme ("Etkinlikler" / sezon adı) ve kaydırılabilir içerik.
        /// Sezon sekmesi seçiliyse onu kurar ve null döner; değilse etkinlik satırlarının ekleneceği kabı döner.
        /// </summary>
        VisualElement SeasonTabsOrHost()
        {
            float keep = _evScroll != null ? _evScroll.scrollOffset.y : 0f;
            _evScroll = null;
            _eventRows.EnableInClassList("ev-host", Seasons.Playable);   // kaydırılabilir içerik: liste panelin içinde kalır
            if (!Seasons.Playable) { _eventsTab = "events"; return _eventRows; }
            var s = Seasons.Current;
            var tabs = new VisualElement();
            tabs.AddToClassList("tabs");
            tabs.AddToClassList("ev-tabs");
            foreach (var (id, text) in new[] { ("events", Loc.T("Etkinlikler")), ("season", Loc.T(s.name)) })
            {
                var t = new Button(() => { if (_eventsTab == id) return; _eventsTab = id; Sfx.Play(SfxId.Pop); BuildEvents(); }) { text = text };
                t.AddToClassList("tab");
                if (id == "events") t.AddToClassList("first");
                t.EnableInClassList("on", _eventsTab == id);
                if (id == "season" && Seasons.ReadyCount > 0) { var b = new Label("!"); b.AddToClassList("tab-new"); t.Add(b); }
                tabs.Add(t);
            }
            _eventRows.Add(tabs);
            _evScroll = new ScrollView(ScrollViewMode.Vertical);
            _evScroll.AddToClassList("ev-scroll");
            _evScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _evScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _evScroll.mouseWheelScrollSize = 60f;
            _eventRows.Add(_evScroll);
            if (keep > 0f)
            {
                // yeniden kurulunca kaydırma konumu korunur (jeton geldikçe liste başa sıçramasın). Yerleşim bir sonraki karede
                // hesaplanır; o zamana kadar kaydırma sınırı 0'dır → konum yerleşimden sonra (iki kare gecikmeyle) verilir.
                var sv = _evScroll;
                sv.scrollOffset = new Vector2(0f, keep);
                sv.schedule.Execute(() => sv.scrollOffset = new Vector2(0f, keep)).StartingIn(ScrollRestoreMs);
            }
            if (_eventsTab != "season") return _evScroll.contentContainer;
            BuildSeasonTab(_evScroll.contentContainer);
            return null;
        }

        void BuildSeasonTab(VisualElement host)
        {
            var s = Seasons.Current;
            // başlık kartı: sezon, takvim, kalan gün, jeton
            var head = new VisualElement();
            head.AddToClassList("season-head");
            head.style.backgroundColor = s.color;
            var ic = new VisualElement();
            ic.AddToClassList("season-head-ic");
            ic.style.backgroundImage = new StyleBackground(LoadIcon("Season_" + s.set));
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var title = new Label(Loc.Upper(Loc.T(s.name)));
            title.AddToClassList("season-head-title");
            var sub = new Label(Loc.F("{0} · {1} gün kaldı", Seasons.Months(s), Seasons.DaysLeftNow));
            sub.AddToClassList("season-head-sub");
            col.Add(title); col.Add(sub);
            var tok = new VisualElement();
            tok.AddToClassList("chip");
            tok.AddToClassList("season-token");
            var ti = new VisualElement();
            ti.AddToClassList("q-reward-icon");
            ti.style.backgroundImage = new StyleBackground(TokenIcon);
            tok.Add(ti);
            tok.Add(new Label(Seasons.Tokens.ToString()));
            head.Add(ic); head.Add(col); head.Add(tok);
            host.Add(head);
            var warn = new Label(Loc.T("Sezon jetonları bu sezona özeldir: sezon bitince sıfırlanır, sonraki sezona taşınmaz."));
            warn.AddToClassList("season-warn");
            host.Add(warn);
            var cust = GameDatabase.I.SeasonCustomer(s.id);
            var info = new Label(Loc.F("Sezon menüsü: {0} · sezon müşterisi: {1} · satış başına {2} jeton",
                Seasons.DrinkName(s.drink), Loc.T(s.customerName), Seasons.TokensPerServe));
            info.AddToClassList("item-sub");
            info.AddToClassList("season-info");
            host.Add(info);

            SeasonSection(host, Loc.T("SEZON GÖREVLERİ"));
            for (int i = 0; i < Seasons.QuestList.Count; i++) host.Add(SeasonQuestRow(i));

            SeasonSection(host, Loc.T("SEZON DEKORU · KALICI"));
            foreach (var id in s.decor)
            {
                var dd = Catalog.FindDecor(id);
                var r = Seasons.CanBuyDecor(id);
                host.Add(TokenItem(LoadIcon(dd.icon), Loc.T(dd.name), Loc.F("+{0:0} ambiyans · sezon bitince de kalır", dd.ambiance),
                    r == Seasons.Buy.Owned, r, (int)dd.price, () => Seasons.BuyDecor(id), null));
            }

            SeasonSection(host, Loc.T("SEZON KOSTÜMÜ"));
            var c = Costumes.Find(s.costume);
            host.Add(CostumeRow(c));
            var more = new Button(() => { _eventsPanel.AddToClassList("hidden"); OpenShop("prem"); }) { text = Loc.T("GARDIROBA GİT") };
            more.AddToClassList("btn"); more.AddToClassList("btn-paper"); more.AddToClassList("btn-sm"); more.AddToClassList("season-more");
            host.Add(more);
        }

        static void SeasonSection(VisualElement host, string title)
        {
            var l = new Label(title);
            l.AddToClassList("depot-section");
            host.Add(l);
        }

        VisualElement SeasonQuestRow(int idx)
        {
            var q = Seasons.QuestList[idx];
            int target = Seasons.Target(q);
            bool done = Seasons.Done(q);
            var row = new VisualElement();
            row.AddToClassList("row");
            row.EnableInClassList("done", done);
            var img = new VisualElement();
            img.AddToClassList("q-img");
            img.style.backgroundImage = new StyleBackground(TokenIcon);
            var text = new VisualElement();
            text.AddToClassList("q-text");
            var name = new Label(Seasons.QuestText(q));
            name.AddToClassList("q-name");
            var bar = new VisualElement();
            bar.AddToClassList("q-bar");
            var fill = new VisualElement();
            fill.AddToClassList("q-fill");
            fill.style.width = Length.Percent(q.progress * 100f / Mathf.Max(1, target));
            bar.Add(fill);
            var reward = new VisualElement();
            reward.AddToClassList("q-reward");
            AddReward(reward, TokenIcon, "+" + Seasons.Reward(q));
            text.Add(name); text.Add(bar); text.Add(reward);
            row.Add(img); row.Add(text);
            if (q.claimed)
            {
                var ok = new Label(Loc.T("TOPLANDI"));
                ok.AddToClassList("chip");
                ok.AddToClassList("ok");
                reward.Add(ok);
            }
            else if (done)
            {
                var claim = new Button { text = Loc.T("TOPLA") };
                claim.AddToClassList("btn"); claim.AddToClassList("btn-sm");
                claim.style.minWidth = 104;
                claim.clicked += () => { if (Seasons.Claim(idx)) { Sfx.Play(SfxId.Claim); Confetti(20); } };
                row.Add(claim);
            }
            else
            {
                var prog = new Label($"{q.progress} / {target}");
                prog.AddToClassList("chip");
                reward.Add(prog);
            }
            return row;
        }

        /// <summary>Jetonla alınan ürün kartı (sezon dekoru / sezon kostümü): alındı · jeton bedeli · sezon dışı.</summary>
        VisualElement TokenItem(Sprite icon, string name, string sub, bool owned, Seasons.Buy state, int price,
                                System.Func<Seasons.Buy> buy, System.Action onPreview)
        {
            var item = new VisualElement();
            item.AddToClassList("item");
            item.EnableInClassList("owned", owned);
            var img = new VisualElement();
            img.AddToClassList("item-img");
            if (icon) img.style.backgroundImage = new StyleBackground(icon);
            if (onPreview != null) img.RegisterCallback<ClickEvent>(_ => onPreview());
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var n = new Label(name);
            n.AddToClassList("item-name");
            var s = new Label(sub);
            s.AddToClassList("item-sub");
            col.Add(n); col.Add(s);
            item.Add(img); item.Add(col);
            Button b;
            Label t;
            if (owned) { b = PriceButton("btn-paper", KitIcon("check"), out t); t.text = Loc.T("ALINDI"); b.AddToClassList("owned"); b.SetEnabled(false); }
            else
            {
                b = PriceButton("btn-teal", TokenIcon, out t);
                t.text = price.ToString();
                b.EnableInClassList("locked", state != Seasons.Buy.Ok);
                b.clicked += () =>
                {
                    var r = buy();
                    if (r == Seasons.Buy.Ok) { Sfx.Play(SfxId.Buy); Confetti(18); GameSettings.Vibrate(); Toast(Loc.F("{0} alındı!", name)); }
                    else Toast(Seasons.BuyMessage(r));
                    BuildEvents();
                    if (!_shopRoot.ClassListContains("hidden")) BuildShop();
                };
            }
            b.AddToClassList("btn-sm");
            item.Add(b);
            return item;
        }

        // ------------------------------------------------------------------ Gardırop (Dükkân › Hazine)
        /// <summary>Hazine sekmesinin başında Gardırop: her sahibin kostümleri.</summary>
        void AddWardrobe()
        {
            AddShopSection(Loc.T("GARDIROP · YALNIZCA GÖRÜNÜM"));
            foreach (var owner in Costumes.Owners)
                foreach (var c in Costumes.List)
                    if (c.owner == owner) _shopScroll.Add(CostumeRow(c));
        }

        /// <summary>Kostüm kartı: portre (dokununca önizleme), ad ve sahip, edinim yolu; giy / çıkar / satın al / yakında / sezonda / ödül.</summary>
        VisualElement CostumeRow(Costumes.Costume c)
        {
            bool owned = Costumes.Owned(GameState.Data, c);
            bool worn = Costumes.Worn(c.owner) == c.id;
            bool hired = c.owner == "barlo" || Management.Hired(c.owner);
            string sub = Costumes.SourceText(c) + (worn ? Loc.T(" · giyili") : "") + (!hired ? Loc.T(" · işe alınınca görünür") : "");
            string name = Loc.F("{0} · {1}", Loc.T(c.name), Costumes.OwnerName(c.owner));
            if (c.source == Costumes.Source.Season && !owned)
                return TokenItem(LoadIcon(Costumes.Icon(c)), name, sub, false, Costumes.CanBuyWithTokens(c), c.tokens,
                                 () => Costumes.BuyWithTokens(c.id), () => OpenCostumePreview(c), seasonGate: c);
            var item = new VisualElement();
            item.AddToClassList("item");
            item.EnableInClassList("owned", worn);
            var img = new VisualElement();
            img.AddToClassList("item-img");
            img.style.backgroundImage = new StyleBackground(LoadIcon(Costumes.Icon(c)));
            img.RegisterCallback<ClickEvent>(_ => OpenCostumePreview(c));
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var n = new Label(name);
            n.AddToClassList("item-name");
            var s = new Label(sub);
            s.AddToClassList("item-sub");
            col.Add(n); col.Add(s);
            item.Add(img); item.Add(col);
            Button b;
            Label t;
            if (owned && worn)
            {
                b = PriceButton("btn-paper", KitIcon("check"), out t); t.text = Loc.T("ÇIKAR");
                b.clicked += () => { Costumes.TakeOff(c.owner); Sfx.Play(SfxId.Pop); Toast(Loc.F("{0} kendi kıyafetinde", Costumes.OwnerName(c.owner))); };
            }
            else if (owned)
            {
                b = PriceButton("btn-teal", KitIcon("star"), out t); t.text = Loc.T("GİY");
                b.clicked += () => { if (Costumes.Wear(c.id)) { Sfx.Play(SfxId.Buy); Confetti(14); Toast(Loc.F("{0} giyildi!", Loc.T(c.name))); HopWearer(c.owner); } };
            }
            else if (c.source == Costumes.Source.Store)
            {
                var p = Premium.Find(c.product);
                bool open = Premium.Available;
                // gerçek para: Hazine ürünleri gibi ikonsuz fiyat (elmas ikonu yanıltır)
                b = new Button();
                b.AddToClassList("btn"); b.AddToClassList(open ? "btn-sun" : "btn-paper");
                t = new Label(open ? Premium.PriceOf(p) : SoonText);
                b.Add(t);
                if (open) b.clicked += () => Premium.Request(p.id);
                else { b.AddToClassList("locked"); b.AddToClassList("prem-soon"); b.SetEnabled(false); }
            }
            else
            {
                b = PriceButton("btn-paper", KitIcon("lock"), out t); t.text = Loc.T("ÖDÜL");
                b.AddToClassList("locked");
                b.clicked += () => Toast(Loc.T("Krala Davet son sahnesini izleyince açılır"));
            }
            b.AddToClassList("btn-sm");
            item.Add(b);
            return item;
        }

        /// <summary>Sezon kostümü kartı: kendi sezonu dışında "SEZONDA" kilidi, içinde jeton bedeli.</summary>
        VisualElement TokenItem(Sprite icon, string name, string sub, bool owned, Seasons.Buy state, int price,
                                System.Func<Seasons.Buy> buy, System.Action onPreview, Costumes.Costume seasonGate)
        {
            if (state != Seasons.Buy.OutOfSeason) return TokenItem(icon, name, sub, owned, state, price, buy, onPreview);
            var item = TokenItem(icon, name, sub, owned, state, price, () => Seasons.Buy.OutOfSeason, onPreview);
            var b = item.Q<Button>();
            if (b != null)
            {
                b.RemoveFromHierarchy();
                var lb = PriceButton("btn-paper", KitIcon("lock"), out var lt);
                lt.text = Loc.T("SEZONDA");
                lb.AddToClassList("btn-sm");
                lb.AddToClassList("locked");
                var sd = Seasons.Get(seasonGate.season);
                lb.clicked += () => Toast(Loc.F("{0} sırasında ({1}) sezon jetonuyla alınır", Loc.T(sd.name), Seasons.Months(sd)));
                item.Add(lb);
            }
            return item;
        }

        /// <summary>Kostüm giyilince sahibi sevinçle zıplar.</summary>
        void HopWearer(string owner)
        {
            foreach (var w in FindObjectsByType<CostumeWearer>(FindObjectsSortMode.None))
                if (w.owner == owner) { var a = w.GetComponent<ToyAnimator>(); if (a) a.Hop(); }
        }

        /// <summary>Kostüm önizlemesi: büyük portre, sahip, açıklama, edinim yolu.</summary>
        void OpenCostumePreview(Costumes.Costume c)
        {
            var overlay = PremCard(Loc.T("GARDIROP"), 520f, out var card);
            var head = new VisualElement();
            head.AddToClassList("cos-preview");
            var img = new VisualElement();
            img.AddToClassList("cos-preview-img");
            img.style.backgroundImage = new StyleBackground(LoadIcon(Costumes.Icon(c)));
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var n = new Label(Loc.T(c.name));
            n.AddToClassList("cos-preview-name");
            var o = new Label(Costumes.OwnerName(c.owner));
            o.AddToClassList("item-sub");
            var d = new Label(Loc.T(c.desc));
            d.AddToClassList("cos-preview-desc");
            var src = new Label(Costumes.SourceText(c));
            src.AddToClassList("chip");
            src.AddToClassList("cos-preview-src");
            col.Add(n); col.Add(o); col.Add(d); col.Add(src);
            head.Add(img); head.Add(col);
            card.Add(head);
            card.Add(Note(Loc.T("Kostümler yalnızca görünüştür: oyunu kolaylaştırmaz. Giyilen kostüm kayıtta kalır, yeni şubede de sürer.")));
            var ok = ModalButton(Loc.T("TAMAM"), "btn-paper", () => overlay.RemoveFromHierarchy());
            ok.style.marginTop = 10;
            card.Add(ok);
        }

        /// <summary>Test botu: etkinlik panelinin kaydırma konumu (aşağı).</summary>
        public string DebugEventsScroll(float y)
        {
            if (_evScroll == null) return "kaydırma yok";
            _evScroll.scrollOffset = new Vector2(0f, y);
            return $"panel {_eventsPanel.layout.height:0} · liste {_eventRows.layout.height:0} · görünüm {_evScroll.layout.height:0} · içerik {_evScroll.contentContainer.layout.height:0} · konum {_evScroll.scrollOffset.y:0}";
        }

        /// <summary>Test botu: kostüm önizlemesini açar.</summary>
        public void DebugCostumePreview(string id) => OpenCostumePreview(Costumes.Find(id));
    }
}
