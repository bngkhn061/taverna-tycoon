using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>Ana menü (yatay): Oyna / Ayarlar / Çıkış + Günlük Ödül + Koleksiyon Defteri.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        public string gameScene = "Game";
        /// <summary>Arka plandaki karaktere dokununca başının üstünden uçan ikonlar (rastgele biri).</summary>
        public Texture2D[] tapIcons;
        SettingsPanel _settings;
        VisualElement _root;

        void OnEnable()
        {
            GameSettings.Apply();
            Sfx.Ensure();
            GameState.Load();
            _root = GetComponent<UIDocument>().rootVisualElement;
            _settings = new SettingsPanel(_root, showToMenu: false);
            _root.Q<Button>("play").clicked += () =>
            {
                Sfx.Play(SfxId.Buy);
                // v0.9: ilk "OYNA"da tavernaya ad verilir (mockup A)
                if (string.IsNullOrWhiteSpace(GameState.Data.tavernName)) OpenNameModal();
                else StartGame();
            };
            SetupNameModal();
            _root.Q<Button>("settings").clicked += () => { Sfx.Play(SfxId.Pop); _settings.Show(); };

            var quit = _root.Q<Button>("quit");
            // iOS uygulamaları kendini kapatmaz (Apple yönergeleri) → Çıkış gizlenir
            quit.EnableInClassList("hidden", Application.platform == RuntimePlatform.IPhonePlayer);
            quit.clicked += () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            };

            // --- Günlük ödül
            _root.Q("daily-badge").EnableInClassList("hidden", !Boosts.DailyAvailable);
            _root.Q<Button>("daily").clicked += OpenDaily;
            _root.Q<Button>("daily-claim").clicked += () =>
            {
                if (!Boosts.DailyAvailable) return;
                Boosts.ClaimDaily();
                Sfx.Play(SfxId.Claim);
                _root.Q("daily-badge").AddToClassList("hidden");
                _root.Q("daily-root").AddToClassList("hidden");
            };
            _root.Q<Button>("daily-close").clicked += () => _root.Q("daily-root").AddToClassList("hidden");

            // --- Koleksiyon defteri: 30 müşteri, görülmeyenler soluk
            _root.Q<Button>("book").clicked += () => { BuildBook(); _root.Q("book-root").RemoveFromClassList("hidden"); Sfx.Play(SfxId.Pop); };
            _root.Q<Button>("book-close").clicked += () => _root.Q("book-root").AddToClassList("hidden");

            StartIntro();
            if (AutoTest.Enabled) StartCoroutine(AutoRun());
        }

        // ------------------------------------------------------------------ v1.0: açılış animasyonu (oturumda bir kez)
        static bool _introShown;
        VisualElement _intro, _introLogo, _introRays, _mugL, _mugR, _foam;
        float _introT = -1f;
        bool _clinked;
        const float IntroEnd = 2.6f;
        readonly System.Collections.Generic.List<(VisualElement e, Vector2 p, Vector2 v)> _dots = new();

        void StartIntro()
        {
            _intro = _root.Q("intro-root");
            if (_intro == null || _introShown) return;
            _introShown = true;
            _introLogo = _root.Q("intro-logo");
            _introRays = _root.Q("intro-rays");
            _mugL = _root.Q("intro-mug-l");
            _mugR = _root.Q("intro-mug-r");
            _foam = _root.Q("intro-foam");
            _intro.RemoveFromClassList("hidden");
            _introT = 0f;
            _clinked = false;
            UpdateIntro();
        }

        static float Back(float t) { const float s = 1.9f; t -= 1f; return t * t * ((s + 1f) * t + s) + 1f; }   // yaylı büyüme

        /// <summary>0–0.6 logo yaylı gelir · 0.6–1.0 kupalar iki yandan kayar · 1.0 tokuşma (köpük, ses) · 2.1–2.6 kararır, menü açılır.</summary>
        void UpdateIntro()
        {
            if (_introT < 0f || _intro == null) return;
            _introT += Time.unscaledDeltaTime;
            var p = Pointer.current;
            if (_introT > 0.25f && _introT < 2.1f && p != null && p.press.wasPressedThisFrame) _introT = 2.1f;
            float t = _introT;
            float lg = Mathf.Clamp01((t - 0.05f) / 0.55f);
            float ls = lg <= 0f ? 0f : Back(lg);
            _introLogo.style.scale = new Scale(new Vector3(ls, ls, 1f));
            _introLogo.style.rotate = new Rotate(Mathf.Sin(t * 6f) * 3f * (1f - Mathf.Clamp01((t - 0.6f) / 0.8f)));
            _introRays.style.rotate = new Rotate(t * 25f);
            _introRays.style.opacity = Mathf.Clamp01((t - 0.3f) / 0.5f);
            // kupalar
            float m = Mathf.Clamp01((t - 0.55f) / 0.45f);
            float me = 1f - (1f - m) * (1f - m);
            float off = Mathf.Lerp(520f, 62f, me);
            float tilt = t < 1.0f ? Mathf.Lerp(-25f, 10f, me) : Mathf.Lerp(18f, 6f, Mathf.Clamp01((t - 1.0f) / 0.4f));
            _mugL.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            _mugL.style.left = new Length(50, LengthUnit.Percent);
            _mugL.style.marginLeft = -off;
            _mugR.style.marginLeft = off;
            _mugL.style.rotate = new Rotate(tilt);
            _mugR.style.rotate = new Rotate(-tilt);
            if (!_clinked && t >= 1.0f)
            {
                _clinked = true;
                Sfx.Play(SfxId.Clink);
                Sfx.Play(SfxId.Claim, 0.9f);
                GameSettings.Vibrate();
                var c = new Vector2(_intro.layout.width * 0.5f, _intro.layout.height * 0.74f);
                for (int i = 0; i < 16; i++)
                {
                    var d = new VisualElement { pickingMode = PickingMode.Ignore };
                    d.AddToClassList("intro-foam-dot");
                    float a = Random.Range(200f, 340f) * Mathf.Deg2Rad, v = Random.Range(220f, 520f);
                    _dots.Add((d, c, new Vector2(Mathf.Cos(a) * v, Mathf.Sin(a) * v)));
                    _foam.Add(d);
                }
            }
            for (int i = 0; i < _dots.Count; i++)
            {
                var (e, pos, vel) = _dots[i];
                vel.y += 900f * Time.unscaledDeltaTime;
                pos += vel * Time.unscaledDeltaTime;
                _dots[i] = (e, pos, vel);
                e.style.left = pos.x; e.style.top = pos.y;
                float s = Mathf.Clamp01(1.6f - (t - 1f));
                e.style.scale = new Scale(new Vector3(s, s, 1f));
            }
            // kararma ve bitiş
            _intro.style.opacity = 1f - Mathf.Clamp01((t - 2.1f) / 0.5f);
            if (t >= IntroEnd)
            {
                _introT = -1f;
                _intro.AddToClassList("hidden");
                _foam.Clear();
                _dots.Clear();
            }
        }

        /// <summary>Açılış animasyonu sürüyor mu? (test: menü çekimleri sonra alınsın)</summary>
        public bool IntroPlaying => _introT >= 0f;

        // ------------------------------------------------------------------ v0.9: taverna adı + oyuna giriş
        static readonly string[] NameIdeas =
        {
            "Yorgun Ejder", "Altın Fıçı", "Kör Baykuş", "Sarhoş Cüce", "Kızıl Tilki", "Uyuyan Dev",
            "Gümüş Kupa", "Neşeli Domuz", "Üç Fıçı", "Mavi Ay", "Kayıp Şövalye", "Bal Arısı",
        };

        TextField _nameField;

        void SetupNameModal()
        {
            _nameField = _root.Q<TextField>("name-field");
            var chips = _root.Q("name-chips");
            chips.Clear();
            for (int i = 0; i < 4; i++)
            {
                var n = NameIdeas[i];
                var b = new Button(() => { _nameField.value = n; Sfx.Play(SfxId.Pop); }) { text = n };
                b.AddToClassList("chip");
                b.AddToClassList("name-chip");
                chips.Add(b);
            }
            _root.Q<Button>("name-dice").clicked += () =>
            {
                _nameField.value = NameIdeas[Random.Range(0, NameIdeas.Length)];
                Sfx.Play(SfxId.Pop, Random.Range(0.9f, 1.3f));
            };
            _root.Q<Button>("name-go").clicked += () =>
            {
                var n = (_nameField.value ?? "").Trim();
                if (n.Length < 3) n = GameState.DefaultTavernName;
                GameState.Data.tavernName = n;
                GameState.Save();
                Sfx.Play(SfxId.Buy);
                StartGame();
            };
        }

        void OpenNameModal()
        {
            _nameField.value = GameState.DefaultTavernName;
            _root.Q("name-root").RemoveFromClassList("hidden");
        }

        void StartGame()
        {
            HudController.PlayIntro = true;
            SceneManager.LoadScene(gameScene);
        }

        // ------------------------------------------------------------------ canlı arka plan: dokun → zıpla
        void Update()
        {
            UpdateIntro();
            if (IntroPlaying) return;
            var p = Pointer.current;
            if (p == null || !p.press.wasPressedThisFrame) return;
            Vector2 screen = p.position.ReadValue();
            if (!IsBackground(screen)) return;
            TapCharacterAt(screen);
        }

        /// <summary>Dokunulan yer bir butona/panele değil, arka plana mı denk geliyor?</summary>
        bool IsBackground(Vector2 screen)
        {
            if (_root?.panel == null) return false;
            var panelPos = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(screen.x, Screen.height - screen.y));
            var picked = _root.panel.Pick(panelPos);
            return picked == null || picked == _root || picked.name == "menu-root";
        }

        void TapCharacterAt(Vector2 screen)
        {
            var cam = Camera.main;
            if (!cam) return;
            ToyAnimator best = null;
            float bestDist = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(cam.ScreenPointToRay(screen), 60f))
            {
                var anim = hit.collider.GetComponentInChildren<ToyAnimator>();
                if (anim && hit.distance < bestDist) { best = anim; bestDist = hit.distance; }
            }
            if (!best) return;
            best.Hop();
            best.Pop();
            Sfx.Play(SfxId.Pop, Random.Range(0.9f, 1.3f));
            GameSettings.Vibrate();
            var head = cam.WorldToScreenPoint(best.transform.position + Vector3.up * 1.5f);
            if (head.z > 0f) FloatIcon(new Vector2(head.x, head.y));
        }

        /// <summary>Ekrandaki noktadan yukarı süzülüp kaybolan küçük ikon.</summary>
        void FloatIcon(Vector2 screen)
        {
            if (tapIcons == null || tapIcons.Length == 0 || _root?.panel == null) return;
            var tex = tapIcons[Random.Range(0, tapIcons.Length)];
            if (!tex) return;
            var start = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(screen.x, Screen.height - screen.y));
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("tap-pop");
            icon.style.backgroundImage = new StyleBackground(tex);
            _root.Q("menu-root").Add(icon);
            float t0 = Time.unscaledTime;
            icon.schedule.Execute(() =>
            {
                float t = (Time.unscaledTime - t0) / 0.9f;
                if (t >= 1f) { icon.RemoveFromHierarchy(); return; }
                float s = t < 0.2f ? Mathf.Lerp(0.4f, 1.15f, t / 0.2f) : Mathf.Lerp(1.15f, 1f, (t - 0.2f) / 0.8f);
                icon.style.left = start.x - 28f;
                icon.style.top = start.y - 28f - 70f * t;
                icon.style.scale = new Scale(new Vector3(s, s, 1f));
                icon.style.opacity = t < 0.6f ? 1f : Mathf.InverseLerp(1f, 0.6f, t);
            }).Every(16).Until(() => icon.parent == null);
        }

        void OpenDaily()
        {
            int s = Boosts.DailyAvailable ? Boosts.NextStreak : Mathf.Max(1, GameState.Data.dailyStreak);
            var r = Boosts.DailyReward(s);
            _root.Q<Label>("daily-sub").text = Boosts.DailyAvailable ? $"Gün {s} / 7 · seriyi bozma!" : "Bugünün ödülü alındı · yarın gel";
            _root.Q<Label>("daily-text").text = $"+{HudController.FormatNum(r.coins)}  +{r.gems} elmas";
            _root.Q<Button>("daily-claim").EnableInClassList("locked", !Boosts.DailyAvailable);
            _root.Q("daily-root").RemoveFromClassList("hidden");
            Sfx.Play(SfxId.Pop);
        }

        void BuildBook()
        {
            var grid = _root.Q("book-grid");
            grid.Clear();
            int seen = 0;
            foreach (var c in GameDatabase.I.customers)
            {
                if (!c) continue;
                bool s = GameState.Data.seenCustomers.Contains(c.number);
                if (s) seen++;
                var cell = new VisualElement();
                cell.AddToClassList("book-cell");
                cell.AddToClassList("card");
                cell.EnableInClassList("unseen", !s);
                var img = new VisualElement();
                img.AddToClassList("book-img");
                img.style.backgroundImage = new StyleBackground(Resources.Load<Sprite>($"Icons/ICO_Cust_{c.number:00}"));
                var l = new Label(s ? c.displayName : "???");
                l.AddToClassList("book-name");
                cell.Add(img);
                cell.Add(l);
                grid.Add(cell);
            }
            _root.Q<Label>("book-info").text = $"{seen} / 30 müşteri ağırlandı · hepsini topla: kalıcı +%10 bahşiş";
        }

        System.Collections.IEnumerator AutoRun()
        {
            // v1.0 açılış animasyonu: logo geldikten sonra ve kupalar tokuştuktan hemen sonra
            yield return new WaitForSecondsRealtime(0.55f);
            AutoTest.Shot("app_intro_a.png"); yield return null; yield return null;
            yield return new WaitForSecondsRealtime(0.55f);
            AutoTest.Shot("app_intro_b.png"); yield return null; yield return null;
            while (IntroPlaying) yield return null;
            yield return new WaitForSecondsRealtime(2f);
            AutoTest.Shot("app_menu.png");
            // Canlı arka plan: hareketi görmek için birkaç kare daha
            yield return new WaitForSecondsRealtime(2.5f);
            AutoTest.Shot("app_menu_b.png");
            yield return new WaitForSecondsRealtime(2.5f);
            AutoTest.Shot("app_menu_c.png");
            // Dokunma testi: Barlo'ya dokunulmuş gibi (zıplama + uçan ikon)
            var barlo = FindAnyObjectByType<MenuWalker>();
            if (barlo && Camera.main)
            {
                var sp = Camera.main.WorldToScreenPoint(barlo.transform.position + Vector3.up * 0.6f);
                TapCharacterAt(new Vector2(sp.x, sp.y));
                yield return new WaitForSecondsRealtime(0.3f);
                AutoTest.Shot("app_menu_tap.png"); yield return null; yield return null; // çekim kare sonunda alınır: önce kareyi bitir
            }
            yield return new WaitForSecondsRealtime(0.4f);
            _settings.Show();
            yield return new WaitForSecondsRealtime(0.5f);
            AutoTest.Shot("app_settings.png"); yield return null; yield return null; // çekim kare sonunda alınır: önce kareyi bitir
            yield return new WaitForSecondsRealtime(0.4f);
            _settings.Hide();
            OpenDaily();
            yield return new WaitForSecondsRealtime(0.5f);
            AutoTest.Shot("app_daily.png"); yield return null; yield return null; // çekim kare sonunda alınır: önce kareyi bitir
            _root.Q("daily-root").AddToClassList("hidden");
            BuildBook();
            _root.Q("book-root").RemoveFromClassList("hidden");
            yield return new WaitForSecondsRealtime(0.5f);
            AutoTest.Shot("app_book.png"); yield return null; yield return null; // çekim kare sonunda alınır: önce kareyi bitir
            _root.Q("book-root").AddToClassList("hidden");
            // v0.9: isim kutusu (mockup A) ve giriş animasyonu
            OpenNameModal();
            yield return new WaitForSecondsRealtime(0.5f);
            AutoTest.Shot("app_name.png"); yield return null; yield return null;
            _root.Q("name-root").AddToClassList("hidden");
            StartGame();
        }
    }
}
