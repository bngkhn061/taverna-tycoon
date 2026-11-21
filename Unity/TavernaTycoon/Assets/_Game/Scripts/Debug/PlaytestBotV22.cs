#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.2 Paket 4 test adımları (sezonlar ve kostümler), "-autotest-v22 &lt;mod&gt;":
    ///   season    — "-autotest-season harvest|winter|spring" ile: rozet ve duyuru, tezgâh + süsleme, sezon müşterisi sezon içeceğini
    ///               istiyor ve servis ediliyor, jeton toplanıyor, Sezon sekmesi, kalıcı dekor ve sezon kostümü jetonla alınıyor,
    ///               görev ödülü, sonra sezon bitişi (jetonlar sona erer, dekor kalır)
    ///   offseason — "-autotest-season none": hiçbir sezon öğesi yok (tezgâh, süsleme, rozet, sekme, sezon müşterisi)
    ///   wardrobe  — Gardırop: önizleme, Barlo ve personel kostümleri sahnede, ödül kostümü, giy / çıkar; yayın taklidinde
    ///               ("-autotest-release-sim") Hazine kostümleri "Yakında"
    /// </summary>
    public partial class PlaytestBot
    {
        string _v22Arg;
        string V22 => _v22Arg ??= ArgAfter("-autotest-v22");

        bool _v22Init;
        int _v22Step;
        float _v22At;
        readonly HashSet<Customer> _v22SeasonGuests = new();
        int _v22SeasonOrders, _v22GuestSeasonOrders;
        readonly HashSet<Customer> _v22OrderSeen = new();
        int _v22Tokens0, _v22Served0;
        string _v22Season = "";

        bool V22After(float s) => _real > _v22At + s;
        void V22Next() { _v22Step++; _v22At = _real; }
        string SeasonTag => Seasons.Active ? Seasons.Current.key : "none";
        string LangTag => Loc.Lang == "tr" ? "" : "-" + Loc.Lang;

        void UpdateV22(HudController hud, PlayerController p)
        {
            if (V22 == "" || !hud) return;
            if (_v20Later != null && Time.frameCount > _v20LaterFrame) { var a = _v20Later; _v20Later = null; a(); }   // Later (PlaytestBotV20)
            if (!_v22Init)
            {
                _v22Init = true;
                _uiStep = UiSteps.Length; _langStep = 4; _tierStep = 9; _premStep = 4; _fireStep = 2;   // görüntüleri örten panel turları kapalı
                _v22At = _real;
                Log($"v2.2 BAŞLADI · mod '{V22}' · tarih {Seasons.Now:yyyy-MM-dd} · sezon {SeasonTag} ({Seasons.KeyAt(Seasons.Now)}) · kalan {Seasons.DaysLeftNow} gün · " +
                    $"oynanabilir={Seasons.Playable} · Sv{GameState.Level} · dil {Loc.Lang} · mağaza açık={Premium.Available}");
            }
            CountSeasonOrders();
            if (V22 == "season") SeasonV22(hud, p);
            else if (V22 == "offseason") OffSeasonV22(hud, p);
            else if (V22 == "wardrobe") WardrobeV22(hud, p);
        }

        void CountSeasonOrders()
        {
            foreach (var c in Customer.All)
            {
                if (!c || !c.def) continue;
                if (c.def.seasonal) _v22SeasonGuests.Add(c);
                if (c.state == Customer.State.Waiting && _v22OrderSeen.Add(c) && Seasons.IsSeasonDrink(c.wanted))
                {
                    _v22SeasonOrders++;
                    if (c.def.seasonal) _v22GuestSeasonOrders++;
                }
                if (c.state != Customer.State.Waiting) _v22OrderSeen.Remove(c);
            }
        }

        static string SceneState()
        {
            var scen = Object.FindAnyObjectByType<SeasonScenery>();
            var sets = new List<string>();
            var stalls = new List<string>();
            if (scen)
            {
                foreach (var s in scen.decorSets) if (s && s.activeSelf) sets.Add(s.name);
                foreach (var s in scen.stalls) if (s && s.activeSelf) stalls.Add(s.name);
            }
            var seasonStations = DrinkStation.All.FindAll(s => s && s.Seasonal).Count;
            return $"süsleme [{string.Join(",", sets)}] · tezgâh [{string.Join(",", stalls)}] · sezon istasyonu {seasonStations}";
        }

        string QuestState() => string.Join(" | ", Seasons.QuestList.ConvertAll(q => $"{Seasons.QuestText(q)} {q.progress}/{Seasons.Target(q)}{(q.claimed ? " ✓" : "")}"));

        static DrinkStation SeasonStation() => DrinkStation.All.Find(s => s && s.Seasonal);

        // ------------------------------------------------------------------ sezon
        void SeasonV22(HudController hud, PlayerController p)
        {
            string tag = SeasonTag + LangTag;
            switch (_v22Step)
            {
                case 0 when V22After(1.4f):
                    Log($"v2.2 SEZON BAŞI: {SceneState()} · rozet görünür={hud.SeasonButtonVisible} '{hud.SeasonButtonText}' · jeton {Seasons.Tokens} · " +
                        $"görülen anahtar '{GameState.Data.seasonSeenKey}' · season_start {Count("season_start")}");
                    Shot($"v22_{tag}_banner.png");
                    V22Next(); break;
                case 1 when V22After(2.5f):
                {
                    ClearUi(hud);
                    var st = SeasonStation();
                    if (st) Later(() => Hold(p, st.ServePos + new Vector3(-0.8f, 0f, 2.4f)));
                    Log($"v2.2 TEZGÂH: {(st ? $"{st.drink.displayName} @ ({st.transform.position.x:0.00},{st.transform.position.z:0.00}) servis ({st.ServePos.x:0.00},{st.ServePos.z:0.00}) · stok {st.Stock}/{st.MaxStock} · fiyat {st.Price} · malzeme bitti={st.OutOfSupply}" : "YOK")}");
                    V22Next(); break;
                }
                case 2 when V22After(1.5f):
                    Shot($"v22_{tag}_stall.png");
                    Later(() => Hold(p, new Vector3(-2.2f, 0f, -3.4f)));
                    V22Next(); break;
                case 3 when V22After(1.5f):
                    Shot($"v22_{tag}_decor_front.png");
                    _v20Hold = null;   // oyna, sezon içeceği servis et
                    _v22Tokens0 = Seasons.Tokens; _v22Served0 = GameState.Data.customersServed;
                    V22Next(); break;
                case 4 when V22After(28f):   // ~110 oyun-sn servis
                {
                    Log($"v2.2 SEZON OYUN: servis +{GameState.Data.customersServed - _v22Served0} · sezon müşterisi gelen {_v22SeasonGuests.Count} · sezon içeceği siparişi {_v22SeasonOrders} " +
                        $"(sezon müşterisinden {_v22GuestSeasonOrders}) · jeton {_v22Tokens0} → {Seasons.Tokens} · season_token {Count("season_token")} · görevler [{QuestState()}]");
                    foreach (var c in Customer.All)
                        if (c && c.def && c.def.seasonal) { Log($"v2.2 SEZON MÜŞTERİSİ: {c.def.displayName} · durum {c.state} · istediği {c.wanted}"); break; }
                    // görüntü: sezon müşterisi varsa yanına
                    var g = Customer.All.Find(c => c && c.def && c.def.seasonal);
                    if (g) Later(() => Hold(p, g.transform.position + new Vector3(1.4f, 0f, -1.2f)));
                    V22Next(); break;
                }
                case 5 when V22After(1.5f):
                    Shot($"v22_{tag}_guest.png");
                    _v20Hold = null;
                    // jetonla alım için yeterli jeton (test; gerçek kazanılan yukarıda günlükte)
                    {
                        var s = Seasons.Current;
                        int need = Seasons.DecorPrice(s.decor[0]) + Costumes.Find(s.costume).tokens;
                        if (Seasons.Tokens < need) { Log($"v2.2 TEST: alım için +{need - Seasons.Tokens} jeton eklendi"); Seasons.AddTokens(need - Seasons.Tokens, "test"); }
                        // bir görevi tamamlayıp ödülünü al
                        var q = Seasons.QuestList[0];
                        q.progress = Seasons.Target(q);
                        int before = Seasons.Tokens;
                        bool ok = Seasons.Claim(0);
                        Log($"v2.2 GÖREV ÖDÜLÜ: {ok} · jeton {before} → {Seasons.Tokens}");
                        float amb0 = AmbianceSource.Total;
                        var r1 = Seasons.BuyDecor(s.decor[0]);
                        Log($"v2.2 DEKOR AL: {s.decor[0]} → {r1} · jeton {Seasons.Tokens} · ambiyans {amb0:0} → {AmbianceSource.Total:0} (yerleşim sonraki karede) · season_purchase {Count("season_purchase")}");
                        var r2 = Costumes.BuyWithTokens(s.costume);
                        Log($"v2.2 KOSTÜM AL: {s.costume} → {r2} · jeton {Seasons.Tokens} · {Costumes.DebugInfo} · costume_purchase {Count("costume_purchase")} · costume_equip {Count("costume_equip")}");
                    }
                    Later(() => hud.DebugOpen("season"));
                    V22Next(); break;
                case 6 when V22After(1.2f):
                    Log($"v2.2 SEZON SEKMESİ: açık sekme '{hud.DebugEventsTab}'");
                    Shot($"v22_{tag}_tab.png");
                    V22Next(); break;
                case 7 when V22After(0.3f):
                    Log($"v2.2 SEZON SEKMESİ KAYDIR: {hud.DebugEventsScroll(1000f)}");
                    V22Next(); break;
                case 8 when V22After(0.8f):
                    Shot($"v22_{tag}_tab_bottom.png");
                    Later(() =>
                    {
                        ClearUi(hud);
                        var slot = System.Array.FindIndex(TavernRegistry.I.decorSlots, sl => sl.item == Seasons.Current.decor[0]);
                        var pos = slot >= 0 ? TavernRegistry.I.decorSlots[slot].pos : Vector3.zero;
                        Hold(p, new Vector3(pos.x - 0.6f, 0f, -4.3f));
                    });
                    V22Next(); break;
                case 9 when V22After(1.6f):
                    Shot($"v22_{tag}_decor_bought.png");
                    Later(() => Hold(p, Seasons.Current.costume == "nana_bahar" ? Nana(p) : new Vector3(0.6f, 0f, 0.6f)));
                    V22Next(); break;
                case 10 when V22After(1.4f):
                    Log($"v2.2 KOSTÜM SAHNEDE: {WearerState()}");
                    Shot($"v22_{tag}_costume.png");
                    _v20Hold = null;
                    V22Next(); break;
                case 11 when V22After(2f):
                {
                    // sezon biter: tarih sezon dışına → jetonlar sona erer, süsleme ve tezgâh kalkar, alınan dekor ve kostüm kalır
                    string key = GameState.Data.seasonKey;
                    _v22Season = SeasonTag;
                    int left = Seasons.Tokens;
                    Seasons.DebugDate = Seasons.ForcedDate("none", Seasons.Now.Year);
                    Seasons.Tick(0f, true);
                    Log($"v2.2 SEZON BİTTİ: {key} → '{GameState.Data.seasonKey}' · kalan jeton {left} → {Seasons.Tokens} · sona eren {GameState.Data.seasonExpiredTokens} ({GameState.Data.seasonExpiredKey}) · " +
                        $"dekor sahipliği {Management.DecorOwned(Seasons.Get(SeasonIdOf(key)).decor[0])} · kostüm {Costumes.DebugInfo}");
                    V22Next(); break;
                }
                case 12 when V22After(3.2f):
                    Log($"v2.2 SEZON SONRASI: {SceneState()} · rozet görünür={hud.SeasonButtonVisible} · sona eren bildirimi gösterildi={GameState.Data.seasonExpiredTokens == 0} · ambiyans {AmbianceSource.Total:0}");
                    Log($"v2.2 SONA EREN PENCERESİ açık={hud.PremiumModalVisible}");
                    Shot($"v22_{tag.Replace("none", _v22Season)}_ended.png");
                    V22Next(); break;
            }
        }

        static SeasonId SeasonIdOf(string key)
        {
            foreach (var s in Seasons.List) if (key.StartsWith(s.key + "-")) return s.id;
            return SeasonId.None;
        }

        static Vector3 Nana(PlayerController p)
        {
            foreach (var w in Object.FindObjectsByType<CostumeWearer>(FindObjectsSortMode.None))
                if (w.owner == "staff_nana" && w.isActiveAndEnabled) return w.transform.position + new Vector3(1.2f, 0f, -1.2f);
            return p.transform.position;
        }

        static string WearerState()
        {
            var parts = new List<string>();
            foreach (var w in Object.FindObjectsByType<CostumeWearer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                parts.Add($"{w.owner}{(w.gameObject.activeInHierarchy ? "" : "(kapalı)")}={(w.Showing == "" ? "-" : w.Showing)}");
            return string.Join(" · ", parts);
        }

        // ------------------------------------------------------------------ sezon dışı
        void OffSeasonV22(HudController hud, PlayerController p)
        {
            switch (_v22Step)
            {
                case 0 when V22After(3f):
                    ClearUi(hud);
                    Log($"v2.2 SEZON DIŞI: aktif={Seasons.Active} · anahtar '{GameState.Data.seasonKey}' · {SceneState()} · rozet görünür={hud.SeasonButtonVisible} · sezon görevi {Seasons.QuestList.Count} · jeton {Seasons.Tokens}");
                    Later(() => Hold(p, new Vector3(-1.5f, 0f, -3.2f)));
                    V22Next(); break;
                case 1 when V22After(1.5f):
                    Shot($"v22_offseason_front{LangTag}.png");
                    _v20Hold = null;
                    hud.DebugOpen("events");
                    V22Next(); break;
                case 2 when V22After(1.2f):
                    Shot($"v22_offseason_events{LangTag}.png");
                    Log($"v2.2 SEZON DIŞI ETKİNLİK PANELİ: sekme '{hud.DebugEventsTab}'");
                    Later(() => { ClearUi(hud); hud.DebugOpen("shop-prem"); });
                    V22Next(); break;
                case 3 when V22After(1.2f):
                    Shot($"v22_offseason_wardrobe{LangTag}.png");
                    Later(() => ClearUi(hud));
                    V22Next(); break;
                case 4 when V22After(25f):
                    Log($"v2.2 SEZON DIŞI OYUN: sezon müşterisi {_v22SeasonGuests.Count} · sezon içeceği siparişi {_v22SeasonOrders} · jeton {Seasons.Tokens} · season_token {Count("season_token")} · season_start {Count("season_start")}");
                    V22Next(); break;
            }
        }

        // ------------------------------------------------------------------ gardırop
        void WardrobeV22(HudController hud, PlayerController p)
        {
            string tag = (Premium.Available ? "" : "-release") + LangTag;
            switch (_v22Step)
            {
                case 0 when V22After(3f):
                    ClearUi(hud);
                    Log($"v2.2 GARDIROP ÖNCE: {Costumes.DebugInfo} · {WearerState()} · mağaza açık={Premium.Available} test={Premium.TestStore}");
                    hud.DebugOpen("shop-prem");
                    V22Next(); break;
                case 1 when V22After(1.2f):
                    Shot($"v22_wardrobe{tag}.png");
                    V22Next(); break;
                case 2 when V22After(0.3f):
                    hud.DebugCostumePreview("barlo_korsan");
                    V22Next(); break;
                case 3 when V22After(1.0f):
                    Shot($"v22_wardrobe_preview{tag}.png");
                    Later(() => ClearUi(hud));   // görüntü kare sonunda alınır: pencere bir sonraki karede kapanır
                    _v22Step = 30; _v22At = _real; break;
                case 30 when V22After(0.4f):
                    _v22Step = 3;
                    if (!Premium.Available)
                    {
                        int req = Count("store_request");
                        Premium.Request("cos_barlo_korsan");
                        Log($"v2.2 YAYIN TAKLİDİ: Hazine kostümü istendi → mağaza kapalı penceresi={hud.PremiumModalVisible} · store_request {req} → {Count("store_request")} · sahip {Costumes.Owned("barlo_korsan")}");
                        V22Next(); break;
                    }
                    // Hazine (test mağazası onayı yerine doğrudan Grant: ödeme onaylandı) + personel kostümleri + ödül kostümü
                    Premium.Grant("cos_barlo_korsan");
                    foreach (var id in new[] { "cos_pip_yelek", "cos_gus_usta", "cos_ayla_sef" }) Premium.Grant(id);
                    Costumes.GrantTo(GameState.Data, "nana_bahar");
                    Costumes.Wear("nana_bahar");
                    Log($"v2.2 GARDIROP ALINDI: {Costumes.DebugInfo} · costume_purchase {Count("costume_purchase")} · costume_equip {Count("costume_equip")} · {WearerState()}");
                    Later(() => Hold(p, new Vector3(1.2f, 0f, 0.4f)));
                    V22Next(); break;
                case 4 when V22After(1.5f):
                    if (!Premium.Available) { Shot($"v22_wardrobe_soon{tag}.png"); _v22Step = 99; break; }
                    Shot($"v22_wardrobe_scene_bar{tag}.png");
                    Later(() => Hold(p, new Vector3(0.5f, 0f, 7.0f)));   // mutfak: Ayla
                    V22Next(); break;
                case 5 when V22After(1.5f):
                    Shot($"v22_wardrobe_scene_kitchen{tag}.png");
                    Later(() => Hold(p, Nana(p)));
                    V22Next(); break;
                case 6 when V22After(1.5f):
                    Shot($"v22_wardrobe_scene_nana{tag}.png");
                    _v20Hold = null;
                    // ödül kostümü: son sahne görülünce
                    bool before = Costumes.Owned("barlo_kral");
                    GameState.Data.endingSeen = true;
                    bool wore = Costumes.Wear("barlo_kral");
                    Log($"v2.2 ÖDÜL KOSTÜMÜ: önce sahip={before} · son görüldü → sahip={Costumes.Owned("barlo_kral")} · giyildi={wore} · {WearerState()}");
                    hud.DebugOpen("shop-prem");
                    V22Next(); break;
                case 7 when V22After(1.2f):
                    Shot($"v22_wardrobe_after{tag}.png");
                    Later(() => { ClearUi(hud); Hold(p, new Vector3(0.6f, 0f, 0.0f)); });
                    V22Next(); break;
                case 8 when V22After(1.4f):
                    Shot($"v22_wardrobe_barlo_kral{tag}.png");
                    _v20Hold = null;
                    Later(() =>   // görüntü kare sonunda alınır: kostüm bir sonraki karede çıkar
                    {
                        Costumes.TakeOff("barlo");
                        Log($"v2.2 ÇIKAR: barlo → '{Costumes.Worn("barlo")}' · {WearerState()} · kayıt giyilen [{string.Join(",", GameState.Data.wornCostumes)}]");
                        Costumes.Wear("barlo_korsan");   // kayıt korsanla kapanır: sonraki koşunun ana menüsünde Barlo kostümlü görünmeli
                        GameState.SaveSync();
                    });
                    V22Next(); break;
            }
        }
    }
}
#endif
