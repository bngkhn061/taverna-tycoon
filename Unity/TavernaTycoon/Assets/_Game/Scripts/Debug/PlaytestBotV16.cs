#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v1.6 Paket D test adımları:
    /// "-autotest-v16 save": eski PlayerPrefs kaydından taşıma, yaz → yeniden yükle, bozuk save.dat → yedek, ikisi de bozuk →
    /// kenara alma + oyuncu bilgisi; sonra oyun eski hâline döner.
    /// "-autotest-release-sim": yayın derlemesi taklidi — reklam düğmeleri gizli, Hazine "Yakında", hiçbir ödül verilmez.
    /// Her koşuda: yavaş kare sayımı (kayıt karesiyle çakışan), kayıt süreleri, ölçüm olay sayıları.
    /// </summary>
    public partial class PlaytestBot
    {
        string _v16Mode;
        int _v16Step;
        float _v16At;
        int _slowAll, _slowWithSave;
        float _slowMaxMs;
        Action _pendingConfirm;
        bool _v16AdAsked;
        int _v16AdBefore;
        float _v16AdAt;

        /// <summary>Yavaş kare eşiği (sn): YAVAŞ KARE günlüğüyle aynı.</summary>
        const float SlowFrameSeconds = 0.08f;

        string V16Mode
        {
            get
            {
                if (_v16Mode != null) return _v16Mode;
                var args = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(args, "-autotest-v16");
                _v16Mode = i >= 0 && i + 1 < args.Length ? args[i + 1] : "";
                return _v16Mode;
            }
        }

        /// <summary>Her kare: yavaş kare sayımı (günlükteki 2 sn sınırı olmadan) ve kayıt karesiyle çakışma.</summary>
        void CountSlowFrame()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= SlowFrameSeconds || Time.frameCount < 5) return;
            _slowAll++;
            _slowMaxMs = Mathf.Max(_slowMaxMs, dt * 1000f);
            if (GameState.LastSaveFrame >= Time.frameCount - 1) _slowWithSave++;
        }

        void UpdateV16(HudController hud)
        {
            if (_v16Step == 0)
            {
                _v16Step = 1;
                Log($"v1.6 TEST: kayıt={V16Mode} · yayın taklidi={Services.ReleaseSimulation} · geliştirme={Debug.isDebugBuild} · reklam açık={Services.RewardedAdsEnabled} · mağaza açık={Services.StoreEnabled} · oturum {Services.SessionId} · kayıt klasörü {SaveStore.Directory} · kaynak {GameState.LoadSource}");
            }
            if (V16Mode == "save") UpdateV16Save(hud);
            if (Services.ReleaseSimulation) UpdateV16Release(hud);
            else UpdateV16DevAd(hud);
        }

        // ------------------------------------------------------------------ (b) geliştirme: reklam arayüz üzerinden
        void UpdateV16DevAd(HudController hud)
        {
            if (!hud || V16Mode == "save") return;
            if (!_v16AdAsked && _t > 100f && !Tutorial.Active && !DayCycle.Ended)
            {
                _v16AdAsked = true;
                _v16AdBefore = HudController.DebugAdRewards;
                _v16AdAt = _real;
                bool got = false;
                hud.ShowAd("bot_test", "Test botu: ödüllü reklam", () => got = true);
                Log($"v1.6 REKLAM: istendi · hizmet {Services.Ads.GetType().Name} (test={Services.Ads.IsTest}) · hazır={Services.Ads.IsRewardedReady} · ödül hemen={got}");
            }
            if (_v16AdAsked && _v16AdAt > 0f && _real > _v16AdAt + TestAdService.ShortSeconds + 1.5f)
            {
                _v16AdAt = -1f;
                Log($"v1.6 REKLAM: {TestAdService.ShortSeconds:0} sn sonra verilen ödül {HudController.DebugAdRewards - _v16AdBefore} · ad_shown {Count("ad_shown")} · ad_rewarded {Count("ad_rewarded")}");
            }
        }

        static int Count(string ev) => Services.LocalAnalytics != null ? Services.LocalAnalytics.Counters.Count(ev) : -1;

        /// <summary>Test mağazasının onay ekranını sarar: ONAYLA'ya basmak yerine bot onayı sonra çağırır.</summary>
        void WrapTestConfirm()
        {
            var orig = TestStoreService.Confirmer;
            if (orig == null) return;
            TestStoreService.Confirmer = (id, ok, cancel) => { _pendingConfirm = ok; orig(id, ok, cancel); TestStoreService.Confirmer = orig; };
        }

        // ------------------------------------------------------------------ (c) kayıt senaryoları
        void UpdateV16Save(HudController hud)
        {
            if (_v16Step == 1 && _real > 3f && hud)
            {
                _v16Step = 2;
                RunSaveScenarios();
                _v16At = _real;
            }
            if (_v16Step == 2 && hud && _real > _v16At + 0.3f) { hud.DebugShowLoadNotice(); _v16Step = 3; _v16At = _real; }
            if (_v16Step == 3 && _real > _v16At + 1.0f) { Shot("v16_save_corrupt_notice.png"); _v16Step = 4; }
        }

        string _v16Snapshot;

        void RunSaveScenarios()
        {
            _v16Snapshot = JsonUtility.ToJson(GameState.Data);
            var sw = new System.Diagnostics.Stopwatch();

            // 1) Eski PlayerPrefs kaydı → dosya (+ eski yöntemin süresi)
            var legacy = JsonUtility.FromJson<SaveData>(_v16Snapshot);
            legacy.coins = 12345; legacy.tavernName = "Göç Testi"; legacy.saveVersion = 0;
            var legacyJson = JsonUtility.ToJson(legacy);
            double sum = 0, max = 0;
            const int Reps = 5;
            for (int i = 0; i < Reps; i++)
            {
                sw.Restart();
                PlayerPrefs.SetString(GameState.LegacyPrefsKey, legacyJson);
                PlayerPrefs.Save();
                double ms = sw.Elapsed.TotalMilliseconds;
                sum += ms; max = Math.Max(max, ms);
            }
            Log($"v1.6 KAYIT ESKİ YÖNTEM: PlayerPrefs.SetString + Save ({legacyJson.Length} karakter) {Reps} kez · ort. {sum / Reps:0.0} ms · en çok {max:0.0} ms");
            SaveStore.DeleteAll();
            sw.Restart();
            GameState.Load();
            Log($"v1.6 KAYIT TAŞIMA: kaynak={GameState.LoadSource} · altın {GameState.Coins} (beklenen 12345) · ad '{GameState.Data.tavernName}' · saveVersion {GameState.Data.saveVersion} · save.dat var={File.Exists(SaveStore.MainPath)} · PlayerPrefs anahtarı kaldı={PlayerPrefs.HasKey(GameState.LegacyPrefsKey)} · {sw.Elapsed.TotalMilliseconds:0.0} ms");

            // 2) Yaz → yeniden yükle
            GameState.Data.coins = 22222;
            sw.Restart();
            GameState.SaveSync();
            double syncMs = sw.Elapsed.TotalMilliseconds;
            GameState.Data.coins = 0;
            GameState.Load();
            Log($"v1.6 KAYIT YAZ→OKU: kaynak={GameState.LoadSource} · altın {GameState.Coins} (beklenen 22222) · save.bak var={File.Exists(SaveStore.BackupPath)} · eşzamanlı yazım {syncMs:0.0} ms · dosya {new FileInfo(SaveStore.MainPath).Length} bayt");
            var head = File.ReadAllText(SaveStore.MainPath);
            Log($"v1.6 KAYIT DOSYASI: ilk satır '{head.Substring(0, head.IndexOf('\n'))}' · imza {head.Split('\n')[1].Length} karakter");

            // 3) Elle bozulmuş save.dat → yedek
            File.WriteAllText(SaveStore.MainPath, head.Replace("\"coins\":22222", "\"coins\":99999"));
            GameState.Load();
            Log($"v1.6 KAYIT BOZUK ANA: kaynak={GameState.LoadSource} · altın {GameState.Coins} (beklenen 12345, bir önceki sürüm) · kenara alınan {SaveStore.LastCorruptName} · bilgi '{GameState.LoadNotice}'");

            // 4) İkisi de bozuk → kenara al, bilgi ver, yeni oyun
            File.WriteAllText(SaveStore.MainPath, "bozuk");
            if (File.Exists(SaveStore.BackupPath)) File.WriteAllText(SaveStore.BackupPath, File.ReadAllText(SaveStore.BackupPath).Replace("\"coins\":12345", "\"coins\":1"));
            GameState.Load();
            var aside = Directory.GetFiles(SaveStore.Directory, SaveStore.CorruptPrefix + "*");
            Log($"v1.6 KAYIT İKİSİ DE BOZUK: kaynak={GameState.LoadSource} · altın {GameState.Coins} · Sv{GameState.Level} (yeni oyun) · save.dat var={File.Exists(SaveStore.MainPath)} · kenarda {aside.Length} dosya [{string.Join(", ", Array.ConvertAll(aside, Path.GetFileName))}] · bilgi '{GameState.LoadNotice}'");

            // 5) Oyunu eski hâline döndür
            GameState.DebugReplace(JsonUtility.FromJson<SaveData>(_v16Snapshot));
            GameState.SaveSync();
            GameState.Load();
            Log($"v1.6 KAYIT GERİ: kaynak={GameState.LoadSource} · Sv{GameState.Level} altın {GameState.Coins} · ad '{GameState.Data.tavernName}'");
        }

        // ------------------------------------------------------------------ (d) yayın güvenliği taklidi
        void UpdateV16Release(HudController hud)
        {
            if (!hud) return;
            var root = FindAnyObjectByType<UIDocument>()?.rootVisualElement;
            if (root == null) return;
            if (_v16Step == 1 && _t > 120f && !Tutorial.Active && !DayCycle.Ended && !hud.LevelCardOpen)
            {
                _v16Step = 2; _v16At = _real;
                long coins = GameState.Coins; int gems = GameState.Data.gems;
                int adBefore = HudController.DebugAdRewards;
                bool rewarded = false;
                hud.ShowAd("bot_test", "Test botu", () => rewarded = true);
                Premium.Grant("gems_100");
                Premium.Grant("staff_lirael");
                Log($"v1.6 YAYIN: ShowAd → ödül={rewarded} (sayaç {HudController.DebugAdRewards - adBefore}) · Premium.Grant(gems_100, staff_lirael) → elmas {gems}→{GameState.Data.gems} · Lirael={Premium.Owned("staff_lirael")} · altın {coins}→{GameState.Coins}");
                bool helperHidden = root.Q("helper-btn")?.ClassListContains("hidden") ?? false;
                Log($"v1.6 YAYIN: yardımcı garson düğmesi gizli={helperHidden} · reklam açık={Services.RewardedAdsEnabled} · mağaza açık={Services.StoreEnabled} · test reklamı hazır={Services.Ads.IsRewardedReady}");
                hud.DebugOpen("shop-staff");
            }
            if (_v16Step == 2 && _real > _v16At + 1.2f)
            {
                Log($"v1.6 YAYIN: Personel sekmesi · reklam düğmesi {CountText(root.Q("shop-scroll"), "REKLAM")} · görevli kartı {root.Q("shop-scroll")?.Query(className: "item").ToList().Count}");
                Shot("v16_rel_staff.png"); _v16Step = 3; _v16At = _real;
            }
            if (_v16Step == 3 && _real > _v16At + 0.6f) { hud.DebugOpen("close"); hud.DebugOpen("shop-prem"); _v16Step = 4; _v16At = _real; }
            if (_v16Step == 4 && _real > _v16At + 1.2f)
            {
                var scroll = root.Q("shop-scroll");
                Log($"v1.6 YAYIN: Hazine · YAKINDA düğmesi {CountText(scroll, "YAKINDA")} · tıklanabilir fiyat düğmesi {CountEnabledPriceButtons(scroll)} · not '{root.Q<Label>("shop-note")?.text}'");
                Shot("v16_rel_hazine.png"); _v16Step = 5; _v16At = _real;
            }
            if (_v16Step == 5 && _real > _v16At + 0.6f)
            {
                hud.DebugOpen("close");
                int gems = GameState.Data.gems;
                Premium.Request("gems_100");
                Log($"v1.6 YAYIN: Premium.Request(gems_100) → pencere={hud.PremiumModalVisible} · elmas {gems}→{GameState.Data.gems} · store_request {Count("store_request")} · store_purchase {Count("store_purchase")}");
                _v16Step = 6; _v16At = _real;
            }
            if (_v16Step == 6 && _real > _v16At + 1.0f) { Shot("v16_rel_store_soon.png"); hud.DebugOpen("close"); hud.DebugOpen("depot"); _v16Step = 7; _v16At = _real; }
            if (_v16Step == 7 && _real > _v16At + 1.2f)
            {
                var rows = root.Q("depot-rows");
                Log($"v1.6 YAYIN: Depo · HEMEN düğmesi {CountText(rows, "HEMEN")} · YOLDA {CountText(rows, "YOLDA")} · yolda sipariş {DrinkStation.All.FindAll(s => s && s.drink && Depot.Incoming(s.drink.id) > 0).Count}");
                Shot("v16_rel_depot.png"); _v16Step = 8; _v16At = _real;
            }
            if (_v16Step == 8 && _real > _v16At + 0.6f) { hud.DebugOpen("close"); hud.DebugOpen("rename"); _v16Step = 9; _v16At = _real; }
            if (_v16Step == 9 && _real > _v16At + 1.0f)
            {
                Log($"v1.6 YAYIN: Ad değiştir · reklam düğmesi gizli={root.Q("rename-ad")?.ClassListContains("hidden")}");
                Shot("v16_rel_rename.png"); hud.DebugOpen("close"); _v16Step = 10; _v16At = _real;
            }
            if (_v16Step == 10 && _real > _v16At + 0.5f) { Shot("v16_rel_hud.png"); _v16Step = 11; }
        }

        static int CountText(VisualElement scope, string text)
        {
            if (scope == null) return -1;
            int n = 0;
            scope.Query<Label>().ForEach(l => { if (l.text != null && l.text.Contains(text) && l.resolvedStyle.display != DisplayStyle.None) n++; });
            return n;
        }

        static int CountEnabledPriceButtons(VisualElement scope)
        {
            if (scope == null) return -1;
            int n = 0;
            scope.Query<Button>().ForEach(b => { if (b.enabledSelf && b.Q<Label>()?.text?.Contains("₺") == true) n++; });
            return n;
        }

        void FinishV16()
        {
            var la = Services.LocalAnalytics;
            if (la != null)
            {
                var c = la.Counters;
                var parts = new System.Collections.Generic.List<string>();
                for (int i = 0; i < c.names.Count; i++) parts.Add($"{c.names[i]}={c.counts[i]}");
                la.Flush(true);
                long size = File.Exists(la.FilePath) ? new FileInfo(la.FilePath).Length : 0;
                Log($"v1.6 ÖLÇÜM: bu süreçte {la.SentThisProcess} olay · dosya {la.FilePath} ({size} bayt) · sayaçlar (tüm oturumlar) [{string.Join(", ", parts)}]");
                Log($"v1.6 ÖLÇÜM ÖZETİ: {la.SummaryText()}");
            }
            Log($"v1.6 KAYIT ÖZET: kayıt çağrısı {GameState.SaveCount} · ana iş parçacığı son {GameState.LastSaveMainMs:0.00} ms en çok {GameState.MaxSaveMainMs:0.00} ms · diske yazım {SaveStore.WriteCount} (arka plan, son {SaveStore.LastWriteMs:0.0} ms en çok {SaveStore.MaxWriteMs:0.0} ms) · hata '{SaveStore.LastError}'");
            Log($"v1.6 YAVAŞ KARE ÖZET: >{SlowFrameSeconds * 1000f:0} ms kare {_slowAll} (en uzun {_slowMaxMs:0} ms) · kayıt karesiyle çakışan {_slowWithSave} · {_real:0} gerçek sn");
        }
    }
}
#endif
