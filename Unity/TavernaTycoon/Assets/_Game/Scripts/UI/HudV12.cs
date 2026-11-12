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

        /// <summary>Etkinlik butonunun açıldığı seviye (v1.4: Tellal etkinliğiyle Sv2, rehber bittikten sonra).</summary>
        const int EventsLevel = 2;
        /// <summary>Uçan mutluluk yazısı bu farkın altındaysa gösterilmez; 1'in altı tek ondalıkla yazılır.</summary>
        const float HappyFloatMin = 0.1f;

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
            Toast(Loc.F("Tabelan yenilendi: {0}!", Loc.T(SignTier.Names[tier])));
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
            _happyNum.text = Loc.F("%{0}", Mathf.RoundToInt(v));   // v1.7: yüzde işaretinin yeri dile göre
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

            // Etkinlik butonu (Sv2, rehber bitince) + mor şerit (tellal dahil)
            bool evOn = !Tutorial.Active && GameState.Level >= EventsLevel;
            // v1.4: açıklama kartı açıkken buton kartın altında kalmasın
            _eventBtn.EnableInClassList("hidden", !evOn || !_happyCard.ClassListContains("hidden"));
            _eventNew.EnableInClassList("hidden", !evOn || GameState.Data.eventsSeen || !_happyCard.ClassListContains("hidden"));
            var act = TavernEvents.Active;
            _eventBanner.EnableInClassList("hidden", !act.HasValue);
            if (act.HasValue)
            {
                int left = Mathf.CeilToInt(TavernEvents.Remaining);
                _eventBanner.text = $"{Loc.Upper(Loc.T(TavernEvents.Get(act.Value).name))} · {left / 60}:{left % 60:00} · {TavernEvents.Effect(act.Value)}";
                _eventBanner.style.top = RushHour.Active ? 196f : 146f;
            }
            if (!_eventsPanel.ClassListContains("hidden"))
            {
                _eventsRefreshT -= Time.unscaledDeltaTime;
                if (_eventsRefreshT <= 0f) { _eventsRefreshT = 1f; BuildEvents(); }
            }
        }

        /// <summary>
        /// Sayının bulunma eki ("2'de", "4'te", "6'da", "10'da"); ünsüz benzeşmesi ve ünlü uyumu.
        /// v1.7: yalnızca Türkçede ek üretir; diğer dillerde boş (çeviri kalıbı eki gerektirmez).
        /// </summary>
        static string Locative(int n)
        {
            if (!Loc.IsTurkish) return "";
            int d = n % 10;
            if (d == 0) return n % 100 == 0 ? "'de" : (n / 10 % 10) switch { 1 or 3 or 6 or 9 => "'da", _ => "'de" };
            return d switch { 3 or 4 or 5 => "'te", 6 or 9 => "'da", _ => "'de" };
        }

        void SetRevealed(string name, bool on) => _root.Q(name)?.EnableInClassList("tut-hidden", !on);

        void OnHappinessChanged(float d, Vector3 at)
        {
            // v1.4: fark = ortalamanın bu müşteriyle değişimi (α·(puan − M)); küçük farklar tek ondalık
            float ad = Mathf.Abs(d);
            if (ad < HappyFloatMin) { if (!_happyCard.ClassListContains("hidden")) RefreshHappyCard(); return; }
            string num = ad < 1f ? ad.ToString("0.0", Loc.Culture) : Mathf.RoundToInt(ad).ToString();
            var l = new Label(Loc.F("{0}{1} mutluluk", d > 0 ? "+" : "−", num)) { pickingMode = PickingMode.Ignore };
            l.AddToClassList("float-happy");
            l.EnableInClassList("down", d < 0f);
            _world.Add(l);
            _happyFloats.Add((l, at, 0f));
            if (!_happyCard.ClassListContains("hidden")) RefreshHappyCard();
        }

        void RefreshHappyCard()
        {
            // v1.4: mutluluk = son müşterilerin puan ortalaması (GAME_DESIGN.md §7.2)
            _happyCardText.text =
                Loc.F("Her müşteri puan verir: çabuk {0:0} · zamanında {1:0} · geç {2:0} · kızgın {3:0}\n", Happiness.FastScore, Happiness.NormalScore, Happiness.SlowScore, Happiness.AngryScore) +
                Loc.F("Mutluluk son müşterilerin ortalaması · Barlo'ya özel: {0:0}, iki kez sayılır\n", Happiness.SpecialScore) +
                Loc.F("Kirli masa: masa başına −{0:0} (en fazla −{1:0}) · şu an −{2:0}\n", Happiness.DirtyPenaltyPerTable, Happiness.MaxDirtyPenalty, Happiness.DirtyPenalty) +
                Loc.F("Şu an %{0}: müşteri gelişi ×{1:0.0#}\n", Mathf.RoundToInt(Happiness.Value), Happiness.SpawnMultiplier) +
                Loc.F("Toplam müşteri çarpanı ×{0:0.0#} (en fazla ×{1:0})\n", CustomerSpawner.BoostMultiplier, Balance.SpawnBoostCap) +
                Loc.T("★ yıldızlar ayrı: ambiyansla artar, zengin müşteri ve bahşiş getirir.");
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
                var n = new Label(Loc.T(d.name));
                n.AddToClassList("item-name");
                string sub = st switch
                {
                    TavernEvents.State.LevelLow => Loc.F("Seviye {0}{1} açılır", d.level, Locative(d.level)),
                    TavernEvents.State.ZoneLocked => Loc.F("Önce {0} açılmalı", Loc.T(d.zoneName)),
                    TavernEvents.State.UsedToday => Loc.T("Bugün yapıldı · yarın yine"),
                    TavernEvents.State.Busy => Loc.F("{0} · başka etkinlik sürüyor", Loc.T(d.desc)),
                    _ => Loc.F("3 dk · {0}", Loc.T(d.desc)),
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
                        b.Add(new Label(st == TavernEvents.State.LevelLow ? Loc.F("SV {0}", d.level) : st == TavernEvents.State.UsedToday ? Loc.T("YARIN") : Loc.T("KİLİTLİ")));
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
                                Toast(Loc.F("{0} başladı! {1}", Loc.T(dd.name), TavernEvents.Effect(dd.id)));
                            }
                            else Toast(r == TavernEvents.State.NoMoney ? Loc.T("Yeterli altın yok") : r == TavernEvents.State.Busy ? Loc.T("Önce süren etkinlik bitsin") : Loc.T("Şu an başlatılamaz"));
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
