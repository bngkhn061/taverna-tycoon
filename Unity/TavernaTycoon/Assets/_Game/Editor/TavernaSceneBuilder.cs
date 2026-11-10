using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Taverna.EditorTools
{
    /// <summary>
    /// Sahneleri koddan kurar (Blender scriptlerindeki gibi tekrar üretilebilir).
    /// Menü: Taverna ▸ Her Şeyi Kur. Komut satırı: -executeMethod Taverna.EditorTools.TavernaSceneBuilder.RunAll
    /// Oyun sahnesi = Aşama 1 "Yol Kenarı Meyhanesi" (GAME_DESIGN.md §3.2).
    /// </summary>
    public static class TavernaSceneBuilder
    {
        const string ScenesDir = TavernaImport.Root + "/Scenes";
        const string UiDir = TavernaImport.Root + "/UI";
        public const string MenuScene = ScenesDir + "/MainMenu.unity";
        public const string GameScene = ScenesDir + "/Game.unity";

        [MenuItem("Taverna/Her Şeyi Kur (içe aktar + veri + sahneler)")]
        public static void RunAll()
        {
            TavernaImport.CopySources();
            TavernaImport.BuildPrefabs();
            TavernaData.Build();
            var panel = PanelSettingsAsset();
            BuildMenuScene(panel);
            BuildGameScene(panel);
            ConfigureProject();
            Debug.Log("[Taverna] Kurulum tamamlandı.");
        }

        [MenuItem("Taverna/Sadece Sahneleri Yeniden Kur")]
        public static void ScenesOnly()
        {
            var panel = PanelSettingsAsset();
            BuildMenuScene(panel);
            BuildGameScene(panel);
        }

        // ------------------------------------------------------------------ proje ayarları
        static void ConfigureProject()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScene, true),
                new EditorBuildSettingsScene(GameScene, true),
            };
            PlayerSettings.productName = "Taverna Tycoon";
            // Yatay oyun: iki yatay yöne de döner, dikeye dönmez
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.tavernatycoon.prototype");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.tavernatycoon.prototype");
            ApplyAppIcons();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// v1.0 logo "Barlo Rozeti": uygulama ikonu. Varsayılan ikon (iOS ve diğerleri) + Android uyarlanabilir (arka plan ışınlar,
        /// ön plan Barlo), yuvarlak ve eski ikonlar. Açılışta Unity logosu yerine oyunun kendi açılış animasyonu (MainMenuController).
        /// </summary>
        public static void ApplyAppIcons()
        {
            const string dir = TavernaImport.Root + "/UI/Images/Logo/";
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "app_icon_1024.png");
            var bg = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "app_icon_bg.png");
            var fg = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "app_icon_fg.png");
            if (!icon) { Debug.LogWarning("[Taverna] Uygulama ikonu bulunamadı: " + dir); return; }
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            var android = UnityEditor.Build.NamedBuildTarget.Android;
            void Fill(PlatformIconKind kind, params Texture2D[] layers)
            {
                var icons = PlayerSettings.GetPlatformIcons(android, kind);
                foreach (var pi in icons) pi.SetTextures(layers);
                PlayerSettings.SetPlatformIcons(android, kind, icons);
            }
            if (bg && fg) Fill(UnityEditor.Android.AndroidPlatformIconKind.Adaptive, bg, fg);
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color32(31, 26, 23, 255);
        }

        static PanelSettings PanelSettingsAsset()
        {
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..", UiDir)));
            string tssPath = UiDir + "/RuntimeTheme.tss";
            string tssFull = Path.GetFullPath(Path.Combine(Application.dataPath, "..", tssPath));
            if (!File.Exists(tssFull))
            {
                File.WriteAllText(tssFull, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(tssPath, ImportAssetOptions.ForceSynchronousImport);
            }
            string psPath = UiDir + "/PanelSettings.asset";
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(psPath);
            if (!ps)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(ps, psPath);
            }
            ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(tssPath);
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1266, 585); // UI v3 taslağının ölçüsü (design/ui/ui_v3_ink.html)
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 1f; // yatay oyun: yüksekliğe göre ölçekle (1 birim = taslaktaki 1 dp)
            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();
            return ps;
        }

        // ------------------------------------------------------------------ ana menü
        static void BuildMenuScene(PanelSettings panel)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("#2A1F1A");
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 60f;
            cam.gameObject.AddComponent<AudioListener>();
            // Uzak ve geniş: oda ekranın alt-ortasına (logo ile butonların arasında kalan boşluğa) oturur
            var drift = cam.gameObject.AddComponent<MenuCameraDrift>();
            drift.target = new Vector3(1.4f, 0.5f, 0.65f);
            drift.yaw = 15f;
            drift.pitch = 40f;
            drift.distance = 11f;

            BuildMenuDiorama();

            var ui = new GameObject("MainMenu UI");
            var doc = ui.AddComponent<UIDocument>();
            BindDocument(doc, panel, UiDir + "/MainMenu.uxml");
            ui.AddComponent<MainMenuController>().tapIcons = new[]
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(TavernaImport.IconsDir + "/ICO_Bira.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>(TavernaImport.IconsDir + "/ICO_Para.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>(TavernaImport.IconsDir + "/ICO_Elmas.png"),
            };
            Save(scene, MenuScene);
        }

        /// <summary>
        /// Ana menünün canlı arka planı (GAME_DESIGN.md §10: "canlı taverna, Barlo ve Pip").
        /// Blender'daki eski menü resminin (tools/blender/render_menu_scene.py) aynı odası,
        /// bu kez oyunun kendi prefablarıyla ve hareketle: Barlo bira taşır, Pip para toplar,
        /// müşteriler sevinir, kapıdan yenileri gelir, ateş titrer, kıvılcımlar uçuşur.
        /// </summary>
        static void BuildMenuDiorama()
        {
            // --- Işık (DESIGN_RULES §5, oyun sahnesiyle aynı ton)
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#A898B4");
            RenderSettings.ambientEquatorColor = Hex("#6A5A7A");
            RenderSettings.ambientGroundColor = Hex("#3A2A2A");
            RenderSettings.skybox = null;
            var sun = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Hex("#FFE2B8");
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.6f;
            sun.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
            AddPoint("FireLight", new Vector3(0f, 0.5f, 2.25f), "#FF8A2A", 4.5f, 4.5f).gameObject.AddComponent<LightFlicker>().amount = 0.3f;
            AddPoint("TorchL", new Vector3(-1.0f, 1.35f, 2.7f), "#FFB060", 1.6f, 3f).gameObject.AddComponent<LightFlicker>();
            AddPoint("TorchR", new Vector3(1.0f, 1.35f, 2.7f), "#FFB060", 1.6f, 3f).gameObject.AddComponent<LightFlicker>();
            AddPoint("TableGlow", new Vector3(-1.3f, 1.4f, 0.2f), "#FFD9A0", 1.2f, 3f);

            // --- Oda: zemin, arka ve sol duvar
            var room = Group("Diorama");
            for (int ix = 0; ix < 9; ix++)
                for (int iz = 0; iz < 6; iz++)
                    P("PR_Building_Zemin", room, -2.5f + ix, -2.5f + iz);
            string[] back = { "Duvar", "Pencere", "Duvar", "Duvar", "Pencere", "Kapi", "Duvar", "Pencere", "Duvar" };
            for (int i = 0; i < back.Length; i++) P("PR_Building_" + back[i], room, -2.5f + i, 3.1f);
            string[] left = { "Duvar", "Duvar", "Pencere", "Duvar", "Duvar", "Duvar" };
            for (int i = 0; i < left.Length; i++) P("PR_Building_" + left[i], room, -3.1f, -2.5f + i, -90f);
            P("PR_Building_Kose", room, -3.1f, 3.1f);

            // --- Dekor
            P("PR_Decor_Somine", room, 0f, 2.72f);
            P("PR_Decor_Mesale", room, -1f, 2.93f);
            P("PR_Decor_Mesale", room, 1f, 2.93f);
            P("PR_Decor_Kalkan", room, -2.93f, 1f, -90f);
            P("PR_Decor_Boynuz", room, -2.93f, -1f, -90f);
            P("PR_Decor_Saksi", room, -2.55f, 2.55f);
            P("PR_Decor_FiciYigini", room, 5.0f, 2.45f, 90f);
            P("PR_Decor_Hali", room, -1.3f, 0.2f, 0f, 0.03f);
            P("PR_Decor_Flama", room, -2.5f, -1.7f, -30f);
            P("PR_Decor_Saksi", room, 3.55f, 2.6f);

            // --- Bar köşesi: arka duvarda, ekranın üst-ortasında (UI v3 menüsünde orası boş;
            // butonlar sağ altta, logo sol üstte küçük)
            P("PR_Furniture_Tezgah", room, 1.65f, 1.15f);
            P("PR_Station_Bira_T1", room, 1.2f, 2.45f);
            P("PR_Station_Pres_T1", room, 2.3f, 2.45f);
            P("PR_Station_Sarap_T1", room, 4.3f, 2.45f);
            P("PR_Furniture_Sandik", room, 2.5f, -1.75f);

            // --- Masalar
            P("PR_Furniture_MasaKare", room, -1.3f, 0.2f);
            P("PR_Furniture_Sandalye", room, -2.05f, 0.2f, 90f);
            P("PR_Furniture_Sandalye", room, -0.55f, 0.2f, -90f);
            P("PR_Furniture_Sandalye", room, -1.3f, 0.95f, 180f);
            P("PR_Drink_Bira", room, -1.45f, 0.15f, 0f, 0.64f);
            P("PR_Drink_Bira", room, -1.05f, 0.3f, 0f, 0.64f);
            P("PR_Furniture_MasaYuvarlak", room, 0.3f, -0.9f);
            P("PR_Furniture_Tabure", room, -0.3f, -0.8f);
            P("PR_Furniture_Tabure", room, 0.9f, -0.8f);
            P("PR_Drink_Sira", room, 0.4f, -0.95f, 0f, 0.63f);

            // --- Kıvılcım ve toz
            Embers(new Vector3(0f, 0.35f, 2.55f));
            Dust();

            var coin = TavernaImport.Prefab("PR_Drink_Para");
            var actors = Group("Actors");
            var tableSq = new Vector3(-1.3f, 0f, 0.2f);
            var tableRound = new Vector3(0.3f, 0f, -0.9f);

            // --- Barlo: tezgâhın arkasındaki fıçıdan bira alır, yuvarlak masaya servis eder
            var barlo = MenuActor(actors, "Barlo", "CH_Main_Barlo", new Vector3(1.2f, 0f, 1.85f), out var barloAnim);
            var tray = TrayStack(barloAnim.transform, out _);
            foreach (var x in new[] { -0.1f, 0.1f })
            {
                var mug = P("PR_Drink_Bira", tray, 0f, 0f);
                mug.transform.localPosition = new Vector3(x, 0f, 0f);
                mug.transform.localRotation = Quaternion.identity;
            }
            var bw = barlo.AddComponent<MenuWalker>();
            bw.anim = barloAnim;
            bw.carried = tray.gameObject;
            bw.coinPrefab = coin;
            bw.speed = 1.5f;
            bw.stops = new[]
            {
                new MenuWalker.Stop { pos = new Vector3(1.2f, 0f, 1.85f), wait = 1.3f, carrying = true, lookAt = new Vector3(1.2f, 0f, 2.6f) },
                new MenuWalker.Stop { pos = new Vector3(0.35f, 0f, 1.75f), carrying = true },
                // Yuvarlak masanın kuzeyinden servis: yüzü kameraya dönük
                new MenuWalker.Stop { pos = new Vector3(0.3f, 0f, -0.25f), wait = 1.8f, carrying = false, coin = true, cheer = true, lookAt = tableRound },
                new MenuWalker.Stop { pos = new Vector3(0.35f, 0f, 1.75f) },
            };

            // --- Pip: kare masadan parayı toplar, ön taraftaki sandığa taşır
            var pip = MenuActor(actors, "Pip", "CH_Main_Pip", new Vector3(-1.3f, 0f, -0.5f), out var pipAnim);
            var bag = P("PR_Drink_ParaYigini", pipAnim.transform, 0f, 0f);
            bag.transform.localPosition = new Vector3(0f, 0.55f, 0.35f);
            bag.transform.localRotation = Quaternion.identity;
            bag.transform.localScale = Vector3.one * 0.7f;
            var pw = pip.AddComponent<MenuWalker>();
            pw.anim = pipAnim;
            pw.carried = bag;
            pw.coinPrefab = coin;
            pw.speed = 1.8f;
            pw.stops = new[]
            {
                new MenuWalker.Stop { pos = new Vector3(-1.3f, 0f, -0.5f), wait = 1.4f, carrying = true, cheer = true, lookAt = tableSq },
                new MenuWalker.Stop { pos = new Vector3(-0.4f, 0f, -1.6f), carrying = true },
                // Sandığın kuzeyinde: yüzü kameraya dönük
                new MenuWalker.Stop { pos = new Vector3(2.5f, 0f, -1.1f), wait = 1.2f, carrying = false, coin = true, lookAt = new Vector3(2.5f, 0f, -1.75f) },
                new MenuWalker.Stop { pos = new Vector3(-0.4f, 0f, -1.6f) },
            };

            // --- Oturan müşteriler + şömine önünde ozan
            Patron(actors, "CH_Cust_01_Ciftci", new Vector3(-2.05f, 0f, 0.2f), tableSq, 0f);
            Patron(actors, "CH_Cust_12_CuceMadenci", new Vector3(-0.55f, 0f, 0.2f), tableSq, 0f);
            Patron(actors, "CH_Cust_21_Cadi", new Vector3(-1.3f, 0f, 0.95f), tableSq, 0f);
            Patron(actors, "CH_Cust_02_Balikci", new Vector3(-0.3f, 0f, -0.8f), tableRound, 0f);
            var bard = Patron(actors, "CH_Cust_04_Ozan", new Vector3(-0.35f, 0f, 2.05f), new Vector3(0.6f, 0f, -0.6f), 7f);
            bard.cheerEvery = new Vector2(2.5f, 4.5f);

            // --- Kapıdan gelip giden ziyaretçi (her turda başka müşteri)
            var visitorRoot = new GameObject("Visitor");
            visitorRoot.transform.SetParent(actors, false);
            AddTapCollider(visitorRoot);
            var v = visitorRoot.AddComponent<MenuVisitor>();
            v.path = new[]
            {
                new Vector3(2.5f, 0f, 2.75f), new Vector3(3.0f, 0f, 1.5f), new Vector3(1.6f, 0f, -0.35f), new Vector3(0.9f, 0f, -0.8f),
            };
            v.lookAtWhenSeated = tableRound;
            var models = new List<GameObject>();
            foreach (var n in new[] { "03_Oduncu", "05_Firinci", "06_CobanKiz", "08_Demirci", "09_Tuccar", "13_Bucukluk", "16_Sovalye",
                                      "17_Goblin", "18_Ork", "19_ElfOkcu", "20_Buyucu", "25_KorsanKaptan", "26_Trol", "29_HayaletSovalye" })
            {
                var pf = TavernaImport.Prefab("CH_Cust_" + n);
                if (pf) models.Add(pf);
            }
            v.models = models.ToArray();
        }

        static GameObject MenuActor(Transform parent, string name, string prefab, Vector3 pos, out ToyAnimator anim)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var visual = P(prefab, root.transform, pos.x, pos.z);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            anim = visual.AddComponent<ToyAnimator>();
            AddTapCollider(root);
            return root;
        }

        static MenuPatron Patron(Transform parent, string prefab, Vector3 pos, Vector3 lookAt, float sway)
        {
            var root = MenuActor(parent, prefab, prefab, pos, out var anim);
            // Masaya ve kameraya yarı yarıya dönük: yüzler görünsün (kamera güney-güneybatıda)
            var toTable = lookAt - pos;
            toTable.y = 0f;
            var dir = toTable.normalized + new Vector3(-0.26f, 0f, -0.97f) * 0.9f;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) root.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            var p = root.AddComponent<MenuPatron>();
            p.anim = anim;
            p.sway = sway;
            return p;
        }

        /// <summary>Dokunma algılaması için karakter boyunda kapsül (fizik çarpışması yok, sadece ışın).</summary>
        static void AddTapCollider(GameObject root)
        {
            var c = root.AddComponent<CapsuleCollider>();
            c.center = new Vector3(0f, 0.65f, 0f);
            c.height = 1.3f;
            c.radius = 0.32f;
            c.isTrigger = true;
        }

        static Material ParticleMaterial(string name, Color c)
        {
            string path = TavernaImport.Root + "/Art/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            // Saydam + toplamalı (additive) karışım: kıvılcımlar ve toz parlak görünür
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetColor("_BaseColor", c);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Embers(Vector3 pos)
        {
            var go = new GameObject("Embers");
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(-90f, 0f, 0f));
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.055f);
            main.startColor = new ParticleSystem.MinMaxGradient(Hex("#FFB040"), Hex("#FF6A20"));
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var em = ps.emission;
            em.rateOverTime = 16f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 16f;
            shape.radius = 0.28f;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.8f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial("M_Ember", Hex("#FFC060"));
        }

        static void Dust()
        {
            var go = new GameObject("Dust");
            go.transform.position = new Vector3(0.5f, 1.4f, 0.4f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.03f);
            main.startColor = new Color(1f, 0.9f, 0.7f, 0.5f);
            main.maxParticles = 70;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var em = ps.emission;
            em.rateOverTime = 7f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(6f, 2.2f, 4.5f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 0.3f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial("M_Dust", Hex("#FFE8C0"));
        }

        // ------------------------------------------------------------------ oyun sahnesi
        // Büyük odalar (v0.6, oyuncu testi "mekan sıkışık"): eski yerleşim koordinatları oda oda yeni boyutlara eşlenir.
        //   Meyhane x −3.1..3.1 → −4.1..4.1 · Salon 3.1..11.1 → 4.1..15.1 · Bahçe −11.1..−3.1 → −15.1..−4.1
        //   Derinlik: ön (z ≤ −2, sokak) aynı; arka duvar 3.1 → 5.1; arka duvara yaslı (z ≥ 2) eşyalar duvarla kayar.
        // Zemin/duvarlar yeni ölçülerle doğrudan kurulur (_layout kapalı); eşya, nokta, ışık, masa eşlenir (_layout açık).
        static bool _layout;
        static float MX(float x) => x <= -3.1f ? -4.1f + (x + 3.1f) * 1.375f : x >= 3.1f ? 4.1f + (x - 3.1f) * 1.375f : x * (4.1f / 3.1f);
        static float MZ(float z) => z <= -2f ? z : z >= 2f ? z + 2f : -2f + (z + 2f) * 1.5f;
        static Vector3 L(float x, float y, float z) => _layout ? new Vector3(MX(x), y, MZ(z)) : new Vector3(x, y, z);

        static GameObject P(string prefab, Transform parent, float x, float z, float rotY = 0f, float y = 0f)
        {
            var src = TavernaImport.Prefab(prefab);
            if (!src) return new GameObject("MISSING_" + prefab);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
            go.transform.SetPositionAndRotation(L(x, y, z), Quaternion.Euler(0f, rotY, 0f));
            return go;
        }

        /// <summary>Eşlemesiz yerleştirme (koordinat zaten yeni haritada).</summary>
        static GameObject PRaw(string prefab, Transform parent, float x, float z, float rotY = 0f, float y = 0f)
        {
            bool lay = _layout;
            _layout = false;
            var go = P(prefab, parent, x, z, rotY, y);
            _layout = lay;
            return go;
        }

        /// <summary>
        /// Tema varyantlı parça grubu: her tema için bir alt grup kurar, ThemeSet seçileni gösterir.
        /// build(grup, önek) her tema için çağrılır; önek prefab son ekidir ("" = varsayılan).
        /// </summary>
        static ThemeSet Themed(Transform parent, Themes.Kind kind, System.Action<Transform, string> build)
        {
            var ids = new List<string>();
            var vars = new List<GameObject>();
            foreach (var th in Themes.List)
            {
                if (th.kind != kind) continue;
                var g = new GameObject(th.id).transform;
                g.SetParent(parent, false);
                build(g, th.suffix);
                ids.Add(th.id);
                vars.Add(g.gameObject);
            }
            var set = parent.gameObject.AddComponent<ThemeSet>();
            set.kind = kind;
            set.ids = ids.ToArray();
            set.variants = vars.ToArray();
            return set;
        }

        static Transform Group(string name) => new GameObject(name).transform;

        static Transform Point(string name, Transform parent, float x, float z)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = L(x, 0f, z);
            return t;
        }

        /// <summary>
        /// Harita (GAME_DESIGN.md §3.2): üç bölge yan yana + önde sokak.
        ///   Bahçe (x −11..−3, Aşama 3)  |  Meyhane (x −3..3, başlangıç)  |  Ana Salon (x 3..11, Aşama 2)
        /// Kilitli bölgelerin zemini ve dekoru baştan görünür — oyuncu neyi açacağını hep görür.
        /// </summary>
        static void BuildGameScene(PanelSettings panel)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Sistemler
            var systems = new GameObject("Systems");
            systems.AddComponent<GameBootstrap>();
            var dayNight = systems.AddComponent<DayNightLighting>();
            systems.AddComponent<RushHour>();
            systems.AddComponent<DayCycle>();

            // --- Işık (DESIGN_RULES §5)
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#A898B4");
            RenderSettings.ambientEquatorColor = Hex("#6A5A7A");
            RenderSettings.ambientGroundColor = Hex("#3A2A2A");
            RenderSettings.skybox = null;
            var sun = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Hex("#FFE2B8");
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.6f;
            sun.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
            dayNight.sun = sun;
            _layout = true;
            AddPoint("FireLight", new Vector3(0f, 0.5f, 2.25f), "#FF8A2A", 4f, 4.5f);
            // Fenerler (gece/gündüz, v0.6): akşam söner, Barlo ya da Fenerci yakar
            var lanterns = Group("Lanterns");
            AddLantern(lanterns, "TorchL", "", -1.05f, 2.7f, 3f);
            AddLantern(lanterns, "TorchR", "", 0.95f, 2.7f, 3f);
            AddLantern(lanterns, "SalonTorchA", "salon", 3.9f, 2.7f, 3.5f);
            AddLantern(lanterns, "SalonTorchB", "salon", 10.4f, 2.7f, 3.5f);
            AddLantern(lanterns, "GardenTorchA", "bahce", -9.0f, 2.7f, 3.5f);
            AddLantern(lanterns, "GardenTorchB", "bahce", -4.6f, 2.7f, 3.5f);

            // ================================================================ ZEMİNLER (v0.6: büyük odalar, temalı)
            _layout = false;
            var ground = Group("Ground");
            // İç mekân (Meyhane 8×7 + Salon 11×7): tema varyantları
            var floorThemes = Group("FloorThemes");
            var themeAmb = floorThemes.gameObject.AddComponent<AmbianceSource>();
            themeAmb.value = 0f;
            Themed(floorThemes, Themes.Kind.Floor, (g, suf) =>
            {
                for (int ix = 0; ix < 8; ix++)
                    for (int iz = -3; iz < 7; iz++)   // v1.2: ön cephe z −2 → −5
                        P("PR_Building_Zemin" + suf, g, -3.5f + ix, -1.5f + iz);
                for (int ix = 0; ix < 11; ix++)
                    for (int iz = -3; iz < 7; iz++)
                        P("PR_Building_Zemin" + suf, g, 4.5f + ix, -1.5f + iz);
            }).ambiance = themeAmb;
            for (int ix = 0; ix < 11; ix++)
                for (int iz = -3; iz < 7; iz++)
                    P("PR_Building_Cim", ground, -14.5f + ix, -1.5f + iz);   // Bahçe 11×10 (v1.2)
            for (int ix = 0; ix < 40; ix++)
            {
                P("PR_Building_Kaldirim", ground, -19.5f + ix, -6f);        // Sokak (v1.2: 3 m geri)
                P("PR_Building_Kaldirim", ground, -19.5f + ix, -7f);
                P("PR_Building_Cim", ground, -19.5f + ix, -8f);
            }

            // ================================================================ DUVARLAR (temalı)
            var walls = Group("Walls");
            var wallThemes = Group("WallThemes");
            wallThemes.SetParent(walls, false);
            string[] back = { "Duvar", "Pencere", "Duvar", "Duvar", "Duvar", "Duvar", "Pencere", "Duvar" };
            string[] back2 = { "Duvar", "Duvar", "Pencere", "Duvar", "Duvar", "Pencere", "Duvar", "Duvar", "Pencere", "Duvar", "Duvar" };
            string[] right2 = { "Duvar", "Pencere", "Duvar", "Pencere", "Duvar", "Pencere", "Duvar" };
            Themed(wallThemes, Themes.Kind.Wall, (g, suf) =>
            {
                for (int i = 1; i < back.Length; i++) P("PR_Building_" + back[i] + suf, g, -3.5f + i, 5.1f);        // Meyhane arka (x −3.5 = mutfak kapısı)
                for (int i = 0; i < back2.Length; i++) if (i != 5) P("PR_Building_" + back2[i] + suf, g, 4.5f + i, 5.1f);      // Salon arka (x 9.5 = oyun salonu kapısı)
                for (int i = 1; i < right2.Length; i++) P("PR_Building_" + right2[i] + suf, g, 15.1f, -1.5f + i, 90f); // Salon sağ (z −1.5 = avlu kapısı)
                for (int i = 1; i <= 3; i++) P("PR_Building_" + (i == 2 ? "Pencere" : "Duvar") + suf, g, 15.1f, -1.5f - i, 90f);   // v1.2 öne uzantı
            });
            // Bahçe çiti (temasız)
            // v1.0: bahçenin arkası Şarap Mahzeni'nin taş cephesi (kapı x = −10.5, mahzen açılınca kalkar)
            for (int i = 0; i < 11; i++) if (i != 4) P(i % 3 == 1 ? "PR_Building_Pencere_Tas" : "PR_Building_Duvar_Tas", walls, -14.5f + i, 5.1f);
            var wallYard = Group("Wall_ToYard");   // v1.0: avlu açılınca kalkar
            Themed(wallYard, Themes.Kind.Wall, (g, suf) => P("PR_Building_Duvar" + suf, g, KitchenLayout.SideWallX, KitchenLayout.YardDoorZ, 90f));
            var wallCellar = Group("Wall_ToCellar");
            P("PR_Building_Duvar_Tas", wallCellar, KitchenLayout.CellarDoorX, 5.1f);
            for (int i = -3; i < 7; i++) if (i != 3) P("PR_Building_Cit", walls, -15.1f, -1.5f + i, 90f);   // z 1.5 = iskele kapısı
            var gateDock = Group("Gate_ToDock");
            P("PR_Building_Cit", gateDock, -KitchenLayout.SideWallX, KitchenLayout.DockDoorZ, 90f);
            foreach (var (x, z) in new[] { (-4.1f, 5.1f), (4.1f, 5.1f), (15.1f, 5.1f), (15.1f, -5f), (-4.1f, -5f), (4.1f, -5f) })
                P("PR_Building_Kose", walls, x, z);

            // Meyhane ↔ Bahçe duvarı (bahçe açılınca yıkılır) ve Meyhane ↔ Salon duvarı (salon açılınca yıkılır)
            var wallLeft = Group("Wall_ToGarden");
            string[] left = { "Duvar", "Pencere", "Duvar", "Duvar", "Duvar", "Pencere", "Duvar" };
            Themed(wallLeft, Themes.Kind.Wall, (g, suf) =>
            {
                for (int i = 0; i < left.Length; i++) P("PR_Building_" + left[i] + suf, g, -4.1f, -1.5f + i, -90f);
                for (int i = 1; i <= 3; i++) P("PR_Building_Duvar" + suf, g, -4.1f, -1.5f - i, -90f);
            });
            var wallGame = Group("Wall_ToGameRoom");     // v0.9: oyun salonu açılınca kalkar (kapı boşluğu)
            Themed(wallGame, Themes.Kind.Wall, (g, suf) => P("PR_Building_Duvar" + suf, g, KitchenLayout.GameDoorX, 5.1f));
            var wallKitchen = Group("Wall_ToKitchen");   // mutfak açılınca kalkar (kapı boşluğu)
            Themed(wallKitchen, Themes.Kind.Wall, (g, suf) => P("PR_Building_Duvar" + suf, g, -3.5f, 5.1f));

            // ================================================================ MUTFAK (v0.7): Meyhane'nin arkası, 8×5 m
            var kitchen = Group("Kitchen");
            for (int ix = 0; ix < 8; ix++)
                for (int iz = 0; iz < 5; iz++)
                    P("PR_Building_Zemin_Tas", kitchen, -3.5f + ix, 5.5f + iz);
            string[] kBack = { "Duvar", "Duvar", "Pencere", "Duvar", "Duvar", "Pencere", "Duvar", "Duvar" };
            for (int i = 0; i < kBack.Length; i++) P("PR_Building_" + kBack[i], kitchen, -3.5f + i, 10.1f);
            string[] kLeft = { "Duvar", "Duvar", "Pencere", "Duvar", "Duvar" };
            string[] kRight = { "Duvar", "Pencere", "Duvar", "Duvar", "Duvar" };
            for (int i = 0; i < 5; i++)
            {
                P("PR_Building_" + kLeft[i], kitchen, -4.1f, 5.5f + i, -90f);
                P("PR_Building_" + kRight[i], kitchen, 4.1f, 5.5f + i, 90f);
            }
            P("PR_Building_Kose", kitchen, -4.1f, 10.1f);
            P("PR_Building_Kose", kitchen, 4.1f, 10.1f);
            P("PR_Decor_Tencere", kitchen, -3.95f, 8.0f, -90f);
            P("PR_Decor_Cuval", kitchen, 3.35f, 6.1f, 20f);
            P("PR_Decor_Sepet", kitchen, 3.35f, 7.4f);
            AddPoint("KitchenFire", new Vector3(-1.6f, 0.8f, 8.9f), "#FF8A2A", 2.5f, 5f);
            AddPoint("KitchenLamp", new Vector3(0.8f, 2.0f, 7.4f), "#FFD9A0", 1.4f, 6f);

            var wallRight = Group("Wall_ToSalon");
            Themed(wallRight, Themes.Kind.Wall, (g, suf) =>
            {
                for (int i = -3; i < 7; i++) P("PR_Building_Duvar" + suf, g, 4.1f, -1.5f + i, 90f);
            });

            // Çarpıştırıcılar: zemin + ön sınır (önde duvar yok, kamera tarafı açık)
            var floor = new GameObject("FloorCollider", typeof(BoxCollider));
            floor.transform.SetParent(walls);
            var fc = floor.GetComponent<BoxCollider>();
            fc.center = new Vector3(0f, -0.5f, 2.5f);
            fc.size = new Vector3(60f, 1f, 20f);   // v1.0: avlu ve iskele
            var front = new GameObject("FrontBarrier", typeof(BoxCollider));
            front.transform.SetParent(walls);
            var frc = front.GetComponent<BoxCollider>();
            frc.center = new Vector3(0f, 1f, -5.25f);   // v1.2: ön cephe z −5
            frc.size = new Vector3(50f, 2f, 0.3f);
            _layout = true; // buradan sonrası eski koordinatlarla yazılı → yeni haritaya eşlenir

            // ================================================================ DEKOR
            var decor = Group("Decor");
            P("PR_Decor_Somine", decor, 0f, 2.72f);
            P("PR_Decor_Mesale", decor, -1.05f, 2.95f);
            P("PR_Decor_Mesale", decor, 0.95f, 2.95f);
            P("PR_Decor_Mesale", decor, 3.9f, 2.95f);
            P("PR_Decor_Mesale", decor, 10.4f, 2.95f);
            P("PR_Decor_Mesale", decor, -9.0f, 2.95f);   // bahçe çitindeki fenerler
            P("PR_Decor_Mesale", decor, -4.6f, 2.95f);
            // Sokak (satılık değil, ortam)
            for (int i = 0; i < 9; i++) P("PR_Decor_Saksi", decor, -11.5f + i * 2.9f, -8.2f);
            P("PR_Decor_Flama", decor, -1.4f, -5.65f, 15f);
            P("PR_Decor_Flama", decor, 1.4f, -5.65f, -15f);

            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(TavernaImport.Root + "/Resources/GameDatabase.asset");

            // ================================================================ BÖLGE NOKTALARI
            var nav = Group("Navigation");
            var ent1 = Point("Entrance_Meyhane", nav, 0f, -5.45f);
            var hub1 = Point("Hub_Meyhane", nav, 0.6f, 1.2f);
            var ent2 = Point("Entrance_Salon", nav, 7.0f, -5.45f);
            var hub2 = Point("Hub_Salon", nav, 7.0f, 1.55f);
            var ent3 = Point("Entrance_Bahce", nav, -3.7f, -5.45f);
            var hub3 = Point("Hub_Bahce", nav, -6.5f, 1.5f);

            // ================================================================ İSTASYONLAR
            var stations = Group("Stations");
            var machineSlots = new (DrinkId d, float x, float z)[]
            {
                (DrinkId.Bira, 1.2f, 2.45f), (DrinkId.Sira, 2.35f, 2.45f), (DrinkId.BalSarabi, -1.85f, 2.45f),
                (DrinkId.Sarap, 4.6f, 2.45f), (DrinkId.Sut, 5.9f, 2.45f), (DrinkId.KaraBira, 8.92f, 2.45f), (DrinkId.ElfCiyi, 7.94f, 2.45f),   // v0.9: x 12.1 / 10.75 (kapı ve merdiven boş)
                (DrinkId.Cay, -10.2f, 2.45f), (DrinkId.Grog, -8.9f, 2.45f), (DrinkId.EjderAtesi, -4.4f, 2.45f),
            };
            var machineEntries = new List<TavernRegistry.MachineEntry>();
            foreach (var (d, x, z) in machineSlots)
            {
                var st = Station(db, d, stations, x, z);
                machineEntries.Add(new TavernRegistry.MachineEntry { drink = d, root = st });
                st.SetActive(d == DrinkId.Bira);
            }
            _layout = false; // mutfak yeni haritada doğrudan yerleşir
            foreach (var (d, x) in new[] { (DrinkId.Ekmek, -2.4f), (DrinkId.Guvec, -0.8f), (DrinkId.KozEt, 0.8f), (DrinkId.Turta, 2.4f) })
            {
                var st = Station(db, d, stations, x, 9.45f);
                machineEntries.Add(new TavernRegistry.MachineEntry { drink = d, root = st });
                st.SetActive(false);
            }
            // v1.1: hamur tezgâhı; aşçılar makineler ile tezgâh arasında çalışır (StaffWork)
            P("PR_Furniture_Tezgah", kitchen, 2.2f, 6.2f);
            var cookSpots = new[] { new Vector3(2.2f, 0f, 6.85f), new Vector3(1.5f, 0f, 6.85f) };
            var cook = MakeWorker(Group("KitchenStaff"), "Cook_Ayla", "CH_Staff_07_Asci", new Vector3(-1.6f, 0f, 8.2f), cookSpots, new[] { 180f, 180f }, true);
            // v1.0 Hazine: Ejder aşçı Ignis (sadece gerçek parayla; yemekler ×2 hızlı)
            var ignis = MakeWorker(Group("PremiumKitchenStaff"), "Cook_Ignis", "CH_Staff_10_Ignis", new Vector3(1.4f, 0f, 8.2f), cookSpots, new[] { 180f, 180f }, true);
            _layout = true;

            // ================================================================ MASALAR
            var tables = Group("Tables");
            Table("Table_A", "PR_Furniture_MasaKare", tables, -1.3f, 0.5f, 4f, ent1, hub1, new[]
            {
                Seat(-2.05f, 0.5f, (-2.05f, 1.35f)), Seat(-0.55f, 0.5f), Seat(-1.3f, 1.25f),
            });
            Table("Table_B", "PR_Furniture_MasaYuvarlak", tables, 0.45f, -0.55f, 3f, ent1, hub1, new[]
            {
                Seat(-0.2f, -0.55f, (-0.2f, 0.25f)), Seat(1.1f, -0.55f, (1.1f, 0.25f)),
            });
            var tC = Table("Table_C", "PR_Furniture_MasaKare", tables, -1.3f, -1.3f, 4f, ent1, hub1, new[]
            {
                Seat(-2.05f, -1.3f, (-0.5f, -0.4f), (-2.05f, -0.4f)), Seat(-0.55f, -1.3f, (-0.55f, -0.4f)),
            });
            var tD = Table("Table_D", "PR_Furniture_MasaYuvarlak", tables, 2.0f, -1.3f, 3f, ent1, hub1, new[]
            {
                Seat(1.35f, -1.3f, (1.35f, -0.6f)), Seat(2.65f, -1.3f, (2.65f, -0.6f)),
            });
            // Salon
            var tE = Table("Table_E", "PR_Furniture_MasaUzun", tables, 7.2f, 0.4f, 8f, ent2, hub2, new[]
            {
                Seat(6.6f, 1.05f), Seat(7.2f, 1.05f), Seat(7.8f, 1.05f),
                Seat(6.6f, -0.25f, (8.7f, 1.0f), (8.7f, -0.85f), (6.6f, -0.85f)),
                Seat(7.2f, -0.25f, (8.7f, 1.0f), (8.7f, -0.85f), (7.2f, -0.85f)),
                Seat(7.8f, -0.25f, (8.7f, 1.0f), (8.7f, -0.85f), (7.8f, -0.85f)),
            });
            var tF = Table("Table_F", "PR_Furniture_MasaKare", tables, 4.4f, -0.9f, 4f, ent2, hub2, new[]
            {
                Seat(3.65f, -0.9f, (3.65f, -0.1f)), Seat(5.15f, -0.9f, (5.15f, -0.1f)),
            });
            var tG = Table("Table_G", "PR_Furniture_MasaYuvarlak", tables, 9.6f, -0.9f, 3f, ent2, hub2, new[]
            {
                Seat(8.95f, -0.85f, (8.95f, -0.1f)), Seat(10.25f, -0.85f, (10.25f, -0.1f)),
            });
            // Bahçe
            var tH = Table("Table_H", "PR_Furniture_MasaKare", tables, -4.6f, 0.6f, 5f, ent3, hub3, new[]
            {
                Seat(-5.35f, 0.6f), Seat(-3.85f, 0.6f, (-3.85f, 1.5f)), Seat(-4.6f, 1.35f),
            });
            var tI = Table("Table_I", "PR_Furniture_MasaKare", tables, -8.4f, 0.6f, 5f, ent3, hub3, new[]
            {
                Seat(-9.15f, 0.6f, (-9.15f, 1.5f)), Seat(-7.65f, 0.6f), Seat(-8.4f, 1.35f),
            });
            var tJ = Table("Table_J", "PR_Furniture_MasaYuvarlak", tables, -6.5f, -1.0f, 4f, ent3, hub3, new[]
            {
                Seat(-7.15f, -0.95f, (-7.3f, 0.0f)), Seat(-5.85f, -0.95f, (-5.7f, 0.0f)),
            });

            // Ozan sahnesi (ambiyans kaynağı)
            var sahne = new GameObject("OzanSahnesi");
            sahne.transform.SetParent(decor);
            P("PR_Decor_Sahne", sahne.transform, -6.5f, 2.55f);
            sahne.AddComponent<AmbianceSource>().value = 20f;

            // ================================================================ PERSONEL
            var staff = Group("Staff");
            var staffEntries = new List<TavernRegistry.StaffEntry>
            {
                new() { id = "staff_pip", root = MakeWaiter(staff, "Waiter_Pip", "CH_Main_Pip", "Pip", new Vector3(0.9f, 0f, 0.2f), hub1.position, 2.4f, 1) },
                new() { id = "staff_mina", root = MakeWaiter(staff, "Waiter_Mina", "CH_Staff_01_GarsonMina", "Mina", new Vector3(5.2f, 0f, 1.4f), hub2.position, 2.6f, 1) },
                new() { id = "staff_tobi", root = MakeWaiter(staff, "Waiter_Tobi", "CH_Staff_02_GarsonTobi", "Tobi", new Vector3(8.0f, 0f, 1.4f), hub2.position, 2.9f, 2) },
                // v1.1: barmen tezgâh boyunca dolaşır, makinelere dönüp doldurur, arada salona dönüp tezgâhı siler
                new() { id = "staff_gus", root = MakeWorker(staff, "Barmen_Gus", "CH_Staff_03_Barmen", new Vector3(1.8f, 0f, 1.75f),
                    new[] { L(0.6f, 0f, 1.75f), L(1.8f, 0f, 1.75f), L(2.7f, 0f, 1.75f), L(1.2f, 0f, 1.75f) }, new[] { 0f, 0f, 0f, 180f }, false) },
                new() { id = "staff_nana", root = MakeCleaner(staff, "Cleaner_Nana", "CH_Staff_04_Temizlikci", new Vector3(-2.4f, 0f, -0.2f)) },
                new() { id = "staff_ayla", root = cook },
                new() { id = "staff_brak", root = MakeLamplighter(staff, "Lamplighter_Brak", "CH_Staff_05_Fedai", new Vector3(-2.2f, 0f, 1.2f)) },
                // v1.0 Hazine: Elf garson Lirael (çok hızlı, 4 bardak) ve Ejder aşçı Ignis
                new() { id = "staff_lirael", root = MakeWaiter(staff, "Waiter_Lirael", "CH_Staff_09_Lirael", "Lirael", new Vector3(0.3f, 0f, 0.9f), hub1.position, 4.2f, 4) },
                new() { id = "staff_ignis", root = ignis },
                new() { id = "staff_lir", root = MakeIdleStaff(staff, "Bard_Lir", "CH_Staff_06_OzanSahne", new Vector3(-0.85f, 0f, 1.95f), 200f) },
            };
            // ================================================================ ÖDEME ALANLARI (hedef zinciri)
            var zones = Group("PayZones");
            Zone(zones, "masa3", "Yeni Masa", 50, 2, "", -1.3f, -1.3f, new[] { tC });
            Zone(zones, "masa4", "Yeni Masa", 110, 3, "", 2.0f, -1.3f, new[] { tD });
            Zone(zones, "salon", "Ana Salonu Aç", 350, 4, "", 2.5f, -0.45f, new GameObject[0], new[] { wallRight.gameObject });
            Zone(zones, "bahce", "Bahçeyi Aç", 1600, 8, "", -2.45f, -0.4f, new GameObject[0], new[] { wallLeft.gameObject });
            Zone(zones, "masaE", "Uzun Masa", 300, 5, "salon", 7.2f, 0.4f, new[] { tE });
            Zone(zones, "masaF", "Yeni Masa", 380, 6, "salon", 4.4f, -0.9f, new[] { tF });
            Zone(zones, "masaG", "Yeni Masa", 420, 7, "salon", 9.6f, -0.9f, new[] { tG });
            Zone(zones, "masaH", "Bahçe Masası", 450, 8, "bahce", -4.6f, 0.6f, new[] { tH });
            Zone(zones, "masaI", "Bahçe Masası", 650, 8, "bahce", -8.4f, 0.6f, new[] { tI });
            Zone(zones, "masaJ", "Bahçe Masası", 800, 8, "bahce", -6.5f, -1.0f, new[] { tJ });
            Zone(zones, "sahne", "Ozan Sahnesi", 2500, 11, "bahce", -6.5f, 1.55f, new[] { sahne });
            // ================================================================ v1.2 ÖN SIRA MASALARI (oyuncu testi: "masalar çok artmalı")
            // Bina sokağa doğru 3 m büyüdü (ön cephe z = −2 → −5). Ham koordinat; müşteri girişten koridora (cz) yürür, oradan oturağa.
            _layout = false;
            GameObject FrontTable(string name, float x, float z, Transform ent, float cz)
            {
                var seats = new[]
                {
                    Seat(x - 0.65f, z, (ent.position.x, cz), (x - 0.65f, cz)),
                    Seat(x + 0.65f, z, (ent.position.x, cz), (x + 0.65f, cz)),
                };
                return Table(name, "PR_Furniture_MasaYuvarlak", tables, x, z, 3f, ent, ent, seats);
            }
            var frontRow = new (string id, float x, float z, Transform ent, float cz, long cost, int lv, string req, string label)[]
            {
                ("masaM1", -2.5f, -3.0f, ent1, -3.9f, 160, 3, "", "Yeni Masa"),
                ("masaM2", 2.5f, -3.0f, ent1, -3.9f, 240, 4, "", "Yeni Masa"),
                ("masaS1", 5.4f, -2.8f, ent2, -3.55f, 320, 5, "salon", "Yeni Masa"),
                ("masaS2", 13.6f, -2.8f, ent2, -3.55f, 400, 5, "salon", "Yeni Masa"),
                ("masaS3", 7.5f, -2.8f, ent2, -3.55f, 480, 6, "salon", "Yeni Masa"),
                ("masaS4", 11.5f, -2.8f, ent2, -3.55f, 560, 6, "salon", "Yeni Masa"),
                ("masaS5", 6.45f, -4.3f, ent2, -3.55f, 650, 7, "salon", "Yeni Masa"),
                ("masaS6", 12.55f, -4.3f, ent2, -3.55f, 750, 7, "salon", "Yeni Masa"),
                ("masaB1", -8.0f, -2.8f, ent3, -3.55f, 900, 8, "bahce", "Bahçe Masası"),
                ("masaB2", -10.2f, -2.8f, ent3, -3.55f, 1000, 8, "bahce", "Bahçe Masası"),
                ("masaB3", -12.4f, -2.8f, ent3, -3.55f, 1100, 9, "bahce", "Bahçe Masası"),
                ("masaB4", -9.1f, -4.3f, ent3, -3.55f, 1200, 9, "bahce", "Bahçe Masası"),
                ("masaB5", -11.3f, -4.3f, ent3, -3.55f, 1350, 10, "bahce", "Bahçe Masası"),
            };
            foreach (var f in frontRow)
            {
                var ft = FrontTable("Table_" + f.id, f.x, f.z, f.ent, f.cz);
                Zone(zones, f.id, f.label, f.cost, f.lv, f.req, f.x, f.z, new[] { ft });
            }
            _layout = true;

            _layout = false;
            Zone(zones, "mutfak", "Mutfağı Aç", 700, 6, "", KitchenLayout.DoorX, 4.05f, new GameObject[0], new[] { wallKitchen.gameObject });
            var hk = BuildInn(zones);
            var gameRoom = BuildGameRoom(zones, ent2, hub2, wallGame.gameObject);
            BuildCellar(zones, ent3, hub3, wallCellar.gameObject, db, stations, machineEntries);
            BuildTerrace(zones, ent2, hub2, db, stations, machineEntries);
            BuildYard(zones, wallYard.gameObject);
            BuildDock(zones, gateDock.gameObject, db, stations, machineEntries);
            var world = BuildWorld();
            BuildTiers(tables);
            staffEntries.Add(new() { id = "staff_sema", root = hk });
            _layout = true;

            // Görevliler müşterilerden ayırt edilsin: baş üstü etiket (HUD) + ayak altı rol renginde halka
            foreach (var e in staffEntries)
            {
                foreach (var def in Catalog.StaffList)
                {
                    if (def.id != e.id) continue;
                    var badge = e.root.AddComponent<StaffBadge>();
                    badge.displayName = def.name;
                    badge.staffId = def.id;
                    badge.role = def.role;
                    AddRoleRing(e.root.transform, def.role);
                }
            }


            // ================================================================ KAYIT (makine / personel / dekor)
            var reg = systems.AddComponent<TavernRegistry>();
            reg.machines = machineEntries.ToArray();
            reg.staff = staffEntries.ToArray();
            reg.decorRoot = Group("PlacedDecor");
            reg.decorSlots = DecorSlots();
            var prefabs = new List<TavernRegistry.DecorPrefab>();
            foreach (var d in Catalog.DecorList) prefabs.Add(new TavernRegistry.DecorPrefab { item = d.id, prefab = TavernaImport.Prefab(d.prefab) });
            reg.decorPrefabs = prefabs.ToArray();

            // ================================================================ MÜŞTERİ YOLU
            var spawner = new GameObject("CustomerSpawner").AddComponent<CustomerSpawner>();
            spawner.outside = Point("StreetWest", spawner.transform, -13.5f, -6.5f);
            spawner.outsideB = Point("StreetEast", spawner.transform, 13.5f, -6.5f);
            spawner.entrance = ent1;
            spawner.hub = hub1;

            // ================================================================ OYUNCU (Barlo)
            var player = new GameObject("Player");
            player.transform.position = L(0.3f, 0f, 0.6f);
            var cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.55f, 0f);
            cc.radius = 0.28f;
            cc.height = 1.1f;
            cc.stepOffset = 0.2f;
            var pc = player.AddComponent<PlayerController>();
            var carrier = player.AddComponent<Carrier>();
            var visual = P("CH_Main_Barlo", player.transform, 0f, 0f);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.name = "Visual";
            pc.anim = visual.AddComponent<ToyAnimator>();
            pc.carrier = carrier;
            carrier.stackPoint = TrayStack(visual.transform, out var tray);
            carrier.trayVisual = tray;

            // ================================================================ KAMERA + HUD
            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.fieldOfView = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("#2A1F1A");
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 60f;
            dayNight.cam = cam;
            var follow = camGo.AddComponent<CameraFollow>();
            follow.target = player.transform;
            follow.minXZ = new Vector2(-13f, -6f);   // büyük odalar (v0.6) + v1.2 ön uzantı
            follow.maxXZ = new Vector2(13f, 9.2f);   // mutfak (v0.7)
            follow.SnapNow();

            var hudGo = new GameObject("HUD");
            var doc = hudGo.AddComponent<UIDocument>();
            BindDocument(doc, panel, UiDir + "/Hud.uxml");
            hudGo.AddComponent<HudController>().cam = cam;

            // Performans: zemin, duvar, dekor tek seferde birleştirilsin (mobilde draw call düşer)
            _layout = false;
            foreach (var g in new[] { ground, floorThemes, walls, decor, wallLeft, wallRight, wallKitchen, kitchen, wallGame, world, wallCellar, wallYard, gateDock })
                foreach (var t in g.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

            Save(scene, GameScene);
        }

        static Transform TrayStack(Transform visual, out GameObject tray)
        {
            var stack = new GameObject("TrayStack").transform;
            stack.SetParent(visual, false);
            stack.localPosition = new Vector3(0f, 0.6f, 0.42f);
            tray = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(tray.GetComponent<Collider>());
            tray.name = "Tray";
            tray.transform.SetParent(stack, false);
            tray.transform.localPosition = new Vector3(0f, -0.012f, 0f);
            tray.transform.localScale = new Vector3(0.46f, 0.012f, 0.34f);
            tray.GetComponent<MeshRenderer>().sharedMaterial = TavernaImport.ColorMaterial(TavernaImport.TrayMatPath, Hex("#9C9488"));
            return stack;
        }

        /// <summary>Görevlinin ayak altına rol renginde ince halka (zeminin 4 cm üst yüzeyinin üstünde, titremesin).</summary>
        static void AddRoleRing(Transform root, Catalog.Role role)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.name = "RoleRing";
            ring.transform.SetParent(root, false);
            ring.transform.localPosition = new Vector3(0f, 0.045f, 0f);
            ring.transform.localScale = new Vector3(0.78f, 0.004f, 0.78f);
            string path = TavernaImport.Root + "/Art/M_Role_" + role + ".mat";
            var r = ring.GetComponent<MeshRenderer>();
            r.sharedMaterial = TavernaImport.ColorMaterial(path, StaffBadge.RoleColor(role));
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>v1.1: duraklar arasında gidip çalışan görevli (barmen, aşçı). Duraklar dünya koordinatında.</summary>
        static GameObject MakeWorker(Transform parent, string name, string prefab, Vector3 pos, Vector3[] spots, float[] faces, bool kitchen)
        {
            var root = MakeIdleStaff(parent, name, prefab, pos, 180f);
            var idle = root.GetComponent<StaffIdle>();
            var w = root.AddComponent<StaffWork>();
            w.anim = idle.anim;
            w.spots = spots;
            w.faces = faces;
            w.kitchenStations = kitchen;
            Object.DestroyImmediate(idle);
            return root;
        }

        static GameObject MakeIdleStaff(Transform parent, string name, string prefab, Vector3 pos, float rotY)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(L(pos.x, 0f, pos.z), Quaternion.Euler(0f, rotY, 0f));
            var visual = P(prefab, root.transform, pos.x, pos.z);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            root.AddComponent<StaffIdle>().anim = visual.AddComponent<ToyAnimator>();
            root.SetActive(false);
            return root;
        }

        /// <summary>
        /// Han (v0.8): salonda merdiven + resepsiyon ("merdiven" ödeme karesiyle açılır, Sv10), üst kat ayrı bir yerde
        /// (z ≈ 37–43, Inn.UpZ üstü): koridor, 6 oda (2 m genişlik), ilk 2 oda merdivenle, diğerleri ödeme kareleriyle.
        /// Konumlar Inn sabitleriyle aynı. Geri dönüş: Hizmetçi kökü (personel kaydı için).
        /// </summary>
        static Transform _terraceDoor;

        static GameObject BuildInn(Transform zones)
        {
            _terraceDoor = null;
            bool lay = _layout;
            _layout = false;
            var inn = Group("Inn");
            // --- Salon: merdiven (önü −x, duvara doğru yükselir) + resepsiyon
            var stairs = P("PR_Furniture_Merdiven", inn, 14.45f, 3.6f, 90f);
            var upTrig = new GameObject("StairsUp").AddComponent<Stairs>();
            upTrig.transform.SetParent(stairs.transform, false);
            upTrig.transform.position = Inn.StairsFoot;
            upTrig.arrive = Inn.UpArrive;
            upTrig.goesUp = true;
            var desk = P("PR_Furniture_Resepsiyon", inn, 14.35f, 1.5f, 90f);
            Zone(zones, "merdiven", "Hanı Aç (merdiven)", 3000, 10, "salon", Inn.StairsFoot.x - 0.1f, 3.6f, new[] { stairs, desk });

            // --- Üst kat
            var up = Group("Upstairs");
            for (int ix = 0; ix < 12; ix++)
                for (int iz = 0; iz < 6; iz++)
                    P("PR_Building_Zemin_Parke", up, -5.5f + ix, 37.5f + iz);
            for (int i = 0; i < 12; i++) P(i % 2 == 0 ? "PR_Building_Pencere" : "PR_Building_Duvar", up, -5.5f + i, 43.1f);
            for (int i = 0; i < 6; i++)
            {
                P("PR_Building_Duvar", up, -6.1f, 37.5f + i, -90f);
                P("PR_Building_Duvar", i < 2 ? _terraceDoor ? _terraceDoor : (_terraceDoor = Group("Wall_ToTerrace")) : up, 6.1f, 37.5f + i, 90f);   // i < 2: teras kapısı
            }
            P("PR_Building_Kose", up, -6.1f, 43.1f);
            P("PR_Building_Kose", up, 6.1f, 43.1f);
            foreach (var k in new[] { -4f, -2f, 0f, 2f, 4f })
                for (int i = 0; i < 4; i++) P("PR_Building_Duvar", up, k, 39.5f + i, 90f).transform.localScale = new Vector3(1f, 0.45f, 1f);   // alçak oda bölmeleri (kesit görünüm)
            var hole = P("PR_Furniture_MerdivenUst", up, Inn.UpHole.x, Inn.UpHole.z);
            Object.DestroyImmediate(hole.GetComponent<Collider>());   // üstünden geçilip aşağı inilsin
            var downTrig = new GameObject("StairsDown").AddComponent<Stairs>();
            downTrig.transform.SetParent(hole.transform, false);
            downTrig.transform.position = Inn.UpHole;
            downTrig.arrive = Inn.SalonArrive;
            downTrig.goesUp = false;
            var upFloor = new GameObject("UpFloorCollider", typeof(BoxCollider));
            upFloor.transform.SetParent(up, false);
            upFloor.GetComponent<BoxCollider>().center = new Vector3(0f, -0.5f, 40f);
            upFloor.GetComponent<BoxCollider>().size = new Vector3(14f, 1f, 8f);
            var upFront = new GameObject("UpFrontBarrier", typeof(BoxCollider));
            upFront.transform.SetParent(up, false);
            upFront.GetComponent<BoxCollider>().center = new Vector3(0f, 1f, 36.85f);
            upFront.GetComponent<BoxCollider>().size = new Vector3(12.4f, 2f, 0.3f);
            foreach (var x in new[] { -3f, 1f, 4.5f })
                AddPoint("UpLamp", new Vector3(x, 2.2f, 38f), "#FFD9A0", 1.4f, 5f).transform.SetParent(up, true);

            // --- Odalar
            var rooms = Group("Rooms");
            int[] costs = { 0, 0, 1500, 2200, 3000, 4000 };
            int[] levels = { 0, 0, 11, 12, 14, 16 };
            for (int i = 0; i < Inn.RoomCount; i++)
            {
                float c = -5f + 2f * i;
                var root = new GameObject($"Room{i + 1}");
                root.transform.SetParent(rooms, false);
                var room = root.AddComponent<Room>();
                room.index = i;
                for (int t = 0; t < 3; t++)
                {
                    room.beds[t] = P($"PR_Furniture_Yatak_T{t + 1}", root.transform, c, 41.8f);
                    room.beds[t].SetActive(t == 0);
                }
                room.messy = P("PR_Furniture_YorganDaginik", root.transform, c, 41.8f);
                room.messy.SetActive(false);
                P("PR_Furniture_Komodin", root.transform, c + 0.72f, 42.55f);
                var bp = new GameObject("BedPoint").transform;
                bp.SetParent(root.transform, false);
                bp.position = new Vector3(c, 0.48f, 40.95f);
                room.bedPoint = bp;
                AddPoint("RoomLamp", new Vector3(c + 0.72f, 0.9f, 42.5f), "#FFB060", 0.8f, 2.2f).transform.SetParent(root.transform, true);
                if (i >= 2)
                    Zone(zones, $"oda{i + 1}", $"Oda {i + 1}", costs[i], levels[i], "merdiven", c + 0.5f, 38.35f, new[] { root });
            }

            // --- Hizmetçi (üst kat koridoru)
            var hk = new GameObject("Housekeeper_Sema");
            hk.transform.SetParent(up, false);
            hk.transform.position = new Vector3(2.5f, 0f, 38.0f);
            var vis = P("CH_Staff_08_Hizmetci", hk.transform, 2.5f, 38.0f);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localRotation = Quaternion.identity;
            var keeper = hk.AddComponent<Housekeeper>();
            keeper.anim = vis.AddComponent<ToyAnimator>();
            keeper.idlePoint = Point("HkIdle", up, 2.5f, 38.0f);
            hk.SetActive(false);

            foreach (var t in up.GetComponentsInChildren<Transform>(true))
                if (!t.GetComponent<Light>() && !t.GetComponent<Stairs>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            _layout = lay;
            return hk;
        }

        /// <summary>
        /// Oyun salonu (v0.9): Salon'un arkası (x 4.1..15.1, z 5.1..10.1), kapı x = 9.5 (KitchenLayout.GameDoorX).
        /// "oyun" ödeme karesi (Sv13) kapı duvarını kaldırır ve zar masasını açar; kart (Sv15) ve bilek güreşi (Sv16) içeride.
        /// Konumlar yeni haritada (eşlemesiz). Müşteri yolu: salon girişi → salon merkezi → kapı → masa.
        /// </summary>
        static Transform BuildGameRoom(Transform zones, Transform ent, Transform hub, GameObject doorWall)
        {
            bool lay = _layout;
            _layout = false;
            var room = Group("GameRoom");
            var shell = Group("GameRoomShell");
            shell.SetParent(room, false);
            for (int ix = 0; ix < 11; ix++)
                for (int iz = 0; iz < 5; iz++)
                    P("PR_Building_Zemin_Dama", shell, 4.6f + ix, 5.6f + iz);
            string[] back = { "Duvar", "Pencere", "Duvar", "Duvar", "Pencere", "Duvar", "Duvar", "Pencere", "Duvar", "Duvar", "Pencere" };
            for (int i = 0; i < back.Length; i++) P("PR_Building_" + back[i], shell, 4.6f + i, 10.1f);
            string[] right = { "Duvar", "Pencere", "Duvar", "Duvar", "Duvar" };
            for (int i = 0; i < right.Length; i++) P("PR_Building_" + right[i], shell, 15.1f, 5.6f + i, 90f);
            P("PR_Building_Kose", shell, 15.1f, 10.1f);
            P("PR_Decor_FiciYigini", shell, 14.4f, 9.3f, -90f);
            P("PR_Decor_Saksi", shell, 5.0f, 9.55f);
            AddPoint("GameRoomLamp", new Vector3(9.5f, 2.3f, 7.6f), "#FFD9A0", 1.5f, 7.5f).transform.SetParent(room, true);

            Vector2[] Via(float x, params (float x, float z)[] tail)
            {
                var l = new List<Vector2> { new(KitchenLayout.GameDoorX, 4.4f), new(KitchenLayout.GameDoorX, 5.8f), new(x, 6.6f) };
                foreach (var t in tail) l.Add(new Vector2(t.x, t.z));
                return l.ToArray();
            }
            SeatSpec S(float x, float z, Vector2[] path)
            {
                var a = new (float, float)[path.Length];
                for (int i = 0; i < a.Length; i++) a[i] = (path[i].x, path[i].y);
                return Seat(x, z, a);
            }
            var zar = Table("GameTable_Zar", "PR_Furniture_ZarMasasi", room, 7.2f, 7.6f, 6f, ent, hub, new[]
            {
                S(6.35f, 7.6f, Via(6.35f)), S(8.05f, 7.6f, Via(8.05f)), S(7.2f, 8.5f, Via(8.45f, (8.45f, 8.5f))),
            });
            var kart = Table("GameTable_Kart", "PR_Furniture_KartMasasi", room, 10.6f, 7.6f, 7f, ent, hub, new[]
            {
                S(9.85f, 7.6f, Via(9.85f)), S(11.35f, 7.6f, Via(11.35f)), S(10.6f, 8.4f, Via(11.9f, (11.9f, 8.4f))),
            });
            var bilek = Table("GameTable_Bilek", "PR_Furniture_BilekMasasi", room, 13.4f, 7.6f, 8f, ent, hub, new[]
            {
                S(12.75f, 7.6f, Via(12.75f)), S(14.05f, 7.6f, Via(14.05f)),
            });
            void Game(GameObject t, GameTable.Kind kind, int fee, int dice)
            {
                var g = t.AddComponent<GameTable>();
                g.kind = kind;
                g.feePerPlayer = fee;
                var props = new List<Transform>();
                for (int i = 0; i < dice; i++)
                {
                    var d = P("PR_Decor_Zar", t.transform, t.transform.position.x - 0.12f + i * 0.2f, t.transform.position.z - 0.15f, 0f, 0.72f);
                    props.Add(d.transform);
                }
                g.props = props.ToArray();
            }
            Game(zar, GameTable.Kind.Zar, Balance.DiceFee, 2);
            Game(kart, GameTable.Kind.Kart, Balance.CardFee, 0);
            Game(bilek, GameTable.Kind.Bilek, Balance.ArmFee, 0);

            var torchA = AddLantern(room, "GameTorchA", "salon", 6.0f, 9.7f, 3.5f);
            var torchB = AddLantern(room, "GameTorchB", "salon", 13.0f, 9.7f, 3.5f);
            Zone(zones, "oyun", "Oyun Salonu", 4000, 13, "salon", KitchenLayout.GameDoorX, 4.05f, new[] { zar, torchA, torchB }, new[] { doorWall });
            Zone(zones, "kart", "Kart Masası", 3000, 15, "oyun", 10.6f, 6.35f, new[] { kart });
            Zone(zones, "bilek", "Bilek Güreşi", 4500, 16, "oyun", 13.4f, 6.35f, new[] { bilek });
            foreach (var t in shell.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            _layout = lay;
            return room;
        }

        /// <summary>
        /// Çevre (v0.9, oyuncu testi "hiçbir yerde boşluk gözükmesin"): kamera sınırları içinde görünen her yer dolu —
        /// büyük çim zemin, arkada tepeler ve dağlar, ağaç kuşağı, yanlarda kaya / çalı / kuyu / araba, sokağın önünde çit ve
        /// çiçek tarhları, üst katın çevresinde çatı kiremidi. Tavernanın tabelası (adı HUD yazar: "TavernSignText").
        /// Rastgele dağılım sabit tohumlu (her kurulumda aynı).
        /// </summary>
        static Transform BuildWorld()
        {
            bool lay = _layout;
            _layout = false;
            var world = Group("World");
            var rnd = new System.Random(909);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Object.DestroyImmediate(plane.GetComponent<Collider>());
            plane.name = "WorldGround";
            plane.transform.SetParent(world, false);
            plane.transform.position = new Vector3(0f, -0.03f, 6.5f);
            plane.transform.localScale = new Vector3(8f, 1f, 4.7f);   // 80 × 47 m: z −17..30
            plane.GetComponent<MeshRenderer>().sharedMaterial = TavernaImport.ColorMaterial(TavernaImport.Root + "/Art/M_WorldGrass.mat", Hex("#5E8F45"));

            GameObject Put(string prefab, float x, float z, float rot = 0f, float s = 1f)
            {
                var go = P(prefab, world, x, z, rot);
                go.transform.localScale = Vector3.one * s;
                return go;
            }
            // Arkada tepeler ve uzak dağlar
            foreach (var (x, z) in new[] { (-26f, 17f), (-13f, 20f), (0f, 22f), (13f, 19f), (26f, 17f) }) Put("PR_Nature_Tepe", x, z, R(0, 360), R(0.9f, 1.2f));
            foreach (var (x, z) in new[] { (-20f, 24f), (6f, 25f), (25f, 24f) }) Put("PR_Nature_Dag", x, z, R(0, 360), R(0.9f, 1.05f));   // z ≤ 25: üst kat görüşüne girmesin
            // Ağaç kuşağı (mutfak ve oyun salonunun arkası)
            int k = 0;
            for (float x = -26f; x <= 26f; x += 2.6f, k++)
            {
                float tx = x + R(-0.6f, 0.6f), tz = R(11.4f, 14.5f), ts = R(0.9f, 1.35f), tr = R(0, 360);
                if (tx < -15.2f) continue;   // göl
                Put(k % 3 == 1 ? "PR_Nature_Cam" : "PR_Nature_Agac", tx, tz, tr, ts);
            }
            // Bahçenin arkası (çitin ötesi)
            // (v1.0: bahçenin arkası artık Şarap Mahzeni; kuyu ve tarhlar binanın soluna taşındı)
            Put("PR_Nature_Kuyu", -21.8f, 9.4f, 20f);
            Put("PR_Nature_CicekTarhi", -16.8f, 5.8f, 90f);
            Put("PR_Nature_Cali", -16.6f, 10.6f); Put("PR_Nature_Kaya1", -15.9f, 11.2f, 30f);
            // Sağ yan (salonun ötesi)
            // (v1.0: salonun sağı artık Arka Avlu + Ahır; ağaçlar ve araba avlunun ötesine)
            Put("PR_Nature_Araba", 25.6f, 3.4f, 100f);
            foreach (var (x, z) in new[] { (24.8f, 8.8f), (26.2f, -0.6f), (27.5f, 6.0f) })
                Put("PR_Nature_Cam", x, z, R(0, 360), R(0.9f, 1.2f));
            Put("PR_Nature_Kaya2", 25.0f, 10.4f, 15f); Put("PR_Nature_Cali", 24.4f, -1.6f); Put("PR_Nature_Cali", 19.2f, 9.8f);
            // Sol yan (bahçenin ötesi)
            // (v1.0: bahçenin solu artık İskele + göl; kıyıda sazlar ve nilüferler)
            foreach (var (x, z) in new[] { (-21.7f, -1.9f), (-15.7f, 6.5f), (-18.6f, 6.4f), (-21.6f, 6.2f), (-27f, -3f) }) Put("PR_Nature_Saz", x, z, R(0, 360), R(0.9f, 1.3f));
            foreach (var (x, z) in new[] { (-24.6f, -2.6f), (-26.5f, 3.2f), (-19.2f, 8.4f), (-29f, 7f) }) Put("PR_Nature_Nilufer", x, z, R(0, 360), R(0.9f, 1.3f));
            // Sokağın önü: alçak çit, çiçek tarhları, çalılar (oyunu kapatmasın diye alçak)
            for (int i = 0; i < 44; i++) P("PR_Building_Cit", world, -21.5f + i, -8.6f);
            foreach (var x in new[] { -10f, -2f, 6f, 13f }) Put("PR_Nature_CicekTarhi", x, -9.7f);
            foreach (var x in new[] { -14f, -6f, 2f, 9.5f, 17f }) Put("PR_Nature_Cali", x, -9.4f, R(0, 360), R(0.8f, 1f));
            Put("PR_Nature_Kaya1", -17.5f, -10.4f); Put("PR_Nature_Kaya1", 19.5f, -10.8f, 120f);
            foreach (var x in new[] { -22f, -12f, 0f, 11f, 22f }) Put("PR_Nature_Agac", x + R(-1f, 1f), R(-11.5f, -9.5f), R(0, 360), R(0.9f, 1.2f));
            // Ot tutamları (bina, sokak ve yollardan uzak)
            for (int i = 0; i < 110; i++)
            {
                float x = R(-28f, 28f), z = R(-12f, 16f);
                bool building = x > -21.5f && x < 24f && z > -5.5f && z < 10.4f;   // bina + avlu + iskele (v1.2 ön uzantı)
                if (x < -21f && z > -9f) continue;   // göl
                bool street = z > -8.8f && z < -5.2f;
                if (building || street) continue;
                Put("PR_Nature_OtTutami", x, z, R(0, 360), R(0.8f, 1.4f));
            }
            // Üst katın çevresi: çatı kiremidi (zeminin altında kalan kısmı görünmez)
            for (int ix = 0; ix < 11; ix++)
                for (int iz = 0; iz < 5; iz++)
                    P("PR_Nature_Cati", world, -20f + ix * 4f, 33f + iz * 4f, 0f, -0.25f);

            // Tabela: Meyhane ile bahçenin köşesinde, sokağa bakar; yazı noktası panonun önünde
            // v1.2: tabela seviyeyle kademe atlar (statik grubun dışında: değişince açılıp kapanır)
            var signHolder = Group("SignTiers");
            var signVariants = new GameObject[5];
            signVariants[0] = P("PR_Decor_Tabela", signHolder, -3.2f, -5.6f);
            for (int i = 1; i < 5; i++) { signVariants[i] = P($"PR_Sign_{i + 1}", signHolder, -3.2f, -5.6f); signVariants[i].SetActive(false); }
            signHolder.gameObject.AddComponent<SignTier>().variants = signVariants;
            // v1.0 Hazine: Kraliyet tabela çerçevesi (statik grubun dışında: alınınca görünür)
            var frameHolder = Group("PremiumSignFrame");
            var frame = P("PR_Decor_TabelaCerceve", frameHolder, -3.2f, -5.6f);
            var pv = frameHolder.gameObject.AddComponent<PremiumVisual>();
            pv.productId = "frame_kraliyet";
            pv.target = frame;
            frame.SetActive(false);
            var signText = new GameObject("TavernSignText").transform;
            signText.SetParent(world, false);
            signText.position = new Vector3(-3.2f, 1.98f, -5.66f);

            _layout = lay;
            return world;
        }

        /// <summary>
        /// "Tavernayı Büyüt" kademe parçaları (v1.0): Taş Han (sütun, kemer, demir avize, keten örtü) ve
        /// Saray Hanı (mermer sütun, altın kemer, saray avizesi, kadife örtü, perdeler, kırmızı halı). Konumlar yeni haritada.
        /// Ayrıca "Tavernayı Büyüt" hedef alanı (Salon önü). Masa örtü ve avizeleri meyhane + salon masalarının üstüne konur.
        /// </summary>
        static void BuildTiers(Transform tables)
        {
            bool lay = _layout;
            _layout = false;
            var root = Group("Tiers");
            var t1 = new GameObject("Tier1_TasHan").transform; t1.SetParent(root, false);
            var t2 = new GameObject("Tier2_SarayHani").transform; t2.SetParent(root, false);
            var set = root.gameObject.AddComponent<TierSet>();
            set.variants = new[] { null, t1.gameObject, t2.gameObject };

            // Sütunlar: oda köşeleri + oyun salonu kapısının iki yanı (duvar yüzeyinde, içeride)
            var pillars = new (float x, float z)[] { (-3.85f, 4.85f), (3.85f, 4.85f), (4.35f, 4.85f), (14.85f, 4.85f), (-3.85f, -4.95f), (14.85f, -4.95f), (8.75f, 4.85f), (10.25f, 4.85f) };
            foreach (var (x, z) in pillars)
            {
                P("PR_Tier_TasSutun", t1, x, z);
                P("PR_Tier_MermerSutun", t2, x, z);
            }
            // Kemer: oyun salonu kapısı ve meyhane–salon geçidinin önü (salon açıkken)
            P("PR_Tier_TasKemer", t1, KitchenLayout.GameDoorX, 4.95f);
            P("PR_Tier_AltinKemer", t2, KitchenLayout.GameDoorX, 4.95f);
            // Avize ve örtü: meyhane + salon masaları (bahçe ve oyun salonu hariç)
            foreach (Transform t in tables)
            {
                var pos = t.position;
                if (pos.x < -4.1f || pos.z > 5.1f) continue;
                bool longTable = t.name == "Table_E";
                // v1.1: kademe avizeleri de ışık versin (gövde 2.0–2.7 m)
                NightLight.Attach(P("PR_Tier_DemirAvize", t1, pos.x, pos.z), 2.1f, 5f, new Color(1f, 0.7f, 0.42f), 2f);
                NightLight.Attach(P("PR_Tier_SarayAvize", t2, pos.x, pos.z), 2.1f, 6f, new Color(1f, 0.84f, 0.6f), 2.6f);
                P("PR_Tier_Ortu1", t1, pos.x, pos.z, longTable ? 90f : 0f);
                P("PR_Tier_Ortu2", t2, pos.x, pos.z, longTable ? 90f : 0f);
            }
            // Saray: arka duvarda perdeler (üst saçak, makinelerin üstünde kalır) + giriş halıları
            foreach (var x in new[] { -2.8f, 2.8f, 5.6f, 12.8f }) P("PR_Tier_Perde", t2, x, 4.98f);
            P("PR_Tier_Hali", t2, 0f, -0.9f);
            P("PR_Tier_Hali", t2, 9.46f, -0.9f);

            // Hedef alanı (mockup M): Salon önü, masa ve yol dışında
            var zoneGo = new GameObject("TierZone");
            zoneGo.transform.SetParent(root, false);
            zoneGo.transform.position = new Vector3(0f, 0.046f, -3.6f);   // v1.2: Meyhane ön koridoru (salon önü masalarla doldu)
            var zone = zoneGo.AddComponent<TierZone>();
            var vis = P("PR_Tier_BuyutKaresi", zoneGo.transform, 0f, -3.6f, 0f, 0.046f);
            vis.transform.localScale = Vector3.one * 0.75f;
            zone.visual = vis;
            vis.SetActive(false);
            _layout = lay;
        }

        /// <summary>
        /// Şarap Mahzeni (v1.0, Sv17): bahçenin arkasında taş kiler (x −15.1..−4.1, z 5.1..10.1), kapı x = −10.5.
        /// İçinde dinlendirme fıçıları (Yıllanmış Şarap) ve bakır imbik (Bal Likörü), şişe rafları ve iki tadım masası.
        /// </summary>
        static void BuildCellar(Transform zones, Transform ent, Transform hub, GameObject doorWall, GameDatabase db, Transform stations,
                                List<TavernRegistry.MachineEntry> machines)
        {
            bool lay = _layout;
            _layout = false;
            var room = Group("Cellar");
            var shell = Group("CellarShell");
            shell.SetParent(room, false);
            for (int ix = 0; ix < 11; ix++)
                for (int iz = 0; iz < 5; iz++)
                    P("PR_Building_Zemin_Tas", shell, -14.6f + ix, 5.6f + iz);
            for (int i = 0; i < 11; i++) P(i % 4 == 2 ? "PR_Building_Pencere_Tas" : "PR_Building_Duvar_Tas", shell, -14.6f + i, 10.1f);
            for (int i = 0; i < 5; i++) P("PR_Building_Duvar_Tas", shell, -15.1f, 5.6f + i, -90f);
            P("PR_Building_Kose", shell, -15.1f, 10.1f);
            P("PR_Building_Kose", shell, -15.1f, 5.1f);
            foreach (var x in new[] { -14.0f, -9.6f, -8.6f, -5.2f }) P("PR_Decor_SarapRafi", shell, x, 9.85f);
            P("PR_Building_TasKemerTavan", shell, -10.5f, 9.7f);
            P("PR_Decor_FiciYigini", shell, -14.4f, 6.0f, 90f);
            AddPoint("CellarLamp", new Vector3(-10.5f, 2.0f, 7.5f), "#FFB060", 1.5f, 7f).transform.SetParent(room, true);
            AddPoint("CellarLamp2", new Vector3(-6.2f, 1.8f, 8.0f), "#FFB060", 1.0f, 4.5f).transform.SetParent(room, true);
            foreach (var t in shell.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

            foreach (var (d, x) in new[] { (DrinkId.YillanmisSarap, -12.5f), (DrinkId.BalLikoru, -6.8f) })
            {
                var st = Station(db, d, stations, x, 9.45f);
                machines.Add(new TavernRegistry.MachineEntry { drink = d, root = st });
                st.SetActive(false);
            }
            Vector2[] Via(float x) => new[] { new Vector2(KitchenLayout.CellarDoorX, 4.4f), new Vector2(KitchenLayout.CellarDoorX, 5.8f), new Vector2(x, 6.4f) };
            SeatSpec S(float x, float z)
            {
                var v = Via(x);
                return Seat(x, z, (v[0].x, v[0].y), (v[1].x, v[1].y), (v[2].x, v[2].y));
            }
            var tA = Table("Table_CellarA", "PR_Furniture_MasaYuvarlak", room, -12.6f, 7.3f, 6f, ent, hub, new[] { S(-13.25f, 7.3f), S(-11.95f, 7.3f) });
            var tB = Table("Table_CellarB", "PR_Furniture_MasaYuvarlak", room, -7.6f, 7.3f, 6f, ent, hub, new[] { S(-8.25f, 7.3f), S(-6.95f, 7.3f) });
            var torch = AddLantern(room, "CellarTorch", "bahce", -11.6f, 9.7f, 4f);
            Zone(zones, "mahzen", "Şarap Mahzeni", 6000, 17, "bahce", KitchenLayout.CellarDoorX, 4.05f, new[] { tA, tB, torch }, new[] { doorWall });
            // v1.2: mahzen 2 → 4 masa (ön sıra, kapı tarafı)
            var tC2 = Table("Table_CellarC", "PR_Furniture_MasaYuvarlak", room, -12.9f, 5.95f, 6f, ent, hub, new[] { S(-13.55f, 5.95f), S(-12.25f, 5.95f) });
            var tD2 = Table("Table_CellarD", "PR_Furniture_MasaYuvarlak", room, -8.0f, 5.95f, 6f, ent, hub, new[] { S(-8.65f, 5.95f), S(-7.35f, 5.95f) });
            Zone(zones, "mahzenC", "Mahzen Masası", 2500, 17, "mahzen", -12.9f, 5.95f, new[] { tC2 });
            Zone(zones, "mahzenD", "Mahzen Masası", 3200, 18, "mahzen", -8.0f, 5.95f, new[] { tD2 });
            _layout = lay;
        }

        /// <summary>
        /// Çatı Terası (v1.0, Sv20): üst katta, han koridorunun sağında açık teras (x 6.1..14.1, z 37..43). Korkuluklar, şemsiye,
        /// fener dizisi, çiçek kutuları; kendi barı (Gün Batımı Punçu) ve üç masa. Müşteriler merdivenden çıkar (Customer kat geçişi).
        /// </summary>
        static void BuildTerrace(Transform zones, Transform ent, Transform hub, GameDatabase db, Transform stations,
                                 List<TavernRegistry.MachineEntry> machines)
        {
            bool lay = _layout;
            _layout = false;
            var room = Group("Terrace");
            var shell = Group("TerraceShell");
            shell.SetParent(room, false);
            for (int ix = 0; ix < 8; ix++)
                for (int iz = 0; iz < 6; iz++)
                    P("PR_Building_Zemin", shell, 6.6f + ix, 37.5f + iz);
            for (int i = 0; i < 8; i++)
            {
                P("PR_Building_Korkuluk", shell, 6.6f + i, 43.1f);
                P("PR_Building_Korkuluk", shell, 6.6f + i, 36.9f);
            }
            for (int i = 0; i < 6; i++) P("PR_Building_Korkuluk", shell, 14.1f, 37.5f + i, 90f);
            foreach (var z in new[] { 39.0f, 40.2f, 41.4f }) P("PR_Decor_CicekKutusu", shell, 13.75f, z, 90f);
            P("PR_Decor_Semsiye", shell, 13.3f, 37.7f);
            P("PR_Decor_FenerDizisi", shell, 10.0f, 42.85f);
            var floorCol = new GameObject("TerraceFloorCollider", typeof(BoxCollider));
            floorCol.transform.SetParent(shell, false);
            floorCol.GetComponent<BoxCollider>().center = new Vector3(10.3f, -0.5f, 40f);
            floorCol.GetComponent<BoxCollider>().size = new Vector3(8.6f, 1f, 7f);
            AddPoint("TerraceLamp", new Vector3(10f, 2.2f, 40f), "#FFD9A0", 1.2f, 7f).transform.SetParent(room, true);
            foreach (var t in shell.GetComponentsInChildren<Transform>(true))
                if (!t.GetComponent<Collider>() || t.GetComponent<MeshRenderer>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

            var st = Station(db, DrinkId.GunBatimiPuncu, stations, 12.6f, 42.4f);
            machines.Add(new TavernRegistry.MachineEntry { drink = DrinkId.GunBatimiPuncu, root = st });
            st.SetActive(false);

            // Müşteri yolu: salon → merdiven ayağı → (üst kat) varış → koridor → teras kapısı → masa
            (float, float)[] Via(float x, params (float, float)[] tail)
            {
                var l = new List<(float, float)> { (Inn.StairsFoot.x, Inn.StairsFoot.z), (Inn.UpArrive.x, Inn.UpArrive.z), (5.6f, 38.0f), (6.9f, 38.0f), (x, 38.3f) };
                l.AddRange(tail);
                return l.ToArray();
            }
            var t1 = Table("Table_TerraceA", "PR_Furniture_MasaYuvarlak", room, 8.0f, 39.5f, 8f, ent, hub, new[] { Seat(7.35f, 39.5f, Via(7.35f)), Seat(8.65f, 39.5f, Via(8.65f)) });
            var t2 = Table("Table_TerraceB", "PR_Furniture_MasaYuvarlak", room, 10.9f, 39.5f, 8f, ent, hub, new[] { Seat(10.25f, 39.5f, Via(10.25f)), Seat(11.55f, 39.5f, Via(11.55f)) });
            var t3 = Table("Table_TerraceC", "PR_Furniture_MasaYuvarlak", room, 8.0f, 41.8f, 8f, ent, hub, new[]
            {
                Seat(7.35f, 41.8f, Via(6.8f, (6.8f, 41.8f))), Seat(8.65f, 41.8f, Via(9.45f, (9.45f, 41.8f))),
            });
            var torchA = AddLantern(room, "TerraceTorchA", "teras", 6.8f, 42.6f, 4f);
            var torchB = AddLantern(room, "TerraceTorchB", "teras", 13.6f, 40.8f, 4f);
            Zone(zones, "teras", "Çatı Terası", 7000, 20, "merdiven", 4.5f, 37.5f, new[] { t1, t2, t3, torchA, torchB }, new[] { _terraceDoor.gameObject });
            // v1.2: teras 3 → 4 masa (barın önü; iki yandaki koridordan oturulur)
            var t4 = Table("Table_TerraceD", "PR_Furniture_MasaYuvarlak", room, 10.9f, 41.8f, 8f, ent, hub, new[]
            {
                Seat(10.25f, 41.8f, Via(9.45f, (9.45f, 41.8f))), Seat(11.55f, 41.8f, Via(12.3f, (12.3f, 41.8f))),
            });
            Zone(zones, "terasD", "Teras Masası", 3500, 20, "teras", 10.9f, 41.8f, new[] { t4 });
            _layout = lay;
        }

        /// <summary>
        /// Arka Avlu + Ahır (v1.0, Sv23): salonun sağı (x 15.1..23.6), kapı salon sağ duvarında z = −1.5. Açık hava masaları;
        /// arkada üç bölmeli ahır (atlar süs). Müşteriler doğrudan sokaktan girer.
        /// </summary>
        static void BuildYard(Transform zones, GameObject door)
        {
            bool lay = _layout;
            _layout = false;
            var room = Group("Yard");
            var shell = Group("YardShell");
            shell.SetParent(room, false);
            for (int ix = 0; ix < 8; ix++)
                for (int iz = 0; iz < 9; iz++)
                    P("PR_Building_Kaldirim", shell, 15.6f + ix, -1.7f + iz);
            foreach (var x in new[] { 17.0f, 19.5f, 22.0f }) P("PR_Building_AhirBolme", shell, x, 7.6f);
            P("PR_Nature_At", shell, 17.0f, 7.5f);
            P("PR_Nature_At2", shell, 22.0f, 7.5f, 15f);
            P("PR_Decor_SamanBalyasi", shell, 19.5f, 7.8f);
            P("PR_Decor_SamanBalyasi", shell, 16.0f, 5.6f, 30f);
            P("PR_Decor_Yalak", shell, 20.6f, 5.7f);
            for (int i = 0; i < 7; i++) P("PR_Building_Cit", shell, 23.6f, -1.5f + i, 90f);
            AddPoint("YardLamp", new Vector3(19.4f, 2.6f, 2.0f), "#FFD9A0", 1.2f, 8f).transform.SetParent(room, true);
            foreach (var t in shell.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

            var nav = Group("YardNav");
            nav.SetParent(room, false);
            var ent = Point("Entrance_Yard", nav, 19.3f, -2.45f);
            var hub = Point("Hub_Yard", nav, 19.3f, -1.2f);
            var tA = Table("Table_YardA", "PR_Furniture_MasaKare", room, 17.4f, 0.5f, 6f, ent, hub, new[] { Seat(16.65f, 0.5f, (16.65f, -0.6f)), Seat(18.15f, 0.5f, (18.15f, -0.6f)) });
            var tB = Table("Table_YardB", "PR_Furniture_MasaKare", room, 21.2f, 0.5f, 6f, ent, hub, new[] { Seat(20.45f, 0.5f, (20.45f, -0.6f)), Seat(21.95f, 0.5f, (21.95f, -0.6f)) });
            var tC = Table("Table_YardC", "PR_Furniture_MasaYuvarlak", room, 19.3f, 3.3f, 6f, ent, hub, new[] { Seat(18.65f, 3.3f, (18.65f, 2.0f)), Seat(19.95f, 3.3f, (19.95f, 2.0f)) });
            var lA = AddLantern(room, "YardTorchA", "avlu", 15.9f, 4.8f, 4f);
            var lB = AddLantern(room, "YardTorchB", "avlu", 23.2f, 1.5f, 4f);
            Zone(zones, "avlu", "Arka Avlu", 8000, 23, "salon", 14.45f, KitchenLayout.YardDoorZ, new[] { tA, tB, tC, lA, lB }, new[] { door });
            // v1.2: avlu 3 → 5 masa
            var tD = Table("Table_YardD", "PR_Furniture_MasaYuvarlak", room, 16.9f, 3.3f, 6f, ent, hub, new[] { Seat(16.25f, 3.3f, (16.25f, 2.0f)), Seat(17.55f, 3.3f, (17.55f, 2.0f)) });
            var tE = Table("Table_YardE", "PR_Furniture_MasaYuvarlak", room, 21.7f, 3.3f, 6f, ent, hub, new[] { Seat(21.05f, 3.3f, (21.05f, 2.0f)), Seat(22.35f, 3.3f, (22.35f, 2.0f)) });
            Zone(zones, "avluD", "Avlu Masası", 3800, 23, "avlu", 16.9f, 3.3f, new[] { tD });
            Zone(zones, "avluE", "Avlu Masası", 4500, 24, "avlu", 21.7f, 3.3f, new[] { tE });
            _layout = lay;
        }

        /// <summary>
        /// İskele (v1.0, Sv26): bahçenin solunda göl kıyısı (güverte x −21.1..−15.1, z −2.2..5.8), kapı bahçe sol çitinde z = 1.5.
        /// Balık ızgarası (Izgara Balık), üç masa, kayıklar, ağ, bağlama direkleri; göl suyu düz renk düzlem.
        /// </summary>
        static void BuildDock(Transform zones, GameObject gate, GameDatabase db, Transform stations, List<TavernRegistry.MachineEntry> machines)
        {
            bool lay = _layout;
            _layout = false;
            var room = Group("Dock");
            var shell = Group("DockShell");
            shell.SetParent(room, false);
            Material water = TavernaImport.ColorMaterial(TavernaImport.Root + "/Art/M_LakeWater.mat", Hex("#4E8DB8"));
            void Water(float cx, float cz, float sx, float sz)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Object.DestroyImmediate(w.GetComponent<Collider>());
                w.name = "LakeWater";
                w.transform.SetParent(shell, false);
                w.transform.position = new Vector3(cx, -0.015f, cz);
                w.transform.localScale = new Vector3(sx / 10f, 1f, sz / 10f);
                w.GetComponent<MeshRenderer>().sharedMaterial = water;
            }
            Water(-33.05f, 6.5f, 23.9f, 31f);    // x −45..−21.1, z −9..22
            Water(-18.1f, 13.9f, 6f, 16.2f);     // güvertenin arkası: x −21.1..−15.1, z 5.8..22
            for (int ix = 0; ix < 6; ix++)
                for (int iz = 0; iz < 8; iz++)
                    P("PR_Building_IskeleTahta", shell, -15.6f - ix, -1.7f + iz);
            for (int i = 0; i < 8; i++) P("PR_Building_Korkuluk", shell, -21.1f, -1.5f + i, 90f);
            for (int i = 0; i < 6; i++) P("PR_Building_Korkuluk", shell, -15.6f - i, 5.8f);
            P("PR_Nature_Kayik", shell, -23.2f, 0.8f, 8f);
            P("PR_Nature_Kayik", shell, -22.8f, 4.1f, -18f);
            P("PR_Decor_Ag", shell, -20.3f, 5.2f);
            foreach (var z in new[] { 0.0f, 3.0f }) P("PR_Decor_BalikciDirek", shell, -21.45f, z);
            AddPoint("DockLamp", new Vector3(-18.2f, 2.6f, 2.0f), "#FFD9A0", 1.2f, 8f).transform.SetParent(room, true);
            foreach (var t in shell.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

            var st = Station(db, DrinkId.IzgaraBalik, stations, -16.7f, 5.05f);
            machines.Add(new TavernRegistry.MachineEntry { drink = DrinkId.IzgaraBalik, root = st });
            st.SetActive(false);

            var nav = Group("DockNav");
            nav.SetParent(room, false);
            var ent = Point("Entrance_Dock", nav, -18.1f, -2.45f);
            var hub = Point("Hub_Dock", nav, -18.1f, -1.2f);
            var tA = Table("Table_DockA", "PR_Furniture_MasaYuvarlak", room, -16.9f, 0.4f, 7f, ent, hub, new[] { Seat(-17.55f, 0.4f, (-17.55f, -0.8f)), Seat(-16.25f, 0.4f, (-16.25f, -0.8f)) });
            var tB = Table("Table_DockB", "PR_Furniture_MasaYuvarlak", room, -19.7f, 0.4f, 7f, ent, hub, new[] { Seat(-20.35f, 0.4f, (-20.35f, -0.8f)), Seat(-19.05f, 0.4f, (-19.05f, -0.8f)) });
            var tC = Table("Table_DockC", "PR_Furniture_MasaYuvarlak", room, -19.0f, 3.3f, 7f, ent, hub, new[] { Seat(-19.65f, 3.3f, (-18.3f, 1.9f), (-19.65f, 2.2f)), Seat(-18.35f, 3.3f, (-18.3f, 1.9f)) });
            var lA = AddLantern(room, "DockTorchA", "iskele", -20.8f, 5.4f, 4f);
            var lB = AddLantern(room, "DockTorchB", "iskele", -15.5f, -1.2f, 4f);
            Zone(zones, "iskele", "İskele", 9000, 26, "bahce", -14.4f, KitchenLayout.DockDoorZ, new[] { tA, tB, tC, lA, lB }, new[] { gate });
            // v1.2: iskele 3 → 4 masa
            var tD = Table("Table_DockD", "PR_Furniture_MasaYuvarlak", room, -16.6f, 2.9f, 7f, ent, hub, new[] { Seat(-17.25f, 2.9f, (-17.25f, 1.9f)), Seat(-15.95f, 2.9f, (-15.95f, 1.9f)) });
            Zone(zones, "iskeleD", "İskele Masası", 5000, 26, "iskele", -16.6f, 2.9f, new[] { tD });
            _layout = lay;
        }

        /// <summary>Fener ışığı + Lantern bileşeni (konum eski koordinatta, eşlenir).</summary>
        static GameObject AddLantern(Transform parent, string name, string zone, float x, float z, float range)
        {
            var l = AddPoint(name, new Vector3(x, 1.35f, z), "#FFB060", 1.6f, range);
            l.transform.SetParent(parent, true);
            var lan = l.gameObject.AddComponent<Lantern>();
            lan.zone = zone;
            l.gameObject.AddComponent<LightFlicker>().amount = 0.18f;
            return l.gameObject;
        }

        static GameObject MakeLamplighter(Transform parent, string name, string prefab, Vector3 pos)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = L(pos.x, 0f, pos.z);
            var visual = P(prefab, root.transform, pos.x, pos.z);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            var c = root.AddComponent<Lamplighter>();
            c.anim = visual.AddComponent<ToyAnimator>();
            c.idlePoint = Point("Idle", parent, pos.x, pos.z);
            root.SetActive(false);
            return root;
        }

        static GameObject MakeCleaner(Transform parent, string name, string prefab, Vector3 pos)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = L(pos.x, 0f, pos.z);
            var visual = P(prefab, root.transform, pos.x, pos.z);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            var c = root.AddComponent<Cleaner>();
            c.anim = visual.AddComponent<ToyAnimator>();
            c.idlePoint = Point("Idle", parent, pos.x, pos.z);
            root.SetActive(false);
            return root;
        }

        /// <summary>Dekor yerleri: (eşya, bölge, konum, yön). Her eşya kendi listesinden ilk boş yere yerleşir.</summary>
        static TavernRegistry.DecorSlot[] DecorSlots()
        {
            var list = new List<TavernRegistry.DecorSlot>();
            void A(string item, string zone, float x, float z, float rot = 0f, float y = 0f) =>
                list.Add(new TavernRegistry.DecorSlot { item = item, zone = zone, pos = new Vector3(MX(x), y, MZ(z)), rotY = rot });
            // Saksı: köşeler
            A("dec_saksi", "", -2.6f, 1.0f); A("dec_saksi", "", -2.65f, -1.7f);   // (−2.6, 2.6) mutfak kapısının önüne düşüyordu
            A("dec_saksi", "salon", 3.55f, -1.6f); A("dec_saksi", "salon", 3.55f, 2.55f);   // (10.6, 2.55) merdivene düşüyordu
            A("dec_saksi", "bahce", -10.5f, 2.5f); A("dec_saksi", "bahce", -3.6f, 2.5f); A("dec_saksi", "bahce", -10.5f, -1.6f);
            // Mumluk: masa üstleri
            A("dec_mum", "", -1.1f, 0.3f, 0f, 0.64f); A("dec_mum", "", -1.1f, -1.5f, 0f, 0.64f); A("dec_mum", "", 0.6f, -0.7f, 0f, 0.64f);
            A("dec_mum", "salon", 7.7f, 0.5f, 0f, 0.64f); A("dec_mum", "salon", 4.6f, -1.05f, 0f, 0.64f);
            A("dec_mum", "bahce", -4.4f, 0.4f, 0f, 0.64f); A("dec_mum", "bahce", -8.2f, 0.4f, 0f, 0.64f);
            // Halı: masa altları
            A("dec_hali", "", -1.3f, 0.5f, 0f, 0.03f); A("dec_hali", "", -1.3f, -1.3f, 0f, 0.034f); // zeminin (0.04) üstünde
            A("dec_hali", "salon", 7.2f, 0.4f, 0f, 0.03f); A("dec_hali", "salon", 4.4f, -0.9f, 0f, 0.034f);
            // Flama
            A("dec_flama", "", -2.6f, -1.75f, 30f); A("dec_flama", "salon", 8.6f, -1.75f, -20f); A("dec_flama", "bahce", -10.6f, -0.4f, 20f);
            // Fıçı yığını
            A("dec_fici", "", 2.62f, 1.05f, 90f); A("dec_fici", "salon", 10.5f, -0.8f, 90f); A("dec_fici", "bahce", -10.4f, 1.0f, -90f);
            // Duvar süsleri
            A("dec_kalkan", "salon", 5.0f, 2.95f);   // (7.25) oyun salonu kapısına düşüyordu A("dec_kalkan", "salon", 10.95f, -0.5f, 90f);
            A("dec_boynuz", "", -1.6f, 2.95f); A("dec_boynuz", "salon", 9.0f, 2.95f);
            A("dec_muhur", "salon", 10.95f, 1.0f, 90f);   // (−1.45) avlu kapısına düşüyordu
            // Avize: masaların üstünde
            A("dec_avize", "", -1.3f, 0.5f, 0f, 0.9f); A("dec_avize", "", 0.45f, -0.55f, 0f, 0.9f);
            A("dec_avize", "salon", 7.2f, 0.4f, 0f, 0.9f); A("dec_avize", "salon", 4.4f, -0.9f, 0f, 0.9f);
            // v1.0 Hazine (premium, birer yer): kristal avize D masasının üstünde; ejder heykeli ve altın fıçı dışarıda, vitrin gibi
            A("dec_kristal", "", 2.0f, -1.3f, 0f, 0.9f);
            list.Add(new TavernRegistry.DecorSlot { item = "dec_ejder", zone = "", pos = new Vector3(3.0f, 0f, -5.85f), rotY = 0f });
            list.Add(new TavernRegistry.DecorSlot { item = "dec_altinfici", zone = "", pos = new Vector3(13.4f, 0f, -5.85f), rotY = 0f });
            return list.ToArray();
        }

        static GameObject MakeWaiter(Transform parent, string name, string prefab, string display, Vector3 pos, Vector3 idle, float speed, int capacity)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = L(pos.x, 0f, pos.z);
            var visual = P(prefab, root.transform, pos.x, pos.z);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            var w = root.AddComponent<Waiter>();
            w.displayName = display;
            w.speed = speed;
            w.anim = visual.AddComponent<ToyAnimator>();
            w.carrier = root.AddComponent<Carrier>();
            w.carrier.capacity = capacity;
            w.carrier.stackPoint = TrayStack(visual.transform, out var tray);
            w.carrier.trayVisual = tray;
            bool lay = _layout;
            _layout = false; // idle zaten eşlenmiş bir noktanın konumu
            w.idlePoint = Point("Idle", parent, idle.x, idle.z);
            _layout = lay;
            root.SetActive(false);
            return root;
        }

        /// <summary>UIDocument referanslarını serileştirme üzerinden atar (özellik ayarlayıcısı editörde kaydedilmiyor).</summary>
        static void BindDocument(UIDocument doc, PanelSettings panel, string uxmlPath)
        {
            var so = new SerializedObject(doc);
            // NewScene kullanılmayan varlıkları boşaltır → referansı her seferinde diskten yeniden yükle
            panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiDir + "/PanelSettings.asset");
            so.FindProperty("m_PanelSettings").objectReferenceValue = panel;
            so.FindProperty("sourceAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            if (!doc.panelSettings) Debug.LogError("[Taverna] PanelSettings atanamadı: " + doc.name);
        }

        static Light AddPoint(string name, Vector3 pos, string color, float intensity, float range)
        {
            var l = new GameObject(name, typeof(Light)).GetComponent<Light>();
            l.type = LightType.Point;
            l.color = Hex(color);
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            l.transform.position = L(pos.x, pos.y, pos.z);
            return l;
        }

        static GameObject Station(GameDatabase db, DrinkId id, Transform parent, float x, float z)
        {
            var def = db ? db.Drink(id) : null;
            var root = new GameObject($"Station_{id}");
            root.transform.SetParent(parent);
            root.transform.position = L(x, 0f, z);
            var st = root.AddComponent<DrinkStation>();
            st.drink = def;
            string key = TavernaData.StationKey[id];
            for (int t = 1; t <= 3; t++)
            {
                var v = P($"PR_Station_{key}_T{t}", root.transform, x, z);
                v.name = $"Tier{t}";
                v.SetActive(t == 1);
                st.tierVisuals[t - 1] = v;
            }
            var sp = new GameObject("ServePoint").transform;     // makinenin 0.75 m önü (eşlemeden bağımsız)
            sp.SetParent(root.transform, false);
            sp.localPosition = new Vector3(0f, 0f, -0.75f);
            st.servePoint = sp;
            return root;
        }

        struct SeatSpec { public Vector2 pos; public Vector2[] approach; }

        static SeatSpec Seat(float x, float z, params (float x, float z)[] approach)
        {
            var a = new Vector2[approach.Length];
            for (int i = 0; i < a.Length; i++) a[i] = new Vector2(approach[i].x, approach[i].z);
            return new SeatSpec { pos = new Vector2(x, z), approach = a };
        }



        static GameObject Table(string name, string prefab, Transform parent, float x, float z, float ambiance,
                                Transform entrance, Transform hub, SeatSpec[] seats)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = L(x, 0f, z);
            // Oturaklar masayla birlikte kayar (masa–sandalye mesafesi korunur); yürüme noktaları eşlenir
            var delta = root.transform.position - new Vector3(x, 0f, z);
            var table = root.AddComponent<Table>();
            table.ambiance = ambiance;
            table.entrance = entrance;
            table.hub = hub;
            P(prefab, root.transform, x, z).name = "Model";
            table.coinPoint = new GameObject("CoinPoint").transform;
            table.coinPoint.SetParent(root.transform, false);
            table.coinPoint.localPosition = new Vector3(0.12f, 0.66f, -0.12f);

            var list = new List<Taverna.Seat>();
            for (int i = 0; i < seats.Length; i++)
            {
                var s = seats[i];
                var seatGo = new GameObject($"Seat{i}");
                seatGo.transform.SetParent(root.transform);
                seatGo.transform.position = new Vector3(s.pos.x, 0f, s.pos.y) + delta;
                var seat = seatGo.AddComponent<Taverna.Seat>();
                seat.table = table;
                seat.approach = new Transform[s.approach.Length];
                for (int k = 0; k < s.approach.Length; k++)
                    seat.approach[k] = Point($"Approach{k}", seatGo.transform, s.approach[k].x, s.approach[k].y);
                // Sandalye müşterinin arkasında, masaya dönük. Uzun masada oturağa en yakın masa noktası esas alınır.
                var tablePoint = (prefab.Contains("Uzun") ? new Vector3(s.pos.x, 0f, z) : new Vector3(x, 0f, z)) + delta;
                var seatPos = seatGo.transform.position;
                var toTable = (tablePoint - seatPos).normalized;
                var chairPos = seatPos - toTable * 0.22f;
                var chair = PRaw(prefab.Contains("Yuvarlak") ? "PR_Furniture_Tabure" : "PR_Furniture_Sandalye", root.transform, chairPos.x, chairPos.z);
                chair.transform.rotation = Quaternion.LookRotation(-toTable);
                list.Add(seat);
            }
            table.seats = list.ToArray();
            return root;
        }

        static void Zone(Transform parent, string id, string label, long cost, int level, string requires, float x, float z,
                         GameObject[] targets, GameObject[] hide = null)
        {
            var root = new GameObject($"PayZone_{id}");
            root.transform.SetParent(parent);
            // Zemin tahtalarının üstü 0.04 m: ödeme karesi onun üstünde dursun (aynı düzlemde titrer)
            root.transform.position = L(x, 0.045f, z);
            P("PR_Building_Odeme", root.transform, x, z, 0f, 0.045f).name = "Visual";
            var pz = root.AddComponent<PayZone>();
            pz.id = id;
            pz.label = label;
            pz.cost = Balance.ZoneCost(cost);   // v0.9 tempo: alanlar ×1.4
            pz.requiredLevel = level;
            pz.requires = requires;
            pz.unlockTargets = targets;
            pz.hideOnUnlock = hide ?? new GameObject[0];
            foreach (var t in targets) t.SetActive(false);
        }

        static void Save(Scene scene, string path)
        {
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..", ScenesDir)));
            EditorSceneManager.SaveScene(scene, path);
        }

        static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;
    }
}
