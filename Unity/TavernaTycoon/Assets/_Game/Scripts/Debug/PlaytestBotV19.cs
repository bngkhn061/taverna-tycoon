#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v1.9 Paket 1 test adımları (production/qa/playtests/feedback-2026-11-17.md):
    /// zorunlu rehber (bot kilitli bir düğmeyi dener, sonra hedefe gerçek olayla basar), görevin otomatik tamamlanması,
    /// seviye kartındaki açılanlar, hedef kutusunun her açılışta yenilenmesi, ses karışımının tepe ölçümü (AudioMeter).
    /// "-autotest-v19 debt": maaş öncesi para harcanır → gün sonu borç, satın alma kapalı; otomasyon elmasla alınır.
    /// "-autotest-v19 rename": ad değiştirme penceresi (yayın taklidinde reklam gizli, altın + elmas görünür).
    /// </summary>
    public partial class PlaytestBot
    {
        string _v19Arg;
        string V19 => _v19Arg ??= ArgAfter("-autotest-v19");

        static string ArgAfter(string flag)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, flag);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : "";
        }

        bool _v19Init, _questTested, _autoGemTested, _renameShot, _debtShotHud, _debtCleared, _starShot;
        int _debtStep, _tutTry = -1, _tutClick = -1, _levelShots, _lastLevelLogged;
        float _audioLogT, _debtAt, _tutAt;
        string _goalId = "", _pendingUnlock;
        long _debtMin;
        HudController _hud19;
        bool _meterOn;
        readonly System.Collections.Generic.HashSet<string> _stateDoneLogged = new();

        void UpdateV19()
        {
            var hud = _hud19 ? _hud19 : (_hud19 = FindAnyObjectByType<HudController>());
            if (!_v19Init)
            {
                _v19Init = true;
                var listener = FindAnyObjectByType<AudioListener>();
                if (listener && !listener.GetComponent<AudioMeter>()) listener.gameObject.AddComponent<AudioMeter>();
                // Ölçüm varsayılan ses ayarlarıyla (önceki testler efekt sesini 0'da bırakmış olabilir)
                if (GameSettings.Sfx < 0.85f || GameSettings.Music < 0.7f) { Log($"v1.9 SES ayarı {GameSettings.Music:0.00}/{GameSettings.Sfx:0.00} → varsayılan 0.70/0.85"); GameSettings.Music = 0.7f; GameSettings.Sfx = 0.85f; }
                Log($"v1.9 SES aygıtı: {AudioSettings.outputSampleRate} Hz · DSP tampon {DspInfo()} · dinleyici {(listener ? listener.name : "yok")}");
                GameState.ZoneUnlocked += id => _pendingUnlock = id;
                Quests.Changed += () =>
                {
                    foreach (var q in Quests.List)
                        if (Quests.IsStateQuest(q.type) && q.Done && !q.claimed && _stateDoneLogged.Add(Quests.Text(q)))
                            Log($"v1.9 GÖREV kendiliğinden tamamlandı: \"{Quests.Text(q)}\" (toplanabilir {Quests.ReadyCount})");
                };
                Log($"v1.9 BAŞLADI · mod '{V19}' · Sv{GameState.Level} · elmas {GameState.Data.gems} · rehber {(Tutorial.Active ? "açık" : "yok")} · reklam {(Services.RewardedAdsEnabled ? "açık" : "kapalı")}");
            }
            AudioMeter.SampleOutput();   // her kare: dinleyicinin son çıkış örnekleri (OnAudioFilterRead gelmeyen aygıtlar için)
            AudioLog();
            GoalCheck();
            LevelCardLog(hud);
            if (!NoTests) QuestAutoTest();
            if (V19 == "debt") DebtScenario(hud);
            if (V19 == "rename") RenameShot(hud);
            if (!_starShot && hud && !Tutorial.Active && _t > 20f && V19 == "")
            {
                _starShot = true;
                Log($"v1.9 YILDIZ KARTI: ambiyans {AmbianceSource.Total:0} ★{Management.Stars:0.#} · sonraki ★{Management.NextStar} için {Management.AmbianceToNextStar}");
            }
        }

        // ------------------------------------------------------------------ ses tepe ölçümü
        void AudioLog()
        {
            _audioLogT += Time.unscaledDeltaTime;
            if (_audioLogT < 15f) return;
            _audioLogT = 0f;
            if (!_meterOn)
            {
                var l = FindAnyObjectByType<AudioListener>();
                if (l && !l.GetComponent<AudioMeter>()) { l.gameObject.AddComponent<AudioMeter>(); _meterOn = true; Log($"v1.9 SES ölçer takıldı: {l.name}"); }
            }
            float wp = AudioMeter.TakeWindowPeak();
            Log($"v1.9 SES çıkış (GetOutputData): tepe {AudioMeter.Db(AudioMeter.OutputPeak):+0.0;-0.0} dBFS · ≥1.0 {AudioMeter.OutputClipped}");
            Log($"v1.9 SES: pencere tepe {AudioMeter.Db(wp):+0.0;-0.0} dBFS · toplam tepe {AudioMeter.Db(AudioMeter.Peak):+0.0;-0.0} dBFS · ≥1.0 örnek {AudioMeter.Clipped}/{AudioMeter.Samples} · atlanan efekt {Sfx.SkippedFx} · müzik {Sfx.MusicNow} · ortam [{Sfx.AmbientInfo}]");
        }

        // ------------------------------------------------------------------ hedef kutusu (madde 3)
        void GoalCheck()
        {
            var g = PayZone.NextGoal();
            if (_pendingUnlock != null)
            {
                string opened = _pendingUnlock;
                _pendingUnlock = null;
                if (PayZone.Find(opened))
                    Log($"v1.9 HEDEF: açılan '{opened}' · önceki hedef '{_goalId}' → yeni '{(g ? g.id : "-")}' · yeni hedef açık mı={(g && GameState.IsUnlocked(g.id))} · görünür={(g && g.Visible)} · yenilendi={(!g || g.id != opened)}");
            }
            _goalId = g ? g.id : "";
        }

        // ------------------------------------------------------------------ seviye kartı (madde 6, 12)
        void LevelCardLog(HudController hud)
        {
            if (!hud || !hud.LevelCardOpen || GameState.Level == _lastLevelLogged) return;
            if (hud.DebugModalOpen) hud.DebugClosePremModal();   // başlangıç paketi kartı seviye kartını örtmesin (görüntü)
            _lastLevelLogged = GameState.Level;
            Log($"v1.9 SEVİYE KARTI Sv{GameState.Level}: {hud.DebugUnlocks}");
            int lv = GameState.Level;
            if (!NoShots && _levelShots < 4) { _levelShots++; hud.StartCoroutine(After(0.2f, () => { if (hud.DebugModalOpen) hud.DebugClosePremModal(); })); hud.StartCoroutine(After(0.5f, () => Shot($"v19_levelup_{lv}.png"))); }
        }

        // ------------------------------------------------------------------ zorunlu rehber (madde 11)
        void TutorialClicksV19(HudController hud)
        {
            if (!hud || !Tutorial.Active || Tutorial.UiTarget == null) return;
            int step = Tutorial.Step;
            if (_tutTry != step && _real > _tutStepAt + 1.5f)
            {
                // Önce hedef dışındaki bir düğmeyi dene: açılmamalı
                _tutTry = step;
                string wrong = step == Tutorial.StepDepot ? "open-quests" : "open-depot";
                string tabBefore = hud.DebugShopTab;
                bool depotBefore = hud.DebugDepotOpen, questsBefore = hud.DebugQuestsOpen;
                int blocked0 = HudController.DebugGateBlocked;
                bool allowed = hud.DebugClick(wrong);
                bool triedTab = !string.IsNullOrEmpty(tabBefore);
                if (triedTab) hud.DebugClick("tab-dec");
                _tutAt = _real;
                hud.StartCoroutine(After(0.3f, () => Log($"v1.9 REHBER KİLİT: adım {step + 1} · '{wrong}' izin={allowed} · depo açıldı={(hud.DebugDepotOpen && !depotBefore)} · görevler açıldı={(hud.DebugQuestsOpen && !questsBefore)}" +
                    $" · sekme '{tabBefore}' → '{hud.DebugShopTab}' (tab-dec denendi={triedTab}) · geri çevrilen dokunuş +{HudController.DebugGateBlocked - blocked0}")));
            }
            else if (_tutTry == step && _tutClick != step && _real > _tutAt + 1.0f)
            {
                _tutClick = step;
                string target = Tutorial.UiTarget;
                if (!string.IsNullOrEmpty(hud.DebugShopTab))
                {
                    if (step == Tutorial.StepStaff) target = "tab-staff";
                    else hud.DebugClick("shop-close");
                }
                if (hud.DebugQuestsOpen) hud.DebugOpen("close");
                bool ok = hud.DebugClick(target);
                Log($"v1.9 REHBER: bot '{target}' düğmesine bastı (adım {step + 1}, izin={ok})");
                if (step == Tutorial.StepQuests) hud.StartCoroutine(After(1.0f, () => hud.DebugOpen("close")));
            }
            else if (_tutClick == step && _tutFallback != step && _real > _tutAt + 5f)
            {
                // Olay düğmeye ulaşmadıysa (UI Toolkit sürümü) eski yolla aç ki koşu sürsün; günlükte HATA olarak görünür
                _tutFallback = step;
                Log($"v1.9 REHBER HATA: adım {step + 1} gerçek olayla geçmedi, DebugOpen ile açılıyor");
                hud.DebugOpen(step == Tutorial.StepQuests ? "quests" : step == Tutorial.StepShop ? "shop" : step == Tutorial.StepStaff ? "shop-staff" : "depot");
            }
        }

        int _tutFallback = -1;

        static string DspInfo() { AudioSettings.GetDSPBufferSize(out int len, out int n); return $"{len}×{n}"; }

        static System.Collections.IEnumerator After(float s, Action a)
        {
            yield return new WaitForSecondsRealtime(s);
            a();
        }

        // ------------------------------------------------------------------ görev otomatik tamamlama (madde 24)
        void QuestAutoTest()
        {
            if (_questTested || Tutorial.Active || _t < 15f || Quests.List.Count < 3) return;
            _questTested = true;
            // Zaten sahip olunan bir şey: Bira makinesi her zaman var; açılmış bir alan varsa onu hedefle
            string openZone = null;
            foreach (var z in PayZone.All) if (z && GameState.IsUnlocked(z.id)) { openZone = z.id; break; }
            var q1 = openZone != null ? Quests.MakeState(QuestType.UnlockZone, openZone) : Quests.MakeState(QuestType.OwnMachine, "", DrinkId.Bira);
            var notYet = Array.Find(Catalog.StaffList, s => !s.premium && !Management.Hired(s.id));
            var q2 = Quests.MakeState(QuestType.HireStaff, notYet.id ?? "staff_nana");
            var q3 = Quests.Make(QuestType.ServeDrink, DrinkId.Bira);
            Log($"v1.9 GÖREV OTOMATİK: \"{Quests.Text(q1)}\" oluşur oluşmaz tamam={q1.Done} · \"{Quests.Text(q2)}\" tamam={q2.Done} (henüz işe alınmadı) · sayaç \"{Quests.Text(q3)}\" {q3.progress}/{q3.target}");
            // Gerçek akış: listedeki ilk göreve işe alım görevi koy, görevli alınınca kendiliğinden tamamlanmalı
            Quests.List[2] = q2;
            if (!string.IsNullOrEmpty(notYet.id) && Management.CanHire(notYet) is Management.BuyResult.Ok or Management.BuyResult.NoMoney)
            {
                if (GameState.Coins < Management.HireCost(notYet)) GameState.AddCoins(Management.HireCost(notYet) - GameState.Coins, Vector3.zero);   // test parası
                var r = Management.Hire(notYet);
                Log($"v1.9 GÖREV OTOMATİK: {notYet.name} işe alındı ({r}) → \"{Quests.Text(Quests.List[2])}\" tamam={Quests.List[2].Done} · toplanabilir görev {Quests.ReadyCount}");
            }
            Log($"v1.9 GÖREV LİSTESİ: [{string.Join(" | ", Quests.List.ConvertAll(q => $"{Quests.Text(q)} {q.progress}/{q.target} ({q.rewardGems} elmas)"))}]");
        }

        // ------------------------------------------------------------------ borç (madde 7) + elmasla otomasyon (madde 9, 16)
        void DebtScenario(HudController hud)
        {
            if (!hud) return;
            if (!_autoGemTested && _t > 10f)
            {
                _autoGemTested = true;
                foreach (var d in Automation.List)
                    Log($"v1.9 OTOMASYON '{d.id}': altın {d.price} (Sv{d.level}) → {Automation.CanBuy(d)} · elmas {d.gems} (Sv{d.gemLevel}) → {Automation.CanBuyWithGems(d)}");
                var depo = Array.Find(Automation.List, a => a.id == "depo");
                if (!Automation.Has("depo"))
                {
                    int g0 = GameState.Data.gems;
                    if (g0 < depo.gems) GameState.Data.gems = depo.gems;   // test elması
                    var r = Automation.BuyWithGems(depo);
                    Log($"v1.9 OTOMASYON elmasla: depo → {r} · elmas {Math.Max(g0, depo.gems)} → {GameState.Data.gems} · açık={Automation.Has("depo")} · otomatik sipariş={GameState.Data.autoOrder}");
                }
            }
            switch (_debtStep)
            {
                case 0 when _t > 40f:
                {
                    // Maaşı garanti et: görevli yoksa en ucuz uygun görevliyi işe al
                    if (Management.DailyWages == 0)
                        foreach (var s in Catalog.StaffList)
                            if (!s.premium && Management.CanHire(s) is Management.BuyResult.Ok or Management.BuyResult.NoMoney) { GameState.Unlock(s.id); break; }
                    long w = Management.DailyWages;
                    long spend = GameState.Coins - w / 3;
                    if (spend > 0) GameState.TrySpend(spend);   // bot maaş öncesi parayı harcadı (test)
                    Log($"v1.9 BORÇ: maaş {w} · bot {Math.Max(0, spend)} altın harcadı · kasada {GameState.Coins} · gün sonuna atlanıyor");
                    DayCycle.DebugSetProgress(0.995f);
                    _debtStep = 1;
                    break;
                }
                case 1 when GameState.InDebt && !DayCycle.Ended:
                {
                    _debtStep = 2;
                    _debtAt = _t;
                    _debtMin = GameState.Coins;
                    var m = Array.Find(Catalog.Machines, x => Management.CanBuyMachine(x) != Management.BuyResult.Owned && Management.CanBuyMachine(x) != Management.BuyResult.LevelLow);
                    var rm = Management.BuyMachine(m);
                    bool spent = GameState.TrySpend(1);
                    var zone = PayZone.NextGoal();
                    Log($"v1.9 BORÇ: sabah bakiye {GameState.Coins} ({HudController.FormatNum(GameState.Coins)}) · borçlu={GameState.InDebt} · küskün={Management.Sulky} · makine alma → {rm} \"{Management.BuyMessage(rm)}\" · 1 altın harcama={spent} · hedef kare '{(zone ? zone.id : "-")}' ödeme açık={(zone && !GameState.InDebt)}");
                    hud.StartCoroutine(After(2.5f, () => { Shot("v19_debt_hud.png"); _debtShotHud = true; }));
                    break;
                }
                case 2:
                    _debtMin = Math.Min(_debtMin, GameState.Coins);
                    if (!_debtCleared && GameState.Coins >= 0)
                    {
                        _debtCleared = true;
                        Log($"v1.9 BORÇ kapandı: {_t - _debtAt:0} oyun-sn sonra · en düşük bakiye {_debtMin} · şimdi {GameState.Coins} · satın alma açık={!GameState.InDebt}");
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ ad değiştirme (elmas seçeneği)
        void RenameShot(HudController hud)
        {
            if (_renameShot || !hud || Tutorial.Active || _t < 8f) return;
            _renameShot = true;
            if (GameState.Data.gems < Balance.RenameGems) GameState.Data.gems = Balance.RenameGems;   // test elması (düğme etkin görünsün)
            hud.DebugOpen("rename");
            Log($"v1.9 AD DEĞİŞTİR: reklam {(Services.RewardedAdsEnabled ? "açık" : "kapalı (yayın taklidi)")} · altın {Balance.RenameCost} · elmas {Balance.RenameGems}");
            // Görüntü kare sonunda alınır: pencere aynı karede kapanırsa görüntüde yok (v1.6 v16_rel_rename de böyleydi)
            hud.StartCoroutine(After(0.8f, () => Shot("v19_rename.png")));
            hud.StartCoroutine(After(1.6f, () => hud.DebugOpen("close")));
        }
    }

    /// <summary>
    /// v1.9 test ölçeri: AudioListener'a takılır, son karışımın örneklerine bakar (tepe, |x| ≥ 1.0 örnek sayısı).
    /// Yalnızca geliştirme derlemesinde otomatik testte eklenir; oyuncu derlemesinde yok.
    /// </summary>
    public class AudioMeter : MonoBehaviour
    {
        static readonly float[] Buf = new float[1024];
        /// <summary>OnAudioFilterRead hiç gelmediyse (bazı aygıtlarda) dinleyicinin son çıkış örneklerinden tepe.</summary>
        public static void SampleOutput()
        {
            for (int ch = 0; ch < 2; ch++)
            {
                AudioListener.GetOutputData(Buf, ch);
                float p = 0f;
                foreach (var v in Buf) { float a = v < 0f ? -v : v; if (a > p) p = a; if (a >= 1f) OutputClipped++; }
                if (p > OutputPeak) OutputPeak = p;
            }
        }
        public static float OutputPeak;
        public static long OutputClipped;
        public static float Peak;
        public static long Clipped, Samples;
        static float _window;

        /// <summary>Son çağrıdan bu yana tepe (sonra sıfırlanır).</summary>
        public static float TakeWindowPeak() { float w = _window; _window = 0f; return w; }
        public static float Db(float v) => v > 0f ? 20f * Mathf.Log10(v) : -120f;

        void OnAudioFilterRead(float[] data, int channels)
        {
            float p = 0f;
            int c = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float a = data[i] < 0f ? -data[i] : data[i];
                if (a > p) p = a;
                if (a >= 1f) c++;
            }
            if (p > Peak) Peak = p;
            if (p > _window) _window = p;
            Clipped += c;
            Samples += data.Length;
        }
    }
}
#endif
