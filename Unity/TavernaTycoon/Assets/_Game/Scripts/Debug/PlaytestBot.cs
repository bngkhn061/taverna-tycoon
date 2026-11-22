#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// Otomatik oyun testi: Barlo'yu bir "bot" oynatır (istasyon → müşteri → para → ödeme alanı → yükseltme),
    /// olayları raporlar ve sonunda arayüzle birleşik ekran görüntüsü alır. Sadece editörde çalışır.
    /// </summary>
    public partial class PlaytestBot : MonoBehaviour
    {
        public float gameSeconds = 420f;
        public float timeScale = 4f;
        public System.Action<string> onFinished;

        readonly StringBuilder _log = new();
        float _t, _logT, _stuckT, _sideT;
        Vector3 _lastPos, _side;
        string _goal = "";
        int _upgradesBought, _angryLeaves;
        bool _done;
        int _shot;
        bool _bubbleShot, _levelShot, _eveningShot, _nightShot, _bigShot;
        float _slowLogT, _kitchenSeenAt = -1f;
        bool _kitchenShot, _guestShot, _sleepShot, _dirtyShot, _comfortBought;
        float _upSince = -1f;
        int _checkIns, _roomsCleaned;
        // v0.9
        float _real, _dayEndAt = -1f, _morningAt = -1f;
        bool _introShot, _tutShot, _dayEndShot, _planShot, _morningShot, _gameShot, _worldShot;
        int _rounds, _daysEnded;
        bool _reportSub, _starterShot;
        int _premStep, _tierStep;
        bool _cellarShot, _terraceShot, _yardShot, _dockShot;
        float _tierAt;
        float _premAt;
        int _gc0;
        float _levelCloseAt, _uiT;
        int _uiStep, _uiPhase;
        static readonly (string, string)[] UiSteps = { ("hud", "ui_hud.png"), ("upgrade", "ui_upgrade.png"), ("quests", "ui_quests.png"), ("shop", "ui_shop.png"), ("shop-staff", "ui_shop_staff.png"), ("shop-dec", "ui_shop_dec.png"), ("depot", "ui_depot.png"), ("shop-auto", "ui_shop_auto.png"), ("shop-prem", "ui_shop_prem.png"), ("vip", "ui_vip.png"), ("happy", "ui_happy.png"), ("events", "ui_events.png"), ("stars", "ui_stars.png") };
        float _fpsAcc; int _fpsN;
        // v1.7 canlı dil değişimi testi
        int _langStep; float _langAt; string _lang0;

        void Start()
        {
            Time.timeScale = timeScale;
            // UI'yi ekrana değil bir dokuya çiz → batchmode'da da arayüzlü görüntü alınabilsin
            var doc = FindAnyObjectByType<UIDocument>();
            if (doc && doc.panelSettings && Application.isBatchMode)
            {
                doc.panelSettings.targetTexture = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);
                doc.panelSettings.clearColor = true;
                doc.panelSettings.colorClearValue = new Color(0, 0, 0, 0);
            }
            Log($"Test başladı. Para={GameState.Coins} Seviye={GameState.Level}");
            _startLv = GameState.Level; _startUn = GameState.Data.un;
            GameState.CoinsGainedAt += (a, _) => _earned += a;
            // v1.4 test kancaları
            Tutorial.Changed += OnTutorialStep;
            _tutStepAt = _real;
            if (Tutorial.Active) Log($"v1.4 REHBER: başladı · adım {Tutorial.Step + 1} · gün saati {DayCycle.Progress01:0.000} · zaman durur={Tutorial.HoldsTime}");
            Log($"v1.4 YILDIZ: ambiyans={AmbianceSource.Total:0} ★{Management.Stars:0.#} · sonraki ★{Management.NextStar} için {Management.AmbianceToNextStar} · eşikler=[{string.Join(",", Balance.StarThresholds)}] premium'suz toplam={Balance.AmbianceNoPremiumTotal:0}");
            Log($"v1.4 GÖREVLER (başlangıç): [{string.Join(" | ", Quests.List.ConvertAll(q => Quests.Text(q) + $" ({q.rewardCoins} altın, {q.rewardGems} elmas)"))}]");
        }

        // ------------------------------------------------------------------ v1.4 test durumu
        float _tutStepAt, _happyMin = 101f, _happyMax = -1f, _avgMin = 101f, _avgMax = -1f, _rawMax, _capMax, _v14LogT;
        int _dirtyMax, _tutOpened = -1;
        bool _capLogged, _tellalStarted, _tellalLogged, _questForced, _dayFlowLogged;
        float _tutEndReal = -1f, _tutEndDay;

        void OnTutorialStep()
        {
            _tutStepAt = _real;
            string s = Tutorial.Active ? $"adım {Tutorial.Step + 1}" : "BİTTİ";
            Log($"v1.4 REHBER: {s} · gerçek {_real:0.0} sn · gün saati {DayCycle.Progress01:0.000} · zaman durur={Tutorial.HoldsTime}");
            if (!Tutorial.Active) { _tutEndReal = _real; _tutEndDay = DayCycle.Progress01; }
        }

        /// <summary>v1.4: rehberde Görevler, Dükkân ve Personel butonlarına bot basar (2 sn sonra); Depo adımını kendiliğinden geçmeye bırakır.</summary>
        void TutorialClicks(HudController hud)
        {
            TutorialClicksV19(hud);   // v1.9: rehber zorunlu, bot düğmelere gerçek olayla basar (atla / kendiliğinden geçme yok)
            return;
            if (!hud || !Tutorial.Active || Tutorial.UiTarget == null || Tutorial.Step == Tutorial.StepDepot) return;
            if (_tutOpened != Tutorial.Step && _real > _tutStepAt + 2f)
            {
                _tutOpened = Tutorial.Step;
                hud.DebugOpen(Tutorial.Step == Tutorial.StepQuests ? "quests" : Tutorial.Step == Tutorial.StepShop ? "shop" : "shop-staff");
                Log($"v1.4 REHBER: bot {Tutorial.UiTarget} butonuna bastı (adım {Tutorial.Step + 1})");
            }
            else if (_tutOpened == Tutorial.Step && _real > _tutStepAt + 3f) hud.DebugOpen("close");
        }

        void TrackV14()
        {
            float v = Happiness.Value, a = Happiness.Average;
            _happyMin = Mathf.Min(_happyMin, v); _happyMax = Mathf.Max(_happyMax, v);
            _avgMin = Mathf.Min(_avgMin, a); _avgMax = Mathf.Max(_avgMax, a);
            _dirtyMax = Mathf.Max(_dirtyMax, Happiness.DirtyTables);
            float raw = CustomerSpawner.RawBoost, cap = CustomerSpawner.BoostMultiplier;
            _rawMax = Mathf.Max(_rawMax, raw); _capMax = Mathf.Max(_capMax, cap);
            if (!_capLogged && raw > Balance.SpawnBoostCap)
            {
                _capLogged = true;
                Log($"v1.4 ÇARPAN SINIRI: ham ×{raw:0.00} (mutluluk ×{Happiness.SpawnMultiplier:0.00} · etkinlik ×{TavernEvents.SpawnMultiplier:0.0} · yoğun={RushHour.Active} · Usta ×{Automation.GuestMultiplier:0.00}) → uygulanan ×{cap:0.00}");
            }
            if (!_dayFlowLogged && _tutEndReal > 0f && _real > _tutEndReal + 10f)
            {
                _dayFlowLogged = true;
                Log($"v1.4 REHBER SONRASI: 10 gerçek sn'de gün saati {_tutEndDay:0.000} → {DayCycle.Progress01:0.000} (akıyor={DayCycle.Progress01 > _tutEndDay})");
            }
            _v14LogT += Time.deltaTime;
            if (_v14LogT >= 30f)
            {
                _v14LogT = 0f;
                Log($"v1.4 MUTLULUK: etkin %{v:0.0} · ortalama %{a:0.0} · kirli masa {Happiness.DirtyTables} (−{Happiness.DirtyPenalty:0}) · puanlanan {Happiness.Samples} · müşteri ×{Happiness.SpawnMultiplier:0.00} · toplam çarpan ×{cap:0.00} (ham ×{raw:0.00})");
            }
        }

        void Log(string s) { var line = $"[{_t,6:0.0}s] {s}"; _log.AppendLine(line); if (!Quiet) Debug.Log("[Playtest] " + line); }

        void Update()
        {
            if (_done) return;
            _real += Time.unscaledDeltaTime;
            CountSlowFrame();   // v1.6: her yavaş kare (günlük 2 sn'de bir yazar, sayım her kare)
            if (UpdateV24()) return;   // v2.4 Paket 6 ("-autotest-v24 audio|intro|ending|daily")
            if (UpdateV23()) return;   // v2.3 Paket 5 ("-autotest-v23 edit|editcheck|cloud"); düzenleme modunda bot oynamaz
            if (UpdateV09()) return;
            _t += Time.deltaTime;
            var p = PlayerController.I;
            if (!p) return;

            foreach (var c in Customer.All)
                if (c.emote == Customer.Emote.Angry && c.state == Customer.State.Leaving && _angrySeen.Add(c)) { _angryLeaves++; OnAngryV18(c); }   // v1.8: ×4 hızda emoteTime eşiği ilk karede kaçıyordu
            if (Measure) UpdateV18();   // v1.8 denge ölçümü
            UpdateV19();

            Vector3? target = Decide(p);
            if (target.HasValue) target = Inn.Route(p.transform.position, target.Value);
            if (target.HasValue) target = InnDoor(p.transform.position, target.Value);
            if (target.HasValue) target = KitchenLayout.Route(p.transform.position, target.Value);
            Steer(p, target);
            if (!(_tierStep < 3 && Tiers.Visible && Tiers.Next != null && Tiers.Next.index == 1) && !(V15Prestige && _v15Step < V15After)) TryUpgrades();   // v1.5: prestij testi bitene kadar da harcama   // v1.0: kademe testi bitene kadar para harcama
            // Teşhis: yavaş kare (>80 ms) olunca o anki durumu yaz (2 sn'de bir)
            _slowLogT -= Time.unscaledDeltaTime;
            if (!Quiet && Sfx.MusicNow != _music) { _music = Sfx.MusicNow; Log($"SES müzik → {_music} · faz={DayCycle.Progress01:0.00} yoğun={RushHour.Active} müşteri={Customer.All.Count} kalabalık={Sfx.CrowdLevel:0.00} ortam=[{Sfx.AmbientInfo}]"); }
            if (!Quiet && Time.unscaledDeltaTime > 0.08f && _slowLogT <= 0f)
            {
                _slowLogT = 2f;
                int gc = System.GC.CollectionCount(0);
                Log($"YAVAŞ KARE {Time.unscaledDeltaTime * 1000f:0} ms · gerçek {_real:0.0} sn · kayıt karesi={GameState.LastSaveFrame >= Time.frameCount - 1} · müşteri={Customer.All.Count} para yığını={CoinPile.All.Count} " +
                    $"dünya UI={HudController.DebugWorldCount} efekt UI={HudController.DebugFxCount} GC+={gc - _gc0} faz={DayCycle.Progress01:0.00} " +
                    $"yoğun={RushHour.Active} masa={Table.All.Count} istasyon={DrinkStation.All.Count} fener={Lantern.All.Count} amb={AmbianceSource.All.Count}");
                _gc0 = gc;
            }

            _fpsAcc += Time.unscaledDeltaTime; _fpsN++;
            _logT += Time.deltaTime;
            if (_logT >= 20f && !Quiet)
            {
                _logT = 0f;
                Log($"Para={GameState.Coins} Seviye={GameState.Level} Ün={GameState.Data.un}/{Balance.UnForLevel(GameState.Level)} " +
                    $"FPS={(_fpsN / Mathf.Max(0.001f, _fpsAcc)):0} Servis={GameState.Data.customersServed} İçeride={Customer.All.Count} Garson={Waiter.All.Count} Yıldız={Management.Stars:0.#} Gün={GameState.Data.day} Yoğun={RushHour.Active} Açılan=[{string.Join(",", GameState.Data.unlocked)}] Hedef={_goal} Poz=({p.transform.position.x:0.0},{p.transform.position.z:0.0})");
                _fpsAcc = 0; _fpsN = 0;
            }
            // Arayüz ekranlarının görüntüleri (bir kez)
            var hud = _hud ? _hud : (_hud = FindAnyObjectByType<HudController>());
            if (!Quiet) TrackV14();
            TutorialClicks(hud);
            UpdateV15(hud, p);
            UpdateV20(hud, p);   // v2.0 Paket 2 ("-autotest-v20 <mod>")
            UpdateV21(hud, p);   // v2.1 Paket 3 ("-autotest-v21 barlo|ending")
            UpdateV22(hud, p);   // v2.2 Paket 4 ("-autotest-v22 season|offseason|wardrobe")
            if (!NoTests) UpdateV16(hud);
            if (hud)
            {
                if (hud.LevelCardOpen)
                {
                    if (!_levelShot) { _levelShot = true; _levelCloseAt = _t + 1f; Shot("ui_levelup.png"); }
                    else if (_t > _levelCloseAt) hud.DebugOpen("close");
                }
                // v1.4: rehber sürerken panel görüntüleri beklenir (bot rehber butonlarını kendisi basar, kalanı kendiliğinden geçer)
                if (NoTests) { }   // v1.8: ölçüm / sessiz koşuda panel turu ve dil testi yok
                else if (_uiStep < UiSteps.Length && !Tutorial.Active && _t > 30f + _uiStep * 4f)
                {
                    var (what, file) = UiSteps[_uiStep];
                    if (_uiPhase == 0) { hud.DebugOpen(what); _uiPhase = 1; _uiT = _t; }
                    else if (_uiPhase == 1 && _t > _uiT + 1.2f) { Shot(file); _uiPhase = 2; }
                    else if (_uiPhase == 2 && _t > _uiT + 2.0f) { hud.DebugOpen("close"); _uiPhase = 0; _uiStep++; }
                }
                // v1.7: oyun içinde canlı dil değişimi (dükkân açıkken) → görüntü → eski dile dönüş
                if (!NoTests && _uiStep >= UiSteps.Length && _langStep == 0) { _langStep = 1; _lang0 = Loc.Lang; hud.DebugOpen("shop"); _langAt = _real; }
                else if (_langStep == 1 && _real > _langAt + 0.5f) { _langStep = 2; Loc.SetLanguage(_lang0 == "ja" ? "de" : "ja"); _langAt = _real; }
                else if (_langStep == 2 && _real > _langAt + 0.6f) { _langStep = 3; Shot("ui_lang_switch.png"); Log($"v1.7 DİL (oyun içi): {_lang0} → {Loc.Lang} · istisna yok"); _langAt = _real; }
                else if (_langStep == 3 && _real > _langAt + 0.5f) { _langStep = 4; Loc.SetLanguage(_lang0); hud.DebugOpen("close"); Log($"v1.7 DİL geri: {Loc.Lang}"); }
            }
            // v1.3: kov → maaş düşer → yeniden işe al (v1.4: ücret = max(katalog, 2 × günlük maaş))
            if (NoTests) { }   // v1.8: ölçümde kovma / görev zorlama / tellal testi yok
            else if (_fireStep == 0 && _t > 120f && Management.Hired("staff_nana"))
            {
                var nana = System.Array.Find(Catalog.StaffList, x => x.id == "staff_nana");
                long w0 = Management.DailyWages;
                long nanaWage = Management.WageOf(nana), costBefore = Management.HireCost(nana);
                bool ok = Management.Fire(nana);
                Log($"v1.3 KOV: Nana → {ok} · çalışıyor={Management.Working("staff_nana")} · maaş {w0} → {Management.DailyWages} · temizlikçi var={Management.HasRole(Catalog.Role.Temizlikci)}");
                Log($"v1.4 İŞE ALIM: Nana Sv{GameState.Level} · katalog {nana.hire} · kovulmadan önce maaş {nanaWage}/gün (ücret {costBefore}) · kovulduktan sonra maaş {Management.WageOf(nana)}/gün → ücret {Management.HireCost(nana)} = max({nana.hire}, {Balance.HireWageMultiplier}×{Management.WageOf(nana)})");
                foreach (var st in Catalog.StaffList)
                    if (!st.premium)
                    {
                        long w20 = Balance.Wage(st.wage, st.level, 20, 0), w30 = Balance.Wage(st.wage, st.level, Balance.MaxLevel, 0);
                        Log($"v1.4 İŞE ALIM ÖLÇEĞİ: {st.name} · şimdi {Management.HireCost(st)} · Sv20'de max({st.hire}, 2×{w20}) = {System.Math.Max(st.hire, 2 * w20)} · Sv30'da {System.Math.Max(st.hire, 2 * w30)}");
                    }
                _fireStep = 1;
            }
            else if (_fireStep == 1 && _t > 126f)
            {
                var nana = System.Array.Find(Catalog.StaffList, x => x.id == "staff_nana");
                long cost = Management.HireCost(nana);
                GameState.Data.coins += cost;
                long before = GameState.Coins;
                var r = Management.Hire(nana);
                Log($"v1.3 YENİDEN İŞE AL: Nana → {r} · çalışıyor={Management.Working("staff_nana")} · seviye {StaffUpgrades.Level("staff_nana")} · ödenen {before - GameState.Coins} (HireCost {cost})");
                _fireStep = 2;
            }
            // v1.4: özel sipariş görevi (Sv3+) — sayaç yalnızca oyuncunun servis ettiği özel siparişte artar
            if (!NoTests && !_questForced && _t > 5f && GameState.Level >= Balance.SpecialQuestLevel && Quests.List.Count > 0)
            {
                _questForced = true;
                if (!Quests.List.Exists(q => q.type == QuestType.SpecialOrder || q.type == QuestType.SpecialOrderBig))
                    Quests.List[0] = Quests.Make(QuestType.SpecialOrder);   // test: görev listesinde yoksa ilk görevi özel sipariş yap
                var sq = Quests.List.Find(q => q.type == QuestType.SpecialOrder || q.type == QuestType.SpecialOrderBig);
                Log($"v1.4 GÖREV: \"{Quests.Text(sq)}\" {sq.progress}/{sq.target} · ödül {sq.rewardCoins} altın {sq.rewardGems} elmas · büyük görev ödülü {Quests.Make(QuestType.SpecialOrderBig).rewardCoins} altın {Quests.Make(QuestType.SpecialOrderBig).rewardGems} elmas · oyuncu servis={Customer.SpecialServed}");
                _specialAtForce = Customer.SpecialServed;
            }
            // v1.4: Tellal etkinliği (Sv2, rehber bitince) — eski "Tellal tut" satın alımının yerine; şeridi çek
            if (!NoTests && !_evStarted && _t > 95f && GameState.Level >= 2 && !Tutorial.Active)
            {
                _evStarted = true;
                var tel = TavernEvents.Get(TavernEvents.Id.Tellal);
                long need = TavernEvents.Cost(tel);
                Log($"v1.4 TELLAL: durum={TavernEvents.StateOf(tel)} · fiyat {need} (pay × 3 dk gelir, Sv{GameState.Level}) · tellal görünür={Campaigns.CrierVisible}");
                if (GameState.Coins < need) GameState.Data.coins += need;   // test: etkinlik parası (bot her şeye harcıyor)
                var r = TavernEvents.Start(tel);
                Log($"v1.2 ETKİNLİK: tellal → {r} · altın {GameState.Coins} · mutluluk %{Happiness.Value:0} (×{Happiness.SpawnMultiplier:0.00}) · etkinlik çarpanı ×{TavernEvents.SpawnMultiplier:0.0}");
                _evShotAt = _t + 2f;
            }
            if (_evStarted && !_tellalLogged && _t > _evShotAt)
            {
                _tellalLogged = true;
                Log($"v1.4 TELLAL: etkinlik={TavernEvents.Active} · tellal sokakta görünür={Campaigns.CrierVisible} · kalan {TavernEvents.Remaining:0} sn · ikinci başlatma → {TavernEvents.StateOf(TavernEvents.Get(TavernEvents.Id.Tellal))}");
            }
            if (_evStarted && !_evShot && _t > _evShotAt) { _evShot = true; Shot("ui_event_banner.png"); }
            if (!_bubbleShot)
                foreach (var c in Customer.All)
                    if (c.state == Customer.State.Waiting && c.patience < c.patienceMax - 1.5f) { _bubbleShot = true; Shot("playtest_bubble.png"); break; }
            if (_shot == 0 && _t > gameSeconds * 0.5f) { _shot = 1; Shot("playtest_mid.png"); }
            // v0.6: akşam (fener yakılırken), gece ve geniş harita görüntüleri
            if (!_eveningShot && Night.IsDark && !Night.IsNight && Night.LanternCount().lit > 0) { _eveningShot = true; Shot("playtest_evening.png"); }
            if (!_nightShot && Night.IsNight && DayCycle.Progress01 > 0.8f) { _nightShot = true; Shot("playtest_night.png"); }
            if (!_bigShot && GameState.IsUnlocked("salon")) { _bigShot = true; Shot("playtest_salon.png"); }
            // v0.7: mutfak açıldıktan ~25 sn sonra (makine kurulup çalışırken)
            if (GameState.IsUnlocked("mutfak") && _kitchenSeenAt < 0f) _kitchenSeenAt = _t;
            if (!_kitchenShot && _kitchenSeenAt >= 0f && _t > _kitchenSeenAt + 25f) { _kitchenShot = true; Shot("playtest_kitchen.png"); }
            // v0.8 han: resepsiyonda misafir, üst katta uyuyan / dağınık odalar
            if (!_guestShot)
                foreach (var gu in Guest.All)
                    if (gu.state == Guest.State.Waiting && gu.patience < gu.patienceMax - 1f) { _guestShot = true; Shot("playtest_inn_guest.png"); break; }
            bool up = Inn.Upstairs(p.transform.position);
            if (up && _upSince < 0f) _upSince = _t;
            if (!up) _upSince = -1f;
            if (up && _t > _upSince + 1.5f)
            {
                foreach (var rm in Room.All)
                {
                    if (!_sleepShot && rm.state == Room.State.Occupied) { _sleepShot = true; Shot("playtest_inn_sleep.png"); break; }
                    if (!_dirtyShot && rm.state == Room.State.Dirty) { _dirtyShot = true; Shot("playtest_inn_dirty.png"); break; }
                }
            }
            if (!_comfortBought && Inn.Open && Inn.CanUpgrade(0) && GameState.Coins > Inn.UpgradeCost(0) + 800)
                if (Inn.UpgradeComfort(0)) { _comfortBought = true; Log($"Konfor: Oda 1 → kademe {Inn.Comfort(0)} (gecelik {Inn.Fee(0)})"); }
            if (_t >= gameSeconds) Finish();
        }

        Vector3? Decide(PlayerController p)
        {
            var pos = p.transform.position;
            if (_v20Hold.HasValue) { _goal = "v2.0 bak"; return _v20Hold.Value; }   // v2.0: görüntü için yerinde dur
            if (_v15Visit.HasValue) { _goal = "v1.5 bak"; return _v15Visit.Value; }   // v1.5: yeni makinelerin önüne git (görüntü)
            // 1) Para topla
            CoinPile nearestCoin = null;
            float best = float.MaxValue;
            foreach (var c in CoinPile.All)
            {
                float d = DrinkStation.FlatDistance(pos, c.transform.position);
                if (d < best) { best = d; nearestCoin = c; }
            }
            // 2) Elimdekini isteyen müşteri
            Customer serveTarget = null;
            float bestC = float.MaxValue;
            foreach (var c in Customer.All)
            {
                if (c.state != Customer.State.Waiting || !p.carrier.Has(c.wanted)) continue;
                // v1.8 "iyi oyuncu": sabrı en az kalan ve yetişilebilen müşteri önce (eski puan sabırlıyı öne alıyordu)
                float d = DrinkStation.FlatDistance(pos, c.transform.position);
                float s = c.patience + d / Balance.PlayerSpeed;
                if (d / Balance.PlayerSpeed > c.patience) s += 1000f;   // yetişilemez: en sona
                if (s < bestC) { bestC = s; serveTarget = c; }
            }
            if (serveTarget && (!nearestCoin || bestC < 1000f || DrinkStation.FlatDistance(pos, serveTarget.transform.position) < best)) { _goal = "servis:" + serveTarget.def.displayName; return serveTarget.transform.position; }
            if (nearestCoin && best < 4f) { _goal = "para"; return nearestCoin.transform.position; }

            // Oyun salonu (v0.9): hazır masada tur başlat (otomasyon yoksa)
            if (!Automation.Has("oyun"))
                foreach (var gt in GameTable.All)
                    if (gt.state == GameTable.State.Ready) { _goal = "oyun:" + gt.Title; _lastGame = gt; return gt.transform.position + new Vector3(0f, 0f, -0.95f); }

            // Test (v1.0): iskeleye bir kez bak (müşteri oturmuşsa)
            if (!NoTests && !_dockShot && GameState.IsUnlocked("iskele") && _t > 45f && Customer.All.Exists(c => c.transform.position.x < -15.3f && c.state != Customer.State.Leaving))
            { _goal = "iskeleye bak"; return new Vector3(-17.8f, 0f, -1.0f); }

            // Han (v0.8): resepsiyonda bekleyen misafiri karşıla (servisten sonra en öncelikli iş)
            foreach (var gu in Guest.All)
                if (gu.state == Guest.State.Waiting)
                {
                    _goal = $"misafir d={DrinkStation.FlatDistance(pos, gu.transform.position):0.0} g=({gu.transform.position.x:0.0},{gu.transform.position.z:0.0})";
                    return gu.transform.position + Vector3.left * 0.6f;
                }
            // Test: uyuyan misafiri ve sabah dağınık odayı görmek için bir kez merdivenden üst kata çık
            if (!NoTests)
            foreach (var rm in Room.All)
                if ((!_sleepShot && rm.state == Room.State.Occupied) || (!_dirtyShot && rm.state == Room.State.Dirty))
                { _goal = "üst kata bak"; return new Vector3(rm.bedPoint.position.x + 0.5f, 0f, 38.3f); }


            // 3) Bekleyen siparişler için istasyona git
            if (p.carrier.HasSpace)
            {
                // v1.8 "iyi oyuncu": garsonun üstlenmediği siparişler içinden (özel siparişler önde) sabrı en az kalan ve
                // istasyon + masa yolu sabrına yeten müşteri; eskiden listedeki ilk müşteriye gidiyordu
                DrinkStation pick = null;
                float bestS = float.MaxValue;
                foreach (var c in Customer.All)
                {
                    if (c.state != Customer.State.Waiting || p.carrier.Has(c.wanted)) continue;
                    if (c.reservedBy && !c.special) continue;
                    DrinkStation st = null;
                    foreach (var s in DrinkStation.All) if (s && s.drink && s.drink.id == c.wanted) { st = s; break; }
                    if (!st || st.Stock <= 0) continue;
                    float travel = (DrinkStation.FlatDistance(pos, st.ServePos) + DrinkStation.FlatDistance(st.ServePos, c.transform.position)) / Balance.PlayerSpeed;
                    float score = c.patience - (c.special ? BotSpecialBonus : 0f);
                    if (travel > c.patience) score += 1000f;
                    if (score < bestS) { bestS = score; pick = st; }
                }
                if (pick) { _goal = "istasyon:" + pick.drink.displayName; return pick.ServePos; }
            }
            if (nearestCoin) { _goal = "para"; return nearestCoin.transform.position; }

            // Akşam: sönük fener (v0.6)
            if (Night.IsDark)
            {
                Lantern lan = null;
                float bl = float.MaxValue;
                foreach (var l in Lantern.All)
                {
                    if (l.lit || !l.ZoneOpen) continue;
                    float d = DrinkStation.FlatDistance(pos, l.transform.position);
                    if (d < bl) { bl = d; lan = l; }
                }
                if (lan) { _goal = "fener"; var t = lan.transform.position; t.y = 0f; t.z -= 1.0f; return t; }
            }

            // Kirli masa (temizlikçi yoksa)
            if (!Management.HasRole(Catalog.Role.Temizlikci))
                foreach (var t in Table.All)
                    if (t.Dirty && !t.Busy) { _goal = "temizlik"; return t.transform.position; }

            // Han: dağınık oda (hizmetçi yoksa) — yatağın önüne
            if (!Management.HasRole(Catalog.Role.Hizmetci))
                foreach (var rm in Room.All)
                    if (rm.state == Room.State.Dirty) { _goal = "oda topla"; _cleaning = rm; return new Vector3(rm.bedPoint.position.x + 0.5f, 0f, 40.0f); }
            if (_cleaning && _cleaning.state == Room.State.Free) { _roomsCleaned++; _cleaning = null; }

            // 4) Oyunun gösterdiği sıradaki alan hedefi (parası yetiyorsa)
            var g = PayZone.NextGoal();
            if (g && g.Available && GameState.Coins >= g.Remaining) { _goal = "satınal:" + g.label; return g.transform.position; }

            // v1.2 rehber: gösterilen hedefe git (ilk adımda bira fıçısı; müşteri ancak o zaman gelir)
            if (Tutorial.Active && Tutorial.Target.HasValue) { _goal = "rehber " + (Tutorial.Step + 1); return Tutorial.Target.Value; }

            _goal = "bekle";
            return new Vector3(0.4f, 0f, 0.5f);
        }

        Room _cleaning;
        /// <summary>v1.8: botun özel siparişe verdiği öncelik (saniye; garsonlar özel siparişi almadığı için).</summary>
        const float BotSpecialBonus = 6f;
        /// <summary>v1.8: HUD her kare aranmasın (sahne yeniden yüklenince Unity null'a döner, yeniden bulunur).</summary>
        HudController _hud;

        GameTable _lastGame;

        /// <summary>
        /// v0.9 test adımları: giriş animasyonu, rehber, gün sonu planı (görüntü + "Sonraki Güne Geç"), sabah şeridi,
        /// oyun salonu, otomasyon alımı. true dönerse bu kare başka iş yapılmaz (zaman durmuşken).
        /// </summary>
        bool UpdateV09()
        {
            if (!_reportSub)
            {
                _reportSub = true;
                DayCycle.DayEnded += r => Log($"RAPOR gün {r.day}: gelir {r.income} · maaş {r.wages} (oran %{(r.income > 0 ? 100f * r.wages / r.income : 0f):0}) · ödenmeyen {r.unpaid} · servis {r.served} · Sv {GameState.Level} · kızgın {_angryLeaves}");
            }
            var hud = _hud ? _hud : (_hud = FindAnyObjectByType<HudController>());
            // v2.1: son sahne oynarken botun başka işi yok (oyun zamanı da saymaz)
            if (Ending.Playing) { PlayerController.JoystickInput = Vector2.zero; UpdateV21(hud, PlayerController.I); return true; }
            if (!_introShot && _real > 1.1f) { _introShot = true; Shot("playtest_intro.png"); }
            if (!_tutShot && Tutorial.Active && _real > 4f) { _tutShot = true; Shot("ui_tutorial.png"); }
            if (DayCycle.Ended && hud)
            {
                if (V20 == "dayend") DayEndV20(hud);   // v2.0: plan ekranındaki durum (müşteri / personel yok)
                if (_dayEndAt < 0f) { _dayEndAt = _real; _daysEnded++; Log($"Gün bitti (plan ekranı) · gün {GameState.Data.day - 1} · para {GameState.Coins}"); }
                if (!_dayEndShot && _real > _dayEndAt + 1.0f) { _dayEndShot = true; Shot("ui_dayend.png"); }
                if (_dayEndShot && !_planShot && _real > _dayEndAt + 1.6f)
                {
                    // Plan › Dekor sekmesi (gece dekor alışverişi)
                    hud.DebugPlanTab("dec");
                }
                if (_dayEndShot && !_planShot && _real > _dayEndAt + 2.4f) { _planShot = true; Shot("ui_dayend_plan.png"); }
                if (_real > _dayEndAt + 3.2f) { hud.NextDayPressed(); _dayEndAt = -1f; _morningAt = _real; Log("Sonraki güne geçildi"); }
                return true;
            }
            // v1.0 yeni bölümler: mahzen ve teras görüntüleri (Barlo oradayken, müşteri varken)
            var pl = PlayerController.I;
            if (pl && !_cellarShot && GameState.IsUnlocked("mahzen") && pl.transform.position.x < -5f && pl.transform.position.z > 5.6f && pl.transform.position.z < 20f && _t > 45f)
            { _cellarShot = true; Shot("playtest_cellar.png"); }
            if (pl && !_terraceShot && GameState.IsUnlocked("teras") && Inn.Upstairs(pl.transform.position) && pl.transform.position.x > 6.3f && _t > 45f)
            { _terraceShot = true; Shot("playtest_terrace.png"); Log($"Teras: müşteri={Customer.All.FindAll(c => Inn.Upstairs(c.transform.position)).Count}"); }
            if (pl && !_yardShot && GameState.IsUnlocked("avlu") && pl.transform.position.x > 15.6f && !Inn.Upstairs(pl.transform.position) && _t > 45f)
            { _yardShot = true; Shot("playtest_yard.png"); }
            if (pl && !_dockShot && GameState.IsUnlocked("iskele") && pl.transform.position.x < -15.6f && _t > 45f)
            { _dockShot = true; Shot("playtest_dock.png"); Log($"İskele: müşteri={Customer.All.FindAll(c => c.transform.position.x < -15.2f).Count}"); }
            // v1.0 Tavernayı Büyüt: kart → dönüşüm (toz, şerit) → yeni kademe → kademe yolu
            if (NoTests && _tierStep == 0) _tierStep = 9;   // v1.8: ölçümde kademe kartı testi yok (harcama da beklemez)
            if (hud && Tiers.Visible && _tierStep == 0 && _real > 4f) { _tierStep = 1; _tierAt = _real; hud.DebugOpen("tier"); Log($"Kademe kartı: şartlar=[{string.Join(", ", Tiers.Requirements(Tiers.Next).ConvertAll(r => (r.ok ? "✓" : "✗") + r.label))}]"); }
            if (hud && _tierStep == 1 && _real > _tierAt + 1.2f) { _tierStep = 2; Shot("ui_tier_card.png"); }
            if (hud && _tierStep == 2 && _real > _tierAt + 2.0f)
            {
                hud.DebugOpen("close");
                bool ok = hud.DebugTierUpgrade();
                Log($"Kademe yükseltme: {(ok ? "OK → " + Tiers.Current.name : "olmadı")} · para {GameState.Coins}");
                _tierStep = ok ? 3 : 6; _tierAt = _real;
            }
            if (_tierStep == 3 && _real > _tierAt + 0.6f) { _tierStep = 4; Shot("playtest_tier_dust.png"); }
            if (_tierStep == 4 && _real > _tierAt + 1.75f) { _tierStep = 5; Shot("playtest_tier_banner.png"); }
            if (hud && _tierStep == 5 && _real > _tierAt + 4.5f) { Shot("playtest_tier_after.png"); _tierStep = 6; _tierAt = _real; }
            if (hud && _tierStep == 6 && _tierAt > 0f && _real > _tierAt + 0.8f) { hud.DebugOpen("close"); hud.DebugOpen("tier-path"); _tierStep = 7; _tierAt = _real; }
            if (hud && _tierStep == 7 && _real > _tierAt + 0.6f) { Shot("ui_tier_path.png"); _tierStep = 8; }
            if (hud && _tierStep == 8 && _real > _tierAt + 1.4f) { hud.DebugOpen("close"); _tierStep = 9; }
            // v1.0 Hazine: başlangıç teklifi görüntüsü; test satın alma (onay ekranı → ürünler sahnede)
            if (hud && !_starterShot && hud.PremiumModalVisible && GameState.Level >= Premium.StarterLevel && _uiStep >= UiSteps.Length)
            { _starterShot = true; Shot("ui_starter.png"); }
            if (NoTests && _premStep == 0) _premStep = 4;   // v1.8: ölçümde Hazine testi yok (VIP / Lirael dengeyi bozar)
            if (hud && _t > 150f && _premStep == 0 && !hud.PremiumModalVisible) { _premStep = 1; _premAt = _real; WrapTestConfirm(); Premium.Request("staff_lirael"); }
            if (hud && _premStep == 1 && _real > _premAt + 1.2f) { _premStep = 2; Shot("ui_prem_confirm.png"); }
            if (hud && _premStep == 2 && _real > _premAt + 2.0f)
            {
                _premStep = 3;
                // v1.6: test onay ekranında ONAYLA (IStoreService → onPurchased → store_purchase → Premium.Grant)
                bool viaStore = _pendingConfirm != null;
                _pendingConfirm?.Invoke();
                _pendingConfirm = null;
                hud.DebugOpen("close");
                Log($"v1.6 HAZİNE: mağaza üzerinden onay={viaStore} · Lirael alındı={Premium.Owned("staff_lirael")} · store_request {Count("store_request")} · store_purchase {Count("store_purchase")} · mağaza {Services.Store.GetType().Name}");
                foreach (var id in new[] { "staff_lirael", "staff_ignis", "frame_kraliyet", "dec_ejder", "dec_kristal", "thm_altin", "vip_month" }) Premium.Grant(id);
                Log($"Hazine (test): alındı · VIP={Premium.IsVip} · garson={Waiter.All.Count} · elmas={GameState.Data.gems} · yıldız={Management.Stars:0.#}");
            }
            if (_premStep == 3 && _real > _premAt + 5f) { _premStep = 4; Shot("playtest_premium.png"); }
            if (!_morningShot && _morningAt > 0f && _real > _morningAt + 0.9f) { _morningShot = true; Shot("playtest_morning.png"); }
            if (_lastGame && _lastGame.state == GameTable.State.Playing) { _rounds++; _lastGame = null; }
            var p = PlayerController.I;
            if (!_gameShot && p && GameState.IsUnlocked("oyun") && p.transform.position.z > 5.5f && p.transform.position.x > 4.2f && _t > 20f)
            { _gameShot = true; Shot("playtest_gameroom.png"); }
            if (!_worldShot && _t > 8f && _t < 60f && p && p.transform.position.x < -8f) { _worldShot = true; Shot("playtest_world_left.png"); }
            // Otomasyon: önce elle oynansın (ilk 200 oyun-sn), sonra parası bolsa al
            if (_t > 200f)
                foreach (var d in Automation.List)
                    if (Automation.CanBuy(d) == Management.BuyResult.Ok && GameState.Coins > d.price * 1.3f && Automation.Buy(d) == Management.BuyResult.Ok)
                        Log($"Otomasyon alındı: {d.name} ({d.price})");
            if (Automation.UstaOpen && _t > 260f && Automation.UstaIncomeLv == 0 && GameState.Coins > Automation.UstaPrice(0) * 1.5f
                && Automation.BuyUsta("gelir") == Management.BuyResult.Ok) Log("Usta: Altın Kese kademe 1");
            return false;
        }

        /// <summary>Üst katta oda içi hedef: önce koridordan kapı hizasına.</summary>
        static Vector3 InnDoor(Vector3 from, Vector3 to)
        {
            if (!Inn.Upstairs(from) || !Inn.Upstairs(to)) return to;
            if (to.x > 6.0f || from.x > 6.3f) return from.x > 6.3f || from.z < 38.9f ? to : new Vector3(from.x, 0f, 38.3f);   // teras: odalardan çıkıp koridordan geç
            bool inA = from.z > 39f, inB = to.z > 39f;
            if (inA == inB && (!inA || Mathf.Abs(from.x - to.x) < 0.9f)) return to;
            if (inA)   // odadan çık: önce kapı hizası (oda içinde), sonra koridor
            {
                float dx = 2f * Mathf.Floor((from.x + 6f) / 2f) - 4.5f;
                return Mathf.Abs(from.x - dx) > 0.25f ? new Vector3(dx, 0f, 39.6f) : new Vector3(dx, 0f, 38.4f);
            }
            float doorX = to.x;
            return Mathf.Abs(from.x - doorX) > 0.25f && from.z < 38.9f ? new Vector3(doorX, 0f, 38.4f) : to;
        }

        void Steer(PlayerController p, Vector3? target)
        {
            if (target == null) { PlayerController.JoystickInput = Vector2.zero; return; }
            // Bölgeler arası geçişte duvar boşluğunun ortasından geç
            var tv = target.Value;
            float px = p.transform.position.x;
            // v1.5: arka odalarda (mutfak, oyun salonu, mahzen) bu kapı kuralı yok — mutfakta bot geri sokağa çekiliyordu
            if (!Inn.Upstairs(p.transform.position) && !KitchenLayout.InBack(p.transform.position))
            foreach (float gate in new[] { 3f, -3f })
                if ((px - gate) * (tv.x - gate) < 0f && Mathf.Abs(px - gate) > 0.4f) { tv = new Vector3(gate, 0f, -0.6f); break; }
            var to = tv - p.transform.position;
            to.y = 0f;
            if (to.magnitude < 0.35f) { PlayerController.JoystickInput = Vector2.zero; _stuckT = 0; return; }
            var dir = new Vector2(to.x, to.z).normalized;

            // Takılma: 0.6 sn ilerleyemezse 0.5 sn yana kay
            float moved = Vector3.Distance(p.transform.position, _lastPos);
            _lastPos = p.transform.position;
            _stuckT = moved < 0.01f ? _stuckT + Time.deltaTime : 0f;
            if (_stuckT > 0.6f) { _sideT = 0.5f; _stuckT = 0f; _side = Random.value > 0.5f ? new Vector3(-dir.y, 0, dir.x) : new Vector3(dir.y, 0, -dir.x); }
            if (_sideT > 0f) { _sideT -= Time.deltaTime; dir = (dir + new Vector2(_side.x, _side.z) * 1.5f).normalized; }
            PlayerController.JoystickInput = dir;
        }

        void TryUpgrades()
        {
            // Sıradaki ödeme karesi için ayrılan para: yeni harcamalar (v0.6) da buna uysun
            var nextZone = PayZone.NextGoal();
            long keep = (nextZone && nextZone.Available ? nextZone.Remaining : 0) + Management.DailyWages + 300;   // v1.0: maaş + malzeme payı
            // v1.8 ölçüm ("iyi oyuncu"): açılan görevliyi önce işe al (eskiden yükseltmeler parayı yiyordu, ozan / aşçı hiç alınmıyordu)
            if (Measure)
                foreach (var st in Catalog.StaffList)
                    if (Management.CanHire(st) == Management.BuyResult.Ok && GameState.Coins - Management.HireCost(st) > Management.DailyWages + 300 && Management.Hire(st) == Management.BuyResult.Ok)
                        Log($"İşe alındı (öncelikli): {st.name} ({st.roleName}) · ücret {Management.HireCost(st)}");
            foreach (var s in Catalog.StaffList)
                if (StaffUpgrades.CanLevel(s) && GameState.Coins - StaffUpgrades.Cost(s, StaffUpgrades.Level(s.id)) > keep + 100 && StaffUpgrades.TryBuy(s))
                    Log($"Görevli gelişti: {s.name} → Sv {StaffUpgrades.Level(s.id)}");
            // Depo (v0.6): raf azalınca ve yolda sipariş yoksa sipariş ver; para artınca rafı büyüt
            foreach (var st in DrinkStation.All)
            {
                if (!st || !st.drink) continue;
                var id = st.drink.id;
                if (Depot.Incoming(id) == 0 && Depot.Supply(id) <= Depot.Capacity(id) * 0.35f)
                {
                    var r = Depot.Order(id);
                    if (r == Depot.OrderResult.Ok) Log($"Sipariş: {id} +{Balance.SupplyPack}");
                }
                else if (Depot.CanUpgradeShelf(id) && GameState.Coins - Balance.DepotUpgradeCost(st.drink, Depot.DepotLevel(id)) > keep + 100 && Depot.UpgradeShelf(id))
                    Log($"Raf büyütüldü: {id} → {Depot.Capacity(id)}");
            }
            for (int i = 0; i < Quests.List.Count; i++)
            {
                var q = Quests.List[i];
                string txt = Quests.Text(q);
                if (q.Done && !q.claimed && Quests.Claim(i)) Log($"Görev ödülü toplandı: \"{txt}\" ({q.progress}/{q.target}) +{q.rewardCoins} altın +{q.rewardGems} elmas");
            }
            if (Offline.Pending > 0) Offline.Collect();
            // Para fazlaysa (ödeme alanlarına yetecek kadar bırakıp) ucuz yükseltmeleri al
            // Yönetim: makine > personel > dekor (ucuzdan pahalıya)
            foreach (var m in Catalog.Machines)
                if (Management.CanBuyMachine(m) == Management.BuyResult.Ok && Management.BuyMachine(m) == Management.BuyResult.Ok)
                    Log($"Makine alındı: {m.drink} ({m.price})");
            foreach (var st in Catalog.StaffList)
            {
                long hc = Management.HireCost(st);
                if (_fireStep == 1 && st.id == "staff_nana") continue;   // kovma testi: yeniden işe alımı test adımı yapsın
                if (Management.CanHire(st) == Management.BuyResult.Ok && GameState.Coins > hc * 1.5f && Management.Hire(st) == Management.BuyResult.Ok)
                    Log($"İşe alındı: {st.name} ({st.roleName}) · ücret {hc}");
            }
            var goal = PayZone.NextGoal();
            long reserve = (goal && goal.Available ? goal.Remaining : 0) + (Measure ? Management.DailyWages : 0);   // v1.8: iyi oyuncu maaşı kasada tutar
            foreach (var d in Catalog.DecorList)
                if (Management.CanBuyDecor(d) == Management.BuyResult.Ok && GameState.Coins - d.price > reserve + 50 && Management.BuyDecor(d) == Management.BuyResult.Ok)
                    Log($"Dekor: {d.name} → ambiyans {AmbianceSource.Total:0} ★{Management.Stars:0.#} (sonraki ★ için {Management.AmbianceToNextStar})");
            foreach (var s in DrinkStation.All)
                foreach (UpgradeStat stat in System.Enum.GetValues(typeof(UpgradeStat)))
                {
                    int lv = s.Save.Get(stat);
                    if (lv >= Balance.MaxUpgradeLevel) continue;
                    int cost = Balance.UpgradeCost(s.drink, stat, lv);
                    if (GameState.Coins - cost >= reserve && s.TryUpgrade(stat))
                    {
                        _upgradesBought++;
                        Log($"Yükseltme: {s.drink.displayName} {stat} → Sv{lv + 1} ({cost} altın)");
                    }
                }
        }

        void OnEnable() { GameState.LeveledUp += OnLevel; }
        void OnDisable() { GameState.LeveledUp -= OnLevel; Tutorial.Changed -= OnTutorialStep; }
        int _specialAtForce;
        void OnLevel(int lv) => Log($"*** SEVİYE {lv} ***");

        string _music = "";
        bool _evStarted, _evShot;
        int _fireStep;
        float _evShotAt;
        int _startLv, _startUn;
        long _earned;

        /// <summary>Tempo ölçümü: kazanılan Ün ve altın, oyun dakikası başına (gün sonu duraklaması sayılmaz).</summary>
        void LogTempo()
        {
            long un = GameState.Data.un - _startUn;
            for (int l = _startLv; l < GameState.Level; l++) un += Balance.UnForLevel(l);
            float min = Mathf.Max(0.01f, _t / 60f);
            Log($"TEMPO: başlangıç Sv{_startLv} → Sv{GameState.Level} · {min:0.0} oyun-dk · Ün {un} ({un / min:0.0}/dk) · altın {_earned} ({_earned / min:0}/dk) · servis {GameState.Data.customersServed} · kızgın {_angryLeaves} · sonraki seviye {Balance.UnForLevel(GameState.Level)} Ün");
        }

        void Finish()
        {
            _done = true;
            LogTempo();
            PlayerController.JoystickInput = Vector2.zero;
            Log($"v0.9: gün sonu={_daysEnded} oyun turu={_rounds} otomasyon=[{string.Join(",", GameState.Data.autos)}] depo-oto={GameState.Data.autoOrder} usta gelir={Automation.UstaIncomeLv} rehber={(Tutorial.Active ? "açık · adım " + (Tutorial.Step + 1) : GameState.Data.tutorialDone ? "bitti" : "yok")} ad={GameState.TavernName}");
            Log($"v1.3: özel sipariş açılan={Customer.SpecialOrders} oyuncu servis etti={Customer.SpecialServed} · garson={Waiter.All.Count} · kasa otomasyonu={Automation.Has("kasa")} · yerdeki para yığını={CoinPile.All.Count}");
            Log($"v1.4 MUTLULUK ÖZET: etkin %{_happyMin:0.0}–%{_happyMax:0.0} · ortalama %{_avgMin:0.0}–%{_avgMax:0.0} · şimdi %{Happiness.Value:0.0} (ort. %{Happiness.Average:0.0}) · en çok kirli masa {_dirtyMax} · puanlanan müşteri {Happiness.Samples}");
            Log($"v1.4 ÇARPAN ÖZET: ham en yüksek ×{_rawMax:0.00} · uygulanan en yüksek ×{_capMax:0.00} (sınır ×{Balance.SpawnBoostCap:0.0})");
            var sq = Quests.List.Find(q => q.type == QuestType.SpecialOrder || q.type == QuestType.SpecialOrderBig);
            Log($"v1.4 GÖREV ÖZET: oyuncunun servis ettiği özel sipariş (zorlamadan sonra) {Customer.SpecialServed - _specialAtForce} · listede özel görev: {(sq != null ? $"\"{Quests.Text(sq)}\" {sq.progress}/{sq.target}" : "yok (toplandı)")}");
            Log($"v1.4 YILDIZ ÖZET: ambiyans={AmbianceSource.Total:0} ★{Management.Stars:0.#} · sonraki ★{Management.NextStar} için {Management.AmbianceToNextStar}");
            Log($"v1.4 TELLAL ÖZET: etkinlik={(TavernEvents.Active.HasValue ? TavernEvents.Active.ToString() : "-")} · tellal görünür={Campaigns.CrierVisible} · bugün=[{string.Join(",", GameState.Data.eventsUsed)}]");
            Log($"v1.4 REHBER ÖZET: {(Tutorial.Active ? "açık · adım " + (Tutorial.Step + 1) : GameState.Data.tutorialDone ? "bitti" : "yok")} · bitiş gerçek {_tutEndReal:0.0} sn");
            FinishV15();
            FinishV16();
            if (Measure) FinishV18();
            if (Quiet) Debug.Log($"[Playtest] v1.8 SESSİZ KOŞU: >{SlowFrameSeconds * 1000f:0} ms kare {_slowAll} (en uzun {_slowMaxMs:0} ms) · {_real:0} gerçek sn · GC {System.GC.CollectionCount(0)} · Sv{GameState.Level}");
            Log($"v1.2: mutluluk=%{Happiness.Value:0} (müşteri ×{Happiness.SpawnMultiplier:0.00}) ★{Management.Stars:0.#} etkinlik={(TavernEvents.Active.HasValue ? TavernEvents.Active.ToString() : "-")} bugün=[{string.Join(",", GameState.Data.eventsUsed)}] masa={Table.All.Count}");
            Log($"SES: grup={Sfx.LoadedGroups} müzik={Sfx.MusicNow} kalabalık={Sfx.CrowdLevel:0.00} konuşma={Sfx.TalkCount} ortam=[{Sfx.AmbientInfo}]");
            Log($"HAN: açık={Inn.Open} giriş={Inn.CheckIns} kızgın misafir={Inn.AngryGuests} toplanan oda={Inn.RoomsCleaned} (bot {_roomsCleaned}) oda durumları=[{string.Join(",", Room.All.ConvertAll(x => x.state.ToString()))}]");
            Log($"SONUÇ: {gameSeconds:0} oyun-sn · Para={GameState.Coins} · Seviye={GameState.Level} · " +
                $"Servis={GameState.Data.customersServed} · Kızgın ayrılan={_angryLeaves} · Yükseltme={_upgradesBought} · " +
                $"Açılan=[{string.Join(",", GameState.Data.unlocked)}]");
            Shot("playtest_end.png");
            var dir = Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "../../captures")) : AutoTest.CaptureDir;
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "playtest_report.txt"), _log.ToString());
            Time.timeScale = 1f;
            onFinished?.Invoke(_log.ToString());
        }

        /// <summary>Kamera görüntüsü + UI Toolkit paneli (targetTexture'a çiziliyorsa) birleşik PNG.</summary>
        void Shot(string file)
        {
            if (NoTests || NoShots) return;   // v1.8: ölçüm / sessiz koşu ve "-autotest-noshots" görüntü almaz (yavaş kare ayrımı)
            if (!Application.isEditor) { AutoTest.Shot(file); return; }
            var cam = Camera.main;
            if (!cam) return;
            const int w = 1080, h = 1920;
            var rt = RenderTexture.GetTemporary(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            RenderTexture.active = rt;
            var bg = new Texture2D(w, h, TextureFormat.RGBA32, false);
            bg.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            var doc = FindAnyObjectByType<UIDocument>();
            var uiRt = doc && doc.panelSettings ? doc.panelSettings.targetTexture : null;
            if (uiRt)
            {
                RenderTexture.active = uiRt;
                var ui = new Texture2D(uiRt.width, uiRt.height, TextureFormat.RGBA32, false);
                ui.ReadPixels(new Rect(0, 0, uiRt.width, uiRt.height), 0, 0);
                RenderTexture.active = null;
                var a = bg.GetPixels32();
                var b = ui.GetPixels32();
                for (int i = 0; i < a.Length && i < b.Length; i++)
                {
                    float al = b[i].a / 255f;
                    a[i] = new Color32((byte)(a[i].r * (1 - al) + b[i].r * al), (byte)(a[i].g * (1 - al) + b[i].g * al),
                                       (byte)(a[i].b * (1 - al) + b[i].b * al), 255);
                }
                bg.SetPixels32(a);
                Destroy(ui);
            }
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../captures"));
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, file), bg.EncodeToPNG());
            Destroy(bg);
        }
    }
}
#endif
