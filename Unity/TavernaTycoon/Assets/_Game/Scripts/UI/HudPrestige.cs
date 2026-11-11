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
                    ShowBanner($"YENİ ŞUBE · {Upper(Prestige.CurrentName)}", "home", true, 3.2f);
                    Confetti(40);
                    Sfx.Play(SfxId.LevelUp);
                }).StartingIn(600);
            }
        }

        void DisablePrestige() => Prestige.ZoneEntered -= OpenBranchCard;

        static string Upper(string s) => s.ToUpper(new System.Globalization.CultureInfo("tr-TR"));

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
            if (ready) _goalText.text = "Yeni Şube hazır! Büyüt karesine git";
            else if (tierMissing) _goalText.text = $"Yeni Şube için: {Tiers.List[Tiers.List.Length - 1].name} kademesi";
            else _goalText.text = $"Yeni Şube: {next}";
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
            var overlay = PremCard("YENİ ŞUBE", 780f, out var card);
            card.AddToClassList("branch-card");

            var compare = new VisualElement();
            compare.AddToClassList("tier-compare");
            compare.Add(BranchShot(Tiers.CurrentIndex, $"{Prestige.CurrentName} · Sv{GameState.Level}"));
            var arrow = new Label("➜");
            arrow.AddToClassList("tier-arrow");
            compare.Add(arrow);
            compare.Add(BranchShot(0, $"{Prestige.BranchName(nb)} · Sv1"));
            card.Add(compare);

            var req = new VisualElement();
            req.AddToClassList("tier-req");
            foreach (var (label, ok) in Prestige.Requirements())
            {
                var c = new Label((ok ? "✓ " : "") + label);
                c.AddToClassList("chip");
                c.AddToClassList(ok ? "ok" : "no");
                req.Add(c);
            }
            card.Add(req);

            var cols = new VisualElement();
            cols.AddToClassList("branch-cols");
            cols.Add(BranchCol("KALIR", "keep", new[]
            {
                "Elmas ve Hazine ürünleri (VIP dahil)",
                "Koleksiyon defteri, taverna adı, ayarlar",
                "Zemin / duvar temaları, dekorlar (bölgesi açılınca)",
                "Reklam hakları ve rehber bilgisi",
            }));
            cols.Add(BranchCol("SIFIRLANIR", "reset", new[]
            {
                "Altın, seviye, Ün ve gün sayısı",
                "Alanlar, masalar, makineler, yükseltmeler, depo",
                "Personel, otomasyon ve Usta",
                $"Taverna kademesi · mutluluk %{Happiness.Start:0}",
            }));
            card.Add(cols);

            var gains = new VisualElement();
            gains.AddToClassList("tier-gains");
            foreach (var (icon, text) in new[]
                     {
                         ("coin", $"Kalıcı gelir ×{Prestige.IncomeMultiplierFor(nb):0.##} (şube başına +%{Prestige.IncomePerBranch * 100f:0})"),
                         ("star", $"Ün kazanımı ×{Prestige.UnMultiplierFor(nb):0.##} (şube başına +%{Prestige.UnPerBranch * 100f:0})"),
                         ("home", $"{Prestige.BranchName(nb)} {FormatNum(Prestige.StartCoins(nb))} altınla açılır"),
                         ("person", "Harita aynı kalır · yeni şube haritaları yakında"),
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
            actions.Add(ModalButton("SONRA", "btn-paper", () => { overlay.RemoveFromHierarchy(); _branchTap = null; }));
            bool can = Prestige.CanPrestige;
            _branchArmed = false;
            var go = ModalButton($"{FormatNum(Prestige.RequiredCoins)} · ŞUBE AÇ", can ? "btn-sun" : "btn-paper", null);
            go.AddToClassList("last");
            go.EnableInClassList("locked", !can);
            _branchTap = () =>
            {
                if (!Prestige.CanPrestige) { Toast("Şartlar henüz tamam değil"); return; }
                if (!_branchArmed)
                {
                    // 1. dokunuş: onay iste (geri dönüşü yok)
                    _branchArmed = true;
                    go.RemoveFromClassList("btn-sun");
                    go.AddToClassList("btn-red");
                    var l = go.Q<Label>();
                    if (l != null) l.text = "EMİN MİSİN? ONAYLA";
                    _branchNote.text = "Son dokunuş: şube açılır, geri dönüşü yok.";
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
            _branchNote = Note("İki dokunuşla onaylanır. Şube açılınca eski tavernaya dönülmez.");
            card.Add(_branchNote);
        }

        /// <summary>Test botu: hedef kutusundaki metin.</summary>
        public string DebugGoalText => _goal.style.display == DisplayStyle.None ? "(gizli)" : $"{_goalText.text} · {_goalNum.text}";

        /// <summary>Test botu: şube kartındaki onay düğmesine bir kez dokun.</summary>
        public void DebugBranchTap() => _branchTap?.Invoke();

        VisualElement BranchShot(int tierIndex, string caption)
        {
            var box = new VisualElement();
            box.AddToClassList("tier-shot");
            box.AddToClassList("branch-shot");
            box.style.backgroundImage = new StyleBackground(TierIcon(tierIndex));
            var cap = new Label(caption);
            cap.AddToClassList("tier-cap");
            box.Add(cap);
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
