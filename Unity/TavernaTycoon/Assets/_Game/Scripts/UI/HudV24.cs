using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v2.4 Paket 6 arayüzü (production/qa/playtests/feedback-2026-11-22.md, GAME_DESIGN.md §18):
    /// giriş sinemasının yazıları, siyah geçişleri, "ATLA" düğmesi ve Kraliyet Fermanı kartı (oyuncunun seçtiği adla);
    /// son sahnede kralın hediyesi kartı; Ayarlar › "Girişi yeniden izle".
    /// </summary>
    public partial class HudController
    {
        /// <summary>Menüden yeni oyunla gelindi ve giriş sineması oynayacak (MainMenuController ayarlar).</summary>
        public static bool PlayOpening;

        OpeningScene _opScene;
        VisualElement _opRoot, _opFade, _decreeCard, _giftCard;
        Label _opCaption;
        Button _opSkip;
        bool _opReplay, _opPalaceLine, _opWorldShown;
        float _opPrevScale = 1f;
        readonly List<VisualElement> _opHiddenWorld = new();

        void InitV24()
        {
            _settings.WatchOpeningRequested += () =>
            {
                _settings.Hide();
                if (DayCycle.Ended || DayCycle.Closing || Ending.Playing) { Toast(Loc.T("Yeni gün başlayınca izlenebilir")); return; }
                StartOpening(true);
            };
            if (PlayOpening) { PlayOpening = false; StartOpening(false); }
        }

        void DisableV24()
        {
            if (_opScene) { _opScene.Cleanup(); _opScene = null; }
            if (Opening.Playing) { Opening.SetPlaying(false); Time.timeScale = _opPrevScale; }
        }

        // ------------------------------------------------------------------ giriş sineması
        /// <summary>Test botu: çekim adı ("" = oynamıyor).</summary>
        public string OpeningPhase => _opScene ? _opScene.Current.ToString() : "";
        public string OpeningDebug => _opScene ? _opScene.DebugInfo : "-";
        public float OpeningPhaseTime => _opScene ? _opScene.PhaseTime : 0f;
        public bool DecreeCardVisible => _decreeCard != null && !_decreeCard.ClassListContains("hidden");
        public bool GiftCardVisible => _giftCard != null && _giftCard.parent != null;
        public string OpeningCaption => _opCaption != null ? _opCaption.text : "";
        public void DebugStartOpening(bool replay) => StartOpening(replay);
        public void DebugSkipOpening() => FinishOpening(true);

        void StartOpening(bool replay)
        {
            if (Opening.Playing || Ending.Playing || !CinemaProps.I) return;
            DebugOpen("close");
            _opReplay = replay;
            Opening.SetPlaying(true);
            PlayerController.JoystickInput = Vector2.zero;
            _opPrevScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 1f;
            _introT = -1f;   // v0.9 kısa giriş (kamera kayması) sinemayla çakışmasın
            _hudRoot.style.display = DisplayStyle.None;
            _world.style.display = DisplayStyle.None;
            BuildOpeningUi();
            _opScene = OpeningScene.Play(cam);
            if (!_opScene) { FinishOpening(true); return; }
            _opScene.PhaseChanged += OnOpeningPhase;
            OnOpeningPhase(OpeningScene.Phase.Journey);
            Services.Track("opening_start", ("tekrar", replay ? 1 : 0));
        }

        void BuildOpeningUi()
        {
            _opRoot?.RemoveFromHierarchy();
            _opRoot = new VisualElement { pickingMode = PickingMode.Ignore };
            _opRoot.AddToClassList("screen");
            _opRoot.AddToClassList("end-root");
            var top = new VisualElement { pickingMode = PickingMode.Ignore };
            top.AddToClassList("end-bar");
            top.AddToClassList("top");
            var bottom = new VisualElement { pickingMode = PickingMode.Ignore };
            bottom.AddToClassList("end-bar");
            bottom.AddToClassList("bottom");
            _opCaption = new Label { pickingMode = PickingMode.Ignore };
            _opCaption.AddToClassList("end-caption");
            bottom.Add(_opCaption);
            _opFade = new VisualElement { pickingMode = PickingMode.Ignore };
            _opFade.AddToClassList("screen");
            _opFade.AddToClassList("op-fade");
            _opRoot.Add(top);
            _opRoot.Add(bottom);
            // Kraliyet Fermanı kartı (Decree çekiminde)
            _decreeCard = new VisualElement { pickingMode = PickingMode.Ignore };
            _decreeCard.AddToClassList("decree-card");
            _decreeCard.AddToClassList("hidden");
            var title = new Label(Loc.Upper(Loc.T("Kraliyet Fermanı"))) { pickingMode = PickingMode.Ignore };
            title.AddToClassList("decree-title");
            var body = new Label(Loc.F("Bu fermanla {0} adlı tavernanın açılmasına izin verilmiştir.", GameState.TavernName)) { pickingMode = PickingMode.Ignore };
            body.AddToClassList("decree-body");
            var joke = new Label(Loc.T("Bira bol, kavga az olsun. Kral'ın birası hep soğuk olsun!")) { pickingMode = PickingMode.Ignore };
            joke.AddToClassList("decree-joke");
            var foot = new VisualElement { pickingMode = PickingMode.Ignore };
            foot.AddToClassList("decree-foot");
            var por = new VisualElement { pickingMode = PickingMode.Ignore };
            por.AddToClassList("decree-por");
            por.style.backgroundImage = new StyleBackground(LoadIcon("King"));
            var sign = new Label(Loc.T("— Kral, mührüyle")) { pickingMode = PickingMode.Ignore };
            sign.AddToClassList("decree-sign");
            var seal = new VisualElement { pickingMode = PickingMode.Ignore };
            seal.AddToClassList("decree-seal");
            foot.Add(por); foot.Add(sign); foot.Add(seal);
            _decreeCard.Add(title); _decreeCard.Add(body); _decreeCard.Add(joke); _decreeCard.Add(foot);
            _opRoot.Add(_decreeCard);
            _opRoot.Add(_opFade);
            _opSkip = new Button(() => FinishOpening(true)) { name = "opening-skip" };
            _opSkip.AddToClassList("btn");
            _opSkip.AddToClassList("btn-paper");
            _opSkip.AddToClassList("op-skip");
            _opSkip.Add(new Label(Loc.T("ATLA")));
            _opRoot.Add(_opSkip);
            _root.Add(_opRoot);
        }

        void OnOpeningPhase(OpeningScene.Phase p)
        {
            string name = GameState.TavernName;
            switch (p)
            {
                case OpeningScene.Phase.Journey: _opCaption.text = Loc.T("Barlo bütün eşyasını el arabasına yükledi ve şehrin yolunu tuttu."); break;
                case OpeningScene.Phase.Ruin: _opCaption.text = Loc.T("Şehirde boş, harap bir han buldu. Çatısı akıyordu ama ruhu vardı!"); break;
                case OpeningScene.Phase.Palace: _opPalaceLine = false; _opCaption.text = Loc.T("Taverna açmak için Kral'ın izni gerekir. Barlo saraya çıktı."); break;
                case OpeningScene.Phase.Decree: _opCaption.text = Loc.F("Kral fermanı imzaladı: \"{0} açılsın!\"", name); break;
                case OpeningScene.Phase.Sign: _decreeCard.AddToClassList("hidden"); _opCaption.text = Loc.F("Barlo tabelayı astı. {0} kapılarını açtı!", name); break;
                case OpeningScene.Phase.Done: FinishOpening(false); break;
            }
        }

        void UpdateV24()
        {
            if (!_opScene) return;
            PlayerController.JoystickInput = Vector2.zero;
            _tutLayer.style.display = DisplayStyle.None;
            _opFade.style.opacity = _opScene.Fade;
            float t = _opScene.PhaseTime;
            switch (_opScene.Current)
            {
                case OpeningScene.Phase.Palace when !_opPalaceLine && t > 3.7f:
                    _opPalaceLine = true;
                    _opCaption.text = Loc.T("\"Majesteleri, krallığın en sıcak tavernasını açmak istiyorum!\"");
                    break;
                case OpeningScene.Phase.Decree:
                    bool show = t > 1.1f && t < 5.9f;
                    if (show == _decreeCard.ClassListContains("hidden"))
                    {
                        _decreeCard.EnableInClassList("hidden", !show);
                        if (show) { Sfx.Play(SfxId.Pop); Confetti(14); }
                    }
                    _decreeCard.style.scale = new Scale(Vector3.one * Mathf.Lerp(0.85f, 1f, Mathf.Clamp01((t - 1.1f) / 0.25f)));
                    break;
                case OpeningScene.Phase.Sign:
                    // tabela yerine oturunca yalnızca tabeladaki ad görünür (diğer dünya etiketleri gizli)
                    if (_opScene.SignShown && !_opWorldShown)
                    {
                        _opWorldShown = true;
                        _opHiddenWorld.Clear();
                        // dünya katmanı HUD kökünün içinde: kök açılır, kökte dünya katmanı dışındaki her şey ve katmanda tabela adı dışındaki her şey gizli
                        foreach (var c in _hudRoot.Children()) if (c != _world && c.visible) { c.visible = false; _opHiddenWorld.Add(c); }
                        foreach (var c in _world.Children()) if (c != _signLabel && c.visible) { c.visible = false; _opHiddenWorld.Add(c); }
                        _world.style.display = DisplayStyle.Flex;
                        _hudRoot.style.display = DisplayStyle.Flex;
                        Confetti(30);
                        GameSettings.Vibrate();
                    }
                    break;
            }
        }

        void FinishOpening(bool skipped)
        {
            if (_opScene) { _opScene.PhaseChanged -= OnOpeningPhase; _opScene.Cleanup(); _opScene = null; }
            _opRoot?.RemoveFromHierarchy();
            _opRoot = null;
            foreach (var c in _opHiddenWorld) c.visible = true;
            _opHiddenWorld.Clear();
            _opWorldShown = false;
            Opening.SetPlaying(false);
            Time.timeScale = _opPrevScale;
            _hudRoot.style.display = DisplayStyle.Flex;
            _world.style.display = DisplayStyle.Flex;
            _tutLayer.style.display = StyleKeyword.Null;
            OnTutorialChanged();
            Services.Track("opening_done", ("atlandi", skipped ? 1 : 0), ("tekrar", _opReplay ? 1 : 0));
            if (!_opReplay)
            {
                Opening.MarkSeen(GameState.Data);
                GameState.Save();
                StartIntro();   // v0.9 kısa açılış: "TAVERNA · GÜN 1" şeridi, HUD kayarak gelir, rehber başlar
            }
        }

        // ------------------------------------------------------------------ son sahne: kralın hediyesi
        void ShowGiftCard()
        {
            HideGiftCard();
            if (_endRoot == null) return;
            _giftCard = new VisualElement { pickingMode = PickingMode.Ignore };
            _giftCard.AddToClassList("gift-card");
            var title = new Label(Loc.Upper(Loc.T("Kral'ın hediyesi"))) { pickingMode = PickingMode.Ignore };
            title.AddToClassList("gift-title");
            _giftCard.Add(title);
            void Row(Sprite icon, string text)
            {
                var r = new VisualElement { pickingMode = PickingMode.Ignore };
                r.AddToClassList("gift-row");
                var ic = new VisualElement { pickingMode = PickingMode.Ignore };
                ic.AddToClassList("gift-ic");
                ic.style.backgroundImage = new StyleBackground(icon);
                var l = new Label(text) { pickingMode = PickingMode.Ignore };
                l.AddToClassList("gift-text");
                r.Add(ic); r.Add(l);
                _giftCard.Add(r);
            }
            Row(LoadIcon("DEC_KraliyetFermani"), Loc.F("Kraliyet Fermanı: tezgâhın üstünde, kalıcı +%{0} gelir (yeni şubelerde de)", Mathf.RoundToInt(RoyalGift.IncomeBonus * 100f)));
            Row(LoadIcon("Cos_Barlo_Kral"), Loc.T("Kral'ın Dostu kostümü"));
            Row(KitIcon("star"), Loc.T("\"Efsanevi Han\" unvanı"));
            _endRoot.Add(_giftCard);
        }

        void HideGiftCard()
        {
            _giftCard?.RemoveFromHierarchy();
            _giftCard = null;
        }
    }
}
