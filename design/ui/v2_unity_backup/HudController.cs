using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Taverna
{
    /// <summary>
    /// Oyun içi arayüz v2 — yatay (design/ui/ui_v2_landscape.html).
    /// Kaynaklar, sıradaki hedef + ok, seviye, joystick, müşteri baloncukları, istasyon/alan etiketleri,
    /// yükseltme çekmecesi, görev çekmecesi, mağaza, "Sen yokken…", seviye kartı, efektler.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
        public Camera cam;
        public string menuScene = "MainMenu";

        const string IconPath = "project://database/Assets/_Game/Art/Icons/";
        VisualElement _root, _world, _fx, _joyZone, _joyBase, _joyKnob, _levelFill, _goal, _goalFill, _goalIcon, _coinPill;
        VisualElement _upgradePanel, _upgradeRows, _upgradeThumb, _questsPanel, _questRows, _shopRoot, _shopGrid;
        VisualElement _offlineRoot, _levelRoot, _unlocks, _rays, _goalArrow;
        Label _coins, _gems, _level, _xpText, _toast, _upgradeTitle, _upgradeInfo, _goalText, _goalNum, _rush, _questBadge;
        Label _offlineText, _offlineSub, _levelNum, _questReset;
        Label[] _tiers;
        VisualElement _starsRow, _dayReport;
        ScrollView _shopScroll;
        Label _shopBadge;
        Label _dayTitle, _dayText, _shopNote;
        float _dayT, _starsT;
        readonly Dictionary<Table, Label> _dirtyTags = new();
        Button _upgradeCta, _offlineDouble;
        SettingsPanel _settings;

        readonly Dictionary<Customer, (VisualElement box, RingElement ring, VisualElement icon, Label emote)> _bubbles = new();
        readonly Dictionary<DrinkStation, (VisualElement box, RingElement ring, Label text)> _stationTags = new();
        readonly Dictionary<PayZone, (VisualElement box, Label title, Label cost)> _zoneTags = new();
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
                st.AddToClassList("star");
                st.style.backgroundImage = new StyleBackground(Resources.Load<Sprite>("Kit/icon_star"));
                _starsRow.Add(st);
            }
            _dayReport = _root.Q("day-report");
            _dayTitle = _root.Q<Label>("day-title");
            _dayText = _root.Q<Label>("day-text");
            _shopScroll = _root.Q<ScrollView>("shop-scroll");
            _shopScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _shopScroll.horizontalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            _shopScroll.mouseWheelScrollSize = 60f;
            // Fare tekerleği yatay kaydırsın (PC'de test için)
            _shopScroll.RegisterCallback<WheelEvent>(e =>
            {
                _shopScroll.scrollOffset += new Vector2(e.delta.y * 40f, 0f);
                e.StopPropagation();
            });
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

            var db = GameDatabase.I;
            if (db)
            {
                if (db.coinIcon) _root.Q("coin-icon").style.backgroundImage = new StyleBackground(db.coinIcon);
                if (db.gemIcon) _root.Q("gem-icon").style.backgroundImage = new StyleBackground(db.gemIcon);
            }

            _upgradeCta.clicked += () => { OpenUpgrade(_ctaStation); Sfx.Play(SfxId.Pop); };
            _root.Q<Button>("upgrade-close").clicked += CloseUpgrade;
            _root.Q<Button>("open-quests").clicked += () => { BuildQuests(); Show(_questsPanel); };
            _root.Q<Button>("quests-close").clicked += () => _questsPanel.AddToClassList("hidden");
            _root.Q<Button>("open-shop").clicked += OpenShop;
            _root.Q<Button>("open-shop-plus").clicked += () => OpenShop("boost");
            _root.Q<Button>("shop-close").clicked += () => _shopRoot.AddToClassList("hidden");
            _root.Q<Button>("offline-collect").clicked += () => CollectOffline(1f);
            _offlineDouble.clicked += () =>
            {
                if (GameState.Data.gems < 5) { Toast("Yeterli elmas yok"); return; }
                GameState.Data.gems -= 5;
                CollectOffline(2f);
            };
            _root.Q<Button>("levelup-claim").clicked += CloseLevelUp;
            _root.schedule.Execute(ShowOfflineIfAny).StartingIn(500);

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
        }

        void OnDisable()
        {
            GameState.Changed -= RefreshTop;
            GameState.LeveledUp -= OnLevelUp;
            GameState.CoinsGainedAt -= OnCoins;
            GameState.ZoneUnlocked -= OnZoneUnlocked;
            Quests.Changed -= OnQuestsChanged;
            RushHour.Started -= OnRushStarted;
            DayCycle.DayEnded -= OnDayEnded;
            Management.Changed -= RefreshTop;
            PlayerController.JoystickInput = Vector2.zero;
        }

        void Show(VisualElement e) { e.RemoveFromClassList("hidden"); Sfx.Play(SfxId.Pop); }

        // ------------------------------------------------------------------ otomatik test kancaları
        public bool LevelCardOpen => !_levelRoot.ClassListContains("hidden");
        public void DebugOpen(string what)
        {
            switch (what)
            {
                case "upgrade": OpenUpgrade(DrinkStation.All.Count > 0 ? DrinkStation.All[0] : null); break;
                case "quests": BuildQuests(); _questsPanel.RemoveFromClassList("hidden"); break;
                case "shop": OpenShop("mac"); break;
                case "shop-staff": OpenShop("staff"); break;
                case "shop-dec": OpenShop("dec"); break;
                default:
                    CloseUpgrade();
                    _questsPanel.AddToClassList("hidden");
                    _shopRoot.AddToClassList("hidden");
                    _offlineRoot.AddToClassList("hidden");
                    if (LevelCardOpen) CloseLevelUp();
                    break;
            }
        }

        /// <summary>Herhangi bir pencere açık mı? (joystick'i kilitler)</summary>
        bool ModalOpen => _settings.Visible || !_shopRoot.ClassListContains("hidden") || !_offlineRoot.ClassListContains("hidden") ||
                          !_levelRoot.ClassListContains("hidden");

        // ------------------------------------------------------------------ üst çubuk
        void RefreshTop()
        {
            _coins.text = FormatNum(GameState.Coins);
            _gems.text = GameState.Data.gems.ToString();
            _level.text = GameState.Level.ToString();
            _levelFill.style.width = Length.Percent(GameState.LevelProgress01 * 100f);
            _xpText.text = $"{GameState.Data.un} / {Balance.UnForLevel(GameState.Level)}";
            if (_panelStation) BuildUpgradeRows();
            if (!_shopRoot.ClassListContains("hidden")) BuildShop();
        }

        public static string FormatNum(long v) =>
            v >= 1_000_000 ? (v / 1_000_000f).ToString("0.#") + "M" :
            v >= 10_000 ? (v / 1000f).ToString("0.#") + "B" :
            v.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));

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
        void LateUpdate()
        {
            if (!cam || _root.panel == null) return;
            UpdateCustomerBubbles();
            UpdateStationTags();
            UpdateZoneTags();
            UpdateFloats();
            UpdateUpgradeCta();
            UpdateGoal();
            UpdateRush();
            UpdateDirtyTags();
            _starsT -= Time.unscaledDeltaTime;
            if (_starsT <= 0f) { _starsT = 0.5f; UpdateStars(); }
            if (_dayT > 0f) { _dayT -= Time.unscaledDeltaTime; if (_dayT <= 0f) _dayReport.AddToClassList("hidden"); }
            if (!_levelRoot.ClassListContains("hidden"))
            {
                _raysRot += Time.unscaledDeltaTime * 20f;
                _rays.style.rotate = new Rotate(_raysRot);
            }
            if (_toastT > 0f) { _toastT -= Time.unscaledDeltaTime; if (_toastT <= 0f) _toast.AddToClassList("hidden"); }
            if (_pulseT > 0f) { _pulseT -= Time.unscaledDeltaTime; if (_pulseT <= 0f) _coinPill.RemoveFromClassList("pulse"); }
        }

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
                    box.Add(ring); box.Add(text);
                    _world.Add(box);
                    t = (box, ring, text);
                    _stationTags[s] = t;
                }
                bool on = ToPanel(s.transform.position + Vector3.up * 1.55f, out var p);
                Place(t.box, p, on);
                t.text.text = $"{s.Stock}/{s.MaxStock}";
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
                    var title = new Label(z.label);
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
                ico.style.backgroundImage = locked ? new StyleBackground(Resources.Load<Sprite>("Kit/icon_lock")) : new StyleBackground(GameDatabase.I.coinIcon);
                t.cost.text = locked ? $"Seviye {z.requiredLevel}" : FormatNum(z.Remaining);
            }
            var dead = new List<PayZone>();
            foreach (var kv in _zoneTags)
                if (!kv.Key || !kv.Key.isActiveAndEnabled) { kv.Value.box.RemoveFromHierarchy(); dead.Add(kv.Key); }
            foreach (var d in dead) _zoneTags.Remove(d);
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
            return GameDatabase.I.coinIcon;
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
                _goalText.text = ok ? $"{def.displayName} makinesi alınabilir! Mağaza" : $"Sıradaki: {def.displayName} makinesi";
                _goalNum.text = $"{FormatNum(Math.Min(GameState.Coins, m.price))} / {FormatNum(m.price)}";
                _goalFill.style.width = Length.Percent(Mathf.Clamp01(GameState.Coins / (float)Mathf.Max(1, m.price)) * 100f);
                _goal.EnableInClassList("ready", ok);
                _goalArrow.style.display = DisplayStyle.None;
                _shopBadge.EnableInClassList("hidden", !shopReady);
                return;
            }
            _shopBadge.EnableInClassList("hidden", true);
            var z = zz;
            _goal.style.display = z ? DisplayStyle.Flex : DisplayStyle.None;
            if (!z) { _goalArrow.style.display = DisplayStyle.None; return; }
            _goalIcon.style.backgroundImage = new StyleBackground(ZoneIcon(z));
            bool ready = z.LevelOk && GameState.Coins >= z.Remaining;
            if (!z.LevelOk)
            {
                _goalText.text = $"Sıradaki: {z.label}";
                _goalNum.text = $"Seviye {z.requiredLevel}";
                _goalFill.style.width = Length.Percent(GameState.Level == z.requiredLevel - 1 ? GameState.LevelProgress01 * 100f : 0f);
            }
            else
            {
                _goalText.text = ready ? $"{z.label} hazır! Kareye git" : $"Sıradaki: {z.label}";
                _goalNum.text = $"{FormatNum(Math.Min(GameState.Coins, z.Remaining))} / {FormatNum(z.Remaining)}";
                _goalFill.style.width = Length.Percent(Mathf.Clamp01(GameState.Coins / (float)Mathf.Max(1, z.Remaining)) * 100f);
            }
            _goal.EnableInClassList("ready", ready);

            bool arrowOn = z.LevelOk;
            _goalArrow.style.display = arrowOn ? DisplayStyle.Flex : DisplayStyle.None;
            if (!arrowOn) return;
            var world = z.transform.position + Vector3.up * 0.3f;
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
            _dayTitle.text = $"GÜN {r.day} BİTTİ";
            _dayText.text = $"Gelir +{FormatNum(r.income)}  ·  Maaş −{FormatNum(r.wages)}  ·  {r.served} müşteri  ·  ★{r.stars:0.#}";
            _dayReport.RemoveFromClassList("hidden");
            _dayT = 6f;
            Sfx.Play(SfxId.Claim);
        }

        void UpdateDirtyTags()
        {
            foreach (var t in Table.All)
            {
                if (!_dirtyTags.TryGetValue(t, out var l))
                {
                    l = new Label("Temizle!") { pickingMode = PickingMode.Ignore };
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

        void OnZoneUnlocked(string id)
        {
            Confetti(28);
            Toast("YENİ ALAN AÇILDI!");
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
            bool on = RushHour.Active || Boosts.IncomeMultiplier > 1f;
            _rush.EnableInClassList("hidden", !on);
            if (RushHour.Active) _rush.text = $"YOĞUN SAAT!  ×2 bahşiş · {Mathf.CeilToInt(RushHour.Remaining)} sn";
            else if (on) _rush.text = $"×2 GELİR · {Mathf.CeilToInt(Boosts.IncomeRemaining / 60f)} dk";
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
            _ => "Elmas",
        };

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
                AddReward(reward, GameDatabase.I.coinIcon, FormatNum(q.rewardCoins));
                AddReward(reward, GameDatabase.I.gemIcon, q.rewardGems.ToString());
                var bar = new VisualElement();
                bar.AddToClassList("q-bar");
                var fill = new VisualElement();
                fill.AddToClassList("q-fill");
                fill.style.width = Length.Percent(q.progress * 100f / Mathf.Max(1, q.target));
                bar.Add(fill);
                text.Add(name); text.Add(reward); text.Add(bar);
                var claim = new Button { text = q.Done ? "AL" : $"{FormatNum(q.progress)}/{FormatNum(q.target)}" };
                claim.AddToClassList("btn");
                claim.AddToClassList("btn-sm");
                claim.AddToClassList(q.Done ? "btn-green" : "btn-gold");
                claim.EnableInClassList("locked", !q.Done);
                claim.style.minWidth = 70;
                claim.clicked += () => { if (Quests.Claim(idx)) { Sfx.Play(SfxId.Claim); Confetti(20); } };
                row.Add(img); row.Add(text); row.Add(claim);
                _questRows.Add(row);
            }
            var left = DateTime.Today.AddDays(1) - DateTime.Now;
            _questReset.text = $"Yenilenme: {left.Hours:00}:{left.Minutes:00}:{left.Seconds:00}";
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
            _shopTab = tab;
            BuildShop();
            Show(_shopRoot);
        }

        void SelectTab(string tab)
        {
            _shopTab = tab;
            Sfx.Play(SfxId.Pop);
            BuildShop();
        }

        void BuildShop()
        {
            foreach (var (name, id) in new[] { ("tab-mac", "mac"), ("tab-staff", "staff"), ("tab-dec", "dec"), ("tab-boost", "boost") })
                _root.Q<Button>(name).EnableInClassList("on", _shopTab == id);
            _shopScroll.Clear();
            switch (_shopTab)
            {
                case "mac":
                    _shopNote.text = "Seviye atladıkça yeni makineler açılır · satın alınca yerine kurulur";
                    foreach (var m in Catalog.Machines)
                    {
                        var def = GameDatabase.I.Drink(m.drink);
                        if (!def || m.drink == DrinkId.Bira) continue;
                        var can = Management.CanBuyMachine(m);
                        int lv = Management.MachineLevel(m.drink);
                        string sub = can switch
                        {
                            Management.BuyResult.Owned => "Kuruldu",
                            Management.BuyResult.LevelLow => $"Seviye {lv}'te açılır",
                            Management.BuyResult.ZoneLocked => $"Önce {m.zoneName}",
                            _ => $"Satış {def.basePrice} altın · {def.baseTime:0} sn",
                        };
                        var mm = m;
                        AddShopItem(def.icon, def.displayName, sub, can, m.price, false,
                            () => Management.BuyMachine(mm), lv, m.zoneName);
                    }
                    break;
                case "staff":
                    _shopNote.text = $"Personel her gün maaş alır · şu an günlük maaş: {FormatNum(Management.DailyWages)} altın";
                    foreach (var s in Catalog.StaffList)
                    {
                        var can = Management.CanHire(s);
                        string sub = can == Management.BuyResult.Owned ? $"Çalışıyor · {s.wage}/gün" :
                                     can == Management.BuyResult.LevelLow ? $"Seviye {s.level}'te açılır" : $"{s.desc}";
                        var ss = s;
                        AddShopItem(LoadIcon(s.icon), $"{s.name} · {s.roleName}", sub, can, s.hire, false,
                            () => Management.Hire(ss), s.level, "", can == Management.BuyResult.Ok ? $"Maaş {s.wage}/gün" : null);
                    }
                    break;
                case "dec":
                    _shopNote.text = $"Dekor taverna puanını artırır · şu an ★{Management.Stars:0.#} · daha çok yıldız = daha çok ve zengin müşteri";
                    foreach (var d in Catalog.DecorList)
                    {
                        var can = Management.CanBuyDecor(d);
                        int owned = Management.DecorOwned(d.id);
                        string sub = can == Management.BuyResult.LevelLow ? $"Seviye {d.level}'te açılır" :
                                     can == Management.BuyResult.NoSlot ? (owned > 0 ? $"{owned} adet · yer kalmadı" : "Yeni alan açılınca") :
                                     $"+{d.ambiance:0} puan" + (owned > 0 ? $" · {owned} adet" : "");
                        var dd = d;
                        AddShopItem(LoadIcon(d.icon), d.name, sub, can, d.price, false, () => Management.BuyDecor(dd), d.level, "");
                    }
                    break;
                default:
                    _shopNote.text = "Elmaslar görevlerden, seviye ve günlük ödüllerden kazanılır · ilerlemek için gerekmez";
                    foreach (var o in Boosts.Offers)
                    {
                        var oo = o;
                        string name = o.id == "coins" ? $"{FormatNum(Boosts.CoinPackAmount)} Altın" : o.name;
                        var can = GameState.Data.gems >= o.gems ? Management.BuyResult.Ok : Management.BuyResult.NoMoney;
                        AddShopItem(LoadIcon(o.icon), name, o.hot ? "EN İYİ" : "", can, o.gems, true,
                            () => Boosts.Buy(oo.id) ? Management.BuyResult.Ok : Management.BuyResult.NoMoney, 0, "");
                    }
                    break;
            }
        }

        void AddShopItem(Sprite icon, string name, string sub, Management.BuyResult state, long price, bool gems,
                         Func<Management.BuyResult> buy, int level, string zone, string extra = null)
        {
            var item = new VisualElement();
            item.AddToClassList("item");
            item.AddToClassList("card");
            item.EnableInClassList("owned", state == Management.BuyResult.Owned);
            item.EnableInClassList("locked", state == Management.BuyResult.LevelLow || state == Management.BuyResult.ZoneLocked);
            var img = new VisualElement();
            img.AddToClassList("item-img");
            if (icon) img.style.backgroundImage = new StyleBackground(icon);
            var n = new Label(name);
            n.AddToClassList("item-name");
            var s = new Label(extra != null ? $"{sub}\n{extra}" : sub);
            s.AddToClassList("item-sub");
            item.Add(img); item.Add(n); item.Add(s);

            var b = new Button();
            b.AddToClassList("btn"); b.AddToClassList("btn-sm");
            if (state == Management.BuyResult.Owned)
            {
                b.AddToClassList("btn-green");
                b.Add(new Label("✓"));
                b.SetEnabled(false);
            }
            else
            {
                b.AddToClassList("btn-gold");
                var pi = new VisualElement();
                pi.AddToClassList("price-icon");
                pi.style.backgroundImage = new StyleBackground(gems ? GameDatabase.I.gemIcon : GameDatabase.I.coinIcon);
                b.Add(pi);
                b.Add(new Label(price == 0 ? "Bedava" : FormatNum(price)));
                b.EnableInClassList("locked", state != Management.BuyResult.Ok);
                b.clicked += () =>
                {
                    var r = buy();
                    if (r == Management.BuyResult.Ok) { Sfx.Play(SfxId.Buy); Confetti(16); GameSettings.Vibrate(); }
                    else Toast(Management.BuyMessage(r, level, zone));
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
            string sure = m >= 60 ? $"{m / 60} sa {m % 60} dk" : $"{m} dk";
            _offlineSub.text = $"{sure} boyunca taverna çalıştı";
            _offlineText.text = $"+{FormatNum(Offline.Pending)}";
            _offlineDouble.EnableInClassList("locked", GameState.Data.gems < 5);
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
            _unlocks.Clear();
            AddUnlock("ÖDÜL", GameDatabase.I.gemIcon, $"+{Boosts.LevelGemReward(level)} Elmas");
            foreach (var d in GameDatabase.I.drinks)
                if (d && d.unlockLevel == level) AddUnlock("YENİ İÇECEK", d.icon, d.displayName);
            foreach (var m in Catalog.Machines)
                if (m.drink != DrinkId.Bira && Management.MachineLevel(m.drink) == level)
                    AddUnlock("YENİ MAKİNE", GameDatabase.I.Drink(m.drink).icon, "Mağazada");
            foreach (var st in Catalog.StaffList)
                if (st.level == level) AddUnlock("PERSONEL", LoadIcon(st.icon), st.name + " · " + st.roleName);
            foreach (var c in GameDatabase.I.customers)
                if (c && c.unlockLevel == level) AddUnlock("YENİ MÜŞTERİ", LoadIcon($"Cust_{c.number:00}"), c.displayName);
            _levelRoot.RemoveFromClassList("hidden");
            Confetti(40);
        }

        void AddUnlock(string tag, Sprite icon, string name)
        {
            if (_unlocks.childCount >= 4) return;
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
            u.Add(t); u.Add(img); u.Add(n);
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
            var db = GameDatabase.I;
            if (!db || !db.coinIcon || _root.panel == null) return;
            var from = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, world, cam);
            var to = _fx.WorldToLocal(_coinPill.worldBound.position + new Vector2(18f, 18f));
            for (int i = 0; i < count; i++)
            {
                var c = new VisualElement { pickingMode = PickingMode.Ignore };
                c.AddToClassList("fly-coin");
                c.style.backgroundImage = new StyleBackground(db.coinIcon);
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
            _upgradeTitle.text = s.drink.displayName.ToUpper(new System.Globalization.CultureInfo("tr-TR"));
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
            _upgradeInfo.text = $"Satış {s.Price} altın · Süre {s.ProduceTime:0.0} sn · Stok {s.MaxStock}";
            _upgradeRows.Clear();
            AddRow(s, UpgradeStat.Hiz, "Hız", "icon_speed", lv => $"{Balance.ProduceTime(s.drink, lv):0.0} sn");
            AddRow(s, UpgradeStat.Stok, "Stok", "icon_stock", lv => $"{Balance.MaxStock(lv)} bardak");
            AddRow(s, UpgradeStat.Kalite, "Kalite", "icon_star", lv => $"{Balance.SellPrice(s.drink, lv)} altın");
        }

        void AddRow(DrinkStation s, UpgradeStat stat, string name, string icon, Func<int, string> describe)
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
            ii.style.backgroundImage = new StyleBackground(Resources.Load<Sprite>("Kit/" + icon));
            ico.Add(ii);
            var text = new VisualElement { style = { flexGrow = 1 } };
            var n = new Label($"{name}  ·  Sv {lv}");
            n.AddToClassList("up-name");
            var f = new Label(maxed ? describe(lv) + " (en yüksek)" : $"{describe(lv)} → {describe(lv + 1)}");
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
            text.Add(n); text.Add(f); text.Add(pips);

            var buy = new Button();
            buy.AddToClassList("btn"); buy.AddToClassList("btn-gold"); buy.AddToClassList("btn-sm"); buy.AddToClassList("price");
            if (!maxed)
            {
                var ci = new VisualElement();
                ci.AddToClassList("price-icon");
                ci.style.backgroundImage = new StyleBackground(GameDatabase.I.coinIcon);
                buy.Add(ci);
            }
            buy.Add(new Label(maxed ? "TAMAM" : FormatNum(cost)));
            buy.EnableInClassList("locked", maxed || GameState.Coins < cost);
            buy.clicked += () => { if (s.TryUpgrade(stat)) { GameSettings.Vibrate(); Confetti(8); BuildUpgradeRows(); } };
            row.Add(ico); row.Add(text); row.Add(buy);
            _upgradeRows.Add(row);
        }
    }
}
