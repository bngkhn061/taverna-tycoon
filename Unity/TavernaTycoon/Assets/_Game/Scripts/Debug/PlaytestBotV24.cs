#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.4 Paket 6 test adımları (production/qa/playtests/feedback-2026-11-22.md), "-autotest-v24 &lt;mod&gt;":
    ///   audio  — dinleyici kaydı (AudioCapture): önce 14 sn "deneme tonu" (bütün sesler susar, 1 kHz ton; kamera Barlo'yu izlerken
    ///            perde oynuyor mu → Doppler), sonra 45 sn gerçek oyun karışımı. Zaman ölçeği 1 (gerçek hız), ses ayarı bellekte
    ///            varsayılan (0.70 / 0.85), hoparlöre ses gitmez. "-autotest-v24-tag önce|sonra" dosya adına eklenir.
    ///            Çözümleme: tools/audio/analyze_capture.py.
    ///   intro  — yeni oyun giriş sineması: yolculuk, harabe, saray, ferman, tabela kareleri; bitince rehber açık mı.
    ///   ending — "-autotest-v21 ending" ile birlikte: son sahnede kralın hediyesi (Kraliyet Fermanı) anı ve sonrasında duvardaki ferman.
    ///   daily  — (ana menüde) günlük ödül: 1. gün ve 7. gün görüntüsü (MainMenuController).
    /// </summary>
    public partial class PlaytestBot
    {
        string _v24Arg, _v24Tag;
        string V24 => _v24Arg ??= ArgAfter("-autotest-v24");
        string V24Tag => _v24Tag ??= ArgAfter("-autotest-v24-tag");

        bool _v24Init;
        float _v24At;
        readonly HashSet<string> _v24Done = new();

        bool V24Once(string key) => _v24Done.Add(key);
        float V24T => _real - _v24At;

        /// <summary>true: bu kare bot oynamaz.</summary>
        bool UpdateV24()
        {
            if (V24 == "") return false;
            if (!_v24Init)
            {
                _v24Init = true;
                _v24At = _real;
                _uiStep = UiSteps.Length; _langStep = 4; _tierStep = 9; _premStep = 4; _fireStep = 2;   // görüntüleri örten panel turları kapalı
                Log($"v2.4 BAŞLADI · mod '{V24}' · Sv{GameState.Level} · ses {AudioSettings.outputSampleRate} Hz · Doppler (ayar) kaynak {DopplerInfo()}");
                if (V24 == "audio") InitAudioV24();
            }
            if (V24 == "audio") AudioV24();
            if (V24 == "intro") return IntroV24();
            if (V24 == "ending") EndingV24();
            return false;
        }

        // ------------------------------------------------------------------ giriş sineması (madde 3)
        string _opPhase = "";
        float _opPhaseAt, _opStart = -1f, _opEnd = -1f;

        bool IntroV24()
        {
            var hud = _hud ? _hud : (_hud = FindAnyObjectByType<HudController>());
            if (!hud) return true;
            if (_opStart < 0f)
            {
                if (V24T < 2f) return true;
                _opStart = _real;
                Log($"v2.4 GİRİŞ başlıyor: ad '{GameState.TavernName}' · kayıtta görüldü={GameState.Data.openingSeen} · cihazda={Opening.DeviceSeen} · kendiliğinden oynar mı (yeni oyun)={Opening.ShouldAutoPlay(GameState.Data, true, false)} · şube {Prestige.Branch}");
                hud.DebugStartOpening(false);
                Log($"v2.4 GİRİŞ: oynuyor={Opening.Playing} · {hud.OpeningDebug}");
            }
            string ph = hud.OpeningPhase;
            if (ph != _opPhase)
            {
                Log($"v2.4 GİRİŞ ÇEKİM: {(_opPhase == "" ? "-" : _opPhase)} → {(ph == "" ? "BİTTİ" : ph)} · {_real - _opStart:0.0} sn · {hud.OpeningDebug} · yazı \"{hud.OpeningCaption}\" · gün {DayCycle.Progress01:0.000} · müşteri {Customer.All.Count}");
                _opPhase = ph; _opPhaseAt = _real;
                if (ph == "" && _opEnd < 0f) { _opEnd = _real; Log($"v2.4 GİRİŞ BİTTİ: toplam {_opEnd - _opStart:0.0} sn · kayıtta görüldü={GameState.Data.openingSeen} · cihazda={Opening.DeviceSeen} · oynuyor={Opening.Playing} · zaman ölçeği {Time.timeScale}"); }
            }
            float at = _real - _opPhaseAt;
            void ShotOnce(string file, float after) { if (at > after && V24Once(file)) { Shot(file); Log($"v2.4 GÖRÜNTÜ {file} · yazı \"{hud.OpeningCaption}\" · ferman kartı={hud.DecreeCardVisible}"); } }
            switch (ph)
            {
                case "Journey": ShotOnce("v24_intro_1_yolculuk.png", 3.6f); break;
                case "Ruin": ShotOnce("v24_intro_2_harabe.png", 2.6f); break;
                case "Palace": ShotOnce("v24_intro_3_saray.png", 1.8f); ShotOnce("v24_intro_3b_egilme.png", 4.3f); break;
                case "Decree": ShotOnce("v24_intro_4_ferman.png", 3.0f); break;
                case "Sign": ShotOnce("v24_intro_5_tabela.png", 3.0f); break;
            }
            if (_opEnd > 0f)
            {
                float f = _real - _opEnd;
                if (f > 3.2f && V24Once("after")) { Shot("v24_intro_6_rehber.png"); Log($"v2.4 GİRİŞ SONRASI: rehber açık={Tutorial.Active} adım {Tutorial.Step + 1} · HUD görünür · zaman ölçeği {Time.timeScale}"); }
                if (f > 6f && V24Once("replay")) { hud.DebugStartOpening(true); Log($"v2.4 YENİDEN İZLE (Ayarlar): oynuyor={Opening.Playing} · {hud.OpeningDebug}"); }
                if (f > 7.5f && V24Once("skipshot")) Shot("v24_intro_7_atla.png");
                if (f > 8f && V24Once("skip")) { hud.DebugSkipOpening(); Log($"v2.4 ATLA: oynuyor={Opening.Playing} · çekim '{hud.OpeningPhase}' · zaman ölçeği {Time.timeScale} · kayıtta görüldü={GameState.Data.openingSeen}"); }
                if (f > 9.5f && V24Once("settings")) { ClearUi(hud); hud.DebugOpen("settings"); }
                if (f > 10.7f && V24Once("settingsshot")) Shot("v24_intro_8_ayarlar.png");
                if (f > 11.5f && V24Once("settingsclose")) { hud.DebugCloseSettings(); var copy = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(GameState.Data)); Log($"v2.4 PRESTİJ SONRASI giriş tekrar oynar mı: {Opening.ShouldAutoPlay(copy, true, Opening.DeviceSeen)} (kayıt görüldü={copy.openingSeen}, cihaz={Opening.DeviceSeen}) · yeni kayıtla (cihaz gördü) {Opening.ShouldAutoPlay(new SaveData(), true, Opening.DeviceSeen)}"); }
            }
            return Opening.Playing;
        }

        // ------------------------------------------------------------------ son sahne: kralın hediyesi (madde 3)
        float _giftAt = -1f, _endFreeAt = -1f;
        string _endPh = "";

        void EndingV24()
        {
            var hud = _hud ? _hud : (_hud = FindAnyObjectByType<HudController>());
            if (!hud) return;
            string ph = hud.EndingPhase;
            if (ph != _endPh)
            {
                if (ph == "Gift") { _giftAt = _real; Log($"v2.4 HEDİYE başladı: hediye kayıtta={RoyalGift.Has} · duvarda görünür={(RoyalDecreeDecor.I && RoyalDecreeDecor.I.visual && RoyalDecreeDecor.I.visual.activeInHierarchy)} · kral modeli {(CinemaProps.I && CinemaProps.I.king ? CinemaProps.I.king.name : "yok")}"); }
                if (_endPh != "" && ph == "" ) _endFreeAt = _real;
                _endPh = ph;
            }
            if (_giftAt > 0f)
            {
                float g = _real - _giftAt;
                if (g > 1.0f && V24Once("gift1")) { Shot("v24_end_1_hediye_kral.png"); Log($"v2.4 GÖRÜNTÜ v24_end_1_hediye_kral.png · hediye kartı={hud.GiftCardVisible}"); }
                if (g > 3.6f && V24Once("gift2")) { Shot("v24_end_2_hediye_duvar.png"); Log($"v2.4 GÖRÜNTÜ v24_end_2_hediye_duvar.png · duvarda görünür={(RoyalDecreeDecor.I && RoyalDecreeDecor.I.visual.activeInHierarchy)} · hediye kartı={hud.GiftCardVisible}"); }
            }
            if (_endFreeAt > 0f)
            {
                float f = _real - _endFreeAt;
                if (f > 1.5f && V24Once("bonus"))
                {
                    var copy = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(GameState.Data));
                    float before = RoyalGift.IncomeMultiplierFor(copy);
                    Prestige.ResetForBranch(copy, copy.branch + 1);
                    Log($"v2.4 HEDİYE SONRASI: kayıtta={RoyalGift.Has} · gelir çarpanı ×{Boosts.IncomeMultiplier:0.000} (ferman ×{RoyalGift.IncomeMultiplier:0.00}) · kostüm 'Kral'ın Dostu' sahip={Costumes.Owned(GameState.Data, Costumes.Find("barlo_kral"))} · Efsanevi Han={Ending.Seen} · yeni şube kopyasında ferman={copy.royalDecree} ×{RoyalGift.IncomeMultiplierFor(copy):0.00} (önce ×{before:0.00}) · şube {copy.branch} Sv{copy.level}");
                }
                if (f > 2.5f && V24Once("wallcam"))
                {
                    _v15Visit = RoyalDecreeDecor.I ? new Vector3(RoyalDecreeDecor.I.transform.position.x, 0f, 2.6f) : (Vector3?)null;
                }
                if (f > 7f && V24Once("wallshot")) { Shot("v24_end_3_duvarda_ferman.png"); Log("v2.4 GÖRÜNTÜ v24_end_3_duvarda_ferman.png (serbest oyunda tezgâhın üstü)"); }
            }
        }

        // ------------------------------------------------------------------ ses kaydı (madde 2)
        AudioCapture _cap;
        string _capPhase = "";
        Vector3 _lisLast;
        float _lisMax, _lisSum;
        int _lisN, _capSkip0;
        float _capReal0;

        static string DopplerInfo()
        {
            var all = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            float maxD = 0f, maxBlend = 0f;
            foreach (var s in all) { maxD = Mathf.Max(maxD, s.dopplerLevel); maxBlend = Mathf.Max(maxBlend, s.spatialBlend); }
            return $"{all.Length} kaynak · en yüksek dopplerLevel {maxD:0.00} · en yüksek spatialBlend {maxBlend:0.00}";
        }

        void InitAudioV24()
        {
            GameSettings.MusicOverride = 0.7f;
            GameSettings.SfxOverride = 0.85f;
            var l = FindAnyObjectByType<AudioListener>();
            // ara ölçüm: v2.3 sahnesine yalnızca dinleyici eklenmiş hâli (Doppler ayarları değişmeden)
            if (!l && HasArg("-autotest-v24-listener") && Camera.main) { l = Camera.main.gameObject.AddComponent<AudioListener>(); Log("v2.4 SES ara ölçüm: kameraya AudioListener eklendi (Doppler ayarı aynı)"); }
            _cap = AudioCapture.I;
            if (_cap && l && !HasArg("-autotest-v24-tap")) _cap.UseListener(l);   // dinleyici varsa tam ölçek süzgeç kaydı
            AudioSettings.GetDSPBufferSize(out int len, out int n);
            Log($"v2.4 SES kayıt hazır: kaydedici {(!_cap ? "YOK (-autotest-hidden gerekli)" : _cap.FilterMode ? "dinleyici süzgeci (OnAudioFilterRead, tam ölçek)" : "çıkış dikişi (GetOutputData + dspTime, ana düzey −60 dB)")} · sahnede AudioListener {(l ? l.name + (l.enabled ? "" : " (kapalı)") : "YOK")} · DSP {len}×{n} · gerçek ses sınırı {AudioSettings.GetConfiguration().numRealVoices} · sanal {AudioSettings.GetConfiguration().numVirtualVoices} · etiket '{V24Tag}'");
        }

        void AudioV24()
        {
            Time.timeScale = DayCycle.Ended ? 0f : 1f;   // gerçek hız (×4 test hızında kamera da 4 kat hızlı gider, Doppler abartılı olurdu)
            float t = V24T;
            var lis = FindAnyObjectByType<AudioListener>();
            var l = lis ? lis.transform : Camera.main ? Camera.main.transform : null;
            if (l && Time.unscaledDeltaTime > 0f)
            {
                float v = (l.position - _lisLast).magnitude / Time.unscaledDeltaTime;
                if (_lisLast != Vector3.zero && v < 200f && _capPhase != "") { _lisMax = Mathf.Max(_lisMax, v); _lisSum += v; _lisN++; }
                _lisLast = l.position;
            }
            string tag = string.IsNullOrEmpty(V24Tag) ? "" : "_" + V24Tag;
            if (t > 6f && V24Once("probe-on")) { Sfx.ProbeMode = true; Log("v2.4 SES deneme tonu açık (diğer sesler sustu)"); }
            if (t > 7f && V24Once("probe-rec") && _cap) { _cap.Begin(14f); _capPhase = "probe"; ResetLis(); }
            if (t > 21.5f && V24Once("probe-end") && _cap)
            {
                var path = _cap.End($"v24_audio_probe{tag}.wav");
                Log($"v2.4 SES deneme kaydı: {path} · {_cap.Seconds:0.0} sn · kamera hızı ort {(_lisN > 0 ? _lisSum / _lisN : 0f):0.00} m/sn, en yüksek {_lisMax:0.00} m/sn · {CapInfo()}");
                Sfx.ProbeMode = false;
                _capPhase = "";
            }
            if (t > 24f && V24Once("mix-rec") && _cap) { _cap.Begin(45f); _capPhase = "mix"; ResetLis(); _capSkip0 = Sfx.SkippedFx; }
            if (_capPhase == "mix" && (int)(t * 2f) % 20 == 0 && V24Once("mixlog" + (int)t))
                Log($"v2.4 SES karışım: müşteri {Customer.All.Count} · çalan kaynak {PlayingSources()} · atlanan efekt +{Sfx.SkippedFx - _capSkip0} · müzik {Sfx.MusicNow} · ortam [{Sfx.AmbientInfo}]");
            if (t > 69.5f && V24Once("mix-end") && _cap)
            {
                var path = _cap.End($"v24_audio_mix{tag}.wav");
                Log($"v2.4 SES karışım kaydı: {path} · {_cap.Seconds:0.0} sn · dinleyici hızı ort {(_lisN > 0 ? _lisSum / _lisN : 0f):0.00} m/sn, en yüksek {_lisMax:0.00} m/sn · atlanan efekt +{Sfx.SkippedFx - _capSkip0} · {DopplerInfo()} · {CapInfo()}");
                _capPhase = "";
                GameSettings.MusicOverride = GameSettings.SfxOverride = null;
            }
        }

        void ResetLis() { _lisMax = _lisSum = 0f; _lisN = 0; _capReal0 = _real; }

        string CapInfo() => $"kayıt {_cap.Seconds:0.0} sn / gerçek {_real - _capReal0:0.0} sn · tekrar okuma {_cap.Retries} · boşluk {_cap.Gaps} · ham tepe {_cap.RawPeak:0.0000000} · geri çarpıldı={_cap.Rescaled}";

        static int PlayingSources()
        {
            int n = 0;
            foreach (var s in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None)) if (s.isPlaying && s.volume > 0.001f) n++;
            return n;
        }
    }
}
#endif
