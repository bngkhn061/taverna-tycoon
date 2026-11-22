using System;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>Settings.uxml şablonunu bağlar (ana menü ve oyun içinde ortak).</summary>
    public class SettingsPanel
    {
        readonly VisualElement _root;
        readonly Button _vib, _notif;
        readonly Button[] _gfx;
        readonly Button _langOpen;
        readonly VisualElement _langPicker;

        public event Action ToMenuRequested;
        public event Action ResetRequested;
        /// <summary>v0.9: Ayarlar › Taverna adı › DEĞİŞTİR (sadece oyun içinde).</summary>
        public event Action RenameRequested;
        /// <summary>v2.1: Ayarlar › "Son sahneyi yeniden izle" (yalnızca oyun içinde ve son görüldükten sonra).</summary>
        public event Action WatchEndingRequested;
        /// <summary>v2.3: buluttaki kayıt yüklendi (sahibi sahneyi yeniden yükler).</summary>
        public event Action CloudRestored;
        /// <summary>v2.3: Ayarlar › Bulut kaydı satırı ve penceresi.</summary>
        public CloudSavePanel Cloud { get; }
        readonly Button _watchEnding;
        readonly Label _nameLabel;
        /// <summary>v1.6: yalnızca geliştirme derlemesinde görünen "Ölçüm özeti" satırı (yerel ölçüm sayaçları).</summary>
        readonly Label _metrics;

        public SettingsPanel(VisualElement host, bool showToMenu)
        {
            _root = host.Q("settings-root");
            _root.Q<Button>("settings-close").clicked += Hide;
            _root.Q("name-row").EnableInClassList("hidden", !showToMenu);
            _nameLabel = _root.Q<Label>("name-label");
            _root.Q<Button>("rename-open").clicked += () => RenameRequested?.Invoke();

            var music = _root.Q<Slider>("music");
            music.value = GameSettings.Music;
            music.RegisterValueChangedCallback(e => GameSettings.Music = e.newValue);
            var sfx = _root.Q<Slider>("sfx");
            sfx.value = GameSettings.Sfx;
            sfx.RegisterValueChangedCallback(e => GameSettings.Sfx = e.newValue);

            _vib = _root.Q<Button>("vibration");
            _vib.clicked += () => { GameSettings.Vibration = !GameSettings.Vibration; Refresh(); };
            _notif = _root.Q<Button>("notifications");
            // v1.7: açılınca izin istenir ve bildirimler planlanır; kapanınca hepsi iptal (Notifications)
            _notif.clicked += () => { Notifications.SetEnabled(!GameSettings.Notifications); Refresh(); };

            // v1.7 dil seçici: 10 dil, her biri kendi adıyla; seçim hemen uygulanır (Loc.Changed → açık arayüzler yenilenir)
            _langOpen = _root.Q<Button>("lang-open");
            _langPicker = _root.Q("lang-picker");
            _langOpen.clicked += () => { BuildLangGrid(); _langPicker.RemoveFromClassList("hidden"); Sfx.Play(SfxId.Pop); };
            _root.Q<Button>("lang-close").clicked += () => _langPicker.AddToClassList("hidden");

            _gfx = new[] { _root.Q<Button>("gfx-0"), _root.Q<Button>("gfx-1"), _root.Q<Button>("gfx-2") };
            for (int i = 0; i < _gfx.Length; i++)
            {
                int lv = i;
                _gfx[i].clicked += () => { GameSettings.Graphics = lv; Refresh(); };
            }

            var toMenu = _root.Q<Button>("to-menu");
            toMenu.EnableInClassList("hidden", !showToMenu);
            toMenu.clicked += () => ToMenuRequested?.Invoke();

            if (ShowMetricsRow(UnityEngine.Debug.isDebugBuild, Services.ReleaseSimulation))
            {
                _metrics = new Label { name = "metrics-summary" };
                _metrics.AddToClassList("small-note");
                _metrics.AddToClassList("metrics-note");
                var card = _root.Q(className: "settings-card");
                var actions = card?.Q(className: "sheet-actions");
                if (card != null && actions != null) card.Insert(card.IndexOf(actions), _metrics);
                else _root.Add(_metrics);
            }

            // v2.1: son sahne görüldüyse (yalnızca oyun içinde) "Son sahneyi yeniden izle"
            if (showToMenu)
            {
                _watchEnding = new Button(() => WatchEndingRequested?.Invoke()) { name = "watch-ending" };
                _watchEnding.AddToClassList("btn");
                _watchEnding.AddToClassList("btn-plum");
                _watchEnding.AddToClassList("watch-ending");
                var wl = new Label(Loc.T("SON SAHNEYİ YENİDEN İZLE"));
                _watchEnding.Add(wl);
                var card = _root.Q(className: "settings-card");
                var actions = card?.Q(className: "sheet-actions");
                if (card != null && actions != null) card.Insert(card.IndexOf(actions), _watchEnding);
            }

            // v2.3: Bulut kaydı satırı (Dil ile Grafik arası) ve penceresi
            Cloud = new CloudSavePanel(_root);
            Cloud.Restored += () => { Hide(); CloudRestored?.Invoke(); };

            var reset = _root.Q<Button>("reset-save");
            bool armed = false;
            reset.clicked += () =>
            {
                // İki adımlı onay: yanlışlıkla silmeyi önler
                if (!armed) { armed = true; reset.text = Loc.T("EMİN MİSİN? TEKRAR"); return; }
                armed = false;
                reset.text = Loc.T("KAYDI SIFIRLA");
                GameState.ResetSave();
                ResetRequested?.Invoke();
                Hide();
            };
            Refresh();
        }

        /// <summary>
        /// v1.8 (birim testi): geliştirici "Ölçüm özeti" satırı yalnızca geliştirme derlemesinde ve yayın taklidi kapalıyken kurulur.
        /// Satır Türkçe sabittir (geliştirme satırı, çevrilmez); yayın derlemesinde hiç oluşturulmaz.
        /// </summary>
        public static bool ShowMetricsRow(bool isDebugBuild, bool releaseSimulation) => isDebugBuild && !releaseSimulation;

        /// <summary>Düğme metinlerini ve seçili durumları yeniler (dil değişince sahibi de çağırır).</summary>
        public void Refresh()
        {
            _nameLabel.text = GameState.TavernName;
            if (_watchEnding != null)
            {
                _watchEnding.EnableInClassList("hidden", !Ending.Seen);
                var wl = _watchEnding.Q<Label>();
                if (wl != null) wl.text = Loc.T("SON SAHNEYİ YENİDEN İZLE");
            }
            if (_metrics != null)
                _metrics.text = Services.LocalAnalytics != null ? Services.LocalAnalytics.SummaryText() : Loc.T("Ölçüm özeti: gerçek ölçüm hizmeti bağlı (veri panelde)");
            _vib.text = GameSettings.Vibration ? Loc.T("AÇIK") : Loc.T("KAPALI");
            _vib.EnableInClassList("on", GameSettings.Vibration);
            _notif.text = GameSettings.Notifications ? Loc.T("AÇIK") : Loc.T("KAPALI");
            _notif.EnableInClassList("on", GameSettings.Notifications);
            int li = Loc.IndexOf(Loc.Lang);
            _langOpen.text = li >= 0 ? Loc.NativeNames[li] : Loc.Lang;
            if (!_langPicker.ClassListContains("hidden")) BuildLangGrid();
            for (int i = 0; i < _gfx.Length; i++) _gfx[i].EnableInClassList("on", GameSettings.Graphics == i);
            Cloud?.Refresh();
        }

        /// <summary>Dil seçicinin düğmeleri: <see cref="Loc.Codes"/> sırasıyla, dilin kendi adı; etkin dil vurgulu.</summary>
        void BuildLangGrid()
        {
            var grid = _root.Q("lang-grid");
            grid.Clear();
            for (int i = 0; i < Loc.Codes.Length; i++)
            {
                string code = Loc.Codes[i];
                var b = new Button { text = Loc.NativeNames[i], name = "lang-" + code };
                b.AddToClassList("toggle");
                b.AddToClassList("lang-item");
                b.EnableInClassList("on", code == Loc.Lang);
                b.clicked += () =>
                {
                    Sfx.Play(SfxId.Pop);
                    Loc.SetLanguage(code);   // Changed → sahibi arayüzü yeniler (bu paneli de)
                    Refresh();
                };
                grid.Add(b);
            }
        }

        /// <summary>Test botu: dil seçiciyi açar.</summary>
        public void DebugOpenLanguages() { BuildLangGrid(); _langPicker.RemoveFromClassList("hidden"); }

        public bool Visible => !_root.ClassListContains("hidden");
        public void Show() { Refresh(); _root.RemoveFromClassList("hidden"); }
        public void Hide() { _langPicker.AddToClassList("hidden"); Cloud?.Close(); _root.AddToClassList("hidden"); }
    }
}
