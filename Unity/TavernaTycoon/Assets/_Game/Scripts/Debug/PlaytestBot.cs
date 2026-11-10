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
    public class PlaytestBot : MonoBehaviour
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
        static readonly (string, string)[] UiSteps = { ("hud", "ui_hud.png"), ("upgrade", "ui_upgrade.png"), ("quests", "ui_quests.png"), ("shop", "ui_shop.png"), ("shop-staff", "ui_shop_staff.png"), ("shop-dec", "ui_shop_dec.png"), ("depot", "ui_depot.png"), ("shop-auto", "ui_shop_auto.png"), ("shop-prem", "ui_shop_prem.png"), ("vip", "ui_vip.png") };
        float _fpsAcc; int _fpsN;

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
        }

        void Log(string s) { var line = $"[{_t,6:0.0}s] {s}"; _log.AppendLine(line); Debug.Log("[Playtest] " + line); }

        void Update()
        {
            if (_done) return;
            _real += Time.unscaledDeltaTime;
            if (UpdateV09()) return;
            _t += Time.deltaTime;
            var p = PlayerController.I;
            if (!p) return;

            foreach (var c in Customer.All)
                if (c.emote == Customer.Emote.Angry && c.emoteTime > 1.45f) { _angryLeaves++; c.emoteTime = 1.44f; }

            Vector3? target = Decide(p);
            if (target.HasValue) target = Inn.Route(p.transform.position, target.Value);
            if (target.HasValue) target = InnDoor(p.transform.position, target.Value);
            if (target.HasValue) target = KitchenLayout.Route(p.transform.position, target.Value);
            Steer(p, target);
            if (!(_tierStep < 3 && Tiers.Visible && Tiers.Next != null && Tiers.Next.index == 1)) TryUpgrades();   // v1.0: kademe testi bitene kadar para harcama
            // Teşhis: yavaş kare (>80 ms) olunca o anki durumu yaz (2 sn'de bir)
            _slowLogT -= Time.unscaledDeltaTime;
            if (Time.unscaledDeltaTime > 0.08f && _slowLogT <= 0f)
            {
                _slowLogT = 2f;
                int gc = System.GC.CollectionCount(0);
                Log($"YAVAŞ KARE {Time.unscaledDeltaTime * 1000f:0} ms · müşteri={Customer.All.Count} para yığını={CoinPile.All.Count} " +
                    $"dünya UI={HudController.DebugWorldCount} efekt UI={HudController.DebugFxCount} GC+={gc - _gc0} faz={DayCycle.Progress01:0.00} " +
                    $"yoğun={RushHour.Active} masa={Table.All.Count} istasyon={DrinkStation.All.Count} fener={Lantern.All.Count} amb={AmbianceSource.All.Count}");
                _gc0 = gc;
            }

            _fpsAcc += Time.unscaledDeltaTime; _fpsN++;
            _logT += Time.deltaTime;
            if (_logT >= 20f)
            {
                _logT = 0f;
                Log($"Para={GameState.Coins} Seviye={GameState.Level} Ün={GameState.Data.un}/{Balance.UnForLevel(GameState.Level)} " +
                    $"FPS={(_fpsN / Mathf.Max(0.001f, _fpsAcc)):0} Servis={GameState.Data.customersServed} İçeride={Customer.All.Count} Garson={Waiter.All.Count} Yıldız={Management.Stars:0.#} Gün={GameState.Data.day} Yoğun={RushHour.Active} Açılan=[{string.Join(",", GameState.Data.unlocked)}] Hedef={_goal} Poz=({p.transform.position.x:0.0},{p.transform.position.z:0.0})");
                _fpsAcc = 0; _fpsN = 0;
            }
            // Arayüz ekranlarının görüntüleri (bir kez)
            var hud = FindAnyObjectByType<HudController>();
            if (hud)
            {
                if (hud.LevelCardOpen)
                {
                    if (!_levelShot) { _levelShot = true; _levelCloseAt = _t + 1f; Shot("ui_levelup.png"); }
                    else if (_t > _levelCloseAt) hud.DebugOpen("close");
                }
                if (_uiStep < UiSteps.Length && _t > 30f + _uiStep * 4f)
                {
                    var (what, file) = UiSteps[_uiStep];
                    if (_uiPhase == 0) { hud.DebugOpen(what); _uiPhase = 1; _uiT = _t; }
                    else if (_uiPhase == 1 && _t > _uiT + 1.2f) { Shot(file); _uiPhase = 2; }
                    else if (_uiPhase == 2 && _t > _uiT + 2.0f) { hud.DebugOpen("close"); _uiPhase = 0; _uiStep++; }
                }
            }
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
                float d = DrinkStation.FlatDistance(pos, c.transform.position) - c.Patience01 * 2f;
                if (d < bestC) { bestC = d; serveTarget = c; }
            }
            if (serveTarget && (!nearestCoin || bestC < best)) { _goal = "servis:" + serveTarget.def.displayName; return serveTarget.transform.position; }
            if (nearestCoin && best < 4f) { _goal = "para"; return nearestCoin.transform.position; }

            // Oyun salonu (v0.9): hazır masada tur başlat (otomasyon yoksa)
            if (!Automation.Has("oyun"))
                foreach (var gt in GameTable.All)
                    if (gt.state == GameTable.State.Ready) { _goal = "oyun:" + gt.Title; _lastGame = gt; return gt.transform.position + new Vector3(0f, 0f, -0.95f); }

            // Test (v1.0): iskeleye bir kez bak (müşteri oturmuşsa)
            if (!_dockShot && GameState.IsUnlocked("iskele") && _t > 45f && Customer.All.Exists(c => c.transform.position.x < -15.3f && c.state != Customer.State.Leaving))
            { _goal = "iskeleye bak"; return new Vector3(-17.8f, 0f, -1.0f); }

            // Han (v0.8): resepsiyonda bekleyen misafiri karşıla (servisten sonra en öncelikli iş)
            foreach (var gu in Guest.All)
                if (gu.state == Guest.State.Waiting)
                {
                    _goal = $"misafir d={DrinkStation.FlatDistance(pos, gu.transform.position):0.0} g=({gu.transform.position.x:0.0},{gu.transform.position.z:0.0})";
                    return gu.transform.position + Vector3.left * 0.6f;
                }
            // Test: uyuyan misafiri ve sabah dağınık odayı görmek için bir kez merdivenden üst kata çık
            foreach (var rm in Room.All)
                if ((!_sleepShot && rm.state == Room.State.Occupied) || (!_dirtyShot && rm.state == Room.State.Dirty))
                { _goal = "üst kata bak"; return new Vector3(rm.bedPoint.position.x + 0.5f, 0f, 38.3f); }


            // 3) Bekleyen siparişler için istasyona git
            if (p.carrier.HasSpace)
            {
                foreach (var c in Customer.All)
                {
                    if (c.state != Customer.State.Waiting || p.carrier.Has(c.wanted)) continue;
                    var st = DrinkStation.All.Find(s => s.drink.id == c.wanted);
                    if (st && st.Stock > 0) { _goal = "istasyon:" + st.drink.displayName; return st.ServePos; }
                }
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

            _goal = "bekle";
            return new Vector3(0.4f, 0f, 0.5f);
        }

        Room _cleaning;

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
            var hud = FindAnyObjectByType<HudController>();
            if (!_introShot && _real > 1.1f) { _introShot = true; Shot("playtest_intro.png"); }
            if (!_tutShot && Tutorial.Active && _real > 4f) { _tutShot = true; Shot("ui_tutorial.png"); }
            if (DayCycle.Ended && hud)
            {
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
            if (hud && _t > 150f && _premStep == 0 && !hud.PremiumModalVisible) { _premStep = 1; _premAt = _real; Premium.Request("staff_lirael"); }
            if (hud && _premStep == 1 && _real > _premAt + 1.2f) { _premStep = 2; Shot("ui_prem_confirm.png"); }
            if (hud && _premStep == 2 && _real > _premAt + 2.0f)
            {
                _premStep = 3;
                hud.DebugOpen("close");
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
            if (!Inn.Upstairs(p.transform.position))
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
            foreach (var s in Catalog.StaffList)
                if (StaffUpgrades.CanLevel(s) && GameState.Coins - StaffUpgrades.Cost(s, StaffUpgrades.Level(s.id)) > keep + 100 && StaffUpgrades.TryBuy(s))
                    Log($"Görevli gelişti: {s.name} → Sv {StaffUpgrades.Level(s.id)}");
            if (!Campaigns.CrierActive && GameState.Coins - Campaigns.CrierCost > keep + 200 && Campaigns.BuyCrier()) Log("Tellal tutuldu");
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
                if (Quests.List[i].Done && !Quests.List[i].claimed && Quests.Claim(i)) Log("Görev ödülü toplandı");
            if (Offline.Pending > 0) Offline.Collect();
            // Para fazlaysa (ödeme alanlarına yetecek kadar bırakıp) ucuz yükseltmeleri al
            // Yönetim: makine > personel > dekor (ucuzdan pahalıya)
            foreach (var m in Catalog.Machines)
                if (Management.CanBuyMachine(m) == Management.BuyResult.Ok && Management.BuyMachine(m) == Management.BuyResult.Ok)
                    Log($"Makine alındı: {m.drink} ({m.price})");
            foreach (var st in Catalog.StaffList)
                if (Management.CanHire(st) == Management.BuyResult.Ok && GameState.Coins > st.hire * 1.5f && Management.Hire(st) == Management.BuyResult.Ok)
                    Log($"İşe alındı: {st.name} ({st.roleName})");
            var goal = PayZone.NextGoal();
            long reserve = goal && goal.Available ? goal.Remaining : 0;
            foreach (var d in Catalog.DecorList)
                if (Management.CanBuyDecor(d) == Management.BuyResult.Ok && GameState.Coins - d.price > reserve + 50 && Management.BuyDecor(d) == Management.BuyResult.Ok)
                    Log($"Dekor: {d.name} → ★{Management.Stars:0.#}");
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
        void OnDisable() { GameState.LeveledUp -= OnLevel; }
        void OnLevel(int lv) => Log($"*** SEVİYE {lv} ***");

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
