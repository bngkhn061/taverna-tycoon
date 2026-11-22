using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Taverna
{
    /// <summary>
    /// Oyun içi arayüz v3 "Mürekkep Çizgi" — yatay (design/ui/ui_v3_ink.html).
    /// Kaynaklar, sıradaki hedef + ok, seviye, joystick, müşteri baloncukları, istasyon/alan etiketleri,
    /// yükseltme çekmecesi, görev çekmecesi, mağaza, "Sen yokken…", seviye kartı, efektler.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public partial class HudController : MonoBehaviour
    {
        public Camera cam;
        public string menuScene = "MainMenu";

        const string IconPath = "project://database/Assets/_Game/Art/Icons/";
        VisualElement _root, _world, _fx, _joyZone, _joyBase, _joyKnob, _levelFill, _goal, _goalFill, _goalIcon, _coinPill;
        VisualElement _upgradePanel, _upgradeRows, _upgradeThumb, _questsPanel, _questRows, _shopRoot, _shopGrid;
        VisualElement _offlineRoot, _levelRoot, _unlocks, _rays, _goalArrow;
        Label _coins, _gems, _level, _xpText, _toast, _upgradeTitle, _upgradeInfo, _goalText, _goalNum, _rush, _questBadge;
        /// <summary>v1.8: hedef metni bu kadar karakteri aşınca küçük punto (iki satıra sığsın).</summary>
        const int GoalLongChars = 34;
        Label _offlineText, _offlineSub, _levelNum, _questReset;
        Label[] _tiers;
        VisualElement _starsRow, _dayReport;
        ScrollView _shopScroll;
        Label _shopBadge;
        Label _dayTitle, _dayText, _shopNote, _dayClock, _shopCoins;
        float _dayT, _starsT;
        readonly Dictionary<Table, Label> _dirtyTags = new();
        readonly Dictionary<Room, Label> _roomTags = new();
        readonly Dictionary<Guest, (VisualElement box, RingElement ring, Label fee)> _guestTags = new();
        Button _upgradeCta, _offlineDouble;
        SettingsPanel _settings;

        readonly Dictionary<Customer, (VisualElement box, RingElement ring, VisualElement icon, Label emote)> _bubbles = new();
        readonly Dictionary<DrinkStation, (VisualElement box, RingElement ring, Label text)> _stationTags = new();
        readonly Dictionary<PayZone, (VisualElement box, Label title, Label cost)> _zoneTags = new();
        readonly Dictionary<StaffBadge, VisualElement> _staffTags = new();
        VisualElement _depotPanel;
        ScrollView _depotRows;
        Label _depotInfo, _depotBadge;
        readonly List<Action> _depotUpdaters = new();
        float _depotT;
        int _depotStationCount = -1;
        Button _helperBtn;
        Label _helperText, _lanternCount;
        VisualElement _dayIcon;
        readonly Dictionary<Lantern, VisualElement> _lanternTags = new();
        bool _wasDarkHud;
        readonly List<(Label l, Vector3 world, float t)> _floats = new();
        readonly Queue<int> _levelQueue = new();

        DrinkStation _ctaStation, _panelStation;
        int _joyPointer = -1;
        Vector2 _joyCenter;
        float _toastT, _pulseT, _raysRot;
        const float JoyRadius = 46f;

        void OnEnable()
        {
            if (!cam) cam = Camera.main;
            _root = GetComponent<UIDocument>().rootVisualElement;
            Loc.Localize(_root);   // v1.7: UXML'deki sabit metinler etkin dile
            Loc.Changed += OnLanguageChanged;
            _world = _root.Q("world-layer");
            _fx = _root.Q("fx-layer");
            _coins = _root.Q<Label>("coins");
            _gems = _root.Q<Label>("gems");
            _level = _root.Q<Label>("level");
            _xpText = _root.Q<Label>("xp-text");
            _levelFill = _root.Q("level-fill");
            _coinPill = _root.Q("coin-pill");
            _toast = _root.Q<Label>("toast");
            _goal = _root.Q("goal");
            _goalText = _root.Q<Label>("goal-text");
            // v1.8: uzun hedef metni "…" ile kesilmesin: iki satıra kırılır, çok uzunsa punto küçülür (USS .goal-text.long)
            _goalText.RegisterCallback<ChangeEvent<string>>(e => _goalText.EnableInClassList("long", (e.newValue?.Length ?? 0) > GoalLongChars));
            _goalNum = _root.Q<Label>("goal-num");
            _goalFill = _root.Q("goal-fill");
            _goalIcon = _root.Q("goal-icon");
            _rush = _root.Q<Label>("rush");
            _upgradeCta = _root.Q<Button>("upgrade-cta");
            _upgradePanel = _root.Q("upgrade-panel");
            _upgradeRows = _root.Q("upgrade-rows");
            _upgradeTitle = _root.Q<Label>("upgrade-title");
            _upgradeInfo = _root.Q<Label>("upgrade-info");
            _upgradeThumb = _root.Q("upgrade-thumb");
            _tiers = new[] { _root.Q<Label>("tier1"), _root.Q<Label>("tier2"), _root.Q<Label>("tier3") };
            _questsPanel = _root.Q("quests-panel");
            _questRows = _root.Q("quest-rows");
            _questBadge = _root.Q<Label>("quest-badge");
            _questReset = _root.Q<Label>("quest-reset");
            _shopRoot = _root.Q("shop-root");
            _shopGrid = _root.Q("shop-grid");
            _offlineRoot = _root.Q("offline-root");
            _offlineText = _root.Q<Label>("offline-text");
            _offlineSub = _root.Q<Label>("offline-sub");
            _offlineDouble = _root.Q<Button>("offline-double");
            _levelRoot = _root.Q("levelup-root");
            _levelNum = _root.Q<Label>("levelup-num");
            _unlocks = _root.Q("unlocks");
            _rays = _root.Q("rays");

            _starsRow = _root.Q("stars");
            for (int i = 0; i < 5; i++)
            {
                var st = new VisualElement();
                st.AddToClassList("star"); // görsel USS'te (dolu / boş / yarım)
                _starsRow.Add(st);
            }
            _dayReport = _root.Q("day-report");
            _dayTitle = _root.Q<Label>("day-title");
            _dayText = _root.Q<Label>("day-text");
            _shopScroll = _root.Q<ScrollView>("shop-scroll");
            // Dikey liste: parmakla sürüklenir, kaydırma çubuğu gizli (fare tekerleği de çalışır)
            _shopScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _shopScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _shopScroll.mouseWheelScrollSize = 60f;
            _shopCoins = _root.Q<Label>("shop-coins");
            _dayClock = _root.Q<Label>("day-clock");
            _root.Q<Button>("day-ok").clicked += () => { _dayReport.AddToClassList("hidden"); _dayT = 0f; };
            _shopNote = _root.Q<Label>("shop-note");
            _shopBadge = _root.Q<Label>("shop-badge");
            _root.Q<Button>("tab-mac").clicked += () => SelectTab("mac");
            _root.Q<Button>("tab-staff").clicked += () => SelectTab("staff");
            _root.Q<Button>("tab-dec").clicked += () => SelectTab("dec");
            _root.Q<Button>("tab-boost").clicked += () => SelectTab("boost");
            DayCycle.DayEnded += OnDayEnded;
            Management.Changed += RefreshTop;
            // Dar ekran (16:9): hedef kutusu bir alt satıra
            _root.RegisterCallback<GeometryChangedEvent>(_ => _root.Q("hud-root").EnableInClassList("narrow", _root.layout.width < 780f));

            _goalArrow = new VisualElement { pickingMode = PickingMode.Ignore };
            _goalArrow.AddToClassList("goal-arrow");
            _world.Add(_goalArrow);


            _upgradeCta.clicked += () => { OpenUpgrade(_ctaStation); Sfx.Play(SfxId.Pop); };
            _root.Q<Button>("upgrade-close").clicked += CloseUpgrade;
            _root.Q<Button>("open-quests").clicked += () => { BuildQuests(); Show(_questsPanel); Tutorial.OnQuestsOpened(); };
            _root.Q<Button>("quests-close").clicked += () => _questsPanel.AddToClassList("hidden");
            _root.Q<Button>("open-shop").clicked += OpenShop;
            _root.Q<Button>("open-staff").clicked += () => { OpenShop("staff"); Tutorial.OnStaffOpened(); };
            _depotPanel = _root.Q("depot-panel");
            _depotRows = _root.Q<ScrollView>("depot-rows");
            _depotRows.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _depotRows.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _depotInfo = _root.Q<Label>("depot-info");
            _depotBadge = _root.Q<Label>("depot-badge");
            _root.Q<Button>("open-depot").clicked += OpenDepot;
            _root.Q<Button>("depot-close").clicked += () => _depotPanel.AddToClassList("hidden");
            _lanternCount = _root.Q<Label>("lantern-count");
            _dayIcon = _root.Q("day-icon");
            _helperBtn = _root.Q<Button>("helper-btn");
            _helperText = _root.Q<Label>("helper-text");
            _helperBtn.clicked += () =>
            {
                if (Campaigns.HelperActive || Campaigns.HelperWait > 0f) { Toast(Loc.T("Yardımcı şu an çağrılamaz")); return; }
                ShowAd("helper", Loc.T("3 dakikalık yardımcı garson"), () => { if (Campaigns.CallHelper()) { Toast(Loc.T("Yardımcı garson geldi! 3 dakika çalışacak")); Confetti(14); } });
            };
            Depot.Delivered += OnDelivered;
            Depot.RanOut += OnRanOut;
            Inn.StairsUsed += OnStairs;
            _root.Q<Button>("open-shop-plus").clicked += () => OpenShop("boost");
            _root.Q<Button>("shop-close").clicked += () => _shopRoot.AddToClassList("hidden");
            _root.Q<Button>("offline-collect").clicked += () => CollectOffline(1f);
            _offlineDouble.clicked += () =>
            {
                if (GameState.Data.gems < Boosts.OfflineDoubleGems) { Toast(Loc.T("Yeterli elmas yok")); return; }
                GameState.Data.gems -= Boosts.OfflineDoubleGems;
                CollectOffline(2f);
            };
            _root.Q<Button>("levelup-claim").clicked += CloseLevelUp;
            _root.schedule.Execute(ShowOfflineIfAny).StartingIn(500);
            // v1.6: kayıt yedekten açıldıysa / bozuk kayıt kenara alındıysa oyuncuya bir kez söyle
            _root.schedule.Execute(ShowLoadNotice).StartingIn(LoadNoticeDelayMs);

            _settings = new SettingsPanel(_root, showToMenu: true);
            _settings.ToMenuRequested += () => { GameState.Save(); SceneManager.LoadScene(menuScene); };
            _settings.ResetRequested += () => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            _root.Q<Button>("open-settings").clicked += () => { _settings.Show(); Sfx.Play(SfxId.Pop); };

            SetupJoystick();

            GameState.Changed += RefreshTop;
            GameState.LeveledUp += OnLevelUp;
            GameState.CoinsGainedAt += OnCoins;
            GameState.ZoneUnlocked += OnZoneUnlocked;
            Quests.Changed += OnQuestsChanged;
            RushHour.Started += OnRushStarted;
            RefreshTop();
            OnQuestsChanged();
            InitV09();
            InitPremium();
            InitTiers();
            InitV12();
            InitPrestige();
            InitV19();
            InitV21();
            InitV22();
            InitV23();   // v2.3 düzenleme modu ve bulut kaydı
            TestAdService.Presenter = PresentTestAd;   // v1.6: test reklamının bekleme ekranı
        }

        /// <summary>Kayıt bilgisi açılıştan bu kadar ms sonra gösterilir (giriş animasyonu otursun).</summary>
        const long LoadNoticeDelayMs = 1500;
        /// <summary>Kayıt bilgisinin ekranda kalma süresi (sn).</summary>
        const float LoadNoticeSeconds = 6f;

        /// <summary>v1.6: <see cref="GameState.LoadNotice"/> varsa uzun bir bildirim olarak gösterir ve siler.</summary>
        void ShowLoadNotice()
        {
            var msg = GameState.LoadNotice;
            if (string.IsNullOrEmpty(msg)) return;
            GameState.LoadNotice = null;
            Toast(msg);
            _toastT = LoadNoticeSeconds;
        }

        /// <summary>Test botu: kayıt bilgisini hemen göster (kayıt senaryosu).</summary>
        public void DebugShowLoadNotice() => ShowLoadNotice();

        /// <summary>
        /// v1.7: dil değişti → UXML metinleri, ayarlar, kurulurken yazılan dünya etiketleri ve açık paneller yeniden kurulur
        /// (sahne yeniden yüklenmez; her kare güncellenen metinler kendiliğinden yenilenir).
        /// </summary>
        void OnLanguageChanged()
        {
            Loc.Localize(_root);
            _settings.Refresh();
            foreach (var kv in _zoneTags) kv.Value.box.RemoveFromHierarchy();
            _zoneTags.Clear();
            foreach (var kv in _staffTags) kv.Value.RemoveFromHierarchy();
            _staffTags.Clear();
            foreach (var kv in _lanternTags) kv.Value.RemoveFromHierarchy();
            _lanternTags.Clear();
            RefreshTop();
            OnQuestsChanged();
            if (!_questsPanel.ClassListContains("hidden")) BuildQuests();
            if (!_depotPanel.ClassListContains("hidden")) BuildDepot();
            if (_panelStation) OpenUpgrade(_panelStation);
            OnTutorialChanged();
            if (!_happyCard.ClassListContains("hidden")) RefreshHappyCard();
        }

        void OnDisable()
        {
            Loc.Changed -= OnLanguageChanged;
            TestAdService.Presenter = null;
            GameState.Changed -= RefreshTop;
            GameState.LeveledUp -= OnLevelUp;
            GameState.CoinsGainedAt -= OnCoins;
            GameState.ZoneUnlocked -= OnZoneUnlocked;
            Quests.Changed -= OnQuestsChanged;
            RushHour.Started -= OnRushStarted;
            DayCycle.DayEnded -= OnDayEnded;
            Depot.Delivered -= OnDelivered;
            Depot.RanOut -= OnRanOut;
            Inn.StairsUsed -= OnStairs;
            DisableV09();
            DisablePremium();
            DisableTiers();
            DisablePrestige();
            DisableV12();
            DisableV21();
            DisableV22();
            DisableV23();
            Management.Changed -= RefreshTop;
            PlayerController.JoystickInput = Vector2.zero;
        }

        void Show(VisualElement e) { e.RemoveFromClassList("hidden"); Sfx.Play(SfxId.Pop); }

        // ------------------------------------------------------------------ otomatik test kancaları
        public bool LevelCardOpen => !_levelRoot.ClassListContains("hidden");
        /// <summary>Test botu (v2.0): açık kalan ayarlar penceresini kapatır (görüntüyü örtmesin).</summary>
        public void DebugCloseSettings() { if (_settings != null && _settings.Visible) _settings.Hide(); }

        public void DebugOpen(string what)
        {
            switch (what)
            {
                case "upgrade": OpenUpgrade(DrinkStation.All.Count > 0 ? DrinkStation.All[0] : null); break;
                case "quests": BuildQuests(); _questsPanel.RemoveFromClassList("hidden"); Tutorial.OnQuestsOpened(); break;
                case "shop": OpenShop("mac"); break;
                case "shop-staff": OpenShop("staff"); Tutorial.OnStaffOpened(); break;
                case "shop-dec": OpenShop("dec"); break;
                case "shop-auto": OpenShop("auto"); break;
                case "shop-prem": OpenShop("prem"); break;
                case "starter": OpenStarterOffer(); break;
                case "vip": OpenVipDetail(); break;
                case "tier": OpenTierCard(); break;
                case "tier-path": OpenTierPath(); break;
                case "branch": OpenBranchCard(); break;
                case "ending": OpenEndingCard(); break;
                case "settings": _settings.Show(); break;
                case "depot": OpenDepot(); break;
                case "events": OpenEvents("events"); break;
                case "season": OpenEvents("season"); break;   // v2.2
                case "happy": _happyCard.RemoveFromClassList("hidden"); RefreshHappyCard(); break;
                case "stars": DebugStarCard(true); break;
                case "rename": OpenRename(); break;
                default:
                    CloseUpgrade();
                    _questsPanel.AddToClassList("hidden");
                    _shopRoot.AddToClassList("hidden");
                    _depotPanel.AddToClassList("hidden");
                    _eventsPanel.AddToClassList("hidden");
                    _happyCard.AddToClassList("hidden");
                    _starCard.AddToClassList("hidden");
                    _offlineRoot.AddToClassList("hidden");
                    _renameRoot.AddToClassList("hidden");
                    if (PremModalOpen) _premModal.RemoveFromHierarchy();
                    if (LevelCardOpen) CloseLevelUp();
                    break;
            }
        }

        /// <summary>Herhangi bir pencere açık mı? (joystick'i kilitler)</summary>
        bool ModalOpen => _adOpen || Ending.Playing || V09ModalOpen || PremModalOpen || TierAnimOpen || _settings.Visible || !_shopRoot.ClassListContains("hidden") || !_offlineRoot.ClassListContains("hidden") ||
                          !_levelRoot.ClassListContains("hidden");

        // ------------------------------------------------------------------ üst çubuk
        void RefreshTop()
        {
            _coins.text = FormatNum(GameState.Coins);
            _coins.EnableInClassList("neg", GameState.InDebt);   // v1.9: borç kırmızı
            _coinPill.EnableInClassList("debt", GameState.InDebt);
            _gems.text = GameState.Data.gems.ToString();
            _level.text = GameState.Level.ToString();
            _levelFill.style.width = Length.Percent(GameState.LevelProgress01 * 100f);
            _xpText.text = $"{GameState.Data.un} / {Balance.UnForLevel(GameState.Level)}";
            RefreshBranchBadge();
            if (_shopCoins != null) _shopCoins.text = FormatNum(GameState.Coins);
            if (_panelStation) BuildUpgradeRows();
            if (!_shopRoot.ClassListContains("hidden")) BuildShop();
            else if (_planMode && _planTab != "depot") BuildPlan();
        }

        /// <summary>Sayı biçimi (bin ayırıcı nokta, 10 bin üstü kısaltma). v1.9: eksi sayılar (borç) "-1.250", "-12,5B".</summary>
        public static string FormatNum(long v) =>
            v < 0 ? "-" + FormatNum(v == long.MinValue ? long.MaxValue : -v) :
            v >= 1_000_000 ? Loc.F("{0}M", (v / 1_000_000f).ToString("0.#")) :
            v >= 10_000 ? Loc.F("{0}B", (v / 1000f).ToString("0.#")) :
            v.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));

        // UI v3: para ve elmas elle çizilmiş ikonlar (Resources/Kit/v3i_*.png); ürün resimleri hâlâ 3D render
        static Sprite _coinIcon, _gemIcon;
        static Sprite CoinIcon => _coinIcon ? _coinIcon : (_coinIcon = Resources.Load<Sprite>("Kit/v3i_coin"));
        static Sprite GemIcon => _gemIcon ? _gemIcon : (_gemIcon = Resources.Load<Sprite>("Kit/v3i_gem"));
        static Sprite KitIcon(string name) => Resources.Load<Sprite>("Kit/v3i_" + name);

        static readonly Dictionary<string, Sprite> _iconCache = new();
        static Sprite LoadIcon(string key)
        {
            if (_iconCache.TryGetValue(key, out var s)) return s;
            s = Resources.Load<Sprite>("Icons/ICO_" + key);
            _iconCache[key] = s;
            return s;
        }

        // ------------------------------------------------------------------ joystick
        void SetupJoystick()
        {
            _joyZone = _root.Q("joy-zone");
            _joyBase = _root.Q("joy-base");
            _joyKnob = _root.Q("joy-knob");
            _joyZone.RegisterCallback<PointerDownEvent>(e =>
            {
                if (_joyPointer != -1 || ModalOpen) return;
                _joyPointer = e.pointerId;
                _joyZone.CapturePointer(e.pointerId);
                _joyCenter = e.localPosition;
                _joyBase.style.left = _joyCenter.x - 58f;
                _joyBase.style.top = _joyCenter.y - 58f;
                _joyBase.style.bottom = StyleKeyword.Auto;
                _joyBase.AddToClassList("active");
                SetKnob(Vector2.zero);
            });
            _joyZone.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (e.pointerId != _joyPointer) return;
                var d = Vector2.ClampMagnitude((Vector2)e.localPosition - _joyCenter, JoyRadius);
                SetKnob(d);
                PlayerController.JoystickInput = new Vector2(d.x, -d.y) / JoyRadius;
            });
            void Release(int id)
            {
                if (id != _joyPointer) return;
                _joyZone.ReleasePointer(id);
                _joyPointer = -1;
                _joyBase.style.left = StyleKeyword.Null;
                _joyBase.style.top = StyleKeyword.Null;
                _joyBase.style.bottom = StyleKeyword.Null;
                _joyBase.RemoveFromClassList("active");
                SetKnob(Vector2.zero);
                PlayerController.JoystickInput = Vector2.zero;
            }
            _joyZone.RegisterCallback<PointerUpEvent>(e => Release(e.pointerId));
            _joyZone.RegisterCallback<PointerCancelEvent>(e => Release(e.pointerId));
        }

        void SetKnob(Vector2 offset)
        {
            _joyKnob.style.left = 30f + offset.x;
            _joyKnob.style.top = 30f + offset.y;
        }

        // ------------------------------------------------------------------ her kare
        /// <summary>Teşhis (otomatik test): dünya ve efekt katmanındaki eleman sayısı.</summary>
        public static int DebugWorldCount, DebugFxCount;

        void LateUpdate()
        {
            DebugWorldCount = _world != null ? _world.childCount : 0;
            DebugFxCount = _fx != null ? _fx.childCount : 0;
            if (!cam || _root.panel == null) return;
            UpdateCustomerBubbles();
            UpdateStationTags();
            UpdateZoneTags();
            UpdateStaffTags();
            UpdateDepot();
            UpdateHelperButton();
            UpdateNight();
            UpdateFloats();
            UpdateUpgradeCta();
            UpdateGoal();
            if (GameState.InDebt && _goal.style.display != DisplayStyle.None)
            {
                // v1.9: borçluyken hedef kutusu "x / y" yerine borcu gösterir
                _goalText.text = Loc.T("Önce borcu öde");
                _goalNum.text = FormatNum(GameState.Coins);
                _goalFill.style.width = Length.Percent(0f);
                _goal.EnableInClassList("ready", false);
            }
            UpdateRush();
            UpdateDirtyTags();
            UpdateInnTags();
            UpdateV09();
            UpdateV12();
            UpdatePremium();
            UpdateTierTransform();
            UpdateV19();
            UpdateV21();
            UpdateV22();
            UpdateV23();
            _starsT -= Time.unscaledDeltaTime;
            if (_starsT <= 0f) { _starsT = 0.5f; UpdateStars(); }
            if (_dayT > 0f) { _dayT -= Time.unscaledDeltaTime; if (_dayT <= 0f) _dayReport.AddToClassList("hidden"); }
            if (_dayClock != null)
            {
                int left = Mathf.Max(0, Mathf.CeilToInt(DayCycle.RemainingSeconds));
                // v2.0 (madde 15): kapanışta saat yerine "kapanış"; son siparişler başlayınca bir kez bildirim
                _dayClock.text = DayCycle.Closing ? Loc.F("GÜN {0} · KAPANIŞ", GameState.Data.day)
                                                  : Loc.F("GÜN {0} · {1}:{2:00}", GameState.Data.day, left / 60, left % 60);
                bool lastCall = DayCycle.LastCall && !DayCycle.Ended;
                if (lastCall && !_lastCallShown && !Tutorial.Active) Toast(Loc.T("Son siparişler! Yeni müşteri gelmiyor"));
                _lastCallShown = lastCall;
            }
            if (!_levelRoot.ClassListContains("hidden"))
            {
                _raysRot += Time.unscaledDeltaTime * 20f;
                _rays.style.rotate = new Rotate(_raysRot);
            }
            if (_toastT > 0f) { _toastT -= Time.unscaledDeltaTime; if (_toastT <= 0f) _toast.AddToClassList("hidden"); }
            if (_pulseT > 0f) { _pulseT -= Time.unscaledDeltaTime; if (_pulseT <= 0f) _coinPill.RemoveFromClassList("pulse"); }
        }

        bool _lastCallShown;

        bool ToPanel(Vector3 world, out Vector2 pos)
        {
            var vp = cam.WorldToViewportPoint(world);
            pos = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, world, cam);
            return vp.z > 0f && vp.x > -0.1f && vp.x < 1.1f && vp.y > -0.1f && vp.y < 1.1f;
        }

        static void Place(VisualElement e, Vector2 p, bool visible)
        {
            e.style.left = p.x;
            e.style.top = p.y;
            e.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void UpdateCustomerBubbles()
        {
            foreach (var c in Customer.All)
            {
                if (!_bubbles.TryGetValue(c, out var b))
                {
                    var box = new VisualElement { pickingMode = PickingMode.Ignore };
                    box.AddToClassList("bubble");
                    var ring = new RingElement { lineWidth = 5f };
                    ring.AddToClassList("bubble-ring");
                    var icon = new VisualElement();
                    icon.AddToClassList("bubble-icon");
                    var emote = new Label();
                    emote.AddToClassList("emote");
                    box.Add(ring); box.Add(icon); box.Add(emote);
                    _world.Add(box);
                    b = (box, ring, icon, emote);
                    _bubbles[c] = b;
                }
                bool waiting = c.state == Customer.State.Waiting;
                bool angry = c.emote == Customer.Emote.Angry;
                bool show = waiting || angry;
                bool onScreen = ToPanel(c.HeadPos, out var p);
                Place(b.box, p, show && onScreen);
                if (!show) continue;
                b.box.EnableInClassList("angry", angry);
                b.box.EnableInClassList("special", waiting && c.special);   // v1.3 Barlo'ya özel: altın çerçeve + yıldız
                b.ring.style.display = waiting ? DisplayStyle.Flex : DisplayStyle.None;
                b.icon.style.display = waiting ? DisplayStyle.Flex : DisplayStyle.None;
                b.emote.text = angry ? "!" : "";
                if (waiting)
                {
                    b.ring.Set(c.Patience01, RingElement.PatienceColor(c.Patience01));
                    var def = GameDatabase.I.Drink(c.wanted);
                    if (def && def.icon) b.icon.style.backgroundImage = new StyleBackground(def.icon);
                }
            }
            var dead = new List<Customer>();
            foreach (var kv in _bubbles) if (!kv.Key) { kv.Value.box.RemoveFromHierarchy(); dead.Add(kv.Key); }
            foreach (var d in dead) _bubbles.Remove(d);
        }

        void UpdateStationTags()
        {
            foreach (var s in DrinkStation.All)
            {
                if (!_stationTags.TryGetValue(s, out var t))
                {
                    var box = new VisualElement { pickingMode = PickingMode.Ignore };
                    box.AddToClassList("station-tag");
                    box.AddToClassList("pill");
                    var ring = new RingElement { lineWidth = 4f };
                    ring.style.width = 20; ring.style.height = 20; ring.style.marginRight = 5;
                    var text = new Label();
                    text.AddToClassList("station-tag-text");
                    // v1.9: malzeme az / bitti iken depo ikonu
                    var dep = new VisualElement();
                    dep.AddToClassList("station-tag-depot");
                    dep.style.backgroundImage = new StyleBackground(KitIcon("stock"));
                    box.Add(ring); box.Add(text); box.Add(dep);
                    _world.Add(box);
                    t = (box, ring, text);
                    _stationTags[s] = t;
                }
                bool on = ToPanel(s.transform.position + Vector3.up * 1.55f, out var p);
                Place(t.box, p, on);
                bool empty = s.OutOfSupply && s.Stock == 0;
                // v1.1 (oyuncu testi): malzeme azalınca etiket kırmızı yanıp sönsün, depoya bakmayı hatırlatsın
                int sup = Depot.Supply(s.drink.id);
                bool low = !empty && Depot.Incoming(s.drink.id) == 0 && sup <= Depot.Capacity(s.drink.id) * Balance.AutoOrderThreshold;
                t.box.EnableInClassList("empty", empty);
                t.box.EnableInClassList("low", low);
                t.box.EnableInClassList("blink", (empty || low) && Mathf.Repeat(Time.unscaledTime, 0.9f) < 0.45f);
                // v1.9 (oyuncu testi "malzeme 2 anlaşılmıyor"): açık metin + depo ikonu
                t.text.text = empty ? Loc.T("Malzeme bitti · depoya git") : low ? Loc.F("{0}/{1} · Malzeme az · {2} kaldı", s.Stock, s.MaxStock, sup) : $"{s.Stock}/{s.MaxStock}";
                t.box.Q(className: "station-tag-depot").style.display = empty || low ? DisplayStyle.Flex : DisplayStyle.None;
                t.ring.Set(s.Stock >= s.MaxStock ? 1f : s.Progress01, new Color(0.95f, 0.76f, 0.31f));
            }
            var dead = new List<DrinkStation>();
            foreach (var kv in _stationTags) if (!kv.Key || !kv.Key.isActiveAndEnabled) { kv.Value.box.RemoveFromHierarchy(); dead.Add(kv.Key); }
            foreach (var d in dead) _stationTags.Remove(d);
        }

        void UpdateZoneTags()
        {
            foreach (var z in PayZone.All)
            {
                if (!_zoneTags.TryGetValue(z, out var t))
                {
                    var box = new VisualElement { pickingMode = PickingMode.Ignore };
                    box.AddToClassList("zone-tag");
                    var title = new Label(z.DisplayLabel);
                    title.AddToClassList("zone-title");
                    var row = new VisualElement();
                    row.AddToClassList("zone-cost");
                    var ic = new VisualElement();
                    ic.AddToClassList("zone-cost-icon");
                    var cost = new Label();
                    cost.AddToClassList("zone-cost-text");
                    row.Add(ic); row.Add(cost);
                    box.Add(title); box.Add(row);
                    _world.Add(box);
                    t = (box, title, cost);
                    _zoneTags[z] = t;
                }
                bool on = ToPanel(z.transform.position + Vector3.up * 0.2f, out var p);
                bool locked = !z.LevelOk;
                // Uzak kamerada kalabalık olmasın: kilitliyse sadece sıradaki hedef ve büyük dönüm noktaları
                bool milestone = (z.id == "salon" || z.id == "bahce") && GameState.Level >= z.requiredLevel - 2;
                bool worthShowing = !locked || z == PayZone.NextGoal() || milestone;
                Place(t.box, p, on && z.Visible && z.enabled && worthShowing);
                t.box.EnableInClassList("locked", locked);
                var ico = t.cost.parent[0];
                ico.style.backgroundImage = locked ? new StyleBackground(KitIcon("lock")) : new StyleBackground(CoinIcon);
                t.cost.text = locked ? Loc.F("Seviye {0}", z.requiredLevel) : GameState.InDebt ? Loc.T("Önce borcu öde") : FormatNum(z.Remaining);
                t.box.EnableInClassList("debt", !locked && GameState.InDebt);
            }
            var dead = new List<PayZone>();
            foreach (var kv in _zoneTags)
                if (!kv.Key || !kv.Key.isActiveAndEnabled) { kv.Value.box.RemoveFromHierarchy(); dead.Add(kv.Key); }
            foreach (var d in dead) _zoneTags.Remove(d);
        }

        /// <summary>Görevlilerin baş üstü etiketi: rol renginde çip, rol ikonu + isim (müşterilerden ayırt edilsin).</summary>
        void UpdateStaffTags()
        {
            foreach (var b in StaffBadge.All)
            {
                if (!_staffTags.TryGetValue(b, out var box))
                {
                    box = new VisualElement { pickingMode = PickingMode.Ignore };
                    box.AddToClassList("staff-tag");
                    box.style.backgroundColor = StaffBadge.RoleColor(b.role);
                    var ic = new VisualElement();
                    ic.AddToClassList("staff-tag-icon");
                    ic.style.backgroundImage = new StyleBackground(KitIcon(StaffBadge.RoleIcon(b.role)));
                    var l = new Label($"{Loc.T(b.displayName)} · {StaffBadge.RoleName(b.role)}");
                    l.AddToClassList("staff-tag-text");
                    box.Add(ic); box.Add(l);
                    _world.Add(box);
                    _staffTags[b] = box;
                }
                bool on = ToPanel(b.transform.position + Vector3.up * b.headHeight, out var p);
                Place(box, p, on);
            }
            var dead = new List<StaffBadge>();
            foreach (var kv in _staffTags)
                if (!kv.Key || !kv.Key.isActiveAndEnabled) { kv.Value.RemoveFromHierarchy(); dead.Add(kv.Key); }
            foreach (var d in dead) _staffTags.Remove(d);
        }

        // ------------------------------------------------------------------ depo (v0.6)
        void OpenDepot()
        {
            Tutorial.OnDepotOpened();
            CloseUpgrade();
            BuildDepot();
            Show(_depotPanel);
        }

        void OnDelivered(DrinkId id, int n)
        {
            var def = GameDatabase.I.Drink(id);
            if (n > 0) Toast(Loc.F("Teslimat geldi: {0} +{1}", (def ? Loc.T(def.displayName) : id.ToString()), n));
            Sfx.Play(SfxId.Claim);
        }

        void OnRanOut(DrinkId id)
        {
            var def = GameDatabase.I.Drink(id);
            Toast(Loc.F("{0} malzemesi bitti! Depodan sipariş ver", (def ? Loc.T(def.displayName) : id.ToString())));
            Sfx.Play(SfxId.Error);
        }

        /// <summary>Rozet + açık depo panelinin canlı değerleri (yeniden kurmadan: butonlar basılırken kaybolmasın).</summary>
        void UpdateDepot()
        {
            bool warn = false;
            foreach (var s in DrinkStation.All)
            {
                if (!s || !s.drink) continue;
                var id = s.drink.id;
                if (Depot.Incoming(id) == 0 && Depot.Supply(id) <= Depot.Capacity(id) * Balance.AutoOrderThreshold) warn = true;
            }
            _depotBadge.EnableInClassList("hidden", !warn);
            _depotBadge.EnableInClassList("blink", warn && Mathf.Repeat(Time.unscaledTime, 0.9f) < 0.45f);
            if (_depotPanel.ClassListContains("hidden")) return;
            if (DrinkStation.All.Count != _depotStationCount) BuildDepot();
            _depotT -= Time.unscaledDeltaTime;
            if (_depotT > 0f) return;
            _depotT = 0.2f;
            foreach (var u in _depotUpdaters) u();
        }

        void BuildDepot()
        {
            _depotRows.Clear();
            _depotUpdaters.Clear();
            _depotStationCount = DrinkStation.All.Count;
            var seen = new HashSet<DrinkId>();
            foreach (var st in DrinkStation.All)
            {
                if (!st || !st.drink || st.Seasonal || !seen.Add(st.drink.id)) continue;   // v2.2: sezon tezgâhı depo kullanmaz
                AddDepotRow(st.drink);
            }
            AddDeliveryRow();
            AddAutoOrderRow();
            _depotUpdaters.Add(() => _depotInfo.text = Loc.F("teslimat {0:0} sn", Depot.DeliverySeconds));
            foreach (var u in _depotUpdaters) u();
        }

        void AddDepotRow(DrinkDef def)
        {
            var id = def.id;
            var row = new VisualElement();
            row.AddToClassList("item");
            var img = new VisualElement();
            img.AddToClassList("item-img");
            if (def.icon) img.style.backgroundImage = new StyleBackground(def.icon);
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var name = new Label();
            name.AddToClassList("item-name");
            var bar = new VisualElement();
            bar.AddToClassList("q-bar");
            var fill = new VisualElement();
            fill.AddToClassList("q-fill");
            bar.Add(fill);
            var sub = new Label();
            sub.AddToClassList("depot-sub");
            col.Add(name); col.Add(bar); col.Add(sub);

            var btns = new VisualElement();
            btns.AddToClassList("depot-btns");
            var order = PriceButton("btn-teal", CoinIcon, out var orderText);
            order.clicked += () =>
            {
                var r = Depot.Order(id);
                if (r == Depot.OrderResult.Ok) { Sfx.Play(SfxId.Buy); GameSettings.Vibrate(); }
                else Toast(r == Depot.OrderResult.Full ? Loc.T("Raf dolu") : Management.NoGoldText);
            };
            var second = PriceButton("btn-paper", CoinIcon, out var secondText);
            second.clicked += () =>
            {
                if (Depot.Incoming(id) > 0 && Services.RewardedAdsEnabled)
                    ShowAd("rush_delivery", Loc.T("Siparişin hemen gelsin"), () => Depot.RushDelivery(id));
                else if (Depot.CanUpgradeShelf(id))
                {
                    if (Depot.UpgradeShelf(id)) { Sfx.Play(SfxId.Buy); Confetti(8); }
                    else Toast(Management.NoGoldText);
                }
            };
            btns.Add(order); btns.Add(second);
            row.Add(img); row.Add(col); row.Add(btns);
            _depotRows.Add(row);

            _depotUpdaters.Add(() =>
            {
                int sup = Depot.Supply(id), cap = Depot.Capacity(id), inc = Depot.Incoming(id);
                name.text = Loc.F("{0} · {1}/{2} bardak kaldı", Loc.T(def.displayName), sup, cap);
                float f = cap > 0 ? sup / (float)cap : 0f;
                fill.style.width = Length.Percent(f * 100f);
                fill.EnableInClassList("empty", sup == 0);
                fill.EnableInClassList("low", sup > 0 && f <= Balance.AutoOrderThreshold);
                sub.EnableInClassList("alert", sup == 0);
                // v1.9 (oyuncu testi "raf açıklayıcı değil"): rafın ne yaptığı ve yükseltmenin getirdiği, somut
                int lvShelf = Depot.DepotLevel(id);
                string shelf = Depot.CanUpgradeShelf(id)
                    ? Loc.F("Raf: {0} → {1} bardaklık malzeme · makine daha geç durur", cap, Balance.DepotCapacity(lvShelf + 1))
                    : Loc.F("Raf: {0} bardaklık malzeme (en büyük)", cap);
                sub.text = inc > 0 ? Loc.F("Yolda +{0} · {1} sn", inc, Mathf.CeilToInt(Depot.Eta(id))) :
                           sup == 0 ? Loc.T("Malzeme bitti! Sipariş ver") : shelf;
                bool room = Depot.HasRoom(id);
                int cost = Depot.PackCost(def);
                orderText.text = room ? $"+{Balance.SupplyPack} · {FormatNum(cost)}" : Loc.T("DOLU");
                order.EnableInClassList("locked", !room || (GameState.Coins < cost && !GameState.InDebt));   // v1.9: borçluyken sipariş borca yazılır
                if (inc > 0 && !Services.RewardedAdsEnabled)
                {
                    // v1.6 yayın güvenliği: reklam yokken "HEMEN" (reklamla hızlandırma) gösterilmez
                    second.RemoveFromClassList("btn-plum"); second.AddToClassList("btn-paper");
                    second.Q(className: "price-icon").style.backgroundImage = new StyleBackground(KitIcon("clock"));
                    secondText.text = Loc.T("YOLDA");
                    second.AddToClassList("locked");
                }
                else if (inc > 0)
                {
                    second.RemoveFromClassList("btn-paper"); second.AddToClassList("btn-plum");
                    second.Q(className: "price-icon").style.backgroundImage = new StyleBackground(KitIcon("play"));
                    secondText.text = Loc.T("HEMEN");
                    second.RemoveFromClassList("locked");
                }
                else
                {
                    second.RemoveFromClassList("btn-plum"); second.AddToClassList("btn-paper");
                    bool can = Depot.CanUpgradeShelf(id);
                    int up = can ? Balance.DepotUpgradeCost(def, Depot.DepotLevel(id)) : 0;
                    second.Q(className: "price-icon").style.backgroundImage = new StyleBackground(can ? CoinIcon : KitIcon("check"));
                    secondText.text = can ? Loc.F("RAF · {0}", FormatNum(up)) : Loc.T("RAF MAX");
                    second.EnableInClassList("locked", !can || GameState.Coins < up);
                }
            });
        }

        void AddDeliveryRow()
        {
            var row = SimpleRow(KitIcon("clock"), out var name, out var sub, out var btn, out var btnText);
            btn.clicked += () => { if (Depot.UpgradeDelivery()) { Sfx.Play(SfxId.Buy); Confetti(8); } else Toast(Management.NoGoldText); };
            _depotRows.Add(row);
            _depotUpdaters.Add(() =>
            {
                int lv = GameState.Data.deliveryLv;
                bool max = lv >= Balance.MaxDeliveryLevel;
                name.text = Loc.F("Teslimat · Sv {0}", lv);
                sub.text = max ? Loc.F("Sipariş {0:0} sn'de gelir (en hızlı)", Balance.DeliverySeconds(lv)) : Loc.F("Sipariş {0:0} sn → {1:0} sn'de gelir · raf daha az boş kalır", Balance.DeliverySeconds(lv), Balance.DeliverySeconds(lv + 1));
                int cost = max ? 0 : Balance.DeliveryUpgradeCost(lv);
                btnText.text = max ? Loc.T("MAX") : FormatNum(cost);
                btn.EnableInClassList("locked", max || GameState.Coins < cost);
            });
        }

        void AddAutoOrderRow()
        {
            var row = SimpleRow(KitIcon("quest"), out var name, out var sub, out var btn, out var btnText);
            btn.clicked += () =>
            {
                if (!Depot.AutoOrderAvailable) { Toast(Loc.F("Seviye {0} gerekli", Balance.AutoOrderLevel)); return; }
                if (Depot.BuyAutoOrder()) { Sfx.Play(SfxId.Buy); Confetti(12); } else if (!GameState.Data.autoOrder) Toast(Management.NoGoldText);
            };
            _depotRows.Add(row);
            _depotUpdaters.Add(() =>
            {
                bool on = GameState.Data.autoOrder, avail = Depot.AutoOrderAvailable;
                name.text = Loc.T("Tedarikçi anlaşması");
                sub.text = on ? Loc.T("Raf azalınca kendiliğinden sipariş verir") : Loc.F("Raf %{0:0}'in altına inince otomatik sipariş", Balance.AutoOrderThreshold * 100);
                btnText.text = on ? Loc.T("AKTİF") : avail ? FormatNum(Balance.AutoOrderPrice) : Loc.F("SV {0}", Balance.AutoOrderLevel);
                btn.EnableInClassList("locked", on || !avail || GameState.Coins < Balance.AutoOrderPrice);
            });
        }

        VisualElement SimpleRow(Sprite icon, out Label name, out Label sub, out Button btn, out Label btnText)
        {
            var row = new VisualElement();
            row.AddToClassList("item");
            var ico = new VisualElement();
            ico.AddToClassList("ico");
            var ii = new VisualElement();
            ii.AddToClassList("ico-img");
            ii.style.backgroundImage = new StyleBackground(icon);
            ico.Add(ii);
            var col = new VisualElement();
            col.AddToClassList("item-col");
            name = new Label();
            name.AddToClassList("item-name");
            sub = new Label();
            sub.AddToClassList("depot-sub");
            col.Add(name); col.Add(sub);
            btn = PriceButton("btn-teal", CoinIcon, out btnText);
            btn.AddToClassList("btn-sm");
            row.Add(ico); row.Add(col); row.Add(btn);
            return row;
        }

        static Button PriceButton(string variant, Sprite icon, out Label text)
        {
            var b = new Button();
            b.AddToClassList("btn"); b.AddToClassList(variant);
            var pi = new VisualElement();
            pi.AddToClassList("price-icon");
            pi.style.backgroundImage = new StyleBackground(icon);
            text = new Label();
            b.Add(pi); b.Add(text);
            return b;
        }

        /// <summary>Gece/gündüz: gün ikonu, fener sayacı, sönük fenerin üstünde "Yak!" etiketi, akşam uyarısı.</summary>
        void UpdateNight()
        {
            bool dark = Night.IsDark;
            if (dark != _wasDarkHud)
            {
                _wasDarkHud = dark;
                _dayIcon.style.backgroundImage = new StyleBackground(KitIcon(dark ? "moon" : "sun"));
                if (dark) { Toast(Loc.T("Akşam oldu! Fenerleri yak")); Sfx.Play(SfxId.Nightfall); }
                else Toast(Loc.T("Sabah oldu"));
            }
            var (lit, total) = Night.LanternCount();
            _lanternCount.EnableInClassList("hidden", !dark || total == 0);
            _lanternCount.EnableInClassList("done", lit >= total);
            _lanternCount.text = Loc.F("Fener {0}/{1}", lit, total);
            foreach (var l in Lantern.All)
            {
                if (!_lanternTags.TryGetValue(l, out var tag))
                {
                    tag = new VisualElement { pickingMode = PickingMode.Ignore };
                    tag.AddToClassList("lantern-tag");
                    var ic = new VisualElement();
                    ic.AddToClassList("staff-tag-icon");
                    ic.style.backgroundImage = new StyleBackground(KitIcon("sun"));
                    tag.Add(ic);
                    tag.Add(new Label(Loc.T("Yak!")));
                    _world.Add(tag);
                    _lanternTags[l] = tag;
                }
                bool on = ToPanel(l.transform.position + Vector3.up * 0.55f, out var p);
                Place(tag, p, on && dark && !l.lit && l.ZoneOpen);
            }
        }

        void UpdateHelperButton()
        {
            _helperBtn.EnableInClassList("hidden", !Services.RewardedAdsEnabled);   // v1.6: reklam yoksa yardımcı çağrılmaz
            static string Mmss(float s) { int t = Mathf.CeilToInt(s); return $"{t / 60}:{t % 60:00}"; }
            if (Campaigns.HelperActive) { _helperText.text = Loc.F("YARDIMCI {0}", Mmss(Campaigns.HelperRemaining)); _helperBtn.AddToClassList("locked"); }
            else if (Campaigns.HelperWait > 0f) { _helperText.text = Loc.F("YARDIMCI · {0}", Mmss(Campaigns.HelperWait)); _helperBtn.AddToClassList("locked"); }
            else { _helperText.text = Loc.T("YARDIMCI ÇAĞIR"); _helperBtn.RemoveFromClassList("locked"); }
        }

        void AddShopSection(string title)
        {
            var l = new Label(title);
            l.AddToClassList("depot-section");
            _shopScroll.Add(l);
        }

        /// <summary>v1.4 dekor kartı eki: sonraki yıldıza kalan ambiyans (5★'da boş).</summary>
        static string StarHint() => Management.NextStar == 0 ? ""
            : Loc.F(" · ★{0}{1} {2}", Management.NextStar, Loc.IsTurkish ? (Management.NextStar == 2 ? "'ye" : "'e") : "", Management.AmbianceToNextStar);

        /// <summary>Dükkân > Dekor: zemin/duvar teması kartı (satın al · kullan · seçili).</summary>
        void AddThemeItem(Themes.Theme t)
        {
            bool owned = Themes.Owned(t);
            bool current = Themes.Current(t.kind) == t.id;
            var can = Themes.CanBuy(t);
            var item = new VisualElement();
            item.AddToClassList("item");
            item.EnableInClassList("owned", current);
            item.EnableInClassList("locked", can == Management.BuyResult.LevelLow);
            if (!t.premium && NewBadge(t.id, t.level)) AddNewBadge(item);   // v1.9
            var ico = new VisualElement();
            ico.AddToClassList("ico");
            var ii = new VisualElement();
            ii.AddToClassList("ico-img");
            ii.style.backgroundImage = new StyleBackground(KitIcon(t.kind == Themes.Kind.Floor ? "brush" : "home"));
            ico.Add(ii);
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var n = new Label(Loc.T(t.name));
            n.AddToClassList("item-name");
            var sub = new Label((t.kind == Themes.Kind.Floor ? Loc.T("Zemin") : Loc.T("Duvar")) +(t.ambiance > 0 ? Loc.F(" · +{0:0} ambiyans", t.ambiance) + StarHint() : Loc.T(" · varsayılan")));
            sub.AddToClassList("item-sub");
            col.Add(n); col.Add(sub);
            Button b;
            Label text;
            if (current) { b = PriceButton("btn-paper", KitIcon("check"), out text); text.text = Loc.T("SEÇİLİ"); b.AddToClassList("owned"); b.SetEnabled(false); }
            else if (owned) { b = PriceButton("btn-paper", KitIcon("brush"), out text); text.text = Loc.T("KULLAN"); }
            else if (can == Management.BuyResult.LevelLow) { b = PriceButton("btn-paper", KitIcon("lock"), out text); text.text = Loc.F("SV {0}", t.level); b.AddToClassList("locked"); }
            else if (can == Management.BuyResult.Premium) { b = PriceButton("btn-plum", GemIcon, out text); text.text = Loc.T("HAZİNE"); }
            else { b = PriceButton("btn-teal", CoinIcon, out text); text.text = FormatNum(t.price); b.EnableInClassList("locked", can != Management.BuyResult.Ok); }
            b.AddToClassList("btn-sm");
            b.clicked += () =>
            {
                if (can == Management.BuyResult.Premium) { OpenShop("prem"); return; }
                var r = Themes.BuyOrSelect(t);
                if (r == Management.BuyResult.Ok) { Sfx.Play(owned ? SfxId.Pop : SfxId.Buy); if (!owned) Confetti(16); }
                else Toast(Management.BuyMessage(r, t.level));
                BuildShop();
            };
            item.Add(ico); item.Add(col); item.Add(b);
            _shopScroll.Add(item);
        }

        /// <summary>Dükkân > Personel: işe alınmış görevlinin gelişim kartı (para veya reklam).</summary>
        void AddStaffUpgradeItem(Catalog.Staff s)
        {
            int lv = StaffUpgrades.Level(s.id);
            bool max = lv >= StaffUpgrades.MaxLevel;
            var item = new VisualElement();
            item.AddToClassList("item");
            item.AddToClassList("owned");
            var img = new VisualElement();
            img.AddToClassList("item-img");
            var icon = LoadIcon(s.icon);
            if (icon) img.style.backgroundImage = new StyleBackground(icon);
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var n = new Label(Loc.F("{0} · {1} · Sv {2}/{3}", s.name, Loc.T(s.roleName), lv, StaffUpgrades.MaxLevel));
            n.AddToClassList("item-name");
            var sub = new Label(max ? StaffUpgrades.EffectText(s, lv) + Loc.T(" (en yüksek)") : $"{StaffUpgrades.EffectText(s, lv)} → {StaffUpgrades.EffectText(s, lv + 1)}");
            sub.AddToClassList("item-sub");
            col.Add(n); col.Add(sub);
            var btns = new VisualElement();
            btns.AddToClassList("staff-btns");
            int cost = StaffUpgrades.Cost(s, lv);
            var buy = PriceButton("btn-teal", max ? KitIcon("check") : CoinIcon, out var buyText);
            buyText.text = max ? Loc.T("MAX") : FormatNum(cost);
            buy.EnableInClassList("locked", max || GameState.Coins < cost);
            buy.clicked += () =>
            {
                if (StaffUpgrades.TryBuy(s)) { Sfx.Play(SfxId.Buy); Confetti(10); GameSettings.Vibrate(); BuildShop(); }
                else Toast(max ? Loc.T("En yüksek seviyede") : Management.NoGoldText);
            };
            btns.Add(buy);
            // v1.3: kov (normal) ya da izne gönder / geri çağır (Hazine); ilk dokunuş sorar, ikincisi yapar
            var fire = new Button();
            fire.AddToClassList("btn"); fire.AddToClassList("btn-sm"); fire.AddToClassList("btn-brick"); fire.AddToClassList("fire-btn");
            bool leave = Management.OnLeave(s.id);
            string fireText = s.premium ? (leave ? Loc.T("GERİ ÇAĞIR") : Loc.T("İZNE GÖNDER")) : Loc.T("KOV");
            var fireLabel = new Label(fireText);
            fire.Add(fireLabel);
            bool armed = false;
            fire.clicked += () =>
            {
                if (!armed && !(s.premium && leave))
                {
                    armed = true;
                    fireLabel.text = Loc.T("EMİN MİSİN?");
                    fire.schedule.Execute(() => { armed = false; fireLabel.text = fireText; }).StartingIn(2500);
                    return;
                }
                if (!Management.Fire(s)) return;
                Sfx.Play(SfxId.Pop, 0.8f);
                Toast(s.premium ? (Management.OnLeave(s.id) ? Loc.F("{0} izne çıktı", s.name) : Loc.F("{0} işe döndü", s.name))
                                : Loc.F("{0} kovuldu · maaşı durdu, istersen yeniden işe alabilirsin", s.name));
                BuildShop();
            };
            if (!max && StaffUpgrades.AdsLeft(s.id) > 0 && !leave && Services.RewardedAdsEnabled)   // v1.6: reklam yoksa düğme yok
            {
                float wait = StaffUpgrades.AdWait(s.id);
                bool longAd = StaffUpgrades.AdIsLong(s);
                var ad = PriceButton("btn-plum", KitIcon("play"), out var adText);
                adText.text = wait > 0f ? Loc.F("{0} dk", Mathf.CeilToInt(wait / 60f)) : (longAd ? Loc.F("UZUN REKLAM {0}/{1}", StaffUpgrades.AdsLeft(s.id), StaffUpgrades.MaxAdLevels) : Loc.F("REKLAM {0}/{1}", StaffUpgrades.AdsLeft(s.id), StaffUpgrades.MaxAdLevels));
                ad.EnableInClassList("locked", wait > 0f);
                ad.clicked += () =>
                {
                    if (!StaffUpgrades.AdAvailable(s)) { Toast(Loc.T("Reklam hakkı yakında yenilenir")); return; }
                    ShowAd("staff_upgrade", Loc.F("{0} bir seviye gelişsin", s.name), () => { StaffUpgrades.GrantByAd(s.id); Confetti(10); if (!_shopRoot.ClassListContains("hidden")) BuildShop(); },
                           longAd ? AdLength.Long : AdLength.Short);
                };
                btns.Add(ad);
            }
            btns.Add(fire);
            if (leave) { item.AddToClassList("locked"); buy.SetEnabled(false); }
            item.Add(img); item.Add(col); item.Add(btns);
            _shopScroll.Add(item);
        }

        // ------------------------------------------------------------------ ödüllü reklam (v0.6 sahte ekran, v1.6 IAdService)
        bool _adOpen;
        /// <summary>Test botu: bu süreçte verilen reklam ödülü sayısı (yayın güvenliği testi).</summary>
        public static int DebugAdRewards;

        /// <summary>
        /// Ödüllü reklam ister (<see cref="Services.Ads"/>): reklam sonuna kadar izlenirse ödülü verir. Ölçüm: ad_shown, ad_rewarded.
        /// v1.6 yayın güvenliği: reklam hizmeti kapalıysa (yayın derlemesi, gerçek ağ yok) hiçbir şey olmaz, ödül verilmez.
        /// VIP reklamsız ödül alır (yalnızca reklamlar açıkken).
        /// </summary>
        public void ShowAd(string placement, string reason, Action onReward, AdLength length = AdLength.Short)
        {
            if (_adOpen) return;
            if (!Services.RewardedAdsEnabled) { Debug.LogWarning($"[Reklam] kapalı, ödül verilmedi: {placement}"); return; }
            void Reward()
            {
                DebugAdRewards++;
                Services.Track("ad_rewarded", ("neden", placement));
                onReward?.Invoke();
                Sfx.Play(SfxId.Claim);
            }
            if (Premium.IsVip) { Toast(Loc.T("VIP: reklamsız ödül")); Reward(); return; }
            if (!Services.Ads.IsRewardedReady) { Toast(Loc.T("Reklam şu an hazır değil")); return; }
            Services.Track("ad_shown", ("neden", placement), ("uzun", length == AdLength.Long));
            _adOpen = true;
            Services.Ads.ShowRewarded(placement, reason, length,
                () => { _adOpen = false; Reward(); },
                why => { _adOpen = false; Services.Track("ad_cancelled", ("neden", placement), ("sebep", why)); });
        }

        /// <summary>Test reklamının bekleme ekranı (<see cref="TestAdService.Presenter"/>): <paramref name="seconds"/> sn sayar, sonra bitirir.</summary>
        void PresentTestAd(string reason, float seconds, Action done)
        {
            var overlay = new VisualElement();
            overlay.AddToClassList("screen"); overlay.AddToClassList("overlay");
            var card = new VisualElement();
            card.AddToClassList("modal"); card.AddToClassList("v3-panel"); card.AddToClassList("ad-card");
            var rib = new Label(Loc.T("REKLAM"));
            rib.AddToClassList("ribbon"); rib.AddToClassList("ribbon-plum");
            var why = new Label(reason);
            why.AddToClassList("item-name");
            var count = new Label(Mathf.CeilToInt(seconds).ToString());
            count.AddToClassList("ad-count");
            var note = new Label(Loc.T("Test sürümü: gerçek reklam yerine bekleme ekranı"));
            note.AddToClassList("small-note");
            card.Add(rib); card.Add(why); card.Add(count); card.Add(note);
            overlay.Add(card);
            _root.Q("hud-root").Add(overlay);
            float t0 = Time.unscaledTime;
            overlay.schedule.Execute(() =>
            {
                float left = seconds - (Time.unscaledTime - t0);
                count.text = Mathf.CeilToInt(Mathf.Max(0f, left)).ToString();
                if (left > 0f) return;
                overlay.RemoveFromHierarchy();
                done?.Invoke();
            }).Every(100).Until(() => overlay.parent == null);
        }

        void OnCoins(long amount, Vector3 world)
        {
            CoinFly(world, (int)Mathf.Clamp(amount / 3, 2, 7));
            var l = new Label("+" + FormatNum(amount)) { pickingMode = PickingMode.Ignore };
            l.AddToClassList("float-text");
            _world.Add(l);
            _floats.Add((l, world + Vector3.up * 0.6f, 0f));
        }

        void UpdateFloats()
        {
            for (int i = _floats.Count - 1; i >= 0; i--)
            {
                var (l, w, t) = _floats[i];
                t += Time.deltaTime;
                ToPanel(w + Vector3.up * t * 0.9f, out var p);
                l.style.left = p.x - 18f;
                l.style.top = p.y;
                l.style.opacity = 1f - Mathf.Clamp01((t - 0.5f) / 0.4f);
                if (t > 0.9f) { l.RemoveFromHierarchy(); _floats.RemoveAt(i); }
                else _floats[i] = (l, w, t);
            }
        }

        // ------------------------------------------------------------------ hedef zinciri
        static Sprite ZoneIcon(PayZone z)
        {
            foreach (var t in z.unlockTargets)
            {
                if (!t) continue;
                var st = t.GetComponentInChildren<DrinkStation>(true);
                if (st && st.drink && st.drink.icon) return st.drink.icon;
            }
            return CoinIcon;
        }

        /// <summary>Sırada alınabilecek en ucuz makine (seviye ve bölge uygunsa).</summary>
        static Catalog.Machine? NextMachine()
        {
            Catalog.Machine? best = null;
            foreach (var m in Catalog.Machines)
            {
                var r = Management.CanBuyMachine(m);
                if (r != Management.BuyResult.Ok && r != Management.BuyResult.NoMoney) continue;
                if (best == null || m.price < best.Value.price) best = m;
            }
            return best;
        }

        void UpdateGoal()
        {
            var nm = NextMachine();
            var zz = PayZone.NextGoal();
            bool shopReady = false;
            if (nm != null && (!zz || !zz.LevelOk || nm.Value.price <= zz.Remaining))
            {
                var m = nm.Value;
                var def = GameDatabase.I.Drink(m.drink);
                bool ok = GameState.Coins >= m.price;
                shopReady = ok;
                _goal.style.display = DisplayStyle.Flex;
                _goalIcon.style.backgroundImage = new StyleBackground(def.icon);
                _goalText.text = ok ? Loc.F("{0} makinesi alınabilir! Mağaza", Loc.T(def.displayName)) : Loc.F("Sıradaki: {0} makinesi", Loc.T(def.displayName));
                _goalNum.text = $"{FormatNum(Math.Min(GameState.Coins, m.price))} / {FormatNum(m.price)}";
                _goalFill.style.width = Length.Percent(Mathf.Clamp01(GameState.Coins / (float)Mathf.Max(1, m.price)) * 100f);
                _goal.EnableInClassList("ready", ok);
                _goalArrow.style.display = DisplayStyle.None;
                _shopBadge.EnableInClassList("hidden", !shopReady);
                return;
            }
            _shopBadge.EnableInClassList("hidden", true);
            if (UpdateBranchGoal(zz)) return;   // v1.5: Sv30'da prestij şartları (kademe eksikse o da burada)
            if (UpdateTierGoal(zz)) return;
            var z = zz;
            _goal.style.display = z ? DisplayStyle.Flex : DisplayStyle.None;
            if (!z) { _goalArrow.style.display = DisplayStyle.None; return; }
            _goalIcon.style.backgroundImage = new StyleBackground(ZoneIcon(z));
            bool ready = z.LevelOk && GameState.Coins >= z.Remaining;
            if (!z.LevelOk)
            {
                _goalText.text = Loc.F("Sıradaki: {0}", z.DisplayLabel);
                _goalNum.text = Loc.F("Seviye {0}", z.requiredLevel);
                _goalFill.style.width = Length.Percent(GameState.Level == z.requiredLevel - 1 ? GameState.LevelProgress01 * 100f : 0f);
            }
            else
            {
                _goalText.text = ready ? Loc.F("{0} hazır! Kareye git", z.DisplayLabel) : Loc.F("Sıradaki: {0}", z.DisplayLabel);
                _goalNum.text = $"{FormatNum(Math.Min(GameState.Coins, z.Remaining))} / {FormatNum(z.Remaining)}";
                _goalFill.style.width = Length.Percent(Mathf.Clamp01(GameState.Coins / (float)Mathf.Max(1, z.Remaining)) * 100f);
            }
            _goal.EnableInClassList("ready", ready);

            bool arrowOn = z.LevelOk;
            _goalArrow.style.display = arrowOn ? DisplayStyle.Flex : DisplayStyle.None;
            if (arrowOn) PlaceGoalArrow(z.transform.position + Vector3.up * 0.3f);
        }

        /// <summary>Hedef okunu dünya noktasına (ekran dışındaysa kenara, yönü gösterecek şekilde) yerleştirir.</summary>
        void PlaceGoalArrow(Vector3 world)
        {
            var vp = cam.WorldToViewportPoint(world);
            var p = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, world, cam);
            var size = _root.layout.size;
            bool inside = vp.z > 0 && vp.x > 0.05f && vp.x < 0.92f && vp.y > 0.12f && vp.y < 0.8f;
            float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 10f;
            if (inside)
            {
                _goalArrow.style.left = p.x;
                _goalArrow.style.top = p.y - bob;
                _goalArrow.style.rotate = new Rotate(0);
            }
            else
            {
                var c = size * 0.5f;
                var dir = (p - c).normalized;
                var edge = c + new Vector2(dir.x * size.x * 0.42f, dir.y * size.y * 0.36f);
                _goalArrow.style.left = edge.x;
                _goalArrow.style.top = edge.y + 40f;
                _goalArrow.style.rotate = new Rotate(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            }
        }

        void UpdateStars()
        {
            float s = Management.Stars;
            for (int i = 0; i < 5; i++)
            {
                var e = _starsRow[i];
                e.EnableInClassList("off", s < i + 0.5f);
                e.EnableInClassList("half", s >= i + 0.5f && s < i + 1f);
            }
        }

        void OnDayEnded(DayCycle.Report r)
        {
            // v0.9: kısa rapor şeridi yerine "Gün Bitti + Yarını Planla" (zaman durur)
            OpenDayEnd(r);
        }

        void UpdateDirtyTags()
        {
            foreach (var t in Table.All)
            {
                if (!_dirtyTags.TryGetValue(t, out var l))
                {
                    l = new Label(Loc.T("Temizle!")) { pickingMode = PickingMode.Ignore };
                    l.AddToClassList("dirty-tag");
                    _world.Add(l);
                    _dirtyTags[t] = l;
                }
                bool on = ToPanel(t.transform.position + Vector3.up * 0.9f, out var p);
                Place(l, p, on && t.Dirty && !t.Busy);
            }
            var dead = new List<Table>();
            foreach (var kv in _dirtyTags) if (!kv.Key || !kv.Key.isActiveAndEnabled) { kv.Value.RemoveFromHierarchy(); dead.Add(kv.Key); }
            foreach (var d in dead) _dirtyTags.Remove(d);
        }

        // ------------------------------------------------------------------ han (v0.8)
        void OnStairs(bool up) => Toast(up ? Loc.T("Üst kat · Han odaları") : Loc.T("Salon"));

        /// <summary>Resepsiyonda bekleyen misafir: yatak simgesi + sabır halkası + gecelik ücret; odalarda Zzz / Topla!</summary>
        void UpdateInnTags()
        {
            foreach (var g in Guest.All)
            {
                if (!_guestTags.TryGetValue(g, out var t))
                {
                    var box = new VisualElement { pickingMode = PickingMode.Ignore };
                    box.AddToClassList("bubble");
                    var ring = new RingElement { lineWidth = 5f };
                    ring.AddToClassList("bubble-ring");
                    var icon = new VisualElement();
                    icon.AddToClassList("bubble-icon");
                    icon.style.backgroundImage = new StyleBackground(LoadIcon("Yatak"));
                    var fee = new Label();
                    fee.AddToClassList("guest-fee");
                    box.Add(ring); box.Add(icon); box.Add(fee);
                    _world.Add(box);
                    t = (box, ring, fee);
                    _guestTags[g] = t;
                }
                bool waiting = g.state == Guest.State.Waiting;
                bool on = ToPanel(g.HeadPos, out var p);
                Place(t.box, p, waiting && on);
                if (!waiting) continue;
                t.ring.Set(g.Patience01, RingElement.PatienceColor(g.Patience01));
                t.fee.text = g.room ? $"{g.room.Fee}" : "";
            }
            var deadG = new List<Guest>();
            foreach (var kv in _guestTags) if (!kv.Key) { kv.Value.box.RemoveFromHierarchy(); deadG.Add(kv.Key); }
            foreach (var d in deadG) _guestTags.Remove(d);

            foreach (var r in Room.All)
            {
                if (!_roomTags.TryGetValue(r, out var l))
                {
                    l = new Label { pickingMode = PickingMode.Ignore };
                    l.AddToClassList("dirty-tag");
                    _world.Add(l);
                    _roomTags[r] = l;
                }
                bool dirty = r.state == Room.State.Dirty, sleep = r.state == Room.State.Occupied;
                l.text = dirty ? Loc.T("Topla!") : Loc.T("Zzz");
                l.EnableInClassList("sleep-tag", sleep);
                bool on = ToPanel(r.bedPoint.position + Vector3.up * 0.9f, out var p);
                Place(l, p, on && (dirty || sleep));
            }
            var deadR = new List<Room>();
            foreach (var kv in _roomTags) if (!kv.Key || !kv.Key.isActiveAndEnabled) { kv.Value.RemoveFromHierarchy(); deadR.Add(kv.Key); }
            foreach (var d in deadR) _roomTags.Remove(d);
        }

        /// <summary>Dükkân > Dekor: han odasının konfor kademesi (yatak) — gecelik ücret 40 → 80 → 150.</summary>
        void AddRoomItem(int i)
        {
            bool open = Inn.RoomOpen(i);
            int c = Inn.Comfort(i);
            var item = new VisualElement();
            item.AddToClassList("item");
            item.EnableInClassList("locked", !open);
            var ico = new VisualElement();
            ico.AddToClassList("ico");
            var ii = new VisualElement();
            ii.AddToClassList("ico-img");
            ii.style.backgroundImage = new StyleBackground(LoadIcon("Yatak"));
            ico.Add(ii);
            var col = new VisualElement();
            col.AddToClassList("item-col");
            // v2.0 (madde 19): konfor seviyesi (Sv0–4) ve sıradaki seviyenin getirdiği eşya, ücret ve misafir artışı
            var n = new Label(Loc.F("Oda {0} · Konfor {1}/{2}", i + 1, c, Inn.Fees.Length - 1));
            n.AddToClassList("item-name");
            int arrivalPct = Mathf.RoundToInt(Inn.ComfortArrivalStep * 100f);
            var sub = new Label(!open ? Loc.T("Üst katta ödeme karesiyle açılır")
                : Inn.CanUpgrade(i) ? Loc.F("Sonraki: {0} · gecelik {1} → {2} · misafir %{3} artar", Loc.T(Inn.ComfortNames[c + 1]), Inn.Fee(i), Inn.Fees[c + 1], arrivalPct)
                : Loc.F("{0} · gecelik {1} · en yüksek konfor", Loc.T(Inn.ComfortNames[c]), Inn.Fee(i)));
            sub.AddToClassList("item-sub");
            col.Add(n); col.Add(sub);
            Label text;
            Button b;
            if (!open) { b = PriceButton("btn-paper", KitIcon("lock"), out text); text.text = Loc.T("KAPALI"); b.AddToClassList("locked"); }
            else if (!Inn.CanUpgrade(i)) { b = PriceButton("btn-paper", KitIcon("check"), out text); text.text = Loc.T("EN İYİ"); b.AddToClassList("owned"); b.SetEnabled(false); }
            else { b = PriceButton("btn-teal", CoinIcon, out text); text.text = FormatNum(Inn.UpgradeCost(i)); b.EnableInClassList("locked", GameState.Data.coins < Inn.UpgradeCost(i)); }
            b.AddToClassList("btn-sm");
            b.clicked += () =>
            {
                if (!open) { Toast(Loc.T("Bu oda henüz açılmadı")); return; }
                if (Inn.UpgradeComfort(i)) { Sfx.Play(SfxId.Buy); Confetti(14); }
                else Toast(Inn.CanUpgrade(i) ? Loc.T("Yeterli altın yok") : Loc.T("En iyi yatak zaten"));
                BuildShop();
            };
            item.Add(ico); item.Add(col); item.Add(b);
            _shopScroll.Add(item);
        }

        void OnZoneUnlocked(string id)
        {
            Confetti(28);
            Toast(Loc.T("YENİ ALAN AÇILDI!"));
        }

        void Toast(string text)
        {
            _toast.text = text;
            _toast.RemoveFromClassList("hidden");
            _toastT = 1.8f;
        }

        // ------------------------------------------------------------------ yoğun saat
        void OnRushStarted() => Confetti(15);

        void UpdateRush()
        {
            // v1.4: tellal artık etkinlik; şeridi etkinlik şeridi (HudV12)
            // v1.5: yalnızca elmasla alınan 10 dk ×2 gelir (kalıcı çarpanlar — Usta, VIP, şube — şerit açmaz)
            bool on = RushHour.Active || Boosts.IncomeRemaining > 0f;
            _rush.EnableInClassList("hidden", !on);
            if (RushHour.Active) _rush.text = Loc.F("YOĞUN SAAT!  ×2 bahşiş · {0} sn", Mathf.CeilToInt(RushHour.Remaining));
            else if (on) _rush.text = Loc.F("×2 GELİR · {0} dk", Mathf.CeilToInt(Boosts.IncomeRemaining / 60f));
        }

        // ------------------------------------------------------------------ görevler
        void OnQuestsChanged()
        {
            int n = Quests.ReadyCount;
            _questBadge.text = n.ToString();
            _questBadge.EnableInClassList("hidden", n == 0);
            if (!_questsPanel.ClassListContains("hidden")) BuildQuests();
        }

        static string QuestIcon(QuestSave q) => q.type switch
        {
            QuestType.ServeDrink => q.drink.ToString(),
            QuestType.EarnCoins => "ParaYigini",
            QuestType.ServeCustomers => "Bira",
            QuestType.RushHour => "EjderAtesi",
            QuestType.SpecialOrder or QuestType.SpecialOrderBig => "Main_Barlo",   // v1.4: "Barlo'ya özel" siparişler
            QuestType.OwnMachine => q.drink.ToString(),
            QuestType.HireStaff => StaffIcon(q.key),
            _ => "Elmas",
        };

        static string StaffIcon(string id)
        {
            foreach (var s in Catalog.StaffList) if (s.id == id) return s.icon;
            return "Elmas";
        }

        void BuildQuests()
        {
            _questRows.Clear();
            for (int i = 0; i < Quests.List.Count; i++)
            {
                var q = Quests.List[i];
                int idx = i;
                var row = new VisualElement();
                row.AddToClassList("row");
                var img = new VisualElement();
                img.AddToClassList("q-img");
                img.style.backgroundImage = new StyleBackground(LoadIcon(QuestIcon(q)));
                var text = new VisualElement();
                text.AddToClassList("q-text");
                var name = new Label(Quests.Text(q));
                name.AddToClassList("q-name");
                var reward = new VisualElement();
                reward.AddToClassList("q-reward");
                AddReward(reward, CoinIcon, "+" + FormatNum(q.rewardCoins));
                if (q.rewardGems > 0) AddReward(reward, GemIcon, "+" + q.rewardGems);
                var bar = new VisualElement();
                bar.AddToClassList("q-bar");
                var fill = new VisualElement();
                fill.AddToClassList("q-fill");
                fill.style.width = Length.Percent(q.progress * 100f / Mathf.Max(1, q.target));
                bar.Add(fill);
                text.Add(name); text.Add(bar); text.Add(reward);
                row.Add(img); row.Add(text);
                row.EnableInClassList("done", q.Done);
                if (q.Done)
                {
                    var claim = new Button { text = Loc.T("TOPLA") };
                    claim.AddToClassList("btn");
                    claim.AddToClassList("btn-sm");
                    claim.style.minWidth = 104;
                    claim.clicked += () => { if (Quests.Claim(idx)) { Sfx.Play(SfxId.Claim); Confetti(20); } };
                    row.Add(claim);
                }
                else
                {
                    var prog = new Label($"{FormatNum(q.progress)} / {FormatNum(q.target)}");
                    prog.AddToClassList("chip");
                    reward.Add(prog);
                }
                _questRows.Add(row);
            }
            var left = DateTime.Today.AddDays(1) - DateTime.Now;
            _questReset.text = Loc.F("yenilenme {0:00}:{1:00}", left.Hours, left.Minutes);
        }

        static void AddReward(VisualElement parent, Sprite icon, string text)
        {
            var i = new VisualElement();
            i.AddToClassList("q-reward-icon");
            i.style.backgroundImage = new StyleBackground(icon);
            var l = new Label(text);
            l.AddToClassList("q-reward-text");
            parent.Add(i); parent.Add(l);
        }

        // ------------------------------------------------------------------ mağaza (sekmeli)
        string _shopTab = "mac";

        void OpenShop() => OpenShop(_shopTab);

        void OpenShop(string tab)
        {
            Tutorial.OnShopOpened();
            _newSession.Clear();
            _shopTab = tab;
            BuildShop();
            Show(_shopRoot);
        }

        void SelectTab(string tab)
        {
            if (tab == "staff") Tutorial.OnStaffOpened();   // v1.9: rehberin Personel adımı dükkân sekmesinden de geçer
            _shopTab = tab;
            Sfx.Play(SfxId.Pop);
            BuildShop();
        }

        void BuildShop()
        {
            foreach (var (name, id) in new[] { ("tab-mac", "mac"), ("tab-staff", "staff"), ("tab-dec", "dec"), ("tab-auto", "auto"), ("tab-prem", "prem"), ("tab-boost", "boost") })
                _root.Q<Button>(name).EnableInClassList("on", _shopTab == id);
            _shopScroll.Clear();
            switch (_shopTab)
            {
                case "mac":
                    _shopNote.text = Loc.T("Seviye atladıkça yeni makineler açılır · satın alınca yerine kurulur");
                    foreach (var m in Catalog.Machines)
                    {
                        var def = GameDatabase.I.Drink(m.drink);
                        if (!def || m.drink == DrinkId.Bira) continue;
                        var can = Management.CanBuyMachine(m);
                        int lv = Management.MachineLevel(m.drink);
                        string sub = can switch
                        {
                            Management.BuyResult.Owned => Loc.T("Kuruldu"),
                            Management.BuyResult.LevelLow => Loc.F("Seviye {0}{1} açılır", lv, Locative(lv)),
                            Management.BuyResult.ZoneLocked => Loc.F("Önce {0}", Loc.T(m.zoneName)),
                            _ => Loc.F("Yeni içecek · {0} altın/bardak · {1:0} sn'de bir bardak", def.basePrice, Balance.ProduceTime(def, 0)),
                        };
                        var mm = m;
                        AddShopItem(def.icon, Loc.T(def.displayName), sub, can, m.price, false,
                            () => Management.BuyMachine(mm), lv, Loc.T(m.zoneName), null, Catalog.MachineKey(m.drink), lv);
                    }
                    break;
                case "staff":
                    _shopNote.text = Loc.F("Personel her gün maaş alır · şu an günlük maaş: {0} altın", FormatNum(Management.DailyWages));
                    // v2.1: Barlo'nun yetenekleri sekmenin başında (rehber sürerken işe alım kartı önde kalsın diye sonda)
                    if (!Tutorial.Active) AddBarloSection();
                    if (!Tutorial.Active) AddShopSection(Loc.T("PERSONEL"));
                    foreach (var s in Catalog.StaffList)
                    {
                        var can = Management.CanHire(s);
                        if (can == Management.BuyResult.Owned) { AddStaffUpgradeItem(s); continue; }
                        if (s.premium) continue;   // efsanevi personel sadece Hazine'de
                        string sub = can == Management.BuyResult.Owned ? Loc.F("Çalışıyor · {0}/gün", Management.WageOf(s)) :
                                     can == Management.BuyResult.LevelLow ? Loc.F("Seviye {0}{1} açılır", s.level, Locative(s.level)) :
                                     can == Management.BuyResult.ZoneLocked ? Loc.F("Önce {0} açılmalı", Loc.T(s.zoneName)) : $"{Loc.T(s.desc)}";
                        var ss = s;
                        AddShopItem(LoadIcon(s.icon), $"{s.name} · {Loc.T(s.roleName)}", sub, can, Management.HireCost(s), false,
                            () => Management.Hire(ss), s.level, Loc.T(s.zoneName) ?? "", Loc.F("Maaş {0}/gün", Management.WageOf(s)), s.id, s.level);
                    }
                    if (Tutorial.Active) AddBarloSection();
                    break;
                case "dec":
                    // v1.1 (oyuncu testi: "artı puan neye?"): ambiyansın neye yaradığı açıkça
                    // v1.4: artan eşikli yıldız ölçeği (Balance.StarThresholds)
                    _shopNote.text = Management.NextStar == 0
                        ? Loc.F("Ambiyans {0:0} · ★5, en yüksek yıldız · ★ zengin müşteri ve bahşiş getirir", AmbianceSource.Total)
                        : Loc.F("Ambiyans {0:0} · ★{1:0.#} · sonraki ★ için {2} · ★ = zengin müşteri + bahşiş (yıldıza dokun)", AmbianceSource.Total, Management.Stars, Management.AmbianceToNextStar);
                    // Zemin ve duvar temaları (v0.6): bir kez alınır, istenince ücretsiz seçilir
                    if (Inn.Open)
                    {
                        AddShopSection(Loc.T("HAN ODALARI"));
                        for (int i = 0; i < Inn.RoomCount; i++) AddRoomItem(i);
                    }
                    AddShopSection(Loc.T("ZEMİN VE DUVAR"));
                    foreach (var t in Themes.List) AddThemeItem(t);
                    AddShopSection(Loc.T("SÜSLER"));
                    foreach (var d in Catalog.DecorList)
                    {
                        if (d.premium) continue;   // premium süsler Hazine'de
                        var can = Management.CanBuyDecor(d);
                        int owned = Management.DecorOwned(d.id);
                        string sub = can == Management.BuyResult.LevelLow ? Loc.F("Seviye {0}{1} açılır", d.level, Locative(d.level)) :
                                     can == Management.BuyResult.NoSlot ? (owned > 0 ? Loc.F("{0} adet · yer kalmadı", owned) : Loc.T("Yeni alan açılınca")) :
                                     Loc.F("+{0:0} ambiyans", d.ambiance) + StarHint() + (owned > 0 ? Loc.F(" · {0} adet", owned) : "");
                        var dd = d;
                        AddShopItem(LoadIcon(d.icon), Loc.T(d.name), sub, can, d.price, false, () => Management.BuyDecor(dd), d.level, "", null, d.id, d.level);
                    }
                    break;
                case "auto":
                    BuildAutomationTab();
                    AddTierShopRow();
                    break;
                case "prem":
                    BuildPremiumTab();
                    break;
                default:
                    _shopNote.text = Loc.T("Elmaslar görevlerden, seviye ve günlük ödüllerden kazanılır · ilerlemek için gerekmez");
                    // v1.9: her hızlandırmanın ne yaptığı somut
                    // v1.4: "Tellal tut" buradan kalktı, Etkinlikler'e taşındı
                    foreach (var o in Boosts.Offers)
                    {
                        var oo = o;
                        string name = o.id == "coins" ? Loc.F("{0} Altın", FormatNum(Boosts.CoinPackAmount)) : Loc.T(o.name);
                        var can = GameState.Data.gems >= o.gems ? Management.BuyResult.Ok : Management.BuyResult.NoMoney;
                        AddShopItem(LoadIcon(o.icon), name, BoostDesc(o.id), can, o.gems, true,
                            () => Boosts.Buy(oo.id) ? Management.BuyResult.Ok : Management.BuyResult.NoMoney, 0, "");
                    }
                    break;
            }
            RefreshTabBadges();   // v1.9: açık sekmenin ürünleri görüldü → rozeti kalkar
        }

        /// <summary>v1.9: hızlandırmanın ne yaptığı (dükkân › Hızlandır).</summary>
        static string BoostDesc(string id) => id switch
        {
            "x2" => Loc.T("10 dakika bütün satışlar ve bahşişler iki kat"),
            "refill" => Loc.T("Bütün makineler hemen dolu bardakla"),
            "rush" => Loc.F("Yoğun saat şimdi: müşteri gelişi ×{0:0.#}, bahşiş ×2", Balance.RushSpawnMultiplier),
            "coins" => Loc.T("Kesedeki altın seviyenle büyür"),
            _ => "",
        };

        void AddShopItem(Sprite icon, string name, string sub, Management.BuyResult state, long price, bool gems,
                         Func<Management.BuyResult> buy, int level, string zone, string extra = null, string newKey = null, int newLevel = 0)
        {
            var item = new VisualElement();
            item.AddToClassList("item");
            if (NewBadge(newKey, newLevel)) AddNewBadge(item);
            bool levelLocked = state == Management.BuyResult.LevelLow || state == Management.BuyResult.ZoneLocked;
            item.EnableInClassList("owned", state == Management.BuyResult.Owned);
            item.EnableInClassList("locked", levelLocked);
            var img = new VisualElement();
            img.AddToClassList("item-img");
            if (icon) img.style.backgroundImage = new StyleBackground(icon);
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var n = new Label(name);
            n.AddToClassList("item-name");
            var s = new Label(extra != null ? $"{sub} · {extra}" : sub);
            s.AddToClassList("item-sub");
            col.Add(n);
            if (!string.IsNullOrEmpty(s.text)) col.Add(s);
            item.Add(img); item.Add(col);

            var b = new Button();
            b.AddToClassList("btn"); b.AddToClassList("btn-sm");
            var pi = new VisualElement();
            pi.AddToClassList("price-icon");
            b.Add(pi);
            if (state == Management.BuyResult.Owned)
            {
                b.AddToClassList("owned");
                pi.style.backgroundImage = new StyleBackground(KitIcon("check"));
                b.Add(new Label(gems ? Loc.T("ALINDI") : Loc.T("VAR")));
                b.SetEnabled(false);
            }
            else if (levelLocked)
            {
                b.AddToClassList("locked");
                pi.style.backgroundImage = new StyleBackground(KitIcon("lock"));
                b.Add(new Label(state == Management.BuyResult.LevelLow ? Loc.F("SV {0}", level) : Loc.T("KİLİTLİ")));
                b.clicked += () => Toast(Management.BuyMessage(state, level, zone));
            }
            else
            {
                b.AddToClassList("btn-teal");
                pi.style.backgroundImage = new StyleBackground(gems ? GemIcon : CoinIcon);
                b.Add(new Label(price == 0 ? Loc.T("Bedava") : FormatNum(price)));
                b.EnableInClassList("locked", state != Management.BuyResult.Ok);
                b.clicked += () =>
                {
                    var r = buy();
                    if (r == Management.BuyResult.Ok) { Sfx.Play(SfxId.Buy); Confetti(16); GameSettings.Vibrate(); }
                    else Toast(gems && r == Management.BuyResult.NoMoney ? Loc.T("Yeterli elmas yok") : Management.BuyMessage(r, level, zone));
                    BuildShop();
                    RefreshTop();
                };
            }
            item.Add(b);
            _shopScroll.Add(item);
        }

        // ------------------------------------------------------------------ çevrimdışı
        void ShowOfflineIfAny()
        {
            if (Offline.Pending <= 0) return;
            int m = Offline.Minutes;
            string sure = m >= 60 ? Loc.F("{0} sa {1} dk", m / 60, m % 60) : Loc.F("{0} dk", m);
            _offlineSub.text = Loc.F("{0} boyunca taverna çalıştı", sure);
            _offlineText.text = $"+{FormatNum(Offline.Pending)}";
            _offlineDouble.EnableInClassList("locked", GameState.Data.gems < Boosts.OfflineDoubleGems);
            _offlineDouble.text = Loc.F("×2 · {0} elmas", Boosts.OfflineDoubleGems);
            Show(_offlineRoot);
        }

        void CollectOffline(float mult)
        {
            Offline.Collect(mult);
            _offlineRoot.AddToClassList("hidden");
            Sfx.Play(SfxId.Claim);
            Confetti(20);
        }

        // ------------------------------------------------------------------ seviye kartı
        void OnLevelUp(int level)
        {
            Sfx.Play(SfxId.LevelUp);
            GameSettings.Vibrate();
            _levelQueue.Enqueue(level);
            if (_levelRoot.ClassListContains("hidden")) ShowNextLevel();
        }

        void ShowNextLevel()
        {
            if (_levelQueue.Count == 0) return;
            int level = _levelQueue.Dequeue();
            _levelNum.text = level.ToString();
            FillUnlocks(level);   // v1.9: o seviyede açılan her şey (HudV19)
            _levelRoot.RemoveFromClassList("hidden");
            Confetti(40);
        }

        void AddUnlock(string tag, Sprite icon, string name)
        {
            var u = new VisualElement();
            u.AddToClassList("unl");
            u.AddToClassList("card");
            var t = new Label(tag);
            t.AddToClassList("unl-tag");
            var img = new VisualElement();
            img.AddToClassList("unl-img");
            img.style.backgroundImage = new StyleBackground(icon);
            var n = new Label(name);
            n.AddToClassList("unl-name");
            u.Add(img); u.Add(n); u.Add(t);
            _unlocks.Add(u);
        }

        void CloseLevelUp()
        {
            _levelRoot.AddToClassList("hidden");
            Sfx.Play(SfxId.Claim);
            if (_levelQueue.Count > 0) _root.schedule.Execute(ShowNextLevel).StartingIn(250);
        }

        // ------------------------------------------------------------------ efektler
        void CoinFly(Vector3 world, int count)
        {
            if (!CoinIcon || _root.panel == null) return;
            var from = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, world, cam);
            var to = _fx.WorldToLocal(_coinPill.worldBound.position + new Vector2(18f, 18f));
            for (int i = 0; i < count; i++)
            {
                var c = new VisualElement { pickingMode = PickingMode.Ignore };
                c.AddToClassList("fly-coin");
                c.style.backgroundImage = new StyleBackground(CoinIcon);
                _fx.Add(c);
                var start = from + Random.insideUnitCircle * 14f;
                var mid = Vector2.Lerp(start, to, 0.5f) + new Vector2(0f, -60f);
                float t = -i * 0.05f;
                c.style.left = start.x;
                c.style.top = start.y;
                c.schedule.Execute(() =>
                {
                    t += 0.016f;
                    float k = Mathf.Clamp01(t / 0.55f);
                    float e = k * k * (3f - 2f * k);
                    var pos = Vector2.Lerp(Vector2.Lerp(start, mid, e), Vector2.Lerp(mid, to, e), e);
                    c.style.left = pos.x;
                    c.style.top = pos.y;
                    if (k >= 1f)
                    {
                        c.RemoveFromHierarchy();
                        _coinPill.AddToClassList("pulse");
                        _pulseT = 0.12f;
                    }
                }).Every(16).Until(() => t >= 0.55f);
            }
        }

        static readonly Color[] ConfettiColors =
        {
            new(0.95f, 0.76f, 0.31f), new(0.78f, 0.27f, 0.23f), new(0.31f, 0.5f, 0.23f), new(0.24f, 0.37f, 0.66f), new(0.96f, 0.91f, 0.82f),
        };

        void Confetti(int n)
        {
            var size = _root.layout.size;
            for (int i = 0; i < n; i++)
            {
                var c = new VisualElement { pickingMode = PickingMode.Ignore };
                c.AddToClassList("confetti");
                c.style.backgroundColor = ConfettiColors[i % ConfettiColors.Length];
                _fx.Add(c);
                var pos = new Vector2(Random.Range(0f, size.x), Random.Range(-60f, 0f));
                var vel = new Vector2(Random.Range(-60f, 60f), Random.Range(160f, 300f));
                float rot = Random.Range(0f, 360f), spin = Random.Range(-400f, 400f), life = 0f;
                c.schedule.Execute(() =>
                {
                    life += 0.016f;
                    pos += vel * 0.016f;
                    vel.y += 100f * 0.016f;
                    rot += spin * 0.016f;
                    c.style.left = pos.x;
                    c.style.top = pos.y;
                    c.style.rotate = new Rotate(rot);
                    if (life > 2.4f || pos.y > size.y) c.RemoveFromHierarchy();
                }).Every(16).Until(() => life > 2.4f || pos.y > size.y);
            }
        }

        // ------------------------------------------------------------------ yükseltme
        void UpdateUpgradeCta()
        {
            var p = PlayerController.I;
            _ctaStation = null;
            if (p && !_panelStation)
            {
                float best = Balance.InteractRadius * 1.2f;
                foreach (var s in DrinkStation.All)
                {
                    float d = DrinkStation.FlatDistance(p.transform.position, s.ServePos);
                    if (d < best) { best = d; _ctaStation = s; }
                }
            }
            _upgradeCta.EnableInClassList("hidden", _ctaStation == null);
        }

        void OpenUpgrade(DrinkStation s)
        {
            if (!s) return;
            _panelStation = s;
            _depotPanel.AddToClassList("hidden");
            _upgradeTitle.text = Loc.Upper(Loc.T(s.drink.displayName));
            _upgradeThumb.style.backgroundImage = new StyleBackground(s.drink.icon);
            BuildUpgradeRows();
            _upgradePanel.RemoveFromClassList("hidden");
        }

        void CloseUpgrade()
        {
            _panelStation = null;
            _upgradePanel.AddToClassList("hidden");
        }

        void BuildUpgradeRows()
        {
            var s = _panelStation;
            if (!s) return;
            int tier = Balance.VisualTier(Mathf.Max(s.Save.speed, Mathf.Max(s.Save.stock, s.Save.quality)));
            for (int i = 0; i < 3; i++) _tiers[i].EnableInClassList("cur", i == tier - 1);
            _upgradeInfo.text = Loc.F("Satış {0} altın · Süre {1:0.0} sn · Stok {2}", s.Price, s.ProduceTime, s.MaxStock);
            _upgradeRows.Clear();
            // v1.9 (oyuncu testi madde 1, 22): her satır önce → sonra ve ne işe yaradığı (somut)
            AddRow(s, UpgradeStat.Hiz, Loc.T("Hız"), "speed", lv => Loc.F("{0:0.0} sn", Balance.ProduceTime(s.drink, lv)),
                   lv => Loc.F("Bir bardak {0:0.0} sn'de · dakikada {1:0.#} bardak", Balance.ProduceTime(s.drink, lv), 60f / Balance.ProduceTime(s.drink, lv)));
            AddRow(s, UpgradeStat.Stok, Loc.T("Stok"), "stock", lv => Loc.F("{0} bardak", Balance.MaxStock(lv)),
                   lv => Loc.T("Makinede hazır bekleyen bardak · sen yokken de dolar"));
            AddRow(s, UpgradeStat.Kalite, Loc.T("Kalite"), "quality", lv => Loc.F("{0} altın", Balance.SellPrice(s.drink, lv)),
                   lv => Loc.T("Her bardağın satış fiyatı · malzeme de biraz pahalanır"));
        }

        void AddRow(DrinkStation s, UpgradeStat stat, string name, string icon, Func<int, string> describe, Func<int, string> hint)
        {
            int lv = s.Save.Get(stat);
            bool maxed = lv >= Balance.MaxUpgradeLevel;
            int cost = Balance.UpgradeCost(s.drink, stat, lv);

            var row = new VisualElement();
            row.AddToClassList("row");
            var ico = new VisualElement();
            ico.AddToClassList("ico");
            var ii = new VisualElement();
            ii.AddToClassList("ico-img");
            ii.style.backgroundImage = new StyleBackground(KitIcon(icon));
            ico.Add(ii);
            var text = new VisualElement { style = { flexGrow = 1 } };
            var n = new Label(Loc.F("{0} · Sv {1}", name, lv));
            n.AddToClassList("up-name");
            var f = new Label(maxed ? describe(lv) + Loc.T(" (en yüksek)") : $"{describe(lv)} → {describe(lv + 1)}");
            f.AddToClassList("up-sub");
            var pips = new VisualElement();
            pips.AddToClassList("pips");
            for (int i = 0; i < Balance.MaxUpgradeLevel; i++)
            {
                var pp = new VisualElement();
                pp.AddToClassList("pip");
                pp.EnableInClassList("on", i < lv);
                pips.Add(pp);
            }
            var h = new Label(hint(lv));
            h.AddToClassList("up-hint");
            text.Add(n); text.Add(f); text.Add(h); text.Add(pips);

            var buy = new Button();
            buy.AddToClassList("btn"); buy.AddToClassList("btn-teal"); buy.AddToClassList("btn-sm"); buy.AddToClassList("price");
            if (!maxed)
            {
                var ci = new VisualElement();
                ci.AddToClassList("price-icon");
                ci.style.backgroundImage = new StyleBackground(CoinIcon);
                buy.Add(ci);
            }
            buy.Add(new Label(maxed ? Loc.T("TAMAM") : FormatNum(cost)));
            buy.EnableInClassList("locked", maxed || GameState.Coins < cost);
            buy.clicked += () => { if (s.TryUpgrade(stat)) { GameSettings.Vibrate(); Confetti(8); BuildUpgradeRows(); } };
            row.Add(ico); row.Add(text); row.Add(buy);
            _upgradeRows.Add(row);
        }
    }
}
