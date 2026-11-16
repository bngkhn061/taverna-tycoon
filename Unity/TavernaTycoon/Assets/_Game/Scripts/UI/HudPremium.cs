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

        /// <summary>v1.8: joystick'i kilitlemeyen paneller (görevler, depo, etkinlikler, yükseltme paneli, mutluluk kartı) açık mı?</summary>
        bool AnyPanelOpen => !_questsPanel.ClassListContains("hidden") || !_depotPanel.ClassListContains("hidden") || !_eventsPanel.ClassListContains("hidden")
                             || _panelStation || !_happyCard.ClassListContains("hidden");
        /// <summary>Test botu: Hazine penceresi (teklif, VIP, onay) açık mı?</summary>
        public bool PremiumModalVisible => PremModalOpen;

        void InitPremium()
        {
            _root.Q<Button>("tab-prem").clicked += () => SelectTab("prem");
            Premium.Unavailable += OnStoreUnavailable;
            Premium.Failed += OnPurchaseFailed;
            Premium.Granted += OnPremiumGranted;
            TestStoreService.Confirmer = OpenTestConfirm;   // v1.6: test mağazasının onay ekranı
        }

        void DisablePremium()
        {
            Premium.Unavailable -= OnStoreUnavailable;
            Premium.Failed -= OnPurchaseFailed;
            Premium.Granted -= OnPremiumGranted;
            TestStoreService.Confirmer = null;
        }

        /// <summary>v1.6: mağaza kapalıyken (yayın derlemesi, gerçek mağaza yok) ürün düğmesinin yazısı.</summary>
        static string SoonText => Loc.T("YAKINDA");

        void UpdatePremium()
        {
            // Sv3: başlangıç paketi teklifi bir kez, sakin bir anda
            // v1.8: başka hiçbir pencere / panel açık değilken (görevler, depo, etkinlikler, yükseltme, mutluluk kartı dahil)
            if (Premium.Available && Premium.ShouldOfferStarter && !ModalOpen && !AnyPanelOpen && !Tutorial.Active && !LevelCardOpen && !DayCycle.Ended && Time.timeSinceLevelLoad > 5f)
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
            _shopNote.text = !Premium.Available ? Loc.T("Hazine yakında açılıyor · şimdilik satın alma yok")
                : Premium.TestStore ? Loc.T("TEST SÜRÜMÜ · gerçek ödeme alınmaz · fiyatlar örnek")
                : Loc.T("Satın almalar hesaba bağlıdır · ilerlemek için gerekmez");
            AddVipCard();
            if (Premium.StarterAvailable)
            {
                var left = Premium.StarterLeft;
                AddShopSection(Loc.F("SINIRLI · BAŞLANGIÇ PAKETİ · {0}g {1}s", (int)left.TotalHours / 24, left.Hours));
                AddPremiumItem(Premium.Find("starter"), Loc.T("%70 indirim"));
            }
            AddShopSection(Loc.T("EFSANEVİ PERSONEL"));
            AddPremiumItem(Premium.Find("staff_lirael"));
            AddPremiumItem(Premium.Find("staff_ignis"));
            AddShopSection(Loc.T("PREMIUM DEKOR"));
            foreach (var id in new[] { "thm_altin", "dec_kristal", "dec_ejder", "frame_kraliyet" }) AddPremiumItem(Premium.Find(id));
            AddShopSection(Loc.T("ELMAS"));
            var row = new VisualElement();
            row.AddToClassList("prem-gems");
            foreach (var id in new[] { "gems_100", "gems_550", "gems_1200", "gems_3000" }) row.Add(GemCard(Premium.Find(id)));
            _shopScroll.Add(row);
            var restore = new Button(() => Premium.Restore(n => Toast(n < 0 ? Loc.T("Mağaza yakında açılıyor") : Loc.F("Geri yüklendi: {0} ürün hesabında", n)))) { text = Loc.T("SATIN ALMALARI GERİ YÜKLE") };
            restore.AddToClassList("btn"); restore.AddToClassList("btn-paper"); restore.AddToClassList("btn-sm"); restore.AddToClassList("prem-restore");
            _shopScroll.Add(restore);
        }

        void AddVipCard()
        {
            var card = new Button(OpenVipDetail);
            card.AddToClassList("prem-vip");
            var crown = new Label(Loc.T("VIP"));
            crown.AddToClassList("prem-crown");
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var title = new Label(Premium.IsVip ? (Premium.Owned("vip_life") ? Loc.T("VIP · ömür boyu") : Loc.T("VIP · aktif")) : Loc.T("VIP Üyelik"));
            title.AddToClassList("prem-vip-title");
            var perks = new VisualElement();
            perks.AddToClassList("prem-perks");
            foreach (var t in new[] { Loc.T("Reklamsız"), Loc.T("Kalıcı ×2 gelir"), Loc.F("Günde {0} elmas", Premium.VipDailyGems), Loc.T("VIP rozet") })
            {
                var c = new Label(t);
                c.AddToClassList("chip");
                perks.Add(c);
            }
            col.Add(title); col.Add(perks);
            var price = new Label(Premium.Owned("vip_life") ? Loc.T("AKTİF") : Premium.Available ? Premium.PriceOf(Premium.Find("vip_month")) : SoonText);
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
            var n = new Label(badge != null ? $"{Loc.T(p.name)} · {badge}" : Loc.T(p.name));
            n.AddToClassList("item-name");
            var sub = new Label(Loc.T(p.desc));
            sub.AddToClassList("item-sub");
            col.Add(n); col.Add(sub);
            var b = new Button();
            b.AddToClassList("btn"); b.AddToClassList("btn-sm");
            bool open = Premium.Available;
            var l = new Label(!can ? Loc.T("ALINDI") : open ? Premium.PriceOf(p) : SoonText);
            b.Add(l);
            if (can && open) { b.AddToClassList("btn-sun"); b.clicked += () => Premium.Request(p.id); }
            else if (can) { b.AddToClassList("btn-paper"); b.AddToClassList("locked"); b.AddToClassList("prem-soon"); b.SetEnabled(false); }
            else { b.AddToClassList("btn-paper"); b.AddToClassList("owned"); b.SetEnabled(false); }
            item.Add(img); item.Add(col); item.Add(b);
            _shopScroll.Add(item);
        }

        VisualElement GemCard(Premium.Product p)
        {
            var c = new VisualElement();
            c.AddToClassList("prem-gem");
            if (p.id == "gems_1200") { var hot = new Label(Loc.T("EN İYİ")); hot.AddToClassList("prem-hot"); c.Add(hot); }
            var ic = new VisualElement();
            ic.AddToClassList("prem-gem-ic");
            ic.style.backgroundImage = new StyleBackground(GemIcon);
            var amount = new Label(FormatNum(p.gems));
            amount.AddToClassList("prem-gem-num");
            bool open = Premium.Available;
            var b = new Button(() => { if (Premium.Available) Premium.Request(p.id); });
            b.AddToClassList("btn"); b.AddToClassList(open ? "btn-sun" : "btn-paper"); b.AddToClassList("prem-gem-btn");
            if (!open) { b.AddToClassList("locked"); b.AddToClassList("prem-soon"); b.SetEnabled(false); }
            b.Add(new Label(open ? Premium.PriceOf(p) : SoonText));
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
            var overlay = PremCard(Loc.T("VIP ÜYELİK"), 560f, out var card, "ribbon-plum");
            foreach (var (icon, text) in new[] { ("play", Loc.T("Reklamsız: tüm reklam ödülleri anında")), ("coin", Loc.T("Kalıcı ×2 gelir (diğer çarpanlarla birleşir)")),
                                                 ("gem", Loc.F("Her gün {0} elmas", Premium.VipDailyGems)), ("star", Loc.T("Tabelada VIP rozeti, mor isim")) })
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
            if (Premium.Owned("vip_life")) { card.Add(Note(Loc.T("Ömür boyu VIP'sin · teşekkürler!"))); return; }
            if (!Premium.Available) { card.Add(Note(Loc.T("Hazine yakında açılıyor"))); return; }
            var actions = new VisualElement();
            actions.AddToClassList("popup-actions");
            actions.Add(ModalButton(Premium.PriceOf(Premium.Find("vip_month")), "btn-sun", () => { overlay.RemoveFromHierarchy(); Premium.Request("vip_month"); }));
            var life = ModalButton(Loc.F("Ömür boyu · {0}", Premium.PriceOf(Premium.Find("vip_life"))), "btn-plum", () => { overlay.RemoveFromHierarchy(); Premium.Request("vip_life"); });
            life.AddToClassList("last");
            actions.Add(life);
            card.Add(actions);
            card.Add(Note(Premium.IsVip ? Loc.T("VIP aktif · aylık alım süreyi 30 gün uzatır") : Loc.T("Abonelik Google Play'den yönetilir · istediğin zaman iptal")));
        }

        /// <summary>v1.6: mağaza kapalı (yayın derlemesi, gerçek mağaza yok): "Yakında" bilgisi, ürün verilmez.</summary>
        void OnStoreUnavailable(Premium.Product p)
        {
            var o = PremCard(Loc.T("MAĞAZA"), 460f, out var c);
            c.Add(Note(Loc.T("Mağaza yakında açılıyor")));
            var ok = ModalButton(Loc.T("TAMAM"), "btn-paper", () => o.RemoveFromHierarchy());
            ok.style.marginTop = 12;
            c.Add(ok);
        }

        void OnPurchaseFailed(Premium.Product p, string why)
        {
            if (why != "vazgeçildi") Toast(Loc.F("Satın alma tamamlanmadı: {0}", why));
        }

        /// <summary>v1.6: <see cref="TestStoreService"/> onay ekranı (yalnızca geliştirme derlemesi): ONAYLA ödemeyi başarılı sayar.</summary>
        void OpenTestConfirm(string productId, Action confirm, Action cancel)
        {
            var p = Premium.Find(productId);
            var overlay = PremCard(Loc.T("SATIN AL"), 500f, out var card);
            var head = new VisualElement();
            head.AddToClassList("prem-confirm-head");
            var img = new VisualElement();
            img.AddToClassList("prem-confirm-img");
            var icon = ProductIcon(p);
            if (icon) img.style.backgroundImage = new StyleBackground(icon);
            var col = new VisualElement();
            col.AddToClassList("item-col");
            var n = new Label(Loc.T(p.name));
            n.AddToClassList("item-name");
            var d = new Label(Loc.T(p.desc));
            d.AddToClassList("item-sub");
            col.Add(n); col.Add(d);
            head.Add(img); head.Add(col);
            card.Add(head);
            var total = new VisualElement();
            total.AddToClassList("dayend-line");
            total.Add(new Label(Loc.T("Toplam")));
            var price = new Label(Premium.PriceOf(p));
            price.AddToClassList("val");
            total.Add(price);
            card.Add(total);
            var actions = new VisualElement();
            actions.AddToClassList("popup-actions");
            actions.Add(ModalButton(Loc.T("VAZGEÇ"), "btn-paper", () => { overlay.RemoveFromHierarchy(); cancel?.Invoke(); }));
            var ok2 = ModalButton(Loc.T("ONAYLA"), "btn-teal", () => { overlay.RemoveFromHierarchy(); confirm?.Invoke(); });
            ok2.AddToClassList("last");
            actions.Add(ok2);
            card.Add(actions);
            card.Add(Note(Loc.T("TEST · geliştirme sürümü: gerçek ödeme alınmaz")));
        }

        void OnPremiumGranted(Premium.Product p)
        {
            Toast(p.kind == Premium.Kind.Gems ? Loc.F("+{0} elmas!", p.gems) : Loc.F("{0} alındı!", Loc.T(p.name)));
            Confetti(26);
            Sfx.Play(SfxId.Buy);
            if (!_shopRoot.ClassListContains("hidden")) BuildShop();
            RefreshTop();
        }

        /// <summary>Sv3: başlangıç paketi teklifi (bir kez kendiliğinden; sonra 72 saat Hazine'de).</summary>
        public void OpenStarterOffer()
        {
            var overlay = PremCard(Loc.T("BAŞLANGIÇ PAKETİ"), 560f, out var card);
            var left = Premium.StarterLeft;
            var timer = new Label(Loc.F("Sınırlı · {0}g {1}s", (int)left.TotalHours / 24, left.Hours));
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
            Box(CoinIcon, "5.000", Loc.T("altın"));
            Box(GemIcon, "100", Loc.T("elmas"));
            Box(LoadIcon("DEC_AltinFici"), Loc.T("Altın Fıçı"), Loc.T("süs"));
            card.Add(row);
            var buy = ModalButton(Premium.PriceOf(Premium.Find("starter")), "btn-sun", () => { overlay.RemoveFromHierarchy(); Premium.Request("starter"); });
            buy.AddToClassList("prem-starter-buy");
            card.Add(buy);
            card.Add(Note(Loc.T("Hesap başına bir kez · sonra 3 gün Hazine'de durur")));
        }
    }
}
