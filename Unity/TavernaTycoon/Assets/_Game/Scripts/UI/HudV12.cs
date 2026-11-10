using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v1.2 arayüzü (design/ui/ui_v12_akis.html, onay 2026-11-10): Mutluluk barı (H) ve uçan "+3" yazıları,
    /// Etkinlikler butonu / çekmecesi / mor şerit (E), rehber v2'de yan butonların adım adım açılması (R1–R3).
    /// </summary>
    public partial class HudController
    {
        VisualElement _happyFill, _happyCard, _eventsPanel, _eventRows, _gemPill;
        Label _happyNum, _happyCardText, _eventBanner, _eventNew, _eventsNote;
        Button _eventBtn;
        FaceElement _happyFace;
        readonly List<(Label l, Vector3 world, float t)> _happyFloats = new();
        float _eventsRefreshT;

        /// <summary>Etkinlik butonunun açıldığı seviye.</summary>
        const int EventsLevel = 4;

        void InitV12()
        {
            var pill = _root.Q<Button>("happy-pill");
            _happyFace = new FaceElement();
            _happyFace.AddToClassList("happy-face");
            pill.Insert(0, _happyFace);
            _happyFill = _root.Q("happy-fill");
            _happyNum = _root.Q<Label>("happy-num");
            _happyCard = _root.Q("happy-card");
            _happyCardText = _root.Q<Label>("happy-card-text");
            pill.clicked += () =>
            {
                Sfx.Play(SfxId.Pop);
                _happyCard.ToggleInClassList("hidden");
                RefreshHappyCard();
            };
            _gemPill = _root.Q("gem-pill");

            _eventBtn = _root.Q<Button>("event-btn");
            _eventNew = _root.Q<Label>("event-new");
            _eventNew.style.color = (Color)new Color32(251, 243, 226, 255);
            _eventsPanel = _root.Q("events-panel");
            _eventRows = _root.Q("event-rows");
            _eventsNote = _root.Q<Label>("events-note");
            _eventBanner = _root.Q<Label>("event-banner");
            _eventBtn.clicked += OpenEvents;
            _root.Q<Button>("events-close").clicked += () => _eventsPanel.AddToClassList("hidden");

            Happiness.Changed += OnHappinessChanged;
            TavernEvents.Changed += OnEventsChanged;
            SignTier.Upgraded += OnSignUpgraded;
        }

        void OnSignUpgraded(int tier)
        {
            Toast($"Tabelan yenilendi: {SignTier.Names[tier]}!");
            Sfx.Play(SfxId.Unlock);
            Confetti(18);
        }

        void DisableV12()
        {
            Happiness.Changed -= OnHappinessChanged;
            TavernEvents.Changed -= OnEventsChanged;
            SignTier.Upgraded -= OnSignUpgraded;
        }

        void UpdateV12()
        {
            // Mutluluk barı
            float v = Happiness.Value;
            _happyFill.style.width = Length.Percent(v);
            _happyFill.style.backgroundColor = (Color)(v >= 50f ? new Color32(106, 168, 79, 255) : v >= 25f ? new Color32(242, 182, 49, 255) : new Color32(212, 85, 58, 255));
            _happyNum.text = $"%{Mathf.RoundToInt(v)}";
            _happyFace.SetMood(Happiness.Mood);

            // Uçan mutluluk yazıları
            for (int i = _happyFloats.Count - 1; i >= 0; i--)
            {
                var (l, w, t) = _happyFloats[i];
                t += Time.deltaTime;
                ToPanel(w + Vector3.up * (0.3f + t * 0.8f), out var p);
                l.style.left = p.x - 16f;
                l.style.top = p.y;
                l.style.opacity = 1f - Mathf.Clamp01((t - 0.7f) / 0.5f);
                if (t > 1.2f) { l.RemoveFromHierarchy(); _happyFloats.RemoveAt(i); }
                else _happyFloats[i] = (l, w, t);
            }

            // Rehber v2: yan butonlar adım adım açılır; elmas, yardımcı ve etkinlik rehber bitince
            SetRevealed("open-quests", Tutorial.Reveals(Tutorial.StepQuests));
            SetRevealed("open-shop", Tutorial.Reveals(Tutorial.StepShop));
            SetRevealed("open-shop-plus", Tutorial.Reveals(Tutorial.StepShop));
            SetRevealed("open-staff", Tutorial.Reveals(Tutorial.StepStaff));
            SetRevealed("open-depot", Tutorial.Reveals(Tutorial.StepDepot));
            _gemPill.EnableInClassList("tut-hidden", Tutorial.Active);
            _helperBtn.EnableInClassList("tut-hidden", Tutorial.Active);
            _root.Q("happy-pill").EnableInClassList("tut-hidden", Tutorial.Active && Tutorial.Step < 3);

            // Etkinlik butonu (Sv4, rehber bitince) + mor şerit
            bool evOn = !Tutorial.Active && GameState.Level >= EventsLevel;
            _eventBtn.EnableInClassList("hidden", !evOn);
            _eventNew.EnableInClassList("hidden", !evOn || GameState.Data.eventsSeen);
            var act = TavernEvents.Active;
            _eventBanner.EnableInClassList("hidden", !act.HasValue);
            if (act.HasValue)
            {
                int left = Mathf.CeilToInt(TavernEvents.Remaining);
                _eventBanner.text = $"{TavernEvents.Get(act.Value).name.ToUpper(new System.Globalization.CultureInfo("tr-TR"))} · {left / 60}:{left % 60:00} · {TavernEvents.Effect(act.Value)}";
                _eventBanner.style.top = RushHour.Active ? 196f : 146f;
            }
            if (!_eventsPanel.ClassListContains("hidden"))
            {
                _eventsRefreshT -= Time.unscaledDeltaTime;
                if (_eventsRefreshT <= 0f) { _eventsRefreshT = 1f; BuildEvents(); }
            }
        }

        void SetRevealed(string name, bool on) => _root.Q(name)?.EnableInClassList("tut-hidden", !on);

        void OnHappinessChanged(float d, Vector3 at)
        {
            var l = new Label((d > 0 ? "+" : "−") + Mathf.RoundToInt(Mathf.Abs(d)) + " mutluluk") { pickingMode = PickingMode.Ignore };
            l.AddToClassList("float-happy");
            l.EnableInClassList("down", d < 0f);
            _world.Add(l);
            _happyFloats.Add((l, at, 0f));
            if (!_happyCard.ClassListContains("hidden")) RefreshHappyCard();
        }

        void RefreshHappyCard()
        {
            _happyCardText.text =
                $"Çabuk servis: +{Happiness.FastServe:0} · zamanında: +{Happiness.NormalServe:0}\n" +
                $"Bekleyip kızgın giden: −{Happiness.AngryLoss:0} · kirli masa: yavaşça düşer\n" +
                $"Şu an %{Mathf.RoundToInt(Happiness.Value)}: müşteri gelişi ×{Happiness.SpawnMultiplier:0.0#}\n" +
                "★ yıldızlar ayrı: dekorla artar, zengin müşteri ve bahşiş getirir.";
        }

        // ------------------------------------------------------------------ etkinlikler
        void OpenEvents()
        {
            CloseUpgrade();
            GameState.Data.eventsSeen = true;
            BuildEvents();
            Show(_eventsPanel);
        }

        void OnEventsChanged() { if (!_eventsPanel.ClassListContains("hidden")) BuildEvents(); }

        void BuildEvents()
        {
            _eventRows.Clear();
            _eventsNote.text = "";
            foreach (var d in TavernEvents.List)
            {
                var st = TavernEvents.StateOf(d);
                var row = new VisualElement();
                row.AddToClassList("ev-row");
                row.EnableInClassList("run", st == TavernEvents.State.Running);
                row.EnableInClassList("lock", st == TavernEvents.State.LevelLow || st == TavernEvents.State.ZoneLocked);
                var img = new VisualElement();
                img.AddToClassList("ev-img");
                var icon = LoadIcon(d.icon);
                if (icon) img.style.backgroundImage = new StyleBackground(icon);
                var col = new VisualElement();
                col.AddToClassList("ev-col");
                var n = new Label(d.name);
                n.AddToClassList("item-name");
                string sub = st switch
                {
                    TavernEvents.State.LevelLow => $"Seviye {d.level}'te açılır",
                    TavernEvents.State.ZoneLocked => $"Önce {d.zoneName} açılmalı",
                    TavernEvents.State.UsedToday => "Bugün yapıldı · yarın yine",
                    TavernEvents.State.Busy => $"{d.desc} · başka etkinlik sürüyor",
                    _ => $"3 dk · {d.desc}",
                };
                var s = new Label(sub);
                s.AddToClassList("item-sub");
                col.Add(n); col.Add(s);
                row.Add(img); row.Add(col);

                var b = new Button();
                b.AddToClassList("btn"); b.AddToClassList("btn-sm");
                var pi = new VisualElement();
                pi.AddToClassList("price-icon");
                b.Add(pi);
                switch (st)
                {
                    case TavernEvents.State.Running:
                        b.AddToClassList("owned");
                        pi.style.backgroundImage = new StyleBackground(KitIcon("clock"));
                        int left = Mathf.CeilToInt(TavernEvents.Remaining);
                        b.Add(new Label($"{left / 60}:{left % 60:00}"));
                        b.SetEnabled(false);
                        break;
                    case TavernEvents.State.LevelLow:
                    case TavernEvents.State.ZoneLocked:
                    case TavernEvents.State.UsedToday:
                        b.AddToClassList("locked");
                        pi.style.backgroundImage = new StyleBackground(KitIcon(st == TavernEvents.State.UsedToday ? "check" : "lock"));
                        b.Add(new Label(st == TavernEvents.State.LevelLow ? $"SV {d.level}" : st == TavernEvents.State.UsedToday ? "YARIN" : "KİLİTLİ"));
                        break;
                    default:
                        b.AddToClassList("btn-teal");
                        b.EnableInClassList("locked", st != TavernEvents.State.Ready);
                        pi.style.backgroundImage = new StyleBackground(CoinIcon);
                        b.Add(new Label(FormatNum(TavernEvents.Cost(d))));
                        var dd = d;
                        b.clicked += () =>
                        {
                            var r = TavernEvents.Start(dd);
                            if (r == TavernEvents.State.Running)
                            {
                                Sfx.Play(dd.id == TavernEvents.Id.Konser ? SfxId.Rush : SfxId.Unlock);
                                Confetti(16);
                                GameSettings.Vibrate();
                                _eventsPanel.AddToClassList("hidden");
                                Toast($"{dd.name} başladı! {TavernEvents.Effect(dd.id)}");
                            }
                            else Toast(r == TavernEvents.State.NoMoney ? "Yeterli altın yok" : r == TavernEvents.State.Busy ? "Önce süren etkinlik bitsin" : "Şu an başlatılamaz");
                        };
                        break;
                }
                row.Add(b);
                _eventRows.Add(row);
            }
        }
    }

    /// <summary>Mutluluk yüzü: -1 (kızgın) .. 1 (çok mutlu), Painter2D ile (font emojisi yok).</summary>
    public class FaceElement : VisualElement
    {
        float _mood;

        public FaceElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public void SetMood(float m)
        {
            m = Mathf.Clamp(m, -1f, 1f);
            if (Mathf.Abs(m - _mood) < 0.02f) return;
            _mood = m;
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width <= 0) return;
            var p = ctx.painter2D;
            var ink = new Color32(31, 26, 23, 255);
            var c = r.center;
            float rad = Mathf.Min(r.width, r.height) * 0.5f - 2f;
            p.fillColor = _mood >= 0.2f ? new Color32(242, 182, 49, 255) : _mood >= -0.4f ? new Color32(239, 210, 140, 255) : new Color32(222, 120, 92, 255);
            p.strokeColor = ink;
            p.lineWidth = 2.5f;
            p.BeginPath(); p.Arc(c, rad, Angle.Degrees(0f), Angle.Degrees(360f)); p.Fill(); p.Stroke();
            // gözler (kızgınken kaşlar çatık)
            p.fillColor = ink;
            foreach (var sx in new[] { -1f, 1f })
            {
                var e = c + new Vector2(sx * rad * 0.36f, -rad * 0.18f);
                p.BeginPath(); p.Arc(e, rad * 0.11f, Angle.Degrees(0f), Angle.Degrees(360f)); p.Fill();
                if (_mood < -0.4f)
                {
                    p.BeginPath();
                    p.MoveTo(e + new Vector2(-sx * rad * 0.2f, -rad * 0.32f));
                    p.LineTo(e + new Vector2(sx * rad * 0.18f, -rad * 0.2f));
                    p.Stroke();
                }
            }
            // ağız: ruh haline göre gülümseme ↔ asık
            float curve = _mood * rad * 0.32f;
            var a = c + new Vector2(-rad * 0.42f, rad * 0.3f);
            var b = c + new Vector2(rad * 0.42f, rad * 0.3f);
            p.BeginPath();
            p.MoveTo(a);
            p.QuadraticCurveTo(c + new Vector2(0f, rad * 0.3f + curve * 2f), b);
            p.Stroke();
        }
    }
}
