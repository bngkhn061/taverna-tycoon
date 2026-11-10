using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v1.0 Dükkân › Hazine (design/ui/ui_v10_hazine.html, onay 2026-11-10): VIP kartı, başlangıç paketi, efsanevi personel,
    /// premium dekor, elmas paketleri; VIP detayı, test satın alma onayı ve Sv3 başlangıç paketi teklifi.
    /// </summary>
    public partial class HudController
    {
        VisualElement _premModal;
        bool PremModalOpen => _premModal != null && _premModal.parent != null;
        /// <summary>Test botu: Hazine penceresi (teklif, VIP, onay) açık mı?</summary>
        public bool PremiumModalVisible => PremModalOpen;

        void InitPremium()
        {
            _root.Q<Button>("tab-prem").clicked += () => SelectTab("prem");
            Premium.PurchaseRequested += OnPurchaseRequested;
            Premium.Granted += OnPremiumGranted;
        }

        void DisablePremium()
        {
            Premium.PurchaseRequested -= OnPurchaseRequested;
            Premium.Granted -= OnPremiumGranted;
        }

        void UpdatePremium()
        {
            // Sv3: başlangıç paketi teklifi bir kez, sakin bir anda
            if (Premium.ShouldOfferStarter && !ModalOpen && !Tutorial.Active && !LevelCardOpen && !DayCycle.Ended && Time.timeSinceLevelLoad > 5f)
            {
                Premium.MarkStarterShown();
                OpenStarterOffer();
            }
            if (_signLabel != null) _signLabel.EnableInClassList("vip", Premium.IsVip);
        }

        static Sprite ProductIcon(Premium.Product p) =>
            p.icon != null && p.icon.StartsWith("Kit:") ? KitIcon(p.icon.Substring(4)) : LoadIcon(p.icon);

        // ------------------------------------------------------------------ sekme
        void BuildPremiumTab()
        {
            _shopNote.text = Premium.TestStore
                ? "TEST SÜRÜMÜ · gerçek ödeme alınmaz · fiyatlar örnek"
                : "Satın almalar hesaba bağlıdır · ilerlemek için gerekmez";
            AddVipCard();
            if (Premium.StarterAvailable)
            {
                var left = Premium.StarterLeft;
                AddShopSection($"SINIRLI · BAŞLANGIÇ PAKETİ · {(int)left.TotalHours / 24}g {left.Hours}s");
                AddPremiumItem(Premium.Find("starter"), "%70 indirim");
            }
            AddShopSection("EFSANEVİ PERSONEL");
            AddPremiumItem(Premium.Find("staff_lirael"));
            AddPremiumItem(Premium.Find("staff_ignis"));
            AddShopSection("PREMIUM DEKOR");
            foreach (var id in new[] { "thm_altin", "dec_kristal", "dec_ejder", "frame_kraliyet" }) AddPremiumItem(Premium.Find(id));
            AddShopSection("ELMAS");
            var row = new VisualElement();
            row.AddToClassList("prem-gems");
            foreach (var id in new[] { "gems_100", "gems_550", "gems_1200", "gems_3000" }) row.Add(GemCard(Premium.Find(id)));
            _shopScroll.Add(row);
            var restore = new Button(() => Toast($"Geri yüklendi: {Premium.Restore()} ürün hesabında")) { text = "SATIN ALMALARI GERİ YÜKLE" };
            restore.AddToClassList("btn"); restore.AddToClassList("btn-paper"); restore.AddToClassList("btn-sm"); restore.AddToClassList("prem-restore");
            _shopScroll.Add(restore);
        }

        void AddVipCard()
        {
            var card = new Button(OpenVipDetail);
            card.AddToClassList("prem-vip");
            var crown = new Label("VIP");
            crown.AddToClassList("prem-crown");
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var title = new Label(Premium.IsVip ? (Premium.Owned("vip_life") ? "VIP · ömür boyu" : "VIP · aktif") : "VIP Üyelik");
            title.AddToClassList("prem-vip-title");
            var perks = new VisualElement();
            perks.AddToClassList("prem-perks");
            foreach (var t in new[] { "Reklamsız", "Kalıcı ×2 gelir", $"Günde {Premium.VipDailyGems} elmas", "VIP rozet" })
            {
                var c = new Label(t);
                c.AddToClassList("chip");
                perks.Add(c);
            }
            col.Add(title); col.Add(perks);
            var price = new Label(Premium.Owned("vip_life") ? "AKTİF" : Premium.Find("vip_month").price);
            price.AddToClassList("prem-price");
            card.Add(crown); card.Add(col); card.Add(price);
            _shopScroll.Add(card);
        }

        void AddPremiumItem(Premium.Product p, string badge = null)
        {
            bool can = Premium.CanBuy(p);
            var item = new VisualElement();
            item.AddToClassList("item");
            item.EnableInClassList("owned", !can);
            var img = new VisualElement();
            img.AddToClassList("item-img");
            var icon = ProductIcon(p);
            if (icon) img.style.backgroundImage = new StyleBackground(icon);
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var n = new Label(badge != null ? $"{p.name} · {badge}" : p.name);
            n.AddToClassList("item-name");
            var sub = new Label(p.desc);
            sub.AddToClassList("item-sub");
            col.Add(n); col.Add(sub);
            var b = new Button();
            b.AddToClassList("btn"); b.AddToClassList("btn-sm");
            var l = new Label(can ? p.price : "ALINDI");
            b.Add(l);
            if (can) { b.AddToClassList("btn-sun"); b.clicked += () => Premium.Request(p.id); }
            else { b.AddToClassList("btn-paper"); b.AddToClassList("owned"); b.SetEnabled(false); }
            item.Add(img); item.Add(col); item.Add(b);
            _shopScroll.Add(item);
        }

        VisualElement GemCard(Premium.Product p)
        {
            var c = new VisualElement();
            c.AddToClassList("prem-gem");
            if (p.id == "gems_1200") { var hot = new Label("EN İYİ"); hot.AddToClassList("prem-hot"); c.Add(hot); }
            var ic = new VisualElement();
            ic.AddToClassList("prem-gem-ic");
            ic.style.backgroundImage = new StyleBackground(GemIcon);
            var amount = new Label(FormatNum(p.gems));
            amount.AddToClassList("prem-gem-num");
            var b = new Button(() => Premium.Request(p.id));
            b.AddToClassList("btn"); b.AddToClassList("btn-sun"); b.AddToClassList("prem-gem-btn");
            b.Add(new Label(p.price));
            c.Add(ic); c.Add(amount); c.Add(b);
            return c;
        }

        // ------------------------------------------------------------------ pencereler
        VisualElement PremCard(string ribbon, float width, out VisualElement card, string ribbonClass = null)
        {
            if (PremModalOpen) _premModal.RemoveFromHierarchy();
            var overlay = new VisualElement();
            overlay.AddToClassList("screen"); overlay.AddToClassList("overlay");
            card = new VisualElement();
            card.AddToClassList("modal"); card.AddToClassList("v3-panel");
            card.style.width = width;
            var rib = new Label(ribbon);
            rib.AddToClassList("ribbon");
            if (ribbonClass != null) rib.AddToClassList(ribbonClass);
            card.Add(rib);
            var close = new Button(() => overlay.RemoveFromHierarchy());
            close.AddToClassList("close");
            var x = new VisualElement();
            x.AddToClassList("x-ic");
            close.Add(x);
            card.Add(close);
            overlay.Add(card);
            _hudRoot.Add(overlay);
            _premModal = overlay;
            Sfx.Play(SfxId.Pop);
            return overlay;
        }

        static Label Note(string text)
        {
            var l = new Label(text);
            l.AddToClassList("small-note");
            l.style.marginTop = 10;
            return l;
        }

        Button ModalButton(string text, string variant, Action onClick)
        {
            var b = new Button(onClick);
            b.AddToClassList("btn");
            if (!string.IsNullOrEmpty(variant)) b.AddToClassList(variant);
            b.Add(new Label(text));
            return b;
        }

        void OpenVipDetail()
        {
            var overlay = PremCard("VIP ÜYELİK", 560f, out var card, "ribbon-plum");
            foreach (var (icon, text) in new[] { ("play", "Reklamsız: tüm reklam ödülleri anında"), ("coin", "Kalıcı ×2 gelir (diğer çarpanlarla birleşir)"),
                                                 ("gem", $"Her gün {Premium.VipDailyGems} elmas"), ("star", "Tabelada VIP rozeti, mor isim") })
            {
                var row = new VisualElement();
                row.AddToClassList("prem-perk");
                var ic = new VisualElement();
                ic.AddToClassList("prem-perk-ic");
                ic.style.backgroundImage = new StyleBackground(KitIcon(icon));
                row.Add(ic);
                row.Add(new Label(text));
                card.Add(row);
            }
            if (Premium.Owned("vip_life")) { card.Add(Note("Ömür boyu VIP'sin · teşekkürler!")); return; }
            var actions = new VisualElement();
            actions.AddToClassList("popup-actions");
            actions.Add(ModalButton(Premium.Find("vip_month").price, "btn-sun", () => { overlay.RemoveFromHierarchy(); Premium.Request("vip_month"); }));
            var life = ModalButton($"Ömür boyu · {Premium.Find("vip_life").price}", "btn-plum", () => { overlay.RemoveFromHierarchy(); Premium.Request("vip_life"); });
            life.AddToClassList("last");
            actions.Add(life);
            card.Add(actions);
            card.Add(Note(Premium.IsVip ? "VIP aktif · aylık alım süreyi 30 gün uzatır" : "Abonelik Google Play'den yönetilir · istediğin zaman iptal"));
        }

        void OnPurchaseRequested(Premium.Product p)
        {
            if (!Premium.TestStore)
            {
                var o = PremCard("MAĞAZA", 460f, out var c);
                c.Add(Note("Mağaza yakında açılıyor"));
                var ok = ModalButton("TAMAM", "btn-paper", () => o.RemoveFromHierarchy());
                ok.style.marginTop = 12;
                c.Add(ok);
                return;
            }
            var overlay = PremCard("SATIN AL", 500f, out var card);
            var head = new VisualElement();
            head.AddToClassList("prem-confirm-head");
            var img = new VisualElement();
            img.AddToClassList("prem-confirm-img");
            var icon = ProductIcon(p);
            if (icon) img.style.backgroundImage = new StyleBackground(icon);
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var n = new Label(p.name);
            n.AddToClassList("item-name");
            var d = new Label(p.desc);
            d.AddToClassList("item-sub");
            col.Add(n); col.Add(d);
            head.Add(img); head.Add(col);
            card.Add(head);
            var total = new VisualElement();
            total.AddToClassList("dayend-line");
            total.Add(new Label("Toplam"));
            var price = new Label(p.price);
            price.AddToClassList("val");
            total.Add(price);
            card.Add(total);
            var actions = new VisualElement();
            actions.AddToClassList("popup-actions");
            actions.Add(ModalButton("VAZGEÇ", "btn-paper", () => overlay.RemoveFromHierarchy()));
            var ok2 = ModalButton("ONAYLA", "btn-teal", () => { overlay.RemoveFromHierarchy(); Premium.Grant(p.id); });
            ok2.AddToClassList("last");
            actions.Add(ok2);
            card.Add(actions);
            card.Add(Note("TEST · geliştirme sürümü: gerçek ödeme alınmaz"));
        }

        void OnPremiumGranted(Premium.Product p)
        {
            Toast(p.kind == Premium.Kind.Gems ? $"+{p.gems} elmas!" : $"{p.name} alındı!");
            Confetti(26);
            Sfx.Play(SfxId.Buy);
            if (!_shopRoot.ClassListContains("hidden")) BuildShop();
            RefreshTop();
        }

        /// <summary>Sv3: başlangıç paketi teklifi (bir kez kendiliğinden; sonra 72 saat Hazine'de).</summary>
        public void OpenStarterOffer()
        {
            var overlay = PremCard("BAŞLANGIÇ PAKETİ", 560f, out var card);
            var left = Premium.StarterLeft;
            var timer = new Label($"Sınırlı · {(int)left.TotalHours / 24}g {left.Hours}s");
            timer.AddToClassList("prem-timer");
            card.Add(timer);
            var row = new VisualElement();
            row.AddToClassList("prem-starter-row");
            void Box(Sprite s, string big, string small)
            {
                var b = new VisualElement();
                b.AddToClassList("prem-gem");
                var ic = new VisualElement();
                ic.AddToClassList("prem-gem-ic");
                ic.style.backgroundImage = new StyleBackground(s);
                var t = new Label(big);
                t.AddToClassList("prem-gem-num");
                var l = new Label(small);
                l.AddToClassList("item-sub");
                b.Add(ic); b.Add(t); b.Add(l);
                row.Add(b);
            }
            Box(CoinIcon, "5.000", "altın");
            Box(GemIcon, "100", "elmas");
            Box(LoadIcon("DEC_AltinFici"), "Altın Fıçı", "süs");
            card.Add(row);
            var buy = ModalButton(Premium.Find("starter").price, "btn-sun", () => { overlay.RemoveFromHierarchy(); Premium.Request("starter"); });
            buy.AddToClassList("prem-starter-buy");
            card.Add(buy);
            card.Add(Note("Hesap başına bir kez · sonra 3 gün Hazine'de durur"));
        }
    }
}
