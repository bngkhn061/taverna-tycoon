using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v1.5 Prestij "Yeni Şube" arayüzü (GAME_DESIGN.md §9): hedef kutusu (eksik şart), şube kartı (önce / sonra,
    /// kalan / sıfırlanan, kazanım, iki dokunuşla onay), seviye kalkanının yanında şube rozeti ve şube şeridi.
    /// "Tavernayı Büyüt" (HudTiers) akışının aynısı: aynı hedef karesi, aynı kart çerçevesi.
    /// </summary>
    public partial class HudController
    {
        Label _branchBadge, _branchNote;
        Action _branchTap;
        bool _branchArmed;

        void InitPrestige()
        {
            Prestige.ZoneEntered += OpenBranchCard;
            // Şube rozeti: kalkanın solunda küçük daire (şube ≥ 1)
            _branchBadge = new Label { pickingMode = PickingMode.Ignore };
            _branchBadge.AddToClassList("branch-badge");
            _level.parent.Add(_branchBadge);
            RefreshBranchBadge();
            if (Prestige.JustBranched)
            {
                Prestige.JustBranched = false;
                _root.schedule.Execute(() =>
                {
                    ShowBanner(Loc.F("YENİ ŞUBE · {0}", Upper(Prestige.CurrentName)), "home", true, 3.2f);
                    Confetti(40);
                    Sfx.Play(SfxId.LevelUp);
                }).StartingIn(600);
            }
        }

        void DisablePrestige() => Prestige.ZoneEntered -= OpenBranchCard;

        static string Upper(string s) => Loc.Upper(s);

        void RefreshBranchBadge()
        {
            if (_branchBadge == null) return;
            int b = Prestige.Branch;
            _branchBadge.EnableInClassList("hidden", b < 1);
            _branchBadge.text = b.ToString();
            _branchBadge.tooltip = Prestige.CurrentName;
        }

        /// <summary>Gün başı şeridindeki ek: şube ≥ 1 iken " · ŞUBE ADI".</summary>
        static string BranchSuffix => Prestige.Branch >= 1 ? " · " + Upper(Prestige.CurrentName) : "";

        // ------------------------------------------------------------------ hedef kutusu
        /// <summary>Sv30'da (açılabilir alan hedefi yokken) prestij şartlarından eksik olanı gösterir.</summary>
        bool UpdateBranchGoal(PayZone zz)
        {
            if (!Prestige.Visible || (zz && zz.LevelOk)) return false;
            bool ready = Prestige.CanPrestige;
            bool tierMissing = Tiers.Next != null;
            _goal.style.display = DisplayStyle.Flex;
            _goalIcon.style.backgroundImage = new StyleBackground(KitIcon("home"));
            string next = Prestige.BranchName(Prestige.Branch + 1);
            if (ready) _goalText.text = Loc.T("Yeni Şube hazır! Büyüt karesine git");
            else if (tierMissing) _goalText.text = Loc.F("Yeni Şube için: {0} kademesi", Loc.T(Tiers.List[Tiers.List.Length - 1].name));
            else _goalText.text = Loc.F("Yeni Şube: {0}", next);
            if (tierMissing)
            {
                var n = Tiers.Next;
                _goalNum.text = $"{FormatNum(Math.Min(GameState.Coins, n.cost))} / {FormatNum(n.cost)}";
                _goalFill.style.width = Length.Percent(Mathf.Clamp01(GameState.Coins / (float)n.cost) * 100f);
            }
            else
            {
                _goalNum.text = $"{FormatNum(Math.Min(GameState.Coins, Prestige.RequiredCoins))} / {FormatNum(Prestige.RequiredCoins)}";
                _goalFill.style.width = Length.Percent(Mathf.Clamp01(GameState.Coins / (float)Prestige.RequiredCoins) * 100f);
            }
            _goal.EnableInClassList("ready", ready || (tierMissing && Tiers.CanUpgrade));
            _shopBadge.EnableInClassList("hidden", true);
            if (!_tierZone) _tierZone = FindAnyObjectByType<TierZone>();
            _goalArrow.style.display = _tierZone ? DisplayStyle.Flex : DisplayStyle.None;
            if (_tierZone) PlaceGoalArrow(_tierZone.transform.position + Vector3.up * 0.3f);
            return true;
        }

        // ------------------------------------------------------------------ şube kartı
        /// <summary>Test botu: şube kartı açık mı?</summary>
        public bool BranchCardOpen => _branchTap != null && PremModalOpen;

        public void OpenBranchCard()
        {
            if (TierAnimOpen || DayCycle.Ended) return;
            int b = Prestige.Branch, nb = b + 1;
            var overlay = PremCard(Loc.T("YENİ ŞUBE"), 780f, out var card);
            card.AddToClassList("branch-card");

            var compare = new VisualElement();
            compare.AddToClassList("tier-compare");
            compare.Add(BranchShot(b, Prestige.CurrentName, Loc.F("Sv{0}", GameState.Level), false));
            compare.Add(CompareArrow());
            compare.Add(BranchShot(nb, Prestige.BranchName(nb), Loc.F("Sv{0}", 1), true));
            card.Add(compare);

            var req = new VisualElement();
            req.AddToClassList("tier-req");
            foreach (var (label, ok) in Prestige.Requirements()) req.Add(ReqChip(label, ok));
            card.Add(req);

            var cols = new VisualElement();
            cols.AddToClassList("branch-cols");
            cols.Add(BranchCol(Loc.T("KALIR"), "keep", new[]
            {
                Loc.T("Elmas ve Hazine ürünleri (VIP dahil)"),
                Loc.T("Koleksiyon defteri, taverna adı, ayarlar"),
                Loc.T("Zemin / duvar temaları, dekorlar (bölgesi açılınca)"),
                Loc.T("Reklam hakları ve rehber bilgisi"),
            }));
            cols.Add(BranchCol(Loc.T("SIFIRLANIR"), "reset", new[]
            {
                Loc.T("Altın, seviye, Ün ve gün sayısı"),
                Loc.T("Alanlar, masalar, makineler, yükseltmeler, depo"),
                Loc.T("Personel, otomasyon ve Usta"),
                Loc.F("Taverna kademesi · mutluluk %{0:0}", Happiness.Start),
            }));
            card.Add(cols);

            var gains = new VisualElement();
            gains.AddToClassList("tier-gains");
            foreach (var (icon, text) in new[]
                     {
                         ("coin", Loc.F("Kalıcı gelir ×{0:0.##} (şube başına +%{1:0})", Prestige.IncomeMultiplierFor(nb), Prestige.IncomePerBranch * 100f)),
                         ("star", Loc.F("Ün kazanımı ×{0:0.##} (şube başına +%{1:0})", Prestige.UnMultiplierFor(nb), Prestige.UnPerBranch * 100f)),
                         ("home", Loc.F("{0} {1} altınla açılır", Prestige.BranchName(nb), FormatNum(Prestige.StartCoins(nb)))),
                         ("person", Loc.T("Harita aynı kalır · yeni şube haritaları yakında")),
                     })
            {
                var g = new VisualElement();
                g.AddToClassList("tier-gain");
                var ic = new VisualElement();
                ic.AddToClassList("prem-perk-ic");
                ic.style.backgroundImage = new StyleBackground(KitIcon(icon));
                g.Add(ic);
                g.Add(new Label(text));
                gains.Add(g);
            }
            card.Add(gains);

            var actions = new VisualElement();
            actions.AddToClassList("popup-actions");
            actions.Add(ModalButton(Loc.T("SONRA"), "btn-paper", () => { overlay.RemoveFromHierarchy(); _branchTap = null; }));
            bool can = Prestige.CanPrestige;
            _branchArmed = false;
            var go = ModalButton(Loc.F("{0} · ŞUBE AÇ", FormatNum(Prestige.RequiredCoins)), can ? "btn-sun" : "btn-paper", null);
            go.AddToClassList("last");
            go.EnableInClassList("locked", !can);
            _branchTap = () =>
            {
                if (!Prestige.CanPrestige) { Toast(Loc.T("Şartlar henüz tamam değil")); return; }
                if (!_branchArmed)
                {
                    // 1. dokunuş: onay iste (geri dönüşü yok)
                    _branchArmed = true;
                    go.RemoveFromClassList("btn-sun");
                    go.AddToClassList("btn-red");
                    var l = go.Q<Label>();
                    if (l != null) l.text = Loc.T("EMİN MİSİN? ONAYLA");
                    _branchNote.text = Loc.T("Son dokunuş: şube açılır, geri dönüşü yok.");
                    Sfx.Play(SfxId.Pop);
                    return;
                }
                // 2. dokunuş: şubeyi aç
                overlay.RemoveFromHierarchy();
                _branchTap = null;
                Prestige.Execute();
            };
            go.clicked += () => _branchTap?.Invoke();
            actions.Add(go);
            card.Add(actions);
            _branchNote = Note(Loc.T("İki dokunuşla onaylanır. Şube açılınca eski tavernaya dönülmez."));
            card.Add(_branchNote);
        }

        /// <summary>Test botu: hedef kutusundaki metin.</summary>
        public string DebugGoalText => _goal.style.display == DisplayStyle.None ? Loc.T("(gizli)") : $"{_goalText.text} · {_goalNum.text}";

        /// <summary>v1.8 test botu: hedef metni kutuya sığıyor mu (gereken yükseklik / ayrılan yükseklik, punto).</summary>
        public string DebugGoalFit
        {
            get
            {
                var need = _goalText.MeasureTextSize(_goalText.text, _goalText.layout.width, VisualElement.MeasureMode.Exactly, 0f, VisualElement.MeasureMode.Undefined);
                bool fits = need.y <= _goalText.layout.height + 1f && need.x <= _goalText.layout.width + 1f;
                return $"sığıyor={fits} · gereken {need.y:0} px / kutu {_goalText.layout.height:0} px · genişlik {_goalText.layout.width:0} · punto {_goalText.resolvedStyle.fontSize:0.#} · uzun={_goalText.ClassListContains("long")}";
            }
        }

        /// <summary>Test botu: şube kartındaki onay düğmesine bir kez dokun.</summary>
        public void DebugBranchTap() => _branchTap?.Invoke();

        /// <summary>
        /// v1.8 önce / sonra kutusu: küçük kademe parçası ikonu yerine oyun logosu (USS .branch-logo) + şube numarası rozeti,
        /// şube adı ve seviye. Yeni şube kutusu vurgulu (.next), eski soluk değil ama sade.
        /// </summary>
        static VisualElement BranchShot(int branch, string name, string level, bool next)
        {
            var box = new VisualElement();
            box.AddToClassList("tier-shot");
            box.AddToClassList("branch-shot");
            if (next) box.AddToClassList("next");
            var logo = new VisualElement { pickingMode = PickingMode.Ignore };
            logo.AddToClassList("branch-logo");
            box.Add(logo);
            var col = new VisualElement { pickingMode = PickingMode.Ignore };
            col.AddToClassList("branch-shot-col");
            var num = new Label((branch + 1).ToString());
            num.AddToClassList("branch-shot-num");
            col.Add(num);
            var n = new Label(name);
            n.AddToClassList("branch-shot-name");
            col.Add(n);
            var lv = new Label(level);
            lv.AddToClassList("branch-shot-lv");
            col.Add(lv);
            box.Add(col);
            return box;
        }

        static VisualElement BranchCol(string title, string cls, string[] lines)
        {
            var col = new VisualElement();
            col.AddToClassList("branch-col");
            col.AddToClassList(cls);
            var t = new Label(title);
            t.AddToClassList("branch-col-title");
            col.Add(t);
            foreach (var line in lines)
            {
                var l = new Label("• " + line);
                l.AddToClassList("branch-line");
                col.Add(l);
            }
            return col;
        }
    }
}
