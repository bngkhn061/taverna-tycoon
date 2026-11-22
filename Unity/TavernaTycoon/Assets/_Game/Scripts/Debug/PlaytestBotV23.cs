#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Taverna
{
    /// <summary>
    /// v2.3 Paket 5 test adımları, "-autotest-v23 &lt;mod&gt;":
    ///   edit      — DÜZENLE: bir masa ve bir dekor geçerli yere taşınır (görüntü), geçersiz yer denenir (kırmızı görüntü, bırakılamaz),
    ///               BİTTİ ile kaydedilir; sahne diskteki kayıttan yeniden yüklenip yerleşim doğrulanır; taşınan masaya müşterinin
    ///               oturup servis aldığı izlenir.
    ///   editcheck — "-autotest-keep-save" ile ikinci süreç: önceki koşunun kaydı açılınca yerleşim geliyor mu.
    ///   cloud     — sahte bulut: Ayarlar › Bulut kaydı, yedekle, aynı ilerlemede yükle, başka cihazın belirsiz kaydıyla çakışma ekranı
    ///               (bulutu yükle → sahne yeniden), bozuk imza reddi, "bu cihazı tut". "-autotest-release-sim" ile "Yakında".
    /// </summary>
    public partial class PlaytestBot
    {
        string _v23Arg;
        string V23 => _v23Arg ??= ArgAfter("-autotest-v23");

        int _v23Step;
        float _v23At;
        bool _v23Init, _v23Served;
        string _v23Table, _v23Decor;
        Layout.Pose _v23TablePose, _v23DecorPose;
        int _v23LevelBefore;

        bool V23After(float s) => _real > _v23At + s;
        System.Action _v23Later;
        int _v23LaterFrame;
        /// <summary>Görüntü karenin sonunda alınır: arayüzü değiştiren işlem bir sonraki kareye ertelenir.</summary>
        void AfterShot(System.Action a) { _v23Later = a; _v23LaterFrame = Time.frameCount; }
        void V23Next() { _v23Step++; _v23At = _real; }

        /// <summary>true: bu kare bot oynamaz (düzenleme modu / bulut adımları sürüyor).</summary>
        bool UpdateV23()
        {
            if (V23 == "") return false;
            if (_v23Later != null)
            {
                if (Time.frameCount > _v23LaterFrame + 1) { var a = _v23Later; _v23Later = null; a(); }
                return true;
            }
            var hud = _hud ? _hud : (_hud = FindAnyObjectByType<HudController>());
            if (!hud) return true;
            if (!_v23Init)
            {
                _v23Init = true;
                _v23At = _real;
                _uiStep = UiSteps.Length; _langStep = 4; _tierStep = 9; _premStep = 4; _fireStep = 2;   // görüntüleri örten panel turları kapalı
                Log($"v2.3 BAŞLADI · mod '{V23}' · Sv{GameState.Level} · bulut açık={CloudSave.Enabled} ({Services.Cloud?.ProviderName}) · yerleşim satırı {GameState.Data.layout.Count} · kayıt sürümü {GameState.Data.saveVersion}/{GameState.Data.layoutVersion}");
            }
            if (V23 == "edit") return EditV23(hud);
            if (V23 == "editcheck") return EditCheckV23(hud);
            if (V23 == "cloud") return CloudV23(hud);
            return false;
        }

        void ShotV23(string file) { Shot("v23_" + file + ".png"); Log($"v2.3 GÖRÜNTÜ v23_{file}.png"); }

        static string PoseText(Layout.Pose p) => $"({p.x:0.##}, {p.z:0.##}) {p.quarter * 90}°";

        bool EditV23(HudController hud)
        {
            var el = EditLayout.I;
            switch (_v23Step)
            {
                case 0:
                    if (Tutorial.Active || !el || !V23After(6f)) return false;
                    foreach (var id in new[] { "dec_saksi", "dec_fici" })
                    {
                        var d = System.Array.Find(Catalog.DecorList, x => x.id == id);
                        GameState.Data.coins += d.price;
                        Log($"v2.3 DEKOR al {id}: {Management.BuyDecor(d)}");
                    }
                    V23Next();
                    return true;
                case 1:
                    if (!V23After(2f)) return true;
                    Log($"v2.3 DÜZENLE düğmesi görünür={hud.EditButtonVisible} · açılabilir={EditLayout.CanOpen}");
                    hud.DebugEditPress();
                    Log($"v2.3 DÜZENLEME AÇIK={EditLayout.Active} · zaman ölçeği {Time.timeScale} · masalar [{string.Join(",", el.DebugKeys(true))}] · dekor [{string.Join(",", el.DebugKeys(false))}]");
                    V23Next();
                    return true;
                case 2:
                    // Görüntü karenin sonunda alınır: her görüntü kendi adımında, sonraki işlem bir sonraki adımda
                    if (!V23After(1.4f)) return true;
                    ShotV23("edit_open");
                    V23Next();
                    return true;
                case 3:
                    if (!V23After(0.3f)) return true;
                    var tables = el.DebugKeys(true);
                    if (tables.Remove("t:Table_F")) tables.Insert(0, "t:Table_F");
                    _v23Table = null;
                    Vector2 tp = default;
                    foreach (var k in tables)
                    {
                        if (el.DebugFindValid(k, 2f, out tp)) { _v23Table = k; break; }
                        Log($"v2.3 IZGARA {k}: 2 m ötede geçerli yer yok · {el.DebugDiagnose(k)}");
                    }
                    var decs = el.DebugKeys(false);
                    _v23Decor = decs.Count > 0 ? decs[0] : null;
                    if (_v23Table == null) { Log("v2.3 HATA: taşınacak masa / yer bulunamadı"); _v23Step = 99; return false; }
                    var r = el.DebugTry(_v23Table, tp, 0, false);
                    EditLayout.DebugFocus(tp);
                    Log($"v2.3 MASA {_v23Table} → {tp} sürükleniyor: {r}");
                    V23Next();
                    return true;
                case 4:
                    if (!V23After(1.0f)) return true;
                    ShotV23("edit_valid");
                    V23Next();
                    return true;
                case 5:
                    if (!V23After(0.3f)) return true;
                    EditLayout.I.PointerUp();
                    el.DebugPose(_v23Table, out _v23TablePose);
                    Log($"v2.3 MASA bırakıldı: {PoseText(_v23TablePose)} · varsayılan değil={!el.DebugIsDefault(_v23Table)}");
                    // geçersiz: aynı bölgedeki bir makinenin önü (makine önü açık kalmalı)
                    var zone = el.DebugZone(_v23Table);
                    var st = DrinkStation.All.Find(s => s && s.servePoint && EditLayout.Zones[zone].bounds.Contains(new Vector2(s.servePoint.position.x, s.servePoint.position.z)));
                    var bad = st ? new Vector2(Layout.Snap(st.servePoint.position.x), Layout.Snap(st.servePoint.position.z - 0.4f)) : new Vector2(EditLayout.Zones[zone].bounds.maxX + 1f, 0f);
                    var rb = el.DebugTry(_v23Table, bad, 0, false);
                    EditLayout.DebugFocus(bad);
                    Log($"v2.3 GEÇERSİZ deneme {_v23Table} → {bad} ({(st ? st.name : "bölge dışı")}): {rb} · \"{EditLayout.Reason(rb)}\" · seçim geçerli={el.SelectionValid}");
                    V23Next();
                    return true;
                case 6:
                    if (!V23After(1.0f)) return true;
                    ShotV23("edit_invalid_red");
                    V23Next();
                    return true;
                case 7:
                    if (!V23After(0.3f)) return true;
                    int rej = el.Rejected;
                    EditLayout.I.PointerUp();
                    el.DebugPose(_v23Table, out var back);
                    Log($"v2.3 GEÇERSİZ bırakılamadı: geri alındı={el.Rejected > rej} · yer {PoseText(back)} (önceki geçerli {PoseText(_v23TablePose)}) · mesaj \"{hud.EditMessage}\"");
                    var rr = el.DebugTry(_v23Table, _v23TablePose.Pos, 1, true);
                    el.DebugPose(_v23Table, out _v23TablePose);
                    Log($"v2.3 DÖNDÜR 90°: {rr} → {PoseText(_v23TablePose)}");
                    if (_v23Decor != null && el.DebugFindValid(_v23Decor, 1.5f, out var dp))
                    {
                        var rd = el.DebugTry(_v23Decor, dp, 0, true);
                        el.DebugPose(_v23Decor, out _v23DecorPose);
                        Log($"v2.3 DEKOR {_v23Decor} → {PoseText(_v23DecorPose)}: {rd}");
                    }
                    else Log("v2.3 DEKOR: taşınacak dekor / yer yok");
                    el.DebugTry(_v23Table, _v23TablePose.Pos, _v23TablePose.quarter, false);   // masa seçili kalsın (görüntüde vurgulu)
                    EditLayout.DebugFocus(_v23TablePose.Pos);
                    V23Next();
                    return true;
                case 8:
                    if (!V23After(1.0f)) return true;
                    ShotV23("edit_moved");
                    V23Next();
                    return true;
                case 9:
                    if (!V23After(0.3f)) return true;
                    EditLayout.I.PointerUp();
                    EditLayout.I.Close(true);
                    Log($"v2.3 BİTTİ: kaydedildi={EditLayout.LastCloseSaved} · zaman ölçeği {Time.timeScale} · yerleşim [{string.Join(" ; ", GameState.Data.layout)}]");
                    var t = el.DebugTable(_v23Table);
                    if (t && PlayerController.I) PlayerController.I.Teleport(t.transform.position + new Vector3(0f, 0f, -1.6f));
                    LogPaths(t);
                    V23Next();
                    return false;
                case 10:
                    if (!V23After(2.5f)) return false;
                    ShotV23("edit_saved");
                    GameState.SaveSync();
                    V23Next();
                    return true;
                case 11:
                    if (!V23After(0.3f)) return true;
                    Log("v2.3 YENİDEN AÇ: kayıt diske yazıldı, sahne diskten yükleniyor");
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    V23Next();
                    return true;
                case 12:
                    if (!V23After(4f) || !EditLayout.I) return true;
                    VerifyLayout("YENİDEN AÇILIŞ");
                    V23Next();
                    return false;
                case 13:
                    // taşınan masaya müşteri oturup servis alıyor mu (bot normal oynar)
                    var mt = EditLayout.I ? EditLayout.I.DebugTable(_v23Table) : null;
                    foreach (var c in Customer.All)
                    {
                        if (!c || !c.Seat || c.Seat.table != mt) continue;
                        if (c.state == Customer.State.Waiting && !_v23Served && PlayerController.I) PlayerController.I.Teleport(c.transform.position + new Vector3(0f, 0f, -1.4f));
                        if (c.state == Customer.State.Drinking && !_v23Served)
                        {
                            _v23Served = true;
                            Log($"v2.3 TAŞINAN MASA: {c.def.displayName} {mt.name} oturağında ({c.Seat.transform.position.x:0.##}, {c.Seat.transform.position.z:0.##}) servis aldı, içiyor ✓");
                            ShotV23("moved_table_served");
                        }
                    }
                    if (!_v23Served && V23After(150f)) { Log("v2.3 TAŞINAN MASA: 150 sn'de müşteri servis almadı ✗"); _v23Served = true; }
                    return false;
                default:
                    return false;
            }
        }

        void LogPaths(Table t)
        {
            if (!t) return;
            var sb = new StringBuilder();
            foreach (var s in t.seats)
            {
                if (!s) continue;
                var pts = CustomerSpawner.InnerPath(s);
                sb.Append($" [{string.Join(" → ", pts.ConvertAll(p => $"({p.x:0.#},{p.z:0.#})"))}]");
            }
            Log($"v2.3 YOL {t.name} (toplanma {(t.hub ? t.hub.name : "-")}):{sb}");
        }

        void VerifyLayout(string tag)
        {
            var el = EditLayout.I;
            var d = GameState.Data;
            int ok = 0, bad = 0;
            var sb = new StringBuilder();
            foreach (var line in d.layout)
            {
                if (!Layout.TryParse(line, out var key, out var saved)) continue;
                bool found = el.DebugPose(key, out var now);
                bool same = found && now.Same(saved);
                if (same) ok++; else bad++;
                sb.Append($" {key}: kayıt {PoseText(saved)} sahne {(found ? PoseText(now) : "yok")} {(same ? "✓" : "✗")};");
            }
            Log($"v2.3 {tag}: yerleşim satırı {d.layout.Count} · eşleşen {ok} · uymayan {bad} · sürüm {d.saveVersion}/{d.layoutVersion} ·{sb}");
            if (_v23Table != null) LogPaths(el.DebugTable(_v23Table));
        }

        bool EditCheckV23(HudController hud)
        {
            if (_v23Step == 0 && V23After(5f) && EditLayout.I)
            {
                VerifyLayout("İKİNCİ SÜREÇ (-autotest-keep-save)");
                foreach (var line in GameState.Data.layout)
                    if (Layout.TryParse(line, out var key, out var p) && key.StartsWith("t:")) { _v23Table = key; if (PlayerController.I) PlayerController.I.Teleport(new Vector3(p.x, 0f, p.z - 1.6f)); break; }
                V23Next();
            }
            else if (_v23Step == 1 && V23After(1.5f)) { ShotV23("reopen"); V23Next(); }
            return _v23Step == 1;   // görüntü alınana kadar bot Barlo'yu yürütmez
        }

        // ------------------------------------------------------------------ bulut
        LocalCloudSaveService FakeCloud => Services.Cloud as LocalCloudSaveService;

        /// <summary>Bu cihazın kaydının ilerlemesi değiştirilmiş kopyası, başka bir kurulumun bulut kaydı gibi imzalı.</summary>
        static byte[] OtherDeviceSave(int levelDelta, long earnedDelta, out int level)
        {
            var d = GameState.FromJson(GameState.SnapshotJson());
            d.level = Mathf.Clamp(d.level + levelDelta, 1, Balance.MaxLevel);
            d.totalEarned = System.Math.Max(0, d.totalEarned + earnedDelta);
            d.coins += 777;
            level = d.level;
            var meta = CloudSave.BuildMeta(d, "baska-cihaz-test", System.DateTime.UtcNow);
            return CloudSaveCodec.Encode(meta, JsonUtility.ToJson(d));
        }

        bool CloudV23(HudController hud)
        {
            var cp = hud.Cloud;
            switch (_v23Step)
            {
                case 0:
                    if (Tutorial.Active || !V23After(5f) || cp == null) return false;
                    FakeCloud?.Clear();
                    hud.DebugOpen("settings");
                    Log($"v2.3 AYARLAR açıldı · bulut açık={CloudSave.Enabled} · sahte bulut klasörü {(FakeCloud != null ? FakeCloud.Folder : "-")}");
                    V23Next();
                    return true;
                case 1:
                    if (!V23After(1.2f)) return true;
                    ShotV23(CloudSave.Enabled ? "settings_cloud_row" : "settings_cloud_soon_row");
                    AfterShot(cp.Open);
                    V23Next();
                    return true;
                case 2:
                    if (!V23After(1.0f)) return true;
                    ShotV23(CloudSave.Enabled ? "cloud_panel" : "cloud_panel_soon");
                    if (!CloudSave.Enabled) { Log("v2.3 BULUT YAYIN TAKLİDİ: sağlayıcı yok → \"Yakında\", düğmeler kapalı ✓"); AfterShot(() => { cp.Close(); hud.DebugCloseSettings(); }); _v23Step = 99; return true; }
                    AfterShot(cp.Backup);
                    V23Next();
                    return true;
                case 3:
                    if (!V23After(1.0f)) return true;
                    Log($"v2.3 YEDEKLE: \"{cp.LastResultText}\" · son eşitleme {Services.Cloud.LastSyncUtc:HH:mm:ss}");
                    ShotV23("cloud_backup");
                    AfterShot(cp.RestoreClicked);
                    V23Next();
                    return true;
                case 4:
                    if (!V23After(1.0f)) return true;
                    Log($"v2.3 YÜKLE (aynı ilerleme): \"{cp.LastResultText}\"");
                    _v23LevelBefore = GameState.Level;
                    FakeCloud.DebugWriteRaw(OtherDeviceSave(2, 500, out int lv));
                    Log($"v2.3 ÇAKIŞMA hazırlığı: bulutta başka cihazın kaydı Sv{lv} (bu cihaz Sv{_v23LevelBefore})");
                    cp.RestoreClicked();
                    V23Next();
                    return true;
                case 5:
                    if (!V23After(1.0f)) return true;
                    Log($"v2.3 ÇAKIŞMA ekranı görünür={cp.ConflictVisible} · \"{cp.ConflictLine}\"");
                    ShotV23("cloud_conflict");
                    AfterShot(() => cp.DebugChoose(true));
                    V23Next();
                    return true;
                case 6:
                    if (!V23After(3.5f)) return true;
                    Log($"v2.3 BULUTU YÜKLE: sahne yeniden yüklendi · Sv{_v23LevelBefore} → Sv{GameState.Level} {(GameState.Level == Mathf.Min(_v23LevelBefore + 2, Balance.MaxLevel) ? "✓" : "✗")}");
                    // bozuk imza: geçerli kaydın içeriği değiştirilir
                    var good = Encoding.UTF8.GetString(OtherDeviceSave(4, 0, out _));
                    int i = good.IndexOf("\"coins\":", System.StringComparison.Ordinal);
                    var tampered = i > 0 ? good.Substring(0, i) + "\"coins\":99999999," + good.Substring(good.IndexOf(',', i) + 1) : good + "x";
                    FakeCloud.DebugWriteRaw(Encoding.UTF8.GetBytes(tampered));
                    _v23LevelBefore = GameState.Level;
                    hud.DebugOpen("settings");
                    V23Next();
                    return true;
                case 7:
                    if (!V23After(0.8f)) return true;
                    hud.Cloud.Open();
                    hud.Cloud.RestoreClicked();
                    V23Next();
                    return true;
                case 8:
                    if (!V23After(1.0f)) return true;
                    Log($"v2.3 BOZUK İMZA: \"{hud.Cloud.LastResultText}\" · seviye değişmedi={GameState.Level == _v23LevelBefore} ✓");
                    ShotV23("cloud_rejected");
                    AfterShot(() =>
                    {
                        FakeCloud.DebugWriteRaw(OtherDeviceSave(-2, 0, out int lv2));
                        hud.Cloud.Backup();
                        Log($"v2.3 YEDEKLE (bulutta başka cihazın Sv{lv2} kaydı): çakışma ekranı bekleniyor");
                    });
                    V23Next();
                    return true;
                case 9:
                    if (!V23After(1.0f)) return true;
                    Log($"v2.3 ÇAKIŞMA (yedekle) görünür={hud.Cloud.ConflictVisible} · \"{hud.Cloud.ConflictLine}\" → BU CİHAZI TUT");
                    hud.Cloud.DebugChoose(false);
                    V23Next();
                    return true;
                case 10:
                    if (!V23After(1.0f)) return true;
                    Log($"v2.3 BU CİHAZI TUT: \"{hud.Cloud.LastResultText}\" · otomatik yedek aralığı {CloudSave.AutoSyncSeconds:0} sn");
                    var la = Services.LocalAnalytics;
                    if (la != null)
                    {
                        var parts = new List<string>();
                        foreach (var ev in new[] { "cloud_backup", "cloud_restore", "cloud_conflict", "edit_mode_open", "edit_mode_save" }) parts.Add($"{ev}={la.Counters.Count(ev)}");
                        Log($"v2.3 ÖLÇÜM: {string.Join(" ", parts)}");
                    }
                    hud.Cloud.Close();
                    hud.DebugCloseSettings();
                    V23Next();
                    return false;
                default:
                    return false;
            }
        }
    }
}
#endif
