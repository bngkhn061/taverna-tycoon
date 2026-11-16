#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Taverna
{
    /// <summary>
    /// v1.5 Paket C test adımları ("-autotest-v15 content" ya da "-autotest-v15 prestige"):
    /// content: Kral Şerbeti ve Şölen Tabağı sipariş / üretim / servis, makine yerleşimi (çakışma), aşçı ve garson yolu,
    /// Kraliyet Ziyareti etkisi. prestige: Sv30 + Saray Hanı + 150.000 altın → şube kartı, iki dokunuşla onay,
    /// sahne yeniden yüklenir, sıfırlanan / kalan değerler ve çarpanlar, yeni şubede normal akış.
    /// </summary>
    public partial class PlaytestBot
    {
        string _v15Mode;
        Vector3? _v15Visit;
        float _v15VisitAt = -1f, _v15VisitStart = -1f;
        int _v15VisitStep, _v15Step, _v15KingBefore, _v15KingDuring, _v15ServesAfter, _v15WaiterSolen;
        float _v15At, _v15CookT, _v15LogT, _v15EventEnd;
        readonly Dictionary<DrinkId, int> _v15Ordered = new(), _v15Served = new(), _v15Produced = new();
        readonly HashSet<Customer> _v15SeenOrder = new();
        readonly Dictionary<int, int> _v15CookStops = new();
        readonly HashSet<Customer> _v15KingSeen = new();
        bool _v15Hooked, _v15Reloaded, _v15AfterLogged;
        int _v15ServedAtReload, _v15SceneLoads;
        float _v15ReloadReal = -1f;
        int _v15PrevStock1, _v15PrevStock2;

        const int V15Waiting = 20, V15After = 21;
        static readonly DrinkId[] V15Items = { DrinkId.KralSerbeti, DrinkId.SolenTabagi };

        string V15Mode
        {
            get
            {
                if (_v15Mode != null) return _v15Mode;
                var args = System.Environment.GetCommandLineArgs();
                int i = System.Array.IndexOf(args, "-autotest-v15");
                _v15Mode = i >= 0 && i + 1 < args.Length ? args[i + 1] : "";
                return _v15Mode;
            }
        }

        /// <summary>Prestij yolu: "prestige" (şube 0 → 1) ya da v1.8 "prestige2" (şube 1'den başlar, ikinci prestij → şube 2).</summary>
        bool V15Prestige => V15Mode == "prestige" || V15Mode == "prestige2";

        void UpdateV15(HudController hud, PlayerController p)
        {
            if (!_v15Hooked)
            {
                _v15Hooked = true;
                GameState.DrinkServed += d => { if (System.Array.IndexOf(V15Items, d) >= 0) _v15Served[d] = (_v15Served.TryGetValue(d, out var n) ? n : 0) + 1; };
                SceneManager.sceneLoaded += OnV15SceneLoaded;
                if (V15Mode != "") Log($"v1.5 TEST: mod={V15Mode} · Sv{GameState.Level} · kademe {Tiers.Current.name} · şube {Prestige.Branch} ({Prestige.CurrentName})");
            }
            if (V15Mode == "content") UpdateV15Content(hud, p);
            else if (V15Prestige) UpdateV15Prestige(hud, p);
        }

        void OnV15SceneLoaded(Scene s, LoadSceneMode m)
        {
            _v15SceneLoads++;
            if (_v15SceneLoads < 1) return;
            Time.timeScale = timeScale;   // Prestige.Execute zamanı 1'e çeker
            _v15Reloaded = true;
            _v15ReloadReal = _real;
            _v15ServedAtReload = GameState.Data.customersServed;
            Log($"v1.5 PRESTİJ: sahne yeniden yüklendi ({s.name}) · şube {Prestige.Branch} {Prestige.CurrentName}");
        }

        // ------------------------------------------------------------------ (a) + (b) yeni ürünler ve Kraliyet Ziyareti
        void UpdateV15Content(HudController hud, PlayerController p)
        {
            // Siparişler: bekleyen müşterilerin istediği yeni ürünler (müşteri başına bir kez)
            foreach (var c in Customer.All)
                if (c.state == Customer.State.Waiting && System.Array.IndexOf(V15Items, c.wanted) >= 0 && _v15SeenOrder.Add(c))
                    _v15Ordered[c.wanted] = (_v15Ordered.TryGetValue(c.wanted, out var n) ? n : 0) + 1;
            // Üretim: stok artışı
            var s1 = DrinkStation.All.Find(s => s && s.drink && s.drink.id == DrinkId.KralSerbeti);
            var s2 = DrinkStation.All.Find(s => s && s.drink && s.drink.id == DrinkId.SolenTabagi);
            if (s1) { if (s1.Stock > _v15PrevStock1) _v15Produced[DrinkId.KralSerbeti] = (_v15Produced.TryGetValue(DrinkId.KralSerbeti, out var a) ? a : 0) + s1.Stock - _v15PrevStock1; _v15PrevStock1 = s1.Stock; }
            if (s2) { if (s2.Stock > _v15PrevStock2) _v15Produced[DrinkId.SolenTabagi] = (_v15Produced.TryGetValue(DrinkId.SolenTabagi, out var b) ? b : 0) + s2.Stock - _v15PrevStock2; _v15PrevStock2 = s2.Stock; }
            // Aşçı durakları (mutfak makinelerinin önü z ≈ 8.6): x'e göre say
            _v15CookT -= Time.deltaTime;
            if (_v15CookT <= 0f)
            {
                _v15CookT = 1f;
                foreach (var w in FindObjectsByType<StaffWork>())
                {
                    if (!w.kitchenStations || !w.isActiveAndEnabled) continue;
                    var wp = w.transform.position;
                    if (Mathf.Abs(wp.z - 8.6f) < 0.15f) { int key = Mathf.RoundToInt(wp.x * 10f); _v15CookStops[key] = (_v15CookStops.TryGetValue(key, out var k) ? k : 0) + 1; }
                }
                if (s2) foreach (var wa in Waiter.All) if (wa && DrinkStation.FlatDistance(wa.transform.position, s2.ServePos) < 0.6f) _v15WaiterSolen++;
            }
            // Makinelerin önüne git, görüntü al
            if (_v15VisitStep == 0 && _t > 4f && s1 && s2)
            {
                _v15VisitStep = 1;
                LogPlacement(s1); LogPlacement(s2);
                foreach (var st in DrinkStation.All)
                    if (st && st.drink && st.drink.isFood && KitchenLayout.InKitchen(st.transform.position))
                        Log($"v1.5 MUTFAK OCAĞI: {st.drink.displayName} x={st.transform.position.x:0.00} z={st.transform.position.z:0.00} · aşçı durağı ({st.transform.position.x:0.00}, {st.transform.position.z - 0.85f:0.00})");
                _v15Visit = s1.ServePos + new Vector3(0f, 0f, -1.2f);
            }
            if (_v15Visit.HasValue && _v15VisitStart < 0f) _v15VisitStart = _t;
            bool timeout = _v15Visit.HasValue && _t > _v15VisitStart + 40f;   // yola takılırsa yine de çek, servise dön
            if (timeout && _v15VisitAt < 0f) { Log($"v1.5 BAK: {_v15VisitStep}. noktaya varılamadı (poz {p.transform.position.x:0.0},{p.transform.position.z:0.0}), yerinde çekiliyor"); _v15VisitAt = _t - 2f; }
            if (_v15Visit.HasValue && p && (timeout || DrinkStation.FlatDistance(p.transform.position, _v15Visit.Value) < 0.5f))
            {
                if (_v15VisitAt < 0f) _v15VisitAt = _t;
                if (hud && hud.PremiumModalVisible && _t > _v15VisitAt + 0.5f) { hud.DebugOpen("close"); _v15VisitAt = _t; }   // başlangıç teklifi görüntüyü kapatmasın
                if (_t > _v15VisitAt + 1.5f)
                {
                    _v15VisitStart = -1f;
                    if (_v15VisitStep == 1) { Shot("v15_salon_machine.png"); _v15VisitStep = 2; _v15VisitAt = -1f; _v15Visit = s2 ? new Vector3(0.5f, 0f, 7.4f) : (Vector3?)null; }   // mutfağın ortası: beş ocak birden görünsün
                    else if (_v15VisitStep == 2) { Shot("v15_kitchen.png"); _v15VisitStep = 3; _v15VisitAt = -1f; _v15Visit = null; }
                }
            }
            // (b) Kraliyet Ziyareti: önce Gizli Kral'ı say, sonra başlat
            foreach (var c in Customer.All)
                if (c && c.def && c.def.number == TavernEvents.KingCustomerNumber && _v15KingSeen.Add(c))
                {
                    if (TavernEvents.Is(TavernEvents.Id.Kraliyet)) _v15KingDuring++; else _v15KingBefore++;
                }
            if (_v15Step == 0 && _t > 60f)
            {
                _v15Step = 1;
                var d = TavernEvents.Get(TavernEvents.Id.Kraliyet);
                int priceBefore = s1 ? s1.Price : 0;
                float tipBefore = TavernEvents.TipMultiplier;
                long cost = TavernEvents.Cost(d);
                Log($"v1.5 KRALİYET: durum={TavernEvents.StateOf(d)} · fiyat {cost} (pay {d.share} × 3 dk gelir {Balance.EventIncomeRef(GameState.Level):0}) · diğer en pahalı {TavernEvents.Cost(TavernEvents.Get(TavernEvents.Id.Turnuva))} · {d.level}. seviye, {TavernEvents.Seconds:0} sn");
                if (GameState.Coins < cost) GameState.Data.coins += cost;
                var r = TavernEvents.Start(d);
                var kral = System.Array.Find(GameDatabase.I.customers, c => c && c.number == TavernEvents.KingCustomerNumber);
                Log($"v1.5 KRALİYET: başlat → {r} · etkin={TavernEvents.Active} · Kral Şerbeti fiyatı {priceBefore} → {(s1 ? s1.Price : 0)} · fiyat ×{TavernEvents.PriceMultiplier:0.0} · bahşiş ×{tipBefore:0.0} → ×{TavernEvents.TipMultiplier:0.0} · Gizli Kral ağırlığı {kral.spawnWeight} × {TavernEvents.CustomerWeight(30):0} · diğer müşteri ×{TavernEvents.CustomerWeight(29):0} · müşteri gelişi ×{TavernEvents.SpawnMultiplier:0.0} (tek etkinlik)");
                foreach (var other in TavernEvents.List)
                    if (other.id != TavernEvents.Id.Kraliyet) Log($"v1.5 KRALİYET: aynı anda {other.name} → {TavernEvents.StateOf(other)}");
                _v15EventEnd = _t + 120f;
                _v15Step = 2;   // etkinlik listesi (6 satır) görüntüsü UiSteps'teki ui_events.png
            }
            if (_v15Step == 2 && _t > _v15EventEnd)
            {
                _v15Step = 3;
                Log($"v1.5 KRALİYET ÖZET: Gizli Kral etkinlikten önce {_v15KingBefore} (60 oyun-sn) · etkinlikte {_v15KingDuring} (120 oyun-sn) · etkin={TavernEvents.Active} kalan {TavernEvents.Remaining:0} sn");
            }
            _v15LogT += Time.deltaTime;
            if (_v15LogT > 60f) { _v15LogT = 0f; LogV15Items(); }
        }

        void LogV15Items()
        {
            foreach (var id in V15Items)
                Log($"v1.5 ÜRÜN {id}: sipariş {(_v15Ordered.TryGetValue(id, out var o) ? o : 0)} · üretilen {(_v15Produced.TryGetValue(id, out var pr) ? pr : 0)} · servis {(_v15Served.TryGetValue(id, out var s) ? s : 0)}");
            var stops = new List<string>();
            foreach (var kv in _v15CookStops) stops.Add($"x{kv.Key / 10f:0.0}:{kv.Value}");
            Log($"v1.5 AŞÇI DURAKLARI (z≈8.6, sn): [{string.Join(", ", stops)}] · garson Şölen ocağının önünde {_v15WaiterSolen} sn");
        }

        /// <summary>Makinenin görünür sınırlarıyla çakışan başka nesne var mı (zemin / duvar hariç)?</summary>
        void LogPlacement(DrinkStation st)
        {
            var rs = st.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) { Log($"v1.5 YERLEŞİM {st.drink.displayName}: görünür model yok!"); return; }
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            var hits = new List<string>();
            foreach (var r in FindObjectsByType<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || r.transform.IsChildOf(st.transform)) continue;
                var n = r.gameObject.name;
                if (n.Contains("Zemin") || n.Contains("Duvar") || n.Contains("Pencere") || n.Contains("Kose") || n.Contains("Floor") || n.Contains("Combined")) continue;
                if (r.bounds.size.x > 3f || r.bounds.size.z > 3f) continue;
                var shrunk = new Bounds(b.center, b.size - new Vector3(0.1f, 0f, 0.1f));
                if (shrunk.Intersects(r.bounds)) hits.Add($"{n}@({r.bounds.center.x:0.0},{r.bounds.center.z:0.0})");
            }
            Log($"v1.5 YERLEŞİM {st.drink.displayName}: konum ({st.transform.position.x:0.00}, {st.transform.position.z:0.00}) · sınır x {b.min.x:0.00}..{b.max.x:0.00} z {b.min.z:0.00}..{b.max.z:0.00} · çakışan: [{string.Join(", ", hits)}]");
        }

        // ------------------------------------------------------------------ (c) prestij
        void UpdateV15Prestige(HudController hud, PlayerController p)
        {
            if (!hud) return;
            if (_v15Step == 0 && _real > 2.5f)
            {
                _v15Step = 1;
                // v1.8 ikinci prestij: bu şube zaten Liman Kenti (şube 1) — bir kez prestij yapılmış gibi
                if (V15Mode == "prestige2")
                {
                    GameState.Data.branch = 1;
                    Log($"v1.8 PRESTİJ 2: şube 1'den başlıyor ({Prestige.CurrentName}) · gelir ×{Prestige.IncomeMultiplier:0.00} (beklenen ×{Prestige.IncomeMultiplierFor(1):0.00}) · Ün ×{Prestige.UnMultiplier:0.00} (beklenen ×{Prestige.UnMultiplierFor(1):0.00}) · Boosts gelir ×{Boosts.IncomeMultiplier:0.00}");
                }
                // Tam bir oyun durumu kur: tüm alanlar, makineler, birkaç görevli, otomasyon, yükseltme, dekor, Hazine
                foreach (var z in new List<PayZone>(PayZone.All)) z.DebugUnlock();
                foreach (var m in Catalog.Machines) if (!Management.OwnsMachine(m.drink)) { GameState.Data.unlocked.Add(Catalog.MachineKey(m.drink)); TavernRegistry.I.ApplyMachine(m.drink, false); }
                GameState.Data.coins += 200000;
                foreach (var id in new[] { "staff_pip", "staff_gus", "staff_ayla" }) Management.Hire(System.Array.Find(Catalog.StaffList, s => s.id == id));
                foreach (var d in Automation.List) Automation.Buy(d);
                var st0 = DrinkStation.All.Find(s => s && s.drink && s.drink.id == DrinkId.Bira);
                if (st0) { st0.TryUpgrade(UpgradeStat.Kalite); st0.TryUpgrade(UpgradeStat.Hiz); }
                Management.BuyDecor(Catalog.FindDecor("dec_saksi"));
                Management.BuyDecor(Catalog.FindDecor("dec_kalkan"));
                Premium.Grant("staff_lirael"); Premium.Grant("thm_altin");
                GameState.Data.gems += 37;
                GameState.MarkSeen(30);
                GameState.Data.coins = 90000;   // şart tutmasın: hedef kutusu eksik altını göstersin
                GameState.Save();
                _v15At = _real;
                // Kart adımları boyunca Barlo hedef karesinden uzakta durur (kareye basmak kartı yeniden açar, onayı sıfırlar)
                _v15Visit = new Vector3(2.4f, 0f, 1.2f);
                if (_uiPhase == 2 || _uiPhase == 1) hud.DebugOpen("close");
                _premStep = 4;              // Hazine test penceresi de kartın yerini almasın
                _uiStep = UiSteps.Length;   // panel görüntü turu ("close" çağırır) kartı kapatmasın; o tur diğer koşularda
                Log($"v1.5 PRESTİJ ÖNCE: {Snapshot()}");
            }
            // Görüntü kare sonunda alınır: çekim ve eylem ayrı karelerde (önce çek, sonra dokun)
            string Req() => string.Join(", ", Prestige.Requirements().ConvertAll(r => (r.ok ? "✓" : "✗") + r.label));
            bool After(float s) => _real > _v15At + s;
            void Next() { _v15Step++; _v15At = _real; }
            switch (_v15Step)
            {
                case 1 when After(1.5f):
                    Log($"v1.5 PRESTİJ HEDEF: şartlar=[{Req()}] · hedef kutusu: \"{hud.DebugGoalText}\" · kare görünür={Prestige.Visible}");
                    Shot("ui_branch_goal.png"); Next(); break;
                case 2 when After(0.5f) && GoalLangsDone(hud): hud.DebugOpen("branch"); Next(); break;   // v1.8: önce hedef kutusu 10 dilde
                case 3 when After(1.2f): Shot("ui_branch_card_locked.png"); Next(); break;
                case 4 when After(0.5f):
                    hud.DebugBranchTap();
                    Log($"v1.5 PRESTİJ: şart tutmazken dokunuş → şube {Prestige.Branch} (değişmedi={Prestige.Branch == 0})");
                    hud.DebugOpen("close");
                    GameState.Data.coins = 160000;
                    Next(); break;
                case 5 when After(1.0f):
                    Log($"v1.5 PRESTİJ: şartlar=[{Req()}] · hedef kutusu: \"{hud.DebugGoalText}\"");
                    Shot("ui_branch_goal_ready.png"); Next(); break;
                case 6 when After(0.5f): hud.DebugOpen("branch"); Next(); break;
                case 7 when After(1.2f): Shot("ui_branch_card.png"); Next(); break;
                case 8 when After(0.5f):
                    hud.DebugBranchTap();
                    Log($"v1.5 PRESTİJ: 1. dokunuş → kart açık={hud.BranchCardOpen} · şube {Prestige.Branch} (henüz açılmadı)");
                    Next(); break;
                case 9 when After(0.8f): Shot("ui_branch_confirm.png"); Next(); break;
                case 10 when After(0.5f):
                    Log($"v1.5 PRESTİJ: 2. dokunuş (onay) · gelir çarpanı önce ×{Boosts.IncomeMultiplier:0.00}");
                    _v15Step = V15Waiting; _v15Visit = null; hud.DebugBranchTap(); break;
            }
            // yeni sahne
            if (_v15Step == V15Waiting && _v15Reloaded && _real > _v15ReloadReal + 1.0f)
            {
                _v15Step = V15After;
                Log($"v1.5 PRESTİJ SONRA: {Snapshot()}");
                Log($"v1.5 PRESTİJ ÇARPAN: şube {Prestige.Branch} · gelir ×{Prestige.IncomeMultiplier:0.00} (Boosts.IncomeMultiplier ×{Boosts.IncomeMultiplier:0.00}) · Ün ×{Prestige.UnMultiplier:0.00} · rehber açık={Tutorial.Active} · tutorialDone={GameState.Data.tutorialDone} · zaman ölçeği {Time.timeScale}");
                Shot("ui_branch_banner.png");
                _v15At = _real;
            }
            if (_v15Step == V15After && _real > _v15At + 4f) { _v15Step = V15After + 1; Shot("ui_branch_badge.png"); }
            if (_v15Step >= V15After && !_v15AfterLogged && _real > _v15ReloadReal + 60f)
            {
                _v15AfterLogged = true;
                Log($"v1.5 YENİ ŞUBE DEKOR: kayıtta {GameState.Data.decor.Count} [{string.Join(",", GameState.Data.decor)}] · sahnede {(TavernRegistry.I && TavernRegistry.I.decorRoot ? TavernRegistry.I.decorRoot.childCount : -1)} · salon açık={GameState.IsUnlocked("salon")}");
                Log($"v1.5 YENİ ŞUBE AKIŞ: yükleme sonrası 60 gerçek sn · servis +{GameState.Data.customersServed - _v15ServedAtReload} · Sv{GameState.Level} Ün {GameState.Data.un} · altın {GameState.Coins} · müşteri {Customer.All.Count} · rehber açık={Tutorial.Active} · açılan=[{string.Join(",", GameState.Data.unlocked)}]");
            }
        }

        static string Snapshot()
        {
            var d = GameState.Data;
            int decorPlaced = TavernRegistry.I && TavernRegistry.I.decorRoot ? TavernRegistry.I.decorRoot.childCount : -1;
            return $"altın {d.coins} · Sv{d.level} Ün {d.un} · kademe {d.tier} · gün {d.day} · mutluluk {d.happiness:0} · etkinlik günü {d.eventDay} [{string.Join(",", d.eventsUsed)}] · " +
                   $"açılan {d.unlocked.Count} [{string.Join(",", d.unlocked)}] · makine kaydı {d.stations.Count} · depo teslimat {d.deliveryLv} oto {d.autoOrder} · personel seviyesi {d.staffIds.Count} · otomasyon [{string.Join(",", d.autos)}] Usta {d.ustaGelir}/{d.ustaMusteri} · " +
                   $"KALAN → elmas {d.gems} · Hazine [{string.Join(",", d.premium)}] · VIP '{d.vipUntilUtc}' · izinli [{string.Join(",", d.benched)}] · koleksiyon {d.seenCustomers.Count} · ad '{d.tavernName}' · rehber {d.tutorialDone} · reklam (ad {d.renameAds}, görevli {d.staffAds.Count}) · " +
                   $"tema {d.floorTheme}/{d.wallTheme} · dekor kaydı {d.decor.Count} [{string.Join(",", d.decor)}] · sahnede yerleşmiş dekor {decorPlaced} · şube {d.branch}";
        }

        void FinishV15()
        {
            if (V15Mode == "content") LogV15Items();
            if (V15Prestige) Log($"v1.5 PRESTİJ ÖZET: şube {Prestige.Branch} ({Prestige.CurrentName}) · gelir ×{Prestige.IncomeMultiplier:0.00} · Ün ×{Prestige.UnMultiplier:0.00} · Sv{GameState.Level} · servis (yükleme sonrası) {GameState.Data.customersServed - _v15ServedAtReload} · sahne yüklemesi {_v15SceneLoads} · salon açık={GameState.IsUnlocked("salon")} · sahnede dekor {(TavernRegistry.I && TavernRegistry.I.decorRoot ? TavernRegistry.I.decorRoot.childCount : -1)} / kayıtta {GameState.Data.decor.Count}");
        }
    }
}
#endif
