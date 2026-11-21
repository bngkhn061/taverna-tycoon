using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v2.1 Paket 3 arayüzü (production/qa/playtests/feedback-2026-11-17.md maddeler 10 ve 25, GAME_DESIGN.md §15):
    /// dükkân › Personel sekmesinin başında Barlo'nun yetenek kartları; 3. tavernada "Krala Davet" hedefi ve kartı; son sahnenin
    /// yazıları, havai fişekleri ve "Efsanevi Han" şeridi; jenerik (dokununca geçilir); seviye kalkanının yanında "Efsanevi Han" rozeti;
    /// Ayarlar › "Son sahneyi yeniden izle".
    /// </summary>
    public partial class HudController
    {
        // ---- son sahne arayüzü
        VisualElement _endRoot, _endTop, _endBottom, _endTitle, _endCredits, _endScroll, _endFireworks;
        Label _endCaption, _endTitleSub;
        Label _legendBadge;
        EndingScene _endScene;
        float _endPrevScale = 1f, _endFwT, _creditsT = -1f, _creditsH;
        bool _endReplay;

        /// <summary>Jenerik süresi (sn): yazılar bu sürede alttan üste kayar, sonra serbest oyun.</summary>
        const float CreditsSeconds = 14f;
        /// <summary>Jenerik açıldıktan sonra dokunuşun geçerli olduğu en erken an (yanlışlıkla geçilmesin).</summary>
        const float CreditsTapGuard = 0.6f;
        /// <summary>Havai fişek patlamaları arası (sn), patlama başına kıvılcım ve kıvılcım ömrü.</summary>
        const float FireworkEvery = 0.42f, SparkLife = 1.15f;
        const int SparksPerBurst = 18;

        static readonly Color[] FireworkColors =
        {
            new(1f, 0.82f, 0.25f), new(0.95f, 0.36f, 0.27f), new(0.45f, 0.82f, 0.95f), new(0.72f, 0.5f, 0.95f), new(0.55f, 0.9f, 0.45f),
        };

        void InitV21()
        {
            Ending.ZoneEntered += OpenEndingCard;
            _settings.WatchEndingRequested += () =>
            {
                _settings.Hide();
                if (DayCycle.Ended || DayCycle.Closing) { Toast(Loc.T("Yeni gün başlayınca izlenebilir")); return; }
                StartEnding(true);
            };
            // "Efsanevi Han" rozeti: kalkanın solunda, şube rozetinin altında (son görüldükten sonra kalıcı)
            _legendBadge = new Label { pickingMode = PickingMode.Ignore, tooltip = Loc.T("Efsanevi Han") };
            _legendBadge.AddToClassList("legend-badge");
            var ic = new VisualElement { pickingMode = PickingMode.Ignore };
            ic.AddToClassList("legend-badge-ic");
            ic.style.backgroundImage = new StyleBackground(KitIcon("star"));
            _legendBadge.Add(ic);
            _level.parent.Add(_legendBadge);
            RefreshLegendBadge();
        }

        void DisableV21()
        {
            Ending.ZoneEntered -= OpenEndingCard;
            if (Ending.Playing) FinishEnding();
        }

        void RefreshLegendBadge()
        {
            if (_legendBadge == null) return;
            _legendBadge.EnableInClassList("hidden", !Ending.Seen);
            _legendBadge.EnableInClassList("solo", Prestige.Branch < 1);
        }

        // ------------------------------------------------------------------ Barlo'nun yetenekleri (madde 10)
        /// <summary>Dükkân › Personel: "BARLO" başlığı ve dört yetenek kartı.</summary>
        void AddBarloSection()
        {
            AddShopSection(Loc.T("BARLO"));
            foreach (var s in BarloSkills.All) AddBarloItem(s);
        }

        void AddBarloItem(BarloSkill s)
        {
            int lv = BarloSkills.Level(s);
            bool max = lv >= BarloSkills.MaxLevel;
            var can = BarloSkills.CanBuy(s);
            var item = new VisualElement();
            item.AddToClassList("item");
            item.AddToClassList("barlo-item");
            item.EnableInClassList("owned", max);
            item.EnableInClassList("locked", can == Management.BuyResult.LevelLow);
            // "YENİ!": alınmamış ve yeni açılmış yetenek seviyeleri (açık seviyenin hepsi görüldü sayılır)
            bool fresh = false;
            for (int n = lv + 1; n <= BarloSkills.MaxLevel; n++)
                if (NewBadge(BarloSkills.Key(s, n), BarloSkills.UnlockLevel(s, n))) fresh = true;
            if (fresh) AddNewBadge(item);
            var ico = new VisualElement();
            ico.AddToClassList("ico");
            var ii = new VisualElement();
            ii.AddToClassList("ico-img");
            ii.style.backgroundImage = new StyleBackground(KitIcon(BarloSkills.Icon(s)));
            ico.Add(ii);
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var n0 = new Label(Loc.F("{0} · Sv {1}/{2}", BarloSkills.Name(s), lv, BarloSkills.MaxLevel));
            n0.AddToClassList("item-name");
            var sub = new Label((max ? BarloSkills.ValueText(s, lv) + Loc.T(" (en yüksek)") : BarloSkills.ChangeText(s, lv)) + " · " + BarloSkills.Desc(s));
            sub.AddToClassList("item-sub");
            col.Add(n0); col.Add(sub);
            Button b;
            Label text;
            int next = Mathf.Min(lv + 1, BarloSkills.MaxLevel);
            int gate = BarloSkills.UnlockLevel(s, next);
            if (max) { b = PriceButton("btn-teal", KitIcon("check"), out text); text.text = Loc.T("MAX"); b.AddToClassList("owned"); b.SetEnabled(false); }
            else if (can == Management.BuyResult.LevelLow) { b = PriceButton("btn-paper", KitIcon("lock"), out text); text.text = Loc.F("SV {0}", gate); b.AddToClassList("locked"); }
            else { b = PriceButton("btn-teal", CoinIcon, out text); text.text = FormatNum(BarloSkills.Price(s, next)); b.EnableInClassList("locked", can != Management.BuyResult.Ok); }
            b.AddToClassList("btn-sm");
            b.clicked += () =>
            {
                var r = BarloSkills.Buy(s);
                if (r == Management.BuyResult.Ok)
                {
                    Sfx.Play(SfxId.Buy); Confetti(12); GameSettings.Vibrate();
                    var p = PlayerController.I;
                    if (p && p.anim) p.anim.Hop();
                    Toast($"{BarloSkills.Name(s)}: {BarloSkills.ValueText(s, BarloSkills.Level(s))}");
                }
                else Toast(Management.BuyMessage(r, gate));
                BuildShop();
                RefreshTop();
            };
            item.Add(ico); item.Add(col); item.Add(b);
            _shopScroll.Add(item);
        }

        // ------------------------------------------------------------------ Krala Davet hedefi ve kartı (madde 25)
        /// <summary>Son açıkken hedef kutusu: "Krala Davet hazır! Büyüt karesine git", ok büyüt karesini gösterir.</summary>
        bool UpdateEndingGoal()
        {
            if (!Ending.Available) return false;
            _goal.style.display = DisplayStyle.Flex;
            _goalIcon.style.backgroundImage = new StyleBackground(LoadIcon("Cust_30"));
            _goalText.text = Loc.T("Krala Davet hazır! Büyüt karesine git");
            _goalNum.text = "";
            _goalFill.style.width = Length.Percent(100f);
            _goal.EnableInClassList("ready", true);
            _shopBadge.EnableInClassList("hidden", true);
            if (!_tierZone) _tierZone = FindAnyObjectByType<TierZone>();
            _goalArrow.style.display = _tierZone ? DisplayStyle.Flex : DisplayStyle.None;
            if (_tierZone) PlaceGoalArrow(_tierZone.transform.position + Vector3.up * 0.3f);
            return true;
        }

        /// <summary>Test botu: Krala Davet kartı açık mı?</summary>
        public bool EndingCardOpen { get; private set; }
        System.Action _endingGo;

        /// <summary>"Krala Davet" kartı: üç şart, ne olacağı, "SONRA" ve "KRALI DAVET ET".</summary>
        public void OpenEndingCard()
        {
            if (!Ending.Available || TierAnimOpen || DayCycle.Ended || DayCycle.Closing || Ending.Playing) return;
            var overlay = PremCard(Loc.T("KRALA DAVET"), 720f, out var card);
            card.AddToClassList("ending-card");
            var head = new VisualElement();
            head.AddToClassList("ending-head");
            var por = new VisualElement { pickingMode = PickingMode.Ignore };
            por.AddToClassList("ending-por");
            por.style.backgroundImage = new StyleBackground(LoadIcon("Cust_30"));
            head.Add(por);
            var txt = new Label(Loc.T("Üç taverna, otuz seviye ve bir Saray Hanı. Ünün saraya ulaştı: Kral seni ziyaret etmek istiyor!"));
            txt.AddToClassList("ending-text");
            head.Add(txt);
            card.Add(head);
            var req = new VisualElement();
            req.AddToClassList("tier-req");
            req.Add(ReqChip(Loc.F("3. taverna: {0}", Prestige.BranchName(Ending.RequiredBranch)), Prestige.Branch >= Ending.RequiredBranch));
            req.Add(ReqChip(Loc.F("Seviye {0}", Ending.RequiredLevel), GameState.Level >= Ending.RequiredLevel));
            req.Add(ReqChip(Loc.F("{0} kademesi", Loc.T(Tiers.List[Tiers.List.Length - 1].name)), Tiers.Next == null));
            card.Add(req);
            var actions = new VisualElement();
            actions.AddToClassList("popup-actions");
            actions.Add(ModalButton(Loc.T("SONRA"), "btn-paper", () => { overlay.RemoveFromHierarchy(); EndingCardOpen = false; _endingGo = null; }));
            _endingGo = () => { overlay.RemoveFromHierarchy(); EndingCardOpen = false; _endingGo = null; StartEnding(false); };
            var go = ModalButton(Loc.T("KRALI DAVET ET"), "btn-sun", () => _endingGo?.Invoke());
            go.AddToClassList("last");
            actions.Add(go);
            card.Add(actions);
            card.Add(Note(Loc.T("Son sahne ve jenerik (yaklaşık 40 sn). Sonra oyun bu tavernada sürer; Yeni Şube yine açık.")));
            EndingCardOpen = true;
        }

        /// <summary>Test botu: kartta "KRALI DAVET ET".</summary>
        public void DebugEndingConfirm() => _endingGo?.Invoke();

        // ------------------------------------------------------------------ son sahne
        /// <summary>Test botu: son sahnenin aşaması ("" = oynamıyor, "credits" = jenerik).</summary>
        public string EndingPhase => !Ending.Playing ? "" : _creditsT >= 0f ? "credits" : _endScene ? _endScene.Current.ToString() : "?";
        /// <summary>Test botu: son sahnenin ayrıntısı (aktörler, kral konumu).</summary>
        public string EndingDebug => _endScene ? _endScene.DebugInfo : "-";

        void StartEnding(bool replay)
        {
            if (Ending.Playing) return;
            _endReplay = replay;
            DebugOpen("close");
            Ending.SetPlaying(true);
            if (!replay) Ending.MarkSeen();
            PlayerController.JoystickInput = Vector2.zero;
            _endPrevScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 1f;
            Sfx.ForceFestive = true;
            Sfx.Play(SfxId.Rush);   // borazan
            _hudRoot.style.display = DisplayStyle.None;
            _world.style.display = DisplayStyle.None;
            BuildEndingUi();
            _endScene = EndingScene.Play(cam);
            _endScene.PhaseChanged += OnEndingPhase;
            OnEndingPhase(EndingScene.Phase.Arrive);
        }

        void BuildEndingUi()
        {
            _endRoot?.RemoveFromHierarchy();
            _endRoot = new VisualElement { pickingMode = PickingMode.Ignore };
            _endRoot.AddToClassList("screen");
            _endRoot.AddToClassList("end-root");
            _endFireworks = new VisualElement { pickingMode = PickingMode.Ignore };
            _endFireworks.AddToClassList("screen");
            _endRoot.Add(_endFireworks);
            _endTop = new VisualElement { pickingMode = PickingMode.Ignore };
            _endTop.AddToClassList("end-bar");
            _endTop.AddToClassList("top");
            _endBottom = new VisualElement { pickingMode = PickingMode.Ignore };
            _endBottom.AddToClassList("end-bar");
            _endBottom.AddToClassList("bottom");
            _endCaption = new Label { pickingMode = PickingMode.Ignore };
            _endCaption.AddToClassList("end-caption");
            _endBottom.Add(_endCaption);
            _endRoot.Add(_endTop);
            _endRoot.Add(_endBottom);
            // "Efsanevi Han" unvan şeridi
            _endTitle = new VisualElement { pickingMode = PickingMode.Ignore };
            _endTitle.AddToClassList("end-title");
            _endTitle.AddToClassList("hidden");
            var t = new Label(Loc.Upper(Loc.T("Efsanevi Han"))) { pickingMode = PickingMode.Ignore };
            t.AddToClassList("end-title-text");
            _endTitleSub = new Label { pickingMode = PickingMode.Ignore };
            _endTitleSub.AddToClassList("end-title-sub");
            // tam genişlikte sarmalayıcı + ortada panel: panel yazı kadar genişler (left: 50% ile yarım ekrana sıkışıp kesiliyordu)
            var panel = new VisualElement { pickingMode = PickingMode.Ignore };
            panel.AddToClassList("end-title-panel");
            panel.Add(t);
            panel.Add(_endTitleSub);
            _endTitle.Add(panel);
            _endRoot.Add(_endTitle);
            _root.Add(_endRoot);
        }

        void OnEndingPhase(EndingScene.Phase p)
        {
            switch (p)
            {
                case EndingScene.Phase.Arrive: _endCaption.text = Loc.T("Kral maiyetiyle geliyor!"); break;
                case EndingScene.Phase.Greet: _endCaption.text = Loc.T("Barlo kralı kapıda karşılıyor"); break;
                case EndingScene.Phase.ToTable: _endCaption.text = Loc.T("Kral en iyi masaya buyuruyor"); break;
                case EndingScene.Phase.Toast: _endCaption.text = Loc.T("Şerefe! Kral kadeh kaldırıyor"); Confetti(20); break;
                case EndingScene.Phase.Orbit:
                    _endCaption.text = "";
                    _endTitleSub.text = Loc.F("{0} artık krallığın en ünlü hanı", GameState.TavernName);
                    _endTitle.RemoveFromClassList("hidden");
                    _endFwT = 0f;
                    Confetti(50);
                    break;
                case EndingScene.Phase.Done: ShowCredits(); break;
            }
        }

        void UpdateV21()
        {
            if (!Ending.Playing) return;
            PlayerController.JoystickInput = Vector2.zero;
            if (_endScene && _endScene.Current == EndingScene.Phase.Orbit)
            {
                _endFwT -= Time.unscaledDeltaTime;
                if (_endFwT <= 0f) { _endFwT = FireworkEvery; Firework(); }
            }
            if (_creditsT >= 0f)
            {
                _creditsT += Time.unscaledDeltaTime;
                float h = _endCredits.layout.height;
                if (h > 1f) _creditsH = h;
                float content = _endScroll.layout.height;
                float k = Mathf.Clamp01(_creditsT / CreditsSeconds);
                _endScroll.style.top = Mathf.Lerp(_creditsH, -content * 0.92f, k);
                var ptr = Pointer.current;
                bool tap = ptr != null && ptr.press.wasPressedThisFrame;
                if ((tap && _creditsT > CreditsTapGuard) || _creditsT >= CreditsSeconds) FinishEnding();
            }
        }

        /// <summary>Test botu: jenerikte dokunuş (geçer).</summary>
        public void DebugEndingTap() { if (_creditsT > CreditsTapGuard) FinishEnding(); }

        /// <summary>Ekranın üst kısmında renkli kıvılcım patlaması (havai fişek).</summary>
        void Firework()
        {
            var size = _root.layout.size;
            if (size.x < 10f) return;
            var center = new Vector2(Random.Range(size.x * 0.12f, size.x * 0.88f), Random.Range(size.y * 0.1f, size.y * 0.5f));
            var color = FireworkColors[Random.Range(0, FireworkColors.Length)];
            Sfx.Play(SfxId.Pop, Random.Range(0.55f, 0.75f));
            for (int i = 0; i < SparksPerBurst; i++)
            {
                var sp = new VisualElement { pickingMode = PickingMode.Ignore };
                sp.AddToClassList("spark");
                sp.style.backgroundColor = color;
                _endFireworks.Add(sp);
                float ang = (i / (float)SparksPerBurst) * Mathf.PI * 2f + Random.Range(-0.1f, 0.1f);
                var vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(150f, 230f);
                var pos = center;
                float life = 0f;
                sp.schedule.Execute(() =>
                {
                    life += 0.016f;
                    pos += vel * 0.016f;
                    vel *= 0.965f;
                    vel.y += 60f * 0.016f;
                    sp.style.left = pos.x;
                    sp.style.top = pos.y;
                    sp.style.opacity = 1f - life / SparkLife;
                    if (life > SparkLife) sp.RemoveFromHierarchy();
                }).Every(16).Until(() => life > SparkLife);
            }
        }

        void ShowCredits()
        {
            if (_endScene) { _endScene.PhaseChanged -= OnEndingPhase; _endScene.Cleanup(); _endScene = null; }
            _endTitle.AddToClassList("hidden");
            _endFireworks.Clear();
            _endCredits = new VisualElement();
            _endCredits.AddToClassList("screen");
            _endCredits.AddToClassList("end-credits");
            _endScroll = new VisualElement { pickingMode = PickingMode.Ignore };
            _endScroll.AddToClassList("end-scroll");
            void Line(string text, string cls) { var l = new Label(text) { pickingMode = PickingMode.Ignore }; l.AddToClassList(cls); _endScroll.Add(l); }
            Line("Taverna Tycoon", "end-game");
            Line(Loc.F("Bir {0} oyunu", "Bn Studio"), "end-line");
            Line(Loc.T("Tasarım, kod ve modeller"), "end-head");
            Line("Bn Studio", "end-line");
            Line(Loc.T("Müzik"), "end-head");
            Line("RandomMind · \"Medieval: Minstrel Dance\"", "end-line");
            Line("RandomMind · \"Medieval: The Old Tower Inn\"", "end-line");
            Line("RandomMind · \"Medieval: Market Day\"", "end-line");
            Line("CC0 · opengameart.org", "end-small");
            Line(Loc.T("Ses efektleri"), "end-head");
            Line("Kenney · Casino, Impact, Interface, RPG Audio", "end-line");
            Line("CC0 · kenney.nl", "end-small");
            Line(Loc.T("Yazı tipleri"), "end-head");
            Line("Noto Sans SC · JP · KR", "end-line");
            Line("Nunito · Rubik · Lilita One", "end-line");
            Line("SIL Open Font License 1.1", "end-small");
            Line(Loc.T("Araçlar"), "end-head");
            Line("Unity · Blender", "end-line");
            Line(Loc.T("Oynadığın için teşekkürler!"), "end-thanks");
            _endCredits.Add(_endScroll);
            var skip = new Label(Loc.T("Dokun: geç")) { pickingMode = PickingMode.Ignore };
            skip.AddToClassList("end-skip");
            _endCredits.Add(skip);
            _endRoot.Add(_endCredits);
            _endCaption.text = "";
            _creditsT = 0f;
            _creditsH = _root.layout.height;
            _endScroll.style.top = _creditsH;
        }

        /// <summary>Son sahne ve jenerik biter: dünya ve arayüz eski hâline, serbest oyun.</summary>
        void FinishEnding()
        {
            if (_endScene) { _endScene.PhaseChanged -= OnEndingPhase; _endScene.Cleanup(); _endScene = null; }
            _endRoot?.RemoveFromHierarchy();
            _endRoot = null;
            _creditsT = -1f;
            Ending.SetPlaying(false);
            Sfx.ForceFestive = false;
            Time.timeScale = _endPrevScale;
            _hudRoot.style.display = DisplayStyle.Flex;
            _world.style.display = DisplayStyle.Flex;
            RefreshLegendBadge();
            GameState.Save();
            if (!_endReplay)
            {
                ShowBanner(Loc.Upper(Loc.T("Efsanevi Han")), "star", true, 2.6f);
                Toast(Loc.T("Serbest oyun: taverna senin. Yeni Şube istersen hâlâ açık"));
            }
        }
    }
}
