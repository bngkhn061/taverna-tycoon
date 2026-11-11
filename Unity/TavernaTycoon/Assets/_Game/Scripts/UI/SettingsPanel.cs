using System;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>Settings.uxml şablonunu bağlar (ana menü ve oyun içinde ortak).</summary>
    public class SettingsPanel
    {
        readonly VisualElement _root;
        readonly Button _vib, _notif;
        readonly Button[] _lang, _gfx;

        public event Action ToMenuRequested;
        public event Action ResetRequested;
        /// <summary>v0.9: Ayarlar › Taverna adı › DEĞİŞTİR (sadece oyun içinde).</summary>
        public event Action RenameRequested;
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
            _notif.clicked += () => { GameSettings.Notifications = !GameSettings.Notifications; Refresh(); };

            _lang = new[] { _root.Q<Button>("lang-tr"), _root.Q<Button>("lang-en") };
            _lang[0].clicked += () => { GameSettings.Language = "TR"; Refresh(); };
            _lang[1].clicked += () => { GameSettings.Language = "EN"; Refresh(); };

            _gfx = new[] { _root.Q<Button>("gfx-0"), _root.Q<Button>("gfx-1"), _root.Q<Button>("gfx-2") };
            for (int i = 0; i < _gfx.Length; i++)
            {
                int lv = i;
                _gfx[i].clicked += () => { GameSettings.Graphics = lv; Refresh(); };
            }

            var toMenu = _root.Q<Button>("to-menu");
            toMenu.EnableInClassList("hidden", !showToMenu);
            toMenu.clicked += () => ToMenuRequested?.Invoke();

            if (UnityEngine.Debug.isDebugBuild && !Services.ReleaseSimulation)
            {
                _metrics = new Label { name = "metrics-summary" };
                _metrics.AddToClassList("small-note");
                _metrics.AddToClassList("metrics-note");
                var card = _root.Q(className: "settings-card");
                var actions = card?.Q(className: "sheet-actions");
                if (card != null && actions != null) card.Insert(card.IndexOf(actions), _metrics);
                else _root.Add(_metrics);
            }

            var reset = _root.Q<Button>("reset-save");
            bool armed = false;
            reset.clicked += () =>
            {
                // İki adımlı onay: yanlışlıkla silmeyi önler
                if (!armed) { armed = true; reset.text = "EMİN MİSİN? TEKRAR"; return; }
                armed = false;
                reset.text = "KAYDI SIFIRLA";
                GameState.ResetSave();
                ResetRequested?.Invoke();
                Hide();
            };
            Refresh();
        }

        void Refresh()
        {
            _nameLabel.text = GameState.TavernName;
            if (_metrics != null)
                _metrics.text = Services.LocalAnalytics != null ? Services.LocalAnalytics.SummaryText() : "Ölçüm özeti: gerçek ölçüm hizmeti bağlı (veri panelde)";
            _vib.text = GameSettings.Vibration ? "AÇIK" : "KAPALI";
            _vib.EnableInClassList("on", GameSettings.Vibration);
            _notif.text = GameSettings.Notifications ? "AÇIK" : "KAPALI";
            _notif.EnableInClassList("on", GameSettings.Notifications);
            _lang[0].EnableInClassList("on", GameSettings.Language == "TR");
            _lang[1].EnableInClassList("on", GameSettings.Language == "EN");
            for (int i = 0; i < _gfx.Length; i++) _gfx[i].EnableInClassList("on", GameSettings.Graphics == i);
        }

        public bool Visible => !_root.ClassListContains("hidden");
        public void Show() { Refresh(); _root.RemoveFromClassList("hidden"); }
        public void Hide() => _root.AddToClassList("hidden");
    }
}
