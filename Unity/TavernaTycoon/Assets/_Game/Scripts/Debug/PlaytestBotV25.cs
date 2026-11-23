#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.5 cila test adımları, "-autotest-v25 &lt;mod&gt;" (GAME_DESIGN.md §19):
    ///   hud        — sade HUD ölçümü: sol blok ve araç kutusu ölçüleri, görünen "YENİ!" sayısı (en çok 1), tam / uzak satın alma etiketi
    ///                sayısı; ETKİNLİK ve DÜZENLE'ye birer kez basılır, rozetler kalkıyor mu. Görüntüler v25_hud_*.png.
    ///   endshots   — son sahne "yeniden izle" ile o anki kademede ("-autotest-tier N") ve şubede ("-autotest-v25-branch N") oynar; her
    ///                çekimin görüntüsü ve kameranın önünden kaldırılan nesneler günlüğe (tabela önü kapatmasın).
    ///   rec-intro  — giriş sineması, rec-ending — son sahne (+ "-autotest-v21 ending"), jenerik tam; rec-game — gerçek hızda oyun.
    ///                <see cref="VideoRecorder"/> kare + dinleyici sesi yazar ("-autotest-v25-out klasör"; varsayılan captures/v25_rec_*).
    ///                Ses ayarı bellekte 0.70 / 0.85 (kayıtlı ayar değişmez), hoparlöre ses gitmez ("-autotest-hidden" gerekli).
    /// </summary>
    public partial class PlaytestBot
    {
        string _v25Arg;
        string V25 => _v25Arg ??= ArgAfter("-autotest-v25");
        bool RecMode => V25.StartsWith("rec-") || V25 == "offtest";

        bool _v25Init;
        float _v25At;
        readonly HashSet<string> _v25Done = new();
        bool V25Once(string key) => _v25Done.Add(key);
        float V25T => _real - _v25At;

        /// <summary>true: bu kare bot oynamaz.</summary>
        bool UpdateV25()
        {
            if (V25 == "") return false;
            var hud = _hud ? _hud : (_hud = FindAnyObjectByType<HudController>());
            if (!_v25Init)
            {
                _v25Init = true;
                _v25At = _real;
                _uiStep = UiSteps.Length; _langStep = 4; _tierStep = 9; _premStep = 4; _fireStep = 2;   // görüntüyü örten panel turları kapalı
                Log($"v2.5 BAŞLADI · mod '{V25}' · Sv{GameState.Level} · kademe {Tiers.CurrentIndex} · şube {Prestige.Branch} · ekran {Screen.width}×{Screen.height}");
                if (RecMode) InitRecV25();
                if (V25 == "endshots")
                {
                    GameState.Data.royalDecree = true;   // hediye anında duvarda ferman görünsün
                    if (int.TryParse(ArgAfter("-autotest-v25-branch"), out int br)) GameState.Data.branch = br;
                }
            }
            if (RecMode) Time.timeScale = DayCycle.Ended ? 0f : 1f;   // kayıt gerçek hızda (ses ile görüntü aynı saatte)
            switch (V25)
            {
                case "hud": HudV25(hud); return false;
                case "endshots": return EndShotsV25(hud);
                case "rec-intro": return RecIntroV25(hud);
                case "rec-ending": RecEndingV25(hud); return false;
                case "rec-game": RecGameV25(hud); return false;
                case "offtest": OffTestV25(hud); return false;
            }
            return false;
        }

        // ------------------------------------------------------------------ sade HUD
        void HudV25(HudController hud)
        {
            if (!hud) return;
            float t = V25T;
            void State(string tag)
            {
                var (full, far) = hud.DebugZoneTagCounts;
                Log($"v2.5 HUD {tag}: {hud.DebugLeftLayout} · YENİ! görünen {hud.DebugNewBadgeCount} · etiket tam {full} / uzak {far} · düzenle görünür={hud.EditButtonVisible} · olaylar görüldü={GameState.Data.eventsSeen}");
            }
            if (t > 7f && V25Once("s1")) { State("ilk"); Shot("v25_hud_1.png"); }
            if (t > 9f && V25Once("ev")) { ClearUi(hud); hud.DebugEventPress(); Log("v2.5 HUD: ETKİNLİK'e basıldı"); }
            if (t > 10.5f && V25Once("evclose")) { ClearUi(hud); State("etkinlikten sonra"); }
            if (t > 11.5f && V25Once("ed")) { hud.DebugEditPress(); Log($"v2.5 HUD: DÜZENLE'ye basıldı · düzenleme açık={EditLayout.Active}"); }
            if (t > 13f && V25Once("edclose")) { if (EditLayout.Active) EditLayout.I.Close(false); }
            if (t > 15f && V25Once("s2")) { State("ikisinden sonra"); Shot("v25_hud_2.png"); }
        }

        // ------------------------------------------------------------------ son sahne çekimleri (her kademe / şube)
        string _esPhase = "";
        float _esPhaseAt, _esStart = -1f;

        bool EndShotsV25(HudController hud)
        {
            if (!hud) return true;
            if (_esStart < 0f)
            {
                if (V25T < 4f || Tutorial.Active) return false;
                ClearUi(hud);
                _esStart = _real;
                hud.DebugReplayEnding();
                Log($"v2.5 SON SAHNE (yeniden izle) · kademe {Tiers.CurrentIndex} · şube {Prestige.Branch} · oynuyor={Ending.EndingPlaying}");
            }
            string ph = hud.EndingPhase;
            if (ph != _esPhase)
            {
                Log($"v2.5 SON ÇEKİM: {(_esPhase == "" ? "-" : _esPhase)} → {(ph == "" ? "BİTTİ" : ph)} · {_real - _esStart:0.0} sn · gizlenen {hud.EndingOccluders} · tabela adı görünür={SignTier.BoardVisible}");
                _esPhase = ph; _esPhaseAt = _real;
            }
            float at = _real - _esPhaseAt;
            string tg = $"t{Tiers.CurrentIndex}b{Prestige.Branch}";
            void ShotOnce(string name, float after)
            {
                if (at > after && V25Once(name)) { Shot($"v25_end_{tg}_{name}.png"); Log($"v2.5 GÖRÜNTÜ v25_end_{tg}_{name}.png · gizlenen {hud.EndingOccluders}"); }
            }
            switch (ph)
            {
                case "Arrive": ShotOnce("1_gelis", 3.5f); break;
                case "Greet": ShotOnce("2_karsilama", 1.4f); break;
                case "ToTable": ShotOnce("3_masaya", 0.8f); break;
                case "Toast": ShotOnce("4_kadeh", 1.8f); break;
                case "Gift": ShotOnce("5_hediye_kral", 1.0f); ShotOnce("6_hediye_duvar", 3.6f); break;
                case "Orbit": ShotOnce("7_tur", 1.6f); ShotOnce("8_tur_son", 7.5f); break;
                case "credits": if (at > 2f && V25Once("tap")) hud.DebugEndingTap(); break;
            }
            return Ending.EndingPlaying;
        }

        // ------------------------------------------------------------------ video kaydı
        VideoRecorder _rec;
        string _recDir;
        double _recEndAt = -1;
        bool _recQuit;

        void InitRecV25()
        {
            GameSettings.MusicOverride = 0.7f;
            GameSettings.SfxOverride = 0.85f;
            var l = FindAnyObjectByType<AudioListener>();
            var cap = AudioCapture.I;
            if (cap && l) cap.UseListener(l);
            var o = ArgAfter("-autotest-v25-out");
            _recDir = !string.IsNullOrEmpty(o) ? o : Path.Combine(AutoTest.CaptureDir, "v25_" + V25.Replace('-', '_'));
            Log($"v2.5 KAYIT hazır: klasör {_recDir} · ses {(cap ? (cap.FilterMode ? "dinleyici süzgeci (hoparlöre gitmez)" : "çıkış dikişi") : "YOK (-autotest-hidden gerekli)")} · dinleyici {(l ? l.name : "YOK")} · {AudioSettings.outputSampleRate} Hz");
        }

        void RecStart(string what)
        {
            if (_rec) return;
            _rec = VideoRecorder.Begin(_recDir, 90f, AudioCapture.I);
            Log($"v2.5 KAYIT BAŞLADI: {what} · gerçek {_real:0.00} sn · müzik {Sfx.MusicNow}");
        }

        /// <summary>Kaydı <paramref name="after"/> sn sonra bitirir (bir kez); dosyalar yazılınca uygulama kapanır.</summary>
        void RecStopAfter(float after)
        {
            if (!_rec || _recEndAt >= 0) return;
            _recEndAt = _rec.Elapsed + after;   // kayıt zamanı (penceresiz kipte kare sayısından, yoksa gerçek saat)
        }

        void RecTick()
        {
            if (_rec && _recEndAt >= 0 && _rec.Elapsed >= _recEndAt && !_recQuit)
            {
                _recQuit = true;
                _rec.End(info =>
                {
                    Log($"v2.5 KAYIT BİTTİ: {info}");
                    GameSettings.MusicOverride = GameSettings.SfxOverride = null;
                    File.WriteAllText(Path.Combine(_recDir, "log.txt"), _log.ToString());
                    Application.Quit();
                });
            }
        }

        /// <summary>Penceresiz kip denemesi: 3 sn sonra bir görüntü ve 5 sn'lik görüntü + ses kaydı, sonra çıkış.</summary>
        void OffTestV25(HudController hud)
        {
            if (V25T > 3f && V25Once("shot"))
            {
                Shot("v25_offtest.png");
                Log($"v2.5 PENCERESİZ: {(OffscreenCapture.I ? OffscreenCapture.I.Info : "kapalı")} · HUD {(hud ? "var" : "yok")} · müşteri {Customer.All.Count} · müzik {Sfx.MusicNow}");
            }
            if (V25T > 3.5f && !_rec) { RecStart("penceresiz deneme"); RecStopAfter(5f); }
            RecTick();
        }

        bool RecIntroV25(HudController hud)
        {
            if (!hud) return true;
            if (!_rec)
            {
                if (V25T < 2.5f) return true;
                RecStart("giriş sineması");
                hud.DebugStartOpening(false);
                _opPhase = "";
            }
            string ph = hud.OpeningPhase;
            if (ph != _opPhase)
            {
                Log($"v2.5 GİRİŞ ÇEKİM: {(_opPhase == "" ? "-" : _opPhase)} → {(ph == "" ? "BİTTİ" : ph)} · kayıt {_rec.Elapsed:0.00} sn · yazı \"{hud.OpeningCaption}\"");
                _opPhase = ph;
                if (ph == "") RecStopAfter(4.5f);   // sonra "GÜN 1" şeridi ve rehberin başı
            }
            RecTick();
            return Opening.Playing;   // sinemadan sonra bot rehberi oynar (Barlo bira fıçısına yürür)
        }

        void RecEndingV25(HudController hud)
        {
            if (!hud) return;
            if (!_rec && Ending.EndingPlaying) RecStart("son sahne");
            if (_rec)
            {
                string ph = hud.EndingPhase;
                if (ph != _endPh)
                {
                    Log($"v2.5 SON ÇEKİM: {(_endPh == "" ? "-" : _endPh)} → {(ph == "" ? "BİTTİ" : ph)} · kayıt {_rec.Elapsed:0.00} sn · gizlenen {hud.EndingOccluders}");
                    _endPh = ph;
                    if (ph == "") RecStopAfter(3f);   // "EFSANEVİ HAN" şeridi ve serbest oyunun başı
                }
            }
            RecTick();
        }

        void RecGameV25(HudController hud)
        {
            if (!hud) return;
            if (hud.DebugModalOpen || hud.LevelCardOpen) ClearUi(hud);   // tanıtım penceresi / seviye kartı videoyu örtmesin
            if (!_rec && V25T > 8f && !Tutorial.Active) { RecStart("oyun"); RecStopAfter(38f); }
            if (_rec && _recEndAt >= 0 && _rec.Elapsed > 19f && V25Once("mid"))
                Log($"v2.5 OYUN (kayıt ortası): müşteri {Customer.All.Count} · müzik {Sfx.MusicNow} · ortam [{Sfx.AmbientInfo}] · çalan kaynak {PlayingSources()}");
            RecTick();
        }
    }
}
#endif
