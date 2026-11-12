using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v1.0 "Tavernayı Büyüt" arayüzü (design/ui/ui_v11_buyut.html, onay 2026-11-10):
    /// hedef kutusu (M), kademe kartı (N), dönüşüm animasyonu (O) ve kademe yolu (P).
    /// </summary>
    public partial class HudController
    {
        TierZone _tierZone;
        VisualElement _tierDust, _tierFlash;
        float _tierAnimT = -1f;
        bool _tierApplied;
        int _tierTicks;
        const float TierApplyAt = 1.1f, TierAnimEnd = 2.6f;

        bool TierAnimOpen => _tierAnimT >= 0f;
        /// <summary>Test botu: dönüşüm animasyonu sürüyor mu?</summary>
        public bool TierAnimating => TierAnimOpen;

        void InitTiers()
        {
            _tierZone = FindAnyObjectByType<TierZone>();
            Tiers.ZoneEntered += OpenTierCard;
            _tierDust = new VisualElement { pickingMode = PickingMode.Ignore };
            _tierDust.AddToClassList("screen"); _tierDust.AddToClassList("tier-dust"); _tierDust.AddToClassList("hidden");
            _tierFlash = new VisualElement { pickingMode = PickingMode.Ignore };
            _tierFlash.AddToClassList("screen"); _tierFlash.AddToClassList("tier-flash"); _tierFlash.AddToClassList("hidden");
            _root.Add(_tierDust);
            _root.Add(_tierFlash);
        }

        void DisableTiers() => Tiers.ZoneEntered -= OpenTierCard;

        /// <summary>Test botu: kademeyi yükselt ve dönüşüm animasyonunu oynat.</summary>
        public bool DebugTierUpgrade() { if (!Tiers.Upgrade()) return false; StartTierTransform(); return true; }

        static Sprite TierIcon(int index) => LoadIcon($"Tier_{index + 1}");

        // ------------------------------------------------------------------ hedef kutusu (M)
        /// <summary>Başka alan hedefi yokken sıradaki kademe hedef kutusunda ve okta gösterilir.</summary>
        bool UpdateTierGoal(PayZone zz)
        {
            if (!Tiers.Visible || (zz && zz.LevelOk)) return false;
            var n = Tiers.Next;
            bool ready = Tiers.CanUpgrade;
            _goal.style.display = DisplayStyle.Flex;
            _goalIcon.style.backgroundImage = new StyleBackground(KitIcon("home"));
            _goalText.text = ready ? Loc.F("{0} hazır! Büyüt karesine git", Loc.T(n.name)) : Loc.F("Tavernayı Büyüt: {0}", Loc.T(n.name));
            _goalNum.text = $"{FormatNum(System.Math.Min(GameState.Coins, n.cost))} / {FormatNum(n.cost)}";
            _goalFill.style.width = Length.Percent(Mathf.Clamp01(GameState.Coins / (float)n.cost) * 100f);
            _goal.EnableInClassList("ready", ready);
            _shopBadge.EnableInClassList("hidden", true);
            if (!_tierZone) _tierZone = FindAnyObjectByType<TierZone>();
            _goalArrow.style.display = _tierZone ? DisplayStyle.Flex : DisplayStyle.None;
            if (_tierZone) PlaceGoalArrow(_tierZone.transform.position + Vector3.up * 0.3f);
            return true;
        }

        // ------------------------------------------------------------------ kademe kartı (N)
        public void OpenTierCard()
        {
            var n = Tiers.Next;
            if (n == null || TierAnimOpen || DayCycle.Ended) return;
            var cur = Tiers.Current;
            var overlay = PremCard(Loc.T("TAVERNAYI BÜYÜT"), 700f, out var card);
            var compare = new VisualElement();
            compare.AddToClassList("tier-compare");
            compare.Add(TierShot(cur));
            var arrow = new Label("➜");
            arrow.AddToClassList("tier-arrow");
            compare.Add(arrow);
            compare.Add(TierShot(n));
            card.Add(compare);
            var req = new VisualElement();
            req.AddToClassList("tier-req");
            foreach (var (label, ok) in Tiers.Requirements(n))
            {
                var c = new Label((ok ? "✓ " : "") + label);
                c.AddToClassList("chip");
                c.AddToClassList(ok ? "ok" : "no");
                req.Add(c);
            }
            card.Add(req);
            var gains = new VisualElement();
            gains.AddToClassList("tier-gains");
            foreach (var (icon, text) in new[] { ("coin", Loc.F("Tüm satış fiyatları ×{0:0.##}", n.price)), ("person", Loc.T(n.guests)),
                                                 ("home", Loc.T(n.looks)), ("star", Loc.F("+{0:0} taverna puanı", n.ambiance)) })
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
            actions.Add(ModalButton(Loc.T("SONRA"), "btn-paper", () => overlay.RemoveFromHierarchy()));
            actions.Add(ModalButton(Loc.T("KADEMELER"), "btn-paper", () => { overlay.RemoveFromHierarchy(); OpenTierPath(); }));
            bool can = Tiers.CanUpgrade;
            var go = ModalButton(Loc.F("{0} · BÜYÜT", FormatNum(n.cost)), can ? "btn-sun" : "btn-paper", () =>
            {
                if (!Tiers.CanUpgrade) { Toast(Loc.T("Şartlar henüz tamam değil")); return; }
                overlay.RemoveFromHierarchy();
                if (Tiers.Upgrade()) StartTierTransform();
            });
            go.AddToClassList("last");
            go.EnableInClassList("locked", !can);
            actions.Add(go);
            card.Add(actions);
        }

        VisualElement TierShot(Tiers.Tier t)
        {
            var box = new VisualElement();
            box.AddToClassList("tier-shot");
            box.style.backgroundImage = new StyleBackground(TierIcon(t.index));
            var cap = new Label(Loc.T(t.name));
            cap.AddToClassList("tier-cap");
            box.Add(cap);
            return box;
        }

        // ------------------------------------------------------------------ dönüşüm (O)
        void StartTierTransform()
        {
            _tierAnimT = 0f;
            _tierApplied = false;
            _tierTicks = 0;
            _tierDust.RemoveFromClassList("hidden");
            _tierDust.style.opacity = 0f;
            PlayerController.JoystickInput = Vector2.zero;
        }

        void UpdateTierTransform()
        {
            if (_tierAnimT < 0f) return;
            _tierAnimT += Time.unscaledDeltaTime;
            var p = Pointer.current;
            if (!_tierApplied && _tierAnimT > 0.3f && p != null && p.press.wasPressedThisFrame) _tierAnimT = TierApplyAt;
            // 1) toz bulutu + çekiç sesleri
            if (!_tierApplied)
            {
                _tierDust.style.opacity = Mathf.Clamp01(_tierAnimT / 0.6f) * 0.78f;
                int ticks = Mathf.FloorToInt(_tierAnimT / 0.16f);
                while (_tierTicks < ticks) { _tierTicks++; Sfx.Play(SfxId.Tick, Random.Range(0.6f, 0.9f)); }
            }
            // 2) ışık patlaması, parçalar yeni kademeye geçer
            if (!_tierApplied && _tierAnimT >= TierApplyAt)
            {
                _tierApplied = true;
                Tiers.Apply();
                _tierDust.AddToClassList("hidden");
                _tierFlash.RemoveFromClassList("hidden");
                Sfx.Play(SfxId.Buy, 0.8f);
            }
            if (_tierApplied)
            {
                float f = Mathf.Clamp01(1f - (_tierAnimT - TierApplyAt) / 0.5f);
                _tierFlash.style.opacity = f;
                if (f <= 0f) _tierFlash.AddToClassList("hidden");
            }
            // 3) şerit + konfeti
            if (_tierApplied && _tierAnimT >= TierApplyAt + 0.4f && _bannerT <= 0f && _tierAnimT < TierApplyAt + 0.5f)
            {
                ShowBanner($"{Loc.Upper(Loc.T(Tiers.Current.name))}!", "home", false, 2.4f);
                Confetti(40);
                Sfx.Play(SfxId.LevelUp);
            }
            if (_tierAnimT >= TierAnimEnd) { _tierAnimT = -1f; _tierFlash.AddToClassList("hidden"); }
        }

        // ------------------------------------------------------------------ kademe yolu (P)
        public void OpenTierPath()
        {
            PremCard(Loc.T("TAVERNA KADEMESİ"), 1040f, out var card);
            var row = new VisualElement();
            row.AddToClassList("tier-path");
            for (int i = 0; i < Tiers.List.Length; i++)
            {
                var t = Tiers.List[i];
                var col = new VisualElement();
                col.AddToClassList("tier-col");
                string state = i == Tiers.CurrentIndex ? "on" : i == Tiers.CurrentIndex + 1 ? "next" : i < Tiers.CurrentIndex ? "done" : "lock";
                col.AddToClassList(state);
                var badge = new Label(state == "on" ? Loc.T("ŞU AN") : state == "next" ? Loc.T("SIRADAKİ") : state == "done" ? Loc.T("TAMAM") : Loc.F("Sv{0}", t.level));
                badge.AddToClassList("tier-badge");
                badge.AddToClassList(state);
                col.Add(badge);
                var img = new VisualElement();
                img.AddToClassList("tier-img");
                img.style.backgroundImage = new StyleBackground(TierIcon(i));
                col.Add(img);
                var n = new Label($"{i + 1} · {Loc.T(t.name)}");
                n.AddToClassList("item-name");
                col.Add(n);
                var sub = new Label(i == 0 ? Loc.T("Başlangıç") : Loc.F("Sv{0} · {1} altın", t.level, FormatNum(t.cost)) + (t.zones.Length > 0 ? " · " + ZoneList(t) : ""));
                sub.AddToClassList("item-sub");
                col.Add(sub);
                foreach (var line in new[] { Loc.T(t.looks), Loc.F("Fiyat ×{0:0.##}", t.price) + (t.ambiance > 0 ? Loc.F(" · +{0:0} puan", t.ambiance) : ""), Loc.T(t.guests) })
                {
                    var l = new Label("• " + line);
                    l.AddToClassList("tier-line");
                    col.Add(l);
                }
                row.Add(col);
                if (i < Tiers.List.Length - 1) { var link = new VisualElement(); link.AddToClassList("tier-link"); row.Add(link); }
            }
            card.Add(row);
        }

        static string ZoneList(Tiers.Tier t)
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (var z in t.zones) names.Add(Loc.T(z.name));
            return t.index == 1 ? Loc.T("tüm alanlar açık") : Loc.F("{0} açık", string.Join(", ", names));
        }

        /// <summary>Dükkân › Otomasyon sekmesinin altında kademe satırı.</summary>
        void AddTierShopRow()
        {
            AddShopSection(Loc.T("TAVERNA KADEMESİ"));
            var n = Tiers.Next;
            var cur = Tiers.Current;
            var item = new VisualElement();
            item.AddToClassList("item");
            var img = new VisualElement();
            img.AddToClassList("item-img");
            img.style.backgroundImage = new StyleBackground(TierIcon(cur.index));
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var name = new Label(Loc.F("{0} · fiyat ×{1:0.##}", Loc.T(cur.name), cur.price));
            name.AddToClassList("item-name");
            var sub = new Label(n == null ? Loc.T("En yüksek kademe") : Loc.F("Sıradaki: {0} · Sv{1} · {2} altın", Loc.T(n.name), n.level, FormatNum(n.cost)));
            sub.AddToClassList("item-sub");
            col.Add(name); col.Add(sub);
            var b = new Button(OpenTierPath);
            b.AddToClassList("btn"); b.AddToClassList("btn-sm"); b.AddToClassList("btn-paper");
            b.Add(new Label(Loc.T("KADEMELER")));
            item.Add(img); item.Add(col); item.Add(b);
            _shopScroll.Add(item);
        }
    }
}
