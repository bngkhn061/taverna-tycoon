#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.1 Paket 3 test adımları (production/qa/playtests/feedback-2026-11-17.md maddeler 10, 25), "-autotest-v21 &lt;mod&gt;":
    ///   barlo  — Barlo'nun yetenekleri: dükkân › Personel kartları önce / sonra, açık seviyelerin hepsi alınır, ölçülen koşu hızı,
    ///            tepsideki en çok bardak, servis ve para toplama mesafesi günlüğe; Sv3'te bir sonraki seviyenin kartı (açılanlar)
    ///   ending — şube 2 + Sv30 + Saray Hanı (+ "-autotest-start-level 30 -autotest-tier 2"): Krala Davet hedefi ve kartı, son sahnenin
    ///            her aşaması, jenerik (dokunarak geçilir), serbest oyun (prestij kartı, Ayarlar › yeniden izle, 30+ sn oyun)
    /// </summary>
    public partial class PlaytestBot
    {
        string _v21Arg;
        string V21 => _v21Arg ??= ArgAfter("-autotest-v21");

        bool _v21Init;
        int _v21Step;
        float _v21At;
        // ölçüm (barlo)
        Vector3 _v21LastPos;
        float _v21WinT, _v21WinD, _v21MaxSpeed, _v21MaxServe, _v21MaxCollect, _v21LogT;
        int _v21MaxCarry;
        // son sahne
        string _v21Phase = "";
        float _v21PhaseAt, _v21EndStart, _v21EndFinish = -1f;
        readonly Dictionary<string, float> _v21PhaseLen = new();
        readonly HashSet<string> _v21Shots = new();
        int _v21ServedFree;

        bool V21After(float s) => _real > _v21At + s;
        void V21Next() { _v21Step++; _v21At = _real; }

        void UpdateV21(HudController hud, PlayerController p)
        {
            if (V21 == "" || !hud) return;
            if (_v20Later != null && Time.frameCount > _v20LaterFrame) { var a = _v20Later; _v20Later = null; a(); }   // Later (PlaytestBotV20)
            if (!_v21Init)
            {
                _v21Init = true;
                _uiStep = UiSteps.Length; _langStep = 4; _tierStep = 9; _premStep = 4; _fireStep = 2;   // görüntüleri örten panel turları kapalı
                _v21At = _real;
                Log($"v2.1 BAŞLADI · mod '{V21}' · Sv{GameState.Level} · şube {Prestige.Branch} · kademe {Tiers.CurrentIndex}");
            }
            if (V21 == "barlo") BarloV21(hud, p);
            else if (V21 == "ending") EndingV21(hud, p);
        }

        static string BarloState() =>
            $"hız {BarloSkills.Speed:0.00} m/sn (Sv{BarloSkills.Level(BarloSkill.Hiz)}) · taşıma {BarloSkills.Carry} (Sv{BarloSkills.Level(BarloSkill.Tasima)}) · " +
            $"servis menzili {BarloSkills.Reach:0.00} m (Sv{BarloSkills.Level(BarloSkill.Servis)}) · para menzili {BarloSkills.CoinReach:0.00} m (Sv{BarloSkills.Level(BarloSkill.Para)})";

        // ------------------------------------------------------------------ Barlo'nun yetenekleri (madde 10)
        void BarloV21(HudController hud, PlayerController p)
        {
            string lv = $"sv{GameState.Level:00}";
            switch (_v21Step)
            {
                case 0 when V21After(6f):
                    ClearUi(hud);
                    Log($"v2.1 BARLO ÖNCE: {BarloState()} · oyuncu hızı {(p ? p.speed : 0):0.00} · tepsi kapasitesi {(p && p.carrier ? p.carrier.capacity : 0)}");
                    hud.DebugOpen("shop-staff");
                    V21Next(); break;
                case 1 when V21After(1.5f):
                    Shot($"v21_barlo_shop_{lv}_before.png");
                    Log($"v2.1 BARLO DÜKKÂN: sekme={hud.DebugShopTab}");
                    V21Next(); break;
                case 2 when V21After(0.4f):
                    foreach (var s in BarloSkills.All)
                        for (int guard = 0; guard < BarloSkills.MaxLevel; guard++)
                        {
                            var can = BarloSkills.CanBuy(s);
                            if (can == Management.BuyResult.Owned || can == Management.BuyResult.LevelLow) break;
                            long price = BarloSkills.Price(s, BarloSkills.Level(s) + 1);
                            if (GameState.Coins < price) GameState.Data.coins += price;
                            long before = GameState.Coins;
                            var r = BarloSkills.Buy(s);
                            Log($"v2.1 BARLO AL: {BarloSkills.Id(s)} → Sv{BarloSkills.Level(s)} ({r}) · bedel {before - GameState.Coins} · değer {BarloSkills.Value(s, BarloSkills.Level(s)):0.00}");
                        }
                    foreach (var s in BarloSkills.All)
                        Log($"v2.1 BARLO SINIR: {BarloSkills.Id(s)} Sv{BarloSkills.Level(s)}/{BarloSkills.MaxLevel} · sıradaki → {BarloSkills.CanBuy(s)}" +
                            (BarloSkills.Level(s) < BarloSkills.MaxLevel ? $" (Sv{BarloSkills.UnlockLevel(s, BarloSkills.Level(s) + 1)}'de, {BarloSkills.Price(s, BarloSkills.Level(s) + 1)} altın)" : ""));
                    hud.DebugOpen("shop-staff");
                    V21Next(); break;
                case 3 when V21After(1.2f):
                    Shot($"v21_barlo_shop_{lv}_after.png");
                    Later(() => ClearUi(hud));   // görüntü kare sonunda alınır: dükkân bir sonraki karede kapanır
                    Log($"v2.1 BARLO SONRA: {BarloState()} · oyuncu hızı {(p ? p.speed : 0):0.00} · tepsi kapasitesi {(p && p.carrier ? p.carrier.capacity : 0)}");
                    _v21MaxSpeed = _v21MaxServe = _v21MaxCollect = 0f; _v21MaxCarry = 0;
                    V21Next(); break;
                case 4 when V21After(25f):   // ~100 oyun-sn ölçüm
                    Log($"v2.1 BARLO ETKİ (ölçülen): en yüksek koşu hızı {_v21MaxSpeed:0.00} m/sn (beklenen {BarloSkills.Speed:0.00}) · tepside en çok {_v21MaxCarry} bardak (kapasite {BarloSkills.Carry}) · " +
                        $"en uzak servis {_v21MaxServe:0.00} m (menzil {BarloSkills.Reach:0.00}) · en uzak para toplama {_v21MaxCollect:0.00} m (menzil {BarloSkills.CoinReach:0.00})");
                    if (GameState.Level < 10)
                    {
                        _levelShot = true; _levelCloseAt = float.MaxValue;   // ana bot kartı kapatmasın (adım 6 kapatır)
                        int need = Balance.UnForLevel(GameState.Level) - GameState.Data.un + 1;
                        GameState.AddUn(Mathf.Max(1, need));
                    }
                    V21Next(); break;
                case 5 when V21After(0.6f):
                    if (hud.LevelCardOpen) { Log($"v2.1 SEVİYE KARTI Sv{GameState.Level}: {hud.DebugUnlocks}"); Shot("v21_barlo_levelcard.png"); }
                    V21Next(); break;
                case 6 when V21After(1.5f):
                    ClearUi(hud);
                    hud.DebugOpen("shop-staff");
                    V21Next(); break;
                case 7 when V21After(1.2f):
                    Shot($"v21_barlo_shop_sv{GameState.Level:00}_new.png");
                    Log($"v2.1 YENİ! rozetleri: personel sekmesi açıldı · görülen anahtarlar {GameState.Data.seenItems.FindAll(k => k.StartsWith("barlo_")).Count} [{string.Join(",", GameState.Data.seenItems.FindAll(k => k.StartsWith("barlo_")))}]");
                    Later(() => ClearUi(hud));
                    V21Next(); break;
            }
            // ölçüm: 0.5 sn pencerelerde yatay hız; tepsi; servis ve toplama mesafesi
            if (p && _v21Step >= 4)
            {
                var pos = p.transform.position; pos.y = 0f;
                // ışınlanma (merdiven, kat) ve takılınca yana itilme ölçümü bozmasın: karedeki adım hızın 1.5 katını geçerse pencere atılır
                float stepD = _v21LastPos != Vector3.zero ? Vector3.Distance(pos, _v21LastPos) : 0f;
                if (stepD > BarloSkills.Speed * Mathf.Max(Time.deltaTime, 0.001f) * 1.5f) { _v21WinT = 0f; _v21WinD = 0f; stepD = 0f; }
                _v21WinD += stepD;
                _v21LastPos = pos;
                _v21WinT += Time.deltaTime;
                if (_v21WinT >= 1.0f) { _v21MaxSpeed = Mathf.Max(_v21MaxSpeed, _v21WinD / _v21WinT); _v21WinT = 0f; _v21WinD = 0f; }
                if (p.carrier) _v21MaxCarry = Mathf.Max(_v21MaxCarry, p.carrier.Count);
                _v21MaxServe = Mathf.Max(_v21MaxServe, Customer.DebugLastServeDist);
                _v21MaxCollect = Mathf.Max(_v21MaxCollect, CoinPile.DebugLastCollectDist);
            }
        }

        // ------------------------------------------------------------------ gerçek son (madde 25)
        void EndingV21(HudController hud, PlayerController p)
        {
            switch (_v21Step)
            {
                case 0 when V21After(3f):
                    GameState.Data.branch = Ending.RequiredBranch;
                    foreach (var z in new List<PayZone>(PayZone.All)) z.DebugUnlock();
                    foreach (var m in Catalog.Machines) if (!Management.OwnsMachine(m.drink)) { GameState.Data.unlocked.Add(Catalog.MachineKey(m.drink)); TavernRegistry.I.ApplyMachine(m.drink, false); }
                    GameState.Data.coins += 200000;
                    foreach (var id in new[] { "staff_pip", "staff_gus", "staff_ayla" }) Management.Hire(System.Array.Find(Catalog.StaffList, s => s.id == id));
                    GameState.Data.coins = 200000;
                    GameState.Data.playSeconds = 18 * 3600f;   // gerçekçi ölçüm değeri (test kaydı yeni)
                    GameState.Data.daysTotal = 140;
                    ClearUi(hud);
                    _v15Visit = new Vector3(2.4f, 0f, 1.2f);   // Barlo büyüt karesinden uzakta beklesin (kare kartı yeniden açar)
                    Log($"v2.1 SON ÖNCE: şube {Prestige.Branch} ({Prestige.CurrentName}) · Sv{GameState.Level} · kademe {Tiers.Current.name} · son açık={Ending.Available} görüldü={Ending.Seen} · prestij kartı açık={Prestige.CanPrestige}");
                    V21Next(); break;
                case 1 when V21After(4.5f):
                    Log($"v2.1 SON HEDEF: \"{hud.DebugGoalText}\"");
                    Shot("v21_end_goal.png"); V21Next(); break;
                case 2 when V21After(0.4f): hud.DebugOpen("ending"); V21Next(); break;
                case 3 when V21After(1.2f):
                    Log($"v2.1 SON KART: açık={hud.EndingCardOpen}");
                    Shot("v21_end_card.png"); V21Next(); break;
                case 4 when V21After(0.4f):
                    _v15Visit = null;
                    _v21EndStart = _real;
                    hud.DebugEndingConfirm();
                    Log($"v2.1 SON BAŞLADI: oynuyor={Ending.Playing} · görüldü={Ending.Seen} · müzik zorla={Sfx.ForceFestive} · zaman ölçeği {Time.timeScale} · game_complete {Count("game_complete")}");
                    V21Next(); break;
            }
            // aşamalar
            string ph = hud.EndingPhase;
            if (_v21Step >= 5 && ph != _v21Phase)
            {
                if (_v21Phase != "") _v21PhaseLen[_v21Phase] = _real - _v21PhaseAt;
                Log($"v2.1 SON AŞAMA: {(_v21Phase == "" ? "-" : _v21Phase)} → {(ph == "" ? "BİTTİ" : ph)} · {_real - _v21EndStart:0.0} sn · {hud.EndingDebug} · müzik {Sfx.MusicNow} · gün {DayCycle.Progress01:0.000} · müşteri {Customer.All.Count}");
                _v21Phase = ph; _v21PhaseAt = _real;
                if (ph == "" && _v21EndFinish < 0f)
                {
                    _v21EndFinish = _real;
                    var parts = new List<string>();
                    foreach (var kv in _v21PhaseLen) parts.Add($"{kv.Key} {kv.Value:0.0}");
                    Log($"v2.1 SON SÜRE: toplam {_v21EndFinish - _v21EndStart:0.0} sn [{string.Join(" · ", parts)}] · görüldü={Ending.Seen} · oynuyor={Ending.Playing} · zaman ölçeği {Time.timeScale} · müzik zorla={Sfx.ForceFestive}");
                    _v21ServedFree = GameState.Data.customersServed;
                }
            }
            if (_v21Step >= 5 && ph != "")
            {
                float at = _real - _v21PhaseAt;
                void ShotOnce(string file, float after) { if (at > after && _v21Shots.Add(file)) Shot(file); }
                switch (ph)
                {
                    case "Arrive": ShotOnce("v21_end_1_arrive.png", 3.5f); break;
                    case "Greet": ShotOnce("v21_end_2_greet.png", 1.4f); break;
                    case "ToTable": ShotOnce("v21_end_3_totable.png", 1.0f); break;
                    case "Toast": ShotOnce("v21_end_4_toast.png", 1.8f); break;
                    case "Orbit": ShotOnce("v21_end_5_orbit.png", 1.6f); ShotOnce("v21_end_6_orbit_late.png", 6.5f); break;
                    case "credits":
                        ShotOnce("v21_end_7_credits.png", 2.5f); ShotOnce("v21_end_8_credits_late.png", 11.5f);
                        if (at > 12.3f && V25 != "rec-ending" && _v21Shots.Add("tap")) { Log("v2.1 JENERİK: dokunuşla geçiliyor"); hud.DebugEndingTap(); }   // v2.5 video: jenerik sonuna kadar
                        break;
                }
            }
            // serbest oyun
            if (_v21EndFinish > 0f)
            {
                float f = _real - _v21EndFinish;
                if (f < 2.5f) _v15Visit = new Vector3(2.4f, 0f, 1.2f); else if (_v21Shots.Add("freego")) _v15Visit = null;   // görüntü için büyüt karesinden uzak
                if (f > 1.0f && _v21Shots.Add("free")) Shot("v21_end_9_free.png");
                if (f > 3.0f && _v21Shots.Add("freelog"))
                {
                    Log($"v2.1 SERBEST OYUN: son açık={Ending.Available} · görüldü={Ending.Seen} · prestij görünür={Prestige.Visible} hazır={Prestige.CanPrestige} · hedef \"{hud.DebugGoalText}\" · kayıt endingSeen={GameState.Data.endingSeen} · game_complete {Count("game_complete")}");
                    hud.DebugOpen("branch");
                }
                if (f > 4.4f && _v21Shots.Add("branch")) { Shot("v21_end_branch_after.png"); Log($"v2.1 SERBEST: şube kartı açık={hud.BranchCardOpen}"); }
                if (f > 5.0f && _v21Shots.Add("settings")) { ClearUi(hud); hud.DebugOpen("settings"); }
                if (f > 6.2f && _v21Shots.Add("settingsshot")) Shot("v21_end_settings.png");
                if (f > 7.0f && _v21Shots.Add("settingsclose")) hud.DebugCloseSettings();
                if (f > 8.0f && _v21Shots.Add("shop")) hud.DebugOpen("shop-staff");
                if (f > 9.2f && _v21Shots.Add("shopshot")) Shot("v21_end_shop_staff.png");
                if (f > 10.0f && _v21Shots.Add("shopclose")) ClearUi(hud);
                if (f > 38f && _v21Shots.Add("free30"))
                    Log($"v2.1 SERBEST 30+ sn: servis +{GameState.Data.customersServed - _v21ServedFree} · müşteri {Customer.All.Count} · gün {DayCycle.Progress01:0.000} · altın {GameState.Coins} · zaman ölçeği {Time.timeScale}");
            }
        }
    }
}
#endif
