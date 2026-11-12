using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v0.9 "Büyüyen Taverna" arayüzü (design/ui/ui_v09_screens.html, onay 2026-11-10):
    /// giriş animasyonu + şerit, rehber (E), Gün Bitti + Yarını Planla (F), sabah şeridi (G),
    /// Dükkân › Otomasyon (H), taverna adı tabelası (C) ve ad değiştirme (D), oyun masası etiketleri.
    /// </summary>
    public partial class HudController
    {
        /// <summary>Menüden "OYNA" ile gelindi: giriş animasyonu oynasın (MainMenuController ayarlar).</summary>
        public static bool PlayIntro;

        VisualElement _hudRoot, _banner, _bannerIc, _dayendRoot, _dayendRows, _dayendChips, _tutLayer, _tutRing, _tutPips, _renameRoot;
        VisualElement[] _tutDim;
        Label _bannerText, _dayendTitle, _planNote, _tutText, _tutStep, _signLabel, _renameAdText, _renameCoinsText;
        ScrollView _planScroll;
        TextField _renameField;
        Transform _signPoint;
        float _bannerT, _introT = -1f;
        const float IntroSeconds = 2.4f;

        // Plan ekranı açıkken Dükkân ve Depo listeleri plana yazılır (aynı satır kurucuları kullanılır)
        bool _planMode;
        string _planTab = "depot";
        ScrollView _shopScrollMain, _depotRowsMain;
        Label _shopNoteMain;

        readonly Dictionary<GameTable, Label> _gameTags = new();
        int _wageWarnDay = -1;

        void InitV09()
        {
            _hudRoot = _root.Q("hud-root");
            _banner = _root.Q("banner");
            _bannerIc = _root.Q("banner-ic");
            _bannerText = _root.Q<Label>("banner-text");

            // --- Gün Bitti + Yarını Planla
            _dayendRoot = _root.Q("dayend-root");
            _dayendTitle = _root.Q<Label>("dayend-title");
            _dayendRows = _root.Q("dayend-rows");
            _dayendChips = _root.Q("dayend-chips");
            _planScroll = _root.Q<ScrollView>("plan-scroll");
            _planScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _planScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _planScroll.mouseWheelScrollSize = 60f;
            _planNote = _root.Q<Label>("plan-note");
            foreach (var (n, id) in new[] { ("plan-depot", "depot"), ("plan-dec", "dec"), ("plan-staff", "staff"), ("plan-mac", "mac") })
                _root.Q<Button>(n).clicked += () => { _planTab = id; Sfx.Play(SfxId.Pop); BuildPlan(); };
            _root.Q<Button>("next-day").clicked += NextDayPressed;
            _root.Q<Button>("tab-auto").clicked += () => SelectTab("auto");

            // --- Rehber
            _tutLayer = _root.Q("tut-layer");
            _tutRing = _root.Q("tut-ring");
            _tutText = _root.Q<Label>("tut-text");
            _tutStep = _root.Q<Label>("tut-step");
            _tutPips = _root.Q("tut-pips");
            _tutDim = new[] { _root.Q("tut-dim-t"), _root.Q("tut-dim-b"), _root.Q("tut-dim-l"), _root.Q("tut-dim-r") };
            _root.Q("tut-por").style.backgroundImage = new StyleBackground(LoadIcon("Main_Barlo"));
            for (int i = 0; i < Tutorial.StepCount; i++)
            {
                var pip = new VisualElement();
                pip.AddToClassList("tut-pip");
                _tutPips.Add(pip);
            }
            _root.Q<Button>("tut-skip").clicked += () => { Tutorial.Skip(); Sfx.Play(SfxId.Pop); };
            Tutorial.Changed += OnTutorialChanged;

            // --- Ad değiştirme (Ayarlar › Taverna)
            _renameRoot = _root.Q("rename-root");
            _renameField = _root.Q<TextField>("rename-field");
            _renameAdText = _root.Q<Label>("rename-ad-text");
            _renameCoinsText = _root.Q<Label>("rename-coins-text");
            _renameCoinsText.text = FormatNum(Balance.RenameCost);
            _root.Q<Button>("rename-close").clicked += () => _renameRoot.AddToClassList("hidden");
            _root.Q<Button>("rename-coins").clicked += () =>
            {
                if (!ValidName(out var n)) return;
                if (!GameState.TrySpend(Balance.RenameCost)) { Toast(Loc.T("Yeterli altın yok")); return; }
                ApplyName(n);
            };
            _root.Q<Button>("rename-ad").clicked += () =>
            {
                if (!ValidName(out var n)) return;
                ShowAd("rename", Loc.T("Taverna adını değiştir"), () =>
                {
                    GameState.Data.renameAds++;
                    if (GameState.Data.renameAds >= Balance.RenameAds) { GameState.Data.renameAds = 0; ApplyName(n); }
                    else { GameState.Save(); RefreshRename(); Toast(Loc.F("Bir reklam daha: {0}/{1}", GameState.Data.renameAds, Balance.RenameAds)); }
                });
            };
            _settings.RenameRequested += OpenRename;

            // --- Tabela (dünyada; yazı HUD'dan)
            var sp = GameObject.Find("TavernSignText");
            _signPoint = sp ? sp.transform : null;
            _signLabel = new Label(GameState.TavernName) { pickingMode = PickingMode.Ignore };
            _signLabel.AddToClassList("sign-text");
            _world.Add(_signLabel);

            DayCycle.DayStarted += OnDayStarted;
            Automation.Changed += RefreshTop;

            OnTutorialChanged();
            if (PlayIntro) { PlayIntro = false; StartIntro(); }
        }

        void DisableV09()
        {
            Tutorial.Changed -= OnTutorialChanged;
            DayCycle.DayStarted -= OnDayStarted;
            Automation.Changed -= RefreshTop;
        }

        bool V09ModalOpen => !_dayendRoot.ClassListContains("hidden") || !_renameRoot.ClassListContains("hidden") || _introT >= 0f;

        void UpdateV09()
        {
            UpdateIntro();
            if (_bannerT > 0f) { _bannerT -= Time.unscaledDeltaTime; if (_bannerT <= 0f) _banner.AddToClassList("hidden"); }
            UpdateTutorial();
            UpdateGameTags();
            // Gece başlarken kasada maaşa yetecek altın yoksa uyar (v1.0: ödenmeyen maaş → küskün personel)
            if (Night.IsNight && _wageWarnDay != GameState.Data.day)
            {
                _wageWarnDay = GameState.Data.day;
                long w = Management.DailyWages;
                if (w > 0 && GameState.Coins < w) Toast(Loc.F("Gün sonu maaş: {0} altın · kasada tut, yoksa personel küser", FormatNum(w)));
            }
            if (_signPoint)
            {
                if (_signLabel.text != GameState.TavernName) _signLabel.text = GameState.TavernName;
                for (int i = 1; i < 5; i++) _signLabel.EnableInClassList("t" + i, SignTier.Current == i);   // v1.2 tabela kademesi
                bool on = ToPanel(_signPoint.position, out var p);
                Place(_signLabel, p, on);
            }
            if (_planMode && _planTab == "depot")
            {
                _depotT -= Time.unscaledDeltaTime;
                if (_depotT <= 0f) { _depotT = 0.2f; foreach (var u in _depotUpdaters) u(); }
            }
        }

        // ------------------------------------------------------------------ şerit
        void ShowBanner(string text, string icon, bool sun, float seconds)
        {
            _bannerText.text = text;
            _bannerIc.style.backgroundImage = new StyleBackground(KitIcon(icon));
            _banner.EnableInClassList("sun", sun);
            _banner.RemoveFromClassList("hidden");
            _bannerT = seconds;
        }

        // ------------------------------------------------------------------ giriş animasyonu (B)
        void StartIntro()
        {
            _introT = 0f;
            _hudRoot.style.opacity = 0f;
            var follow = cam ? cam.GetComponent<CameraFollow>() : null;
            if (follow) follow.Intro(IntroSeconds);
            ShowBanner(Loc.F("{0} · GÜN {1}{2}", Loc.Upper(GameState.TavernName), GameState.Data.day, BranchSuffix), "door", false, IntroSeconds + 0.6f);
            _banner.style.opacity = 0f;
            Sfx.Play(SfxId.Door);
        }

        void UpdateIntro()
        {
            if (_introT < 0f) return;
            _introT += Time.unscaledDeltaTime;
            var p = Pointer.current;
            bool skip = _introT > 0.3f && p != null && p.press.wasPressedThisFrame;
            if (skip)
            {
                _introT = IntroSeconds + 1f;
                var follow = cam ? cam.GetComponent<CameraFollow>() : null;
                if (follow) follow.SkipIntro();
            }
            // Şerit 0.9 sn'de belirir, HUD 1.6 sn'den itibaren kayarak gelir
            _banner.style.opacity = Mathf.Clamp01((_introT - 0.9f) / 0.3f);
            float h = Mathf.Clamp01((_introT - 1.6f) / 0.6f);
            _hudRoot.style.opacity = h;
            _hudRoot.style.translate = new Translate(0f, (1f - h) * 24f);
            if (_introT >= IntroSeconds + 0.6f)
            {
                _introT = -1f;
                _hudRoot.style.opacity = 1f;
                _hudRoot.style.translate = new Translate(0f, 0f);
                _banner.style.opacity = 1f;
            }
        }

        // ------------------------------------------------------------------ gün sonu + plan (F) ve sabah (G)
        void OpenDayEnd(DayCycle.Report r)
        {
            _dayendTitle.text = Loc.F("GÜN {0} BİTTİ", r.day);
            _dayendRows.Clear();
            void Line(string label, string value, string cls)
            {
                var row = new VisualElement();
                row.AddToClassList("dayend-line");
                var l = new Label(label);
                var v = new Label(value);
                v.AddToClassList("val");
                if (!string.IsNullOrEmpty(cls)) v.AddToClassList(cls);
                row.Add(l); row.Add(v);
                _dayendRows.Add(row);
            }
            Line(Loc.T("Gelir"), $"+{FormatNum(r.income)}", "");
            Line(Loc.T("Maaşlar"), $"−{FormatNum(r.wages)}", "neg");
            if (r.unpaid > 0) Line(Loc.T("Ödenemeyen maaş"), $"−{FormatNum(r.unpaid)}", "neg");
            long net = r.income - r.wages;
            Line(Loc.T("Net kazanç"), (net >= 0 ? "+" : "−") + FormatNum(System.Math.Abs(net)), net >= 0 ? "pos" : "neg");
            Line(Loc.T("Kasadaki altın"), FormatNum(GameState.Coins), "");
            _dayendChips.Clear();
            var chipsText = new List<string> { Loc.F("{0} servis", r.served), $"★ {r.stars:0.#}", Loc.F("Sv {0}", GameState.Level) };
            if (r.unpaid > 0) chipsText.Add(Loc.T("Personel yarın küskün (−%20 hız)"));
            foreach (var c in chipsText)
            {
                var chip = new Label(c);
                chip.AddToClassList("chip");
                _dayendChips.Add(chip);
            }
            // Açık pencereleri kapat, plan modunda listeleri plana yönlendir
            DebugOpen("close");
            _settings.Hide();
            _renameRoot.AddToClassList("hidden");
            EnterPlanMode();
            _planTab = "depot";
            BuildPlan();
            _dayendRoot.RemoveFromClassList("hidden");
            PlayerController.JoystickInput = Vector2.zero;
            Sfx.Play(SfxId.Claim);
        }

        void EnterPlanMode()
        {
            if (_planMode) return;
            _planMode = true;
            _shopScrollMain = _shopScroll; _shopNoteMain = _shopNote; _depotRowsMain = _depotRows;
            _shopScroll = _planScroll; _shopNote = _planNote; _depotRows = _planScroll;
        }

        void ExitPlanMode()
        {
            if (!_planMode) return;
            _planMode = false;
            _shopScroll = _shopScrollMain; _shopNote = _shopNoteMain; _depotRows = _depotRowsMain;
        }

        void BuildPlan()
        {
            foreach (var (n, id) in new[] { ("plan-depot", "depot"), ("plan-dec", "dec"), ("plan-staff", "staff"), ("plan-mac", "mac") })
                _root.Q<Button>(n).EnableInClassList("on", _planTab == id);
            if (_planTab == "depot")
            {
                BuildDepot();
                _planNote.text = Loc.T("Gece verilen siparişler sabah hepsi birden gelir");
            }
            else
            {
                var keep = _shopTab;
                _shopTab = _planTab;
                BuildShop();
                _shopTab = keep;
                // Satın alma sonrası BuildShop yeniden çağrılınca plan sekmesi kalsın
                _shopTab = _planTab;
            }
        }

        /// <summary>Test botu: plan sekmesini seç.</summary>
        public void DebugPlanTab(string tab) { if (_planTab == tab) return; _planTab = tab; BuildPlan(); }

        public bool DayEndOpen => !_dayendRoot.ClassListContains("hidden");

        /// <summary>"Sonraki Güne Geç" (test botu da çağırır).</summary>
        public void NextDayPressed()
        {
            if (!DayCycle.Ended) return;
            ExitPlanMode();
            _shopTab = "mac";
            _dayendRoot.AddToClassList("hidden");
            DayCycle.NextDay();
        }

        void OnDayStarted(int day)
        {
            ShowBanner(Loc.F("GÜN {0}{1}", day, BranchSuffix), "sun", true, 2.2f);   // v1.5: şube adı
            if (Management.Sulky) Toast(Loc.T("Maaşlar ödenmedi: personel bugün küskün, yavaş çalışıyor"));
            Sfx.Play(SfxId.Dawn);
        }

        // ------------------------------------------------------------------ rehber (E)
        void OnTutorialChanged()
        {
            bool on = Tutorial.Active;
            _tutLayer.EnableInClassList("hidden", !on);
            if (!on) return;
            _tutText.text = Loc.T(Tutorial.Texts[Tutorial.Step]);
            _tutStep.text = Loc.F("ADIM {0} / {1}", Tutorial.Step + 1, Tutorial.StepCount);
            for (int i = 0; i < _tutPips.childCount; i++) _tutPips[i].EnableInClassList("on", i <= Tutorial.Step);
        }

        void UpdateTutorial()
        {
            if (!Tutorial.Active) return;
            // Hedef: dünya noktası ya da (v1.2) yeni açılan yan buton
            Rect hole;
            var t = Tutorial.Target;
            if (Tutorial.UiTarget != null)
            {
                var b = _root.Q<Button>(Tutorial.UiTarget).worldBound;
                var c = _root.WorldToLocal(b.center);
                hole = new Rect(c.x - 60f, c.y - 60f, 120f, 120f);
            }
            else if (t.HasValue && ToPanel(t.Value + Vector3.up * 0.5f, out var p))
                hole = new Rect(p.x - 75f, p.y - 75f, 150f, 150f);
            else hole = Rect.zero;
            bool show = hole.width > 0f && _introT < 0f;
            float W = _root.layout.width, H = _root.layout.height;
            void Set(VisualElement e, float x, float y, float w, float h)
            {
                e.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                e.style.left = x; e.style.top = y; e.style.width = Mathf.Max(0f, w); e.style.height = Mathf.Max(0f, h);
            }
            Set(_tutDim[0], 0, 0, W, hole.yMin);
            Set(_tutDim[1], 0, hole.yMax, W, H - hole.yMax);
            Set(_tutDim[2], 0, hole.yMin, hole.xMin, hole.height);
            Set(_tutDim[3], hole.xMax, hole.yMin, W - hole.xMax, hole.height);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.06f;
            Set(_tutRing, hole.x, hole.y, hole.width, hole.height);
            _tutRing.style.scale = new Scale(new Vector3(pulse, pulse, 1f));
        }

        // ------------------------------------------------------------------ oyun salonu etiketleri
        void UpdateGameTags()
        {
            foreach (var g in GameTable.All)
            {
                if (!_gameTags.TryGetValue(g, out var l))
                {
                    l = new Label { pickingMode = PickingMode.Ignore };
                    l.AddToClassList("dirty-tag");
                    l.AddToClassList("play-tag");
                    _world.Add(l);
                    _gameTags[g] = l;
                }
                bool ready = g.state == GameTable.State.Ready, playing = g.state == GameTable.State.Playing;
                l.text = playing ? $"{Loc.T(g.Title)} · {Mathf.RoundToInt(g.Progress01 * 100f)}%" : Loc.F("{0}: Oyna!", Loc.T(g.Title));
                l.EnableInClassList("busy", playing);
                bool on = ToPanel(g.TagPos, out var p);
                Place(l, p, on && (ready || playing) && !(ready && Automation.Has("oyun")));
            }
            var dead = new List<GameTable>();
            foreach (var kv in _gameTags) if (!kv.Key || !kv.Key.isActiveAndEnabled) { kv.Value.RemoveFromHierarchy(); dead.Add(kv.Key); }
            foreach (var d in dead) _gameTags.Remove(d);
        }

        // ------------------------------------------------------------------ Dükkân › Otomasyon (H)
        void BuildAutomationTab()
        {
            int owned = Automation.OwnedCount;
            _shopNote.text = Loc.F("USTA · {0}/{1} otomasyon", owned, Automation.List.Length) + (Automation.UstaOpen ? Loc.T(" · Usta geliştirmeleri açık") : Loc.T(" · ilk otomasyonla Usta geliştirmeleri açılır"));
            AddShopSection(Loc.T("OTOMASYON"));
            foreach (var d in Automation.List)
            {
                var dd = d;
                var can = Automation.CanBuy(d);
                string sub = can == Management.BuyResult.Owned ? Loc.F("Açık · {0}", Loc.T(d.desc)) : can == Management.BuyResult.LevelLow ? Loc.F("Seviye {0}'te açılır · {1}", d.level, Loc.T(d.desc)) : Loc.T(d.desc);
                AddShopItem(KitIcon(d.icon), Loc.T(d.name), sub, can, d.price, false, () => Automation.Buy(dd), d.level, "");
            }
            if (!Automation.UstaOpen) return;
            AddShopSection(Loc.T("USTA GELİŞTİRMELERİ"));
            AddUstaItem("gelir", Loc.T("Altın Kese"), Loc.F("Tüm gelir +%{0} (kalıcı)", Mathf.RoundToInt(Automation.UstaIncomePerLv * 100)), Automation.UstaIncomeLv, "coin");
            AddUstaItem("musteri", Loc.T("Ünlü Taverna"), Loc.F("Müşteri sıklığı +%{0} (kalıcı)", Mathf.RoundToInt(Automation.UstaGuestPerLv * 100)), Automation.UstaGuestLv, "horn");
        }

        void AddUstaItem(string which, string name, string effect, int lv, string icon)
        {
            bool max = lv >= Automation.UstaMaxLevel;
            long price = Automation.UstaPrice(lv);
            var state = max ? Management.BuyResult.Owned : GameState.Coins >= price ? Management.BuyResult.Ok : Management.BuyResult.NoMoney;
            AddShopItem(KitIcon(icon), Loc.F("{0} · Kademe {1}/{2}", name, lv, Automation.UstaMaxLevel), max ? Loc.T("En yüksek kademe") : effect, state, price, false,
                () => Automation.BuyUsta(which), 0, "");
        }

        // ------------------------------------------------------------------ ad değiştir (D)
        void OpenRename()
        {
            _settings.Hide();
            _root.Q<Button>("rename-ad").EnableInClassList("hidden", !Services.RewardedAdsEnabled);   // v1.6: reklam yoksa yalnızca altınla
            _renameField.value = GameState.TavernName;
            RefreshRename();
            Show(_renameRoot);
        }

        void RefreshRename() => _renameAdText.text = Loc.F("REKLAM {0}/{1}", GameState.Data.renameAds, Balance.RenameAds);

        bool ValidName(out string n)
        {
            n = (_renameField.value ?? "").Trim();
            if (n.Length < 3) { Toast(Loc.T("Ad en az 3 harf olmalı")); return false; }
            if (n == GameState.TavernName) { Toast(Loc.T("Bu zaten tavernanın adı")); return false; }
            return true;
        }

        void ApplyName(string n)
        {
            GameState.Data.tavernName = n;
            GameState.Save();
            GameState.NotifyChanged();
            _signLabel.text = n;
            _renameRoot.AddToClassList("hidden");
            Toast(Loc.F("Yeni ad: {0}", n));
            Confetti(18);
            Sfx.Play(SfxId.Buy);
        }
    }
}
