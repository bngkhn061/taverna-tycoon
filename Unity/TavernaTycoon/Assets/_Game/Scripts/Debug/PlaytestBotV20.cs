#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.0 Paket 2 test adımları (production/qa/playtests/feedback-2026-11-17.md maddeler 14, 15, 17–21), "-autotest-v20 &lt;mod&gt;":
    ///   tiers  — kademe görünümü: Meyhane ve Salon, gündüz ve gece (masa avizesi yok, sütun aplikleri, küçük avizeler)
    ///   garden — bahçe: uzun masa, sahnede çalan ozan, taş yol ve bahçe fenerleri; gece görüntüsü
    ///   inn    — han: 8 oda, konfor seviyeleri 0–4 yan yana, dükkândaki "Han odaları" kartı, konfor yükseltme
    ///   game   — oyun salonu: 5 masa, her masada oynanan tur sayısı
    ///   dayend — gün sonu: son siparişler (yeni müşteri yok), kapanış (müşteri ve personel çıkar), boş plan ekranı, sabah personel girişi
    /// Bot görüntü alırken Barlo'yu sabit bir noktaya ışınlar ve orada tutar (_v20Hold).
    /// </summary>
    public partial class PlaytestBot
    {
        string _v20Arg;
        string V20 => _v20Arg ??= ArgAfter("-autotest-v20");

        Vector3? _v20Hold;
        bool _v20Init;
        int _v20Step;
        float _v20At;
        readonly Dictionary<string, int> _v20Rounds = new();
        readonly Dictionary<GameTable, GameTable.State> _v20GameState = new();
        float _v20LogT;
        // gün sonu
        bool _v20LastCallSeen, _v20ClosingSeen, _v20EndedSeen, _v20EndedLogged, _v20MorningSeen;
        int _v20SpawnsAfterLastCall, _v20CustAtLastCall, _v20AngryAtClose;
        long _v20CoinsAtClose;
        float _v20MorningAt;
        readonly HashSet<Customer> _v20Known = new();

        void UpdateV20(HudController hud, PlayerController p)
        {
            if (V20 == "") return;
            if (_v20Later != null && Time.frameCount > _v20LaterFrame) { var a = _v20Later; _v20Later = null; a(); }
            if (!_v20Init)
            {
                _v20Init = true;
                DayCycle.DayEnded += _ => _v20EndedSeen = true;   // plan ekranında bot Update'i erken döner: olayla yakala
                _uiStep = UiSteps.Length; _langStep = 4; _tierStep = 9; _premStep = 4; _fireStep = 2;   // görüntüleri örten panel turları kapalı
                Log($"v2.0 BAŞLADI · mod '{V20}' · Sv{GameState.Level} · kademe {Tiers.CurrentIndex} · gün uzunluğu {Balance.DayLength} sn · son sipariş %{Balance.LastCallPhase * 100f:0}");
                if (V20 == "tiers") Premium.Grant("dec_kristal");
                if (V20 == "inn")
                {
                    int[] lv = { 0, 1, 2, 3, 4, 4, 2, 0 };
                    var l = GameState.Data.roomLvs;
                    while (l.Count < Inn.RoomCount) l.Add(0);
                    for (int i = 0; i < lv.Length; i++) l[i] = lv[i];
                }
                if (V20 == "garden")
                    foreach (var id in new[] { "dec_tasyol", "dec_tasyol", "dec_tasyol", "dec_tasyol", "dec_bahcefener", "dec_bahcefener", "dec_bahcefener" })
                    {
                        var d = Catalog.FindDecor(id);
                        GameState.Data.coins += d.price;
                        var r = Management.BuyDecor(d);
                        Log($"v2.0 BAHÇE DEKOR: {id} → {r} · ambiyans {AmbianceSource.Total:0}");
                    }
                _v20At = _real;
            }
            switch (V20)
            {
                case "tiers": TiersV20(hud, p); break;
                case "garden": GardenV20(hud, p); break;
                case "inn": InnV20(hud, p); break;
                case "game": GameV20(hud, p); break;
                case "dayend": DayEndV20(hud); break;
            }
        }

        bool V20After(float s) => _real > _v20At + s;

        /// <summary>Ekran görüntüsü kare sonunda alınır: görüntüden sonraki işler (ışınlama, gece, panel) bir sonraki kareye kalır.</summary>
        System.Action _v20Later;
        int _v20LaterFrame;
        void Later(System.Action a) { _v20Later = a; _v20LaterFrame = Time.frameCount; }
        void V20Next() { _v20Step++; _v20At = _real; }

        /// <summary>Barlo'yu ışınlar ve orada tutar; kamera hemen yerine oturur.</summary>
        void Hold(PlayerController p, Vector3 pos)
        {
            _v20Hold = pos;
            if (!p) return;
            var cc = p.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            p.transform.position = pos;
            if (cc) cc.enabled = true;
            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam) cam.SnapNow();
        }

        /// <summary>Görüntü: önce ayarlar / teklif penceresi kapanır (aynı karede çizilir).</summary>
        void ShotV20(string file)
        {
            var hud = _hud ? _hud : (_hud = FindAnyObjectByType<HudController>());
            if (hud) { hud.DebugCloseSettings(); if (hud.DebugModalOpen) hud.DebugClosePremModal(); }
            Shot(file);
        }

        void ClearUi(HudController hud)
        {
            if (!hud) return;
            if (hud.DebugModalOpen) hud.DebugClosePremModal();
            hud.DebugCloseSettings();
            hud.DebugOpen("close");
        }

        void NightNow()
        {
            DayCycle.DebugSetProgress(V20NightPhase);
            foreach (var l in Lantern.All) if (l && l.ZoneOpen && !l.lit) l.Ignite();
        }
        const float V20NightPhase = 0.8f;

        // ------------------------------------------------------------------ kademe (madde 14)
        void TiersV20(HudController hud, PlayerController p)
        {
            switch (_v20Step)
            {
                case 0 when V20After(10f): ClearUi(hud); Hold(p, new Vector3(0.5f, 0f, -1.2f)); V20Next(); break;
                case 1 when V20After(1.5f): ShotV20($"v20_tier{Tiers.CurrentIndex}_meyhane_gunduz.png"); Later(() => Hold(p, new Vector3(9.0f, 0f, -0.8f))); V20Next(); break;
                case 2 when V20After(1.5f): ShotV20($"v20_tier{Tiers.CurrentIndex}_salon_gunduz.png"); Later(NightNow); V20Next(); break;
                case 3 when V20After(3f): NightNow(); ClearUi(hud); Hold(p, new Vector3(0.5f, 0f, -1.2f)); V20Next(); break;
                case 4 when V20After(1.5f):
                    ShotV20($"v20_tier{Tiers.CurrentIndex}_meyhane_gece.png");
                    Log($"v2.0 KADEME {Tiers.CurrentIndex}: masa avizesi (PR_Tier_*Avize) sahnede {CountNamed("PR_Tier_DemirAvize") + CountNamed("PR_Tier_SarayAvize")} · aplik ışığı {CountLights("Aplik")} · satın alınan avize {Management.DecorOwned("dec_avize")} · kristal {Management.DecorOwned("dec_kristal")}");
                    Later(() => Hold(p, new Vector3(9.0f, 0f, -0.8f))); V20Next(); break;
                case 5 when V20After(1.5f): ShotV20($"v20_tier{Tiers.CurrentIndex}_salon_gece.png"); _v20Hold = null; V20Next(); break;
            }
        }

        static int CountNamed(string prefix)
        {
            int n = 0;
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (t.name.StartsWith(prefix) && t.gameObject.activeInHierarchy) n++;
            return n;
        }

        static int CountLights(string parentContains)
        {
            int n = 0;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.isActiveAndEnabled && l.transform.parent && l.transform.parent.name.Contains(parentContains)) n++;
            return n;
        }

        // ------------------------------------------------------------------ bahçe (madde 17, 18, 21)
        void GardenV20(HudController hud, PlayerController p)
        {
            var bard = FindAnyObjectByType<StaffIdle>();
            switch (_v20Step)
            {
                case 0 when V20After(14f):
                    ClearUi(hud); Hold(p, new Vector3(-9.6f, 0f, -1.6f));
                    V20Next(); break;
                case 1 when V20After(1.5f):
                    ShotV20("v20_garden_day.png");
                    Log($"v2.0 BAHÇE: uzun masa açık={GameState.IsUnlocked("masaBU")} oturan={SeatedAt("Table_BahceUzun")}/6 · ozan sahnede={(bard && bard.OnStage)} konum={(bard ? bard.transform.position.ToString("F2") : "-")} · taş yol {Management.DecorOwned("dec_tasyol")} · fener {Management.DecorOwned("dec_bahcefener")}");
                    Later(NightNow); V20Next(); break;
                case 2 when V20After(3f): NightNow(); ClearUi(hud); Hold(p, new Vector3(-9.6f, 0f, -1.6f)); V20Next(); break;
                case 3 when V20After(1.5f):
                    ShotV20("v20_garden_night.png");
                    Log($"v2.0 BAHÇE GECE: bahçe fener ışığı {CountGardenLamps()} (yanık) · bölge fenerleri yanan {Night.LanternCount().lit}/{Night.LanternCount().total} · ozan sahnede={(bard && bard.OnStage)}");
                    Later(() => Hold(p, new Vector3(-8.8f, 0f, 2.2f))); V20Next(); break;
                case 4 when V20After(1.5f): ShotV20("v20_garden_stage.png"); _v20Hold = null; V20Next(); break;
            }
            if (_v20Step >= 5) RoundLog();
        }

        static int CountGardenLamps()
        {
            int n = 0;
            foreach (var l in FindObjectsByType<NightLight>(FindObjectsSortMode.None))
                if (l.transform.parent && l.transform.parent.name.StartsWith("PR_Decor_BahceFeneri") && l.GetComponent<Light>().intensity > 0.5f) n++;
            return n;
        }

        static string SeatedAt(string tableName)
        {
            foreach (var t in Table.All)
                if (t && t.name == tableName)
                {
                    int n = 0;
                    foreach (var s in t.seats) if (s && s.occupant) n++;
                    return n.ToString();
                }
            return "-";
        }

        // ------------------------------------------------------------------ han (madde 19, 20)
        void InnV20(HudController hud, PlayerController p)
        {
            switch (_v20Step)
            {
                case 0 when V20After(6f):
                    ClearUi(hud); Hold(p, new Vector3(-1.0f, 0f, 38.3f));
                    var sb = new System.Text.StringBuilder();
                    foreach (var r in Room.All)
                    {
                        int on = 0;
                        foreach (var it in r.items) if (it && it.activeSelf) on++;
                        sb.Append($" Oda{r.index + 1}: konfor {Inn.Comfort(r.index)} gecelik {r.Fee} eşya {on}/{r.items.Length} x={r.bedPoint.position.x:0.0};");
                    }
                    Log($"v2.0 HAN: oda {Room.All.Count}/{Inn.RoomCount} · geliş çarpanı ×{Inn.ArrivalMultiplier:0.00} (aralık {Inn.GuestSpawnEvery / Inn.ArrivalMultiplier:0.0} sn) ·{sb}");
                    V20Next(); break;
                case 1 when V20After(1.5f): ShotV20("v20_inn_rooms_right.png"); Later(() => Hold(p, new Vector3(-6.0f, 0f, 38.3f))); V20Next(); break;
                case 2 when V20After(1.5f):
                    ShotV20("v20_inn_rooms_left.png");
                    V20Next(); break;
                case 3 when V20After(0.3f):
                    int room = 7;
                    long need = Inn.UpgradeCost(room);
                    GameState.Data.coins += need;
                    int before = Inn.Comfort(room), feeBefore = Inn.Fee(room);
                    bool ok = Inn.UpgradeComfort(room);
                    Log($"v2.0 HAN KONFOR: Oda {room + 1} {before} → {Inn.Comfort(room)} ({ok}) · bedel {need} · gecelik {feeBefore} → {Inn.Fee(room)} · geliş ×{Inn.ArrivalMultiplier:0.00}");
                    V20Next(); break;
                case 4 when V20After(1.5f): ShotV20("v20_inn_upgrade.png"); Later(NightNow); V20Next(); break;
                case 5 when V20After(3f): ClearUi(hud); Hold(p, new Vector3(-1.0f, 0f, 38.3f)); V20Next(); break;
                case 6 when V20After(1.5f): ShotV20("v20_inn_night.png"); Later(() => { Hold(p, new Vector3(12.0f, 0f, 1.0f)); hud.DebugOpen("shop-dec"); }); V20Next(); break;
                case 7 when V20After(1.5f): ShotV20("v20_inn_shop.png"); Later(() => ClearUi(hud)); _v20Hold = null; V20Next(); break;
            }
        }

        // ------------------------------------------------------------------ oyun salonu (madde 20)
        void GameV20(HudController hud, PlayerController p)
        {
            foreach (var g in GameTable.All)
            {
                if (!g) continue;
                _v20GameState.TryGetValue(g, out var prev);
                if (g.state == GameTable.State.Playing && prev != GameTable.State.Playing)
                    _v20Rounds[g.name] = (_v20Rounds.TryGetValue(g.name, out var n) ? n : 0) + 1;
                _v20GameState[g] = g.state;
            }
            switch (_v20Step)
            {
                case 0 when V20After(60f): ClearUi(hud); Hold(p, new Vector3(9.5f, 0f, 6.0f)); V20Next(); break;
                case 1 when V20After(1.5f): ShotV20("v20_gameroom.png"); _v20Hold = null; V20Next(); break;
            }
            RoundLog();
        }

        void RoundLog()
        {
            _v20LogT += Time.unscaledDeltaTime;
            if (_v20LogT < 10f) return;
            _v20LogT = 0f;
            var sb = new System.Text.StringBuilder();
            foreach (var g in GameTable.All) if (g) sb.Append($" {g.name}: {g.state} oturan {SeatedAt(g.name)} tur {(_v20Rounds.TryGetValue(g.name, out var n) ? n : 0)};");
            if (sb.Length > 0) Log($"v2.0 OYUN SALONU: masa {GameTable.All.Count} ·{sb}");
            Log($"v2.0 BAHÇE UZUN MASA: oturan {SeatedAt("Table_BahceUzun")}/6");
        }

        // ------------------------------------------------------------------ gün sonu (madde 15)
        void DayEndV20(HudController hud)
        {
            if (_v20Step == 0 && V20After(10f)) { DayCycle.DebugSetProgress(0.86f); Log("v2.0 GÜN SONU: gün %86'ya atlatıldı"); V20Next(); }
            foreach (var c in Customer.All)
                if (c && _v20Known.Add(c) && DayCycle.LastCall && _v20LastCallSeen) _v20SpawnsAfterLastCall++;
            if (!_v20LastCallSeen && DayCycle.LastCall && !DayCycle.Ended)
            {
                _v20LastCallSeen = true;
                _v20CustAtLastCall = Customer.All.Count;
                Log($"v2.0 SON SİPARİŞ başladı · faz {DayCycle.Progress01:0.000} · içeride {Customer.All.Count} müşteri ({States()})");
                ShotV20("v20_lastcall.png");
            }
            if (!_v20ClosingSeen && DayCycle.Closing)
            {
                _v20ClosingSeen = true;
                _v20CoinsAtClose = GameState.Coins;
                _v20AngryAtClose = _angryLeaves;
                Log($"v2.0 KAPANIŞ başladı · içeride {Customer.All.Count} müşteri ({States()}) · son siparişten sonra gelen yeni müşteri {_v20SpawnsAfterLastCall} · yürüyen görevli {StaffCommute.WalkingCount} · masadaki para yığını {CoinPile.All.Count}");
                hud.StartCoroutine(After(0.5f, () => ShotV20("v20_closing.png")));
            }
            if (DayCycle.Closing)
            {
                _v20LogT += Time.unscaledDeltaTime;
                if (_v20LogT > 0.5f)
                {
                    _v20LogT = 0f;
                    var inside = new System.Text.StringBuilder();
                    foreach (var c in Customer.All) if (c && c.transform.position.z > -5.6f) inside.Append($" ({c.transform.position.x:0.0},{c.transform.position.z:0.0})");
                    Log($"v2.0 KAPANIŞ: müşteri {Customer.All.Count} ({States()}) · içeride:{inside} · yürüyen görevli {StaffCommute.WalkingCount} · evde {StaffCommute.AwayCount}");
                }
            }
            if (_v20EndedSeen && !_v20EndedLogged)
            {
                _v20EndedLogged = true;
                hud.StartCoroutine(After(0.15f, () =>
                {
                    Log($"v2.0 PLAN EKRANI: tavernada müşteri {Customer.All.Count} · sahnede görevli {ActiveStaff()} · evde {StaffCommute.AwayCount} · kapanışta kızgın ayrılan {_angryLeaves - _v20AngryAtClose} · altın {_v20CoinsAtClose} → {GameState.Coins} (maaş düştü)");
                    if (hud.DebugModalOpen) hud.DebugClosePremModal();
                    ShotV20("v20_dayend_plan.png");
                }));
            }
            if (_v20EndedSeen && !_v20MorningSeen && !DayCycle.Ended)
            {
                _v20MorningSeen = true;
                _v20MorningAt = _real;
                Log($"v2.0 SABAH: yürüyen görevli {StaffCommute.WalkingCount} · evde {StaffCommute.AwayCount} · sahnede {ActiveStaff()}");
            }
            if (_v20MorningAt > 0f && _real > _v20MorningAt + 2.0f) { _v20MorningAt = -_v20MorningAt; ShotV20("v20_morning_arrive.png"); }
            if (_v20MorningAt < 0f && _real > -_v20MorningAt + 6f)
            {
                _v20MorningAt = 0f;
                int off = 0;
                foreach (var e in TavernRegistry.I.staff)
                    if (e.root && e.root.activeInHierarchy)
                        foreach (var b in e.root.GetComponents<Behaviour>())
                            if (!b.enabled && (b is Waiter || b is StaffWork || b is Cleaner || b is Lamplighter || b is Housekeeper || b is StaffIdle)) off++;
                Log($"v2.0 SABAH +6 sn: sahnede görevli {ActiveStaff()} · yürüyen {StaffCommute.WalkingCount} · işi kapalı kalan {off} · garson {Waiter.All.Count} · müşteri {Customer.All.Count}");
            }
        }

        static string States()
        {
            int w = 0, wi = 0, d = 0, l = 0;
            foreach (var c in Customer.All)
                switch (c.state)
                {
                    case Customer.State.Waiting: w++; break;
                    case Customer.State.WalkingIn: wi++; break;
                    case Customer.State.Drinking: d++; break;
                    default: l++; break;
                }
            return $"geliyor {wi} · bekliyor {w} · içiyor {d} · çıkıyor {l}";
        }

        static string ActiveStaff()
        {
            var sb = new System.Text.StringBuilder();
            int n = 0;
            foreach (var e in TavernRegistry.I.staff)
                if (e.root && e.root.activeInHierarchy) { n++; sb.Append(e.id.Replace("staff_", "")).Append(' '); }
            return $"{n} [{sb.ToString().Trim()}]";
        }
    }
}
#endif
