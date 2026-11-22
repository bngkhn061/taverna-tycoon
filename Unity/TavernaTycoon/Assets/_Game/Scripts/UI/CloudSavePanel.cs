using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v2.3 Ayarlar › Bulut kaydı (GAME_DESIGN.md §17.6): Ayarlar kartında bir satır (durum + YÖNET) ve açılan pencere
    /// (sağlayıcı, son eşitleme, bu cihazın özeti, "ŞİMDİ YEDEKLE", "BULUTTAN YÜKLE"). Sağlayıcı yokken (yayında gerçek sağlayıcı
    /// bağlanmadan) düğmeler "YAKINDA" ve kapalıdır. Belirsiz çakışmada iki kaydı karşılaştırmalı gösterir, oyuncu seçer.
    /// Ana menü ve oyun içinde ortak (SettingsPanel kurar).
    /// </summary>
    public class CloudSavePanel
    {
        readonly VisualElement _host;
        readonly Label _rowStatus;
        VisualElement _overlay, _conflict;
        Label _result;

        /// <summary>Buluttaki kayıt bu cihaza yüklendi (sahibi sahneyi yeniden yükler).</summary>
        public event Action Restored;

        /// <summary>Test botu: pencere ve çakışma ekranı açık mı, son sonuç metni.</summary>
        public bool Visible => _overlay != null && _overlay.parent != null;
        public bool ConflictVisible => _conflict != null && _conflict.parent != null;
        public string LastResultText => _result != null ? _result.text : "";
        /// <summary>Test botu: çakışma ekranının karşılaştırma satırı.</summary>
        public string ConflictLine { get; private set; } = "";
        Action _keepLocal, _useCloud;

        /// <param name="settingsRoot">settings-root öğesi.</param>
        public CloudSavePanel(VisualElement settingsRoot)
        {
            _host = settingsRoot;
            var card = settingsRoot.Q(className: "settings-card");
            var last = card?.Q(className: "last");
            var row = new VisualElement { name = "cloud-row" };
            row.AddToClassList("srow");
            var ico = new VisualElement();
            ico.AddToClassList("ico");
            var img = new VisualElement();
            img.AddToClassList("ico-img");
            img.style.backgroundImage = new StyleBackground(Resources.Load<Sprite>("Kit/v3i_arrow"));
            img.style.rotate = new Rotate(-90f);
            ico.Add(img);
            row.Add(ico);
            var lab = new Label(Loc.T("Bulut kaydı"));
            lab.AddToClassList("row-label");
            row.Add(lab);
            _rowStatus = new Label();
            _rowStatus.AddToClassList("cloud-row-status");
            row.Add(_rowStatus);
            var open = new Button(Open) { name = "cloud-open", text = Loc.T("YÖNET") };
            open.AddToClassList("toggle");
            row.Add(open);
            if (card != null && last != null) card.Insert(card.IndexOf(last), row);
            else card?.Add(row);
            CloudSave.Changed += Refresh;
            Refresh();
        }

        /// <summary>Satırdaki durum metni (Ayarlar açılırken ve dil değişince).</summary>
        public void Refresh()
        {
            if (!CloudSave.Enabled) _rowStatus.text = Loc.T("Yakında");
            else
            {
                var t = Services.Cloud.LastSyncUtc;
                _rowStatus.text = t.HasValue ? Loc.F("Son: {0}", t.Value.ToLocalTime().ToString("dd.MM HH:mm")) : Loc.T("Henüz eşitlenmedi");
            }
            if (Visible) Build();
        }

        /// <summary>Bulut kaydı penceresini açar.</summary>
        public void Open()
        {
            Sfx.Play(SfxId.Pop);
            if (_overlay == null)
            {
                _overlay = new VisualElement { name = "cloud-overlay" };
                _overlay.AddToClassList("screen");
                _overlay.AddToClassList("overlay");
            }
            _host.Add(_overlay);
            if (_result != null) _result.text = "";
            Build();
        }

        /// <summary>Pencereleri kapatır.</summary>
        public void Close()
        {
            _overlay?.RemoveFromHierarchy();
            _conflict?.RemoveFromHierarchy();
        }

        void Build()
        {
            string keep = _result != null ? _result.text : "";
            _overlay.Clear();
            var card = Card(_overlay, Loc.T("BULUT KAYDI"), () => _overlay.RemoveFromHierarchy());
            bool on = CloudSave.Enabled;
            if (on)
            {
                card.Add(Line(Loc.F("Sağlayıcı: {0}", Services.Cloud.ProviderName), true));
                var t = Services.Cloud.LastSyncUtc;
                card.Add(Line(t.HasValue ? Loc.F("Son eşitleme: {0}", t.Value.ToLocalTime().ToString("dd.MM HH:mm")) : Loc.T("Henüz eşitlenmedi"), false));
                card.Add(Line(Loc.F("Bu cihaz: {0}", CloudSave.Summary(CloudSave.LocalMeta())), false));
                if (CloudSave.ConflictPending) card.Add(Note(Loc.T("Bulutta daha ileri bir kayıt olabilir: BULUTTAN YÜKLE ile karşılaştır."), true));
                card.Add(Note(Loc.T("Otomatik yedek: uygulama arka plana geçerken ve 10 dakikada bir."), false));
            }
            else
            {
                card.Add(Line(Loc.T("Bulut kaydı yakında"), true));
                card.Add(Note(Loc.T("Hesap bağlanınca ilerlemen cihazlar arasında taşınabilecek. Şimdilik kayıt yalnızca bu cihazda."), false));
            }
            var actions = new VisualElement();
            actions.AddToClassList("sheet-actions");
            var up = Btn(on ? Loc.T("ŞİMDİ YEDEKLE") : Loc.T("YAKINDA"), on ? "btn-teal" : "btn-paper", Backup);
            var down = Btn(on ? Loc.T("BULUTTAN YÜKLE") : Loc.T("YAKINDA"), on ? "btn-plum" : "btn-paper", RestoreClicked);
            up.name = "cloud-backup"; down.name = "cloud-restore";
            up.SetEnabled(on && !CloudSave.Busy); down.SetEnabled(on && !CloudSave.Busy);
            down.AddToClassList("last");
            actions.Add(up); actions.Add(down);
            card.Add(actions);
            _result = new Label(keep);
            _result.AddToClassList("small-note");
            _result.AddToClassList("cloud-result");
            card.Add(_result);
        }

        /// <summary>"Şimdi yedekle".</summary>
        public void Backup()
        {
            CloudSave.Backup(false, r =>
            {
                switch (r.outcome)
                {
                    case CloudSave.Outcome.Uploaded: Say(Loc.T("Yedeklendi.")); break;
                    case CloudSave.Outcome.NeedsChoice: ShowConflict(r); break;
                    default: Say(Fail(r)); break;
                }
            });
        }

        /// <summary>"Buluttan yükle".</summary>
        public void RestoreClicked()
        {
            CloudSave.Restore(r =>
            {
                switch (r.outcome)
                {
                    case CloudSave.Outcome.Restored: Say(Loc.T("Buluttaki kayıt yüklendi.")); Close(); Restored?.Invoke(); break;
                    case CloudSave.Outcome.UpToDate: Say(Loc.T("Bu cihaz zaten güncel.")); break;
                    case CloudSave.Outcome.NoCloudSave: Say(Loc.T("Bulutta kayıt yok.")); break;
                    case CloudSave.Outcome.Rejected: Say(Loc.T("Buluttaki kayıt doğrulanamadı (bozuk ya da imzasız); yüklenmedi.")); break;
                    case CloudSave.Outcome.NeedsChoice: ShowConflict(r); break;
                    default: Say(Fail(r)); break;
                }
            });
        }

        static string Fail(CloudSave.Result r) => r.outcome == CloudSave.Outcome.Unavailable ? Loc.T("Bulut kaydı yakında") : Loc.F("Olmadı: {0}", r.error ?? "-");

        void Say(string text)
        {
            if (_result != null) _result.text = text;
            Sfx.Play(SfxId.Pop);
        }

        // ------------------------------------------------------------------ çakışma ekranı
        /// <summary>İki kaydı yan yana gösterir: "BU CİHAZI TUT" (bulutun üstüne yazar) ya da "BULUTU YÜKLE".</summary>
        void ShowConflict(CloudSave.Result r)
        {
            _conflict?.RemoveFromHierarchy();
            _conflict = new VisualElement { name = "cloud-conflict" };
            _conflict.AddToClassList("screen");
            _conflict.AddToClassList("overlay");
            var card = Card(_conflict, Loc.T("KAYIT SEÇ"), () => { CloudSave.TrackChoice("vazgec"); _conflict.RemoveFromHierarchy(); });
            card.AddToClassList("cloud-conflict-card");
            ConflictLine = CloudSave.CompareLine(r.local, r.cloud);
            card.Add(Line(Loc.T("İki kayıt farklı. Hangisiyle devam edilsin?"), true));
            card.Add(Line(ConflictLine, false));
            var cols = new VisualElement();
            cols.AddToClassList("cloud-cols");
            cols.Add(Column(Loc.T("BU CİHAZ"), r.local));
            cols.Add(Column(Loc.T("BULUT"), r.cloud));
            card.Add(cols);
            card.Add(Note(Loc.T("Seçilmeyen kayıt silinir."), true));
            var actions = new VisualElement();
            actions.AddToClassList("sheet-actions");
            string json = r.cloudJson;
            _keepLocal = () =>
            {
                CloudSave.TrackChoice("cihaz");
                _conflict.RemoveFromHierarchy();
                CloudSave.ForceUpload(x => Say(x.outcome == CloudSave.Outcome.Uploaded ? Loc.T("Bu cihazın kaydı buluta yazıldı.") : Fail(x)));
            };
            _useCloud = () =>
            {
                CloudSave.TrackChoice("bulut");
                _conflict.RemoveFromHierarchy();
                if (CloudSave.ApplyCloud(json)) { Close(); Restored?.Invoke(); }
                else Say(Loc.T("Buluttaki kayıt doğrulanamadı (bozuk ya da imzasız); yüklenmedi."));
            };
            var a = Btn(Loc.T("BU CİHAZI TUT"), "btn-paper", () => _keepLocal());
            var b = Btn(Loc.T("BULUTU YÜKLE"), "btn-teal", () => _useCloud());
            b.AddToClassList("last");
            actions.Add(a); actions.Add(b);
            card.Add(actions);
            _host.Add(_conflict);
            Sfx.Play(SfxId.Pop);
        }

        /// <summary>Test botu: çakışma ekranında seçim (true: bulutu yükle, false: bu cihazı tut).</summary>
        public void DebugChoose(bool cloud) { if (ConflictVisible) (cloud ? _useCloud : _keepLocal)?.Invoke(); }

        static VisualElement Column(string title, CloudSaveMeta m)
        {
            var c = new VisualElement();
            c.AddToClassList("cloud-col");
            var t = new Label(title);
            t.AddToClassList("cloud-col-title");
            c.Add(t);
            if (m == null) { c.Add(Line(Loc.T("kayıt yok"), false)); return c; }
            c.Add(Line(Loc.F("Seviye {0}", m.level), true));
            c.Add(Line(Loc.F("{0} prestij", m.branch), false));
            c.Add(Line(Loc.F("Toplam kazanç: {0}", HudController.FormatNum(m.totalEarned)), false));
            c.Add(Line(Loc.F("Kayıt: {0}", CloudSave.When(m.savedUtc)), false));
            return c;
        }

        static VisualElement Card(VisualElement overlay, string ribbon, Action close)
        {
            var card = new VisualElement();
            card.AddToClassList("modal");
            card.AddToClassList("v3-panel");
            card.AddToClassList("cloud-card");
            var rib = new Label(ribbon);
            rib.AddToClassList("ribbon");
            card.Add(rib);
            var x = new Button(close);
            x.AddToClassList("close");
            var xi = new VisualElement();
            xi.AddToClassList("x-ic");
            x.Add(xi);
            card.Add(x);
            overlay.Add(card);
            return card;
        }

        static Label Line(string text, bool strong)
        {
            var l = new Label(text);
            l.AddToClassList("cloud-line");
            if (strong) l.AddToClassList("strong");
            return l;
        }

        static Label Note(string text, bool warn)
        {
            var l = new Label(text);
            l.AddToClassList("small-note");
            l.AddToClassList("cloud-note");
            if (warn) l.AddToClassList("warn");
            return l;
        }

        static Button Btn(string text, string variant, Action click)
        {
            var b = new Button(click);
            b.AddToClassList("btn");
            b.AddToClassList(variant);
            b.Add(new Label(text));
            return b;
        }
    }
}
