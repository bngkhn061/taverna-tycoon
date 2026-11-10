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
            _root.Q<Button>("play").clicked += () => { Sfx.Play(SfxId.Buy); SceneManager.LoadScene(gameScene); };
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

            if (AutoTest.Enabled) StartCoroutine(AutoRun());
        }

        // ------------------------------------------------------------------ canlı arka plan: dokun → zıpla
        void Update()
        {
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
                AutoTest.Shot("app_menu_tap.png");
            }
            yield return new WaitForSecondsRealtime(0.4f);
            _settings.Show();
            yield return new WaitForSecondsRealtime(0.5f);
            AutoTest.Shot("app_settings.png");
            yield return new WaitForSecondsRealtime(0.4f);
            _settings.Hide();
            OpenDaily();
            yield return new WaitForSecondsRealtime(0.5f);
            AutoTest.Shot("app_daily.png");
            _root.Q("daily-root").AddToClassList("hidden");
            BuildBook();
            _root.Q("book-root").RemoveFromClassList("hidden");
            yield return new WaitForSecondsRealtime(0.5f);
            AutoTest.Shot("app_book.png");
            _root.Q("book-root").AddToClassList("hidden");
            SceneManager.LoadScene(gameScene);
        }
    }
}
