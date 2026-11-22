using System.IO;
using UnityEditor;
using UnityEngine;

namespace Taverna.EditorTools
{
    /// <summary>
    /// Blender çıktılarını (../../assets) projeye kopyalar ve içe aktarma ayarlarını uygular.
    /// Tüm modeller tek palet materyalini (M_Palette) paylaşır — DESIGN_RULES §4.
    /// </summary>
    public class TavernaImport : AssetPostprocessor
    {
        public const string Root = "Assets/_Game";
        public const string ModelsDir = Root + "/Art/Models";
        public const string PrefabsDir = Root + "/Art/Prefabs";
        public const string IconsDir = Root + "/Art/Icons";
        public const string TexDir = Root + "/Art/Textures";
        public const string AudioDir = Root + "/Resources/Audio";
        public const string MatPath = Root + "/Art/M_Palette.mat";
        public const string TrayMatPath = Root + "/Art/M_Tray.mat";

        /// <summary>Blender'ın "ön" yüzünü (−Y) Unity'de kameraya (−Z) çeviren dönüş. Görsel testle doğrulandı.</summary>
        public const float FrontYaw = 0f;

        /// <summary>FBX'ler Z-yukarı geliyor; Blender'ın Z'sini Unity'nin Y'sine çeviren düzeltme.</summary>
        public const float UpFixPitch = 90f;

        static string SourceRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "../../../assets"));

        // ------------------------------------------------------------------ içe aktarma ayarları
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelsDir)) return;
            var m = (ModelImporter)assetImporter;
            m.bakeAxisConversion = true;
            m.materialImportMode = ModelImporterMaterialImportMode.None;
            m.importAnimation = false;
            m.animationType = ModelImporterAnimationType.None;
            m.importCameras = false;
            m.importLights = false;
            m.useFileScale = true;
            m.globalScale = 1f;
            m.importNormals = ModelImporterNormals.Import;
            m.meshCompression = ModelImporterMeshCompression.Low;
            m.isReadable = false;
        }

        /// <summary>
        /// v1.1 ses: müzik akışla (bellek az), ortam döngüleri sıkıştırılmış bellekte,
        /// kısa efekt ve konuşmalar yüklemede açılır (çalarken işlemci yükü yok).
        /// </summary>
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioDir)) return;
            var a = (AudioImporter)assetImporter;
            var st = a.defaultSampleSettings;
            st.compressionFormat = AudioCompressionFormat.Vorbis;
            // v1.9 (oyuncu testi "ses cızırtılı", Android + iPhone): müzik akış yerine sıkıştırılmış bellekte (akışla açılan Vorbis
            // düşük uçlu cihazda dosya okuması gecikince boşluk / çıtırtı yapabiliyor; 3 parça ≈ 3 MB), ortam kalitesi 0.4 → 0.5,
            // bütün klipler 44.1 kHz'e sabit (kaynaklar 44.1 kHz; platformun "optimize" seçimi yeniden örneklemesin).
            if (assetPath.Contains("/music/")) { st.loadType = AudioClipLoadType.CompressedInMemory; st.quality = 0.5f; }
            // v2.4: ortam döngüleri (room, murmur) tek kanal, normalleştirilmez (düzeyi sentez ve Sfx belirler), kalite 0.7
            else if (assetPath.Contains("/amb/")) { st.loadType = AudioClipLoadType.CompressedInMemory; st.quality = 0.7f; }
            else { st.loadType = AudioClipLoadType.DecompressOnLoad; st.quality = 0.6f; }
            st.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            st.sampleRateOverride = 44100;
            st.preloadAudioData = !assetPath.Contains("/music/");
            a.defaultSampleSettings = st;
            a.loadInBackground = assetPath.Contains("/music/");
            if (assetPath.Contains("/amb/"))
            {
                a.forceToMono = true;
                var so = new SerializedObject(a);   // "normalize" için genel API yok (meta alanı)
                var np = so.FindProperty("normalize") ?? so.FindProperty("m_Normalize");
                if (np != null) { np.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
        }

        void OnPreprocessTexture()
        {
            var t = (TextureImporter)assetImporter;
            if (assetPath.StartsWith(IconsDir) || assetPath.Contains("/UI/Images/") || assetPath.Contains("/Resources/Icons/") || assetPath.Contains("/Resources/Kit/"))
            {
                t.textureType = TextureImporterType.Sprite;
                t.alphaIsTransparency = true;
                t.mipmapEnabled = false;
                // UI kit parçaları küçük ve keskin kenarlı: sıkıştırma kenarlarda bozulma yapar
                t.textureCompression = assetPath.Contains("/Kit/") ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
                t.wrapMode = TextureWrapMode.Clamp;
                // Logo büyük çizilip küçük gösterilir: mipmap olmadan kenarlar tırtıklanır
                if (assetPath.Contains("/UI/Images/Logo/")) { t.mipmapEnabled = true; t.textureCompression = TextureImporterCompression.Uncompressed; }
            }
            else if (assetPath.StartsWith(TexDir))
            {
                t.filterMode = FilterMode.Point;
                t.mipmapEnabled = false;
                t.textureCompression = TextureImporterCompression.Uncompressed;
                t.wrapMode = TextureWrapMode.Clamp;
            }
        }

        // ------------------------------------------------------------------ kopyalama
        public static void CopySources()
        {
            string src = SourceRoot;
            if (!Directory.Exists(src)) throw new IOException("Blender çıktı klasörü bulunamadı: " + src);
            int n = 0;
            n += CopyDir(Path.Combine(src, "export/fbx"), ModelsDir + "/Characters", "*.fbx", topOnly: true);
            n += CopyDir(Path.Combine(src, "export/fbx/props"), ModelsDir + "/Props", "*.fbx", topOnly: true);
            n += CopyDir(Path.Combine(src, "icons"), IconsDir, "*.png", topOnly: true);
            n += CopyFile(Path.Combine(src, "textures/T_Palette_Main.png"), TexDir + "/T_Palette_Main.png");
            n += CopyFile(Path.Combine(src, "renders/Menu_Background.png"), Root + "/UI/Images/Menu_Background.png");
            n += CopyFile(Path.Combine(src, "renders/Menu_Background_Wide.png"), Root + "/UI/Images/Menu_Background_Wide.png");
            n += CopyDir(Path.Combine(src, "ui"), Root + "/UI/Images/Kit", "*.png", topOnly: true);
            n += CopyDir(Path.Combine(src, "ui/logo"), Root + "/UI/Images/Logo", "*.png", topOnly: true);   // v1.0 logo + uygulama ikonu
            // Çalışma anında koddan yüklenen ikonlar (Resources.Load)
            n += CopyDir(Path.Combine(src, "icons"), Root + "/Resources/Icons", "*.png", topOnly: true);
            n += CopyDir(Path.Combine(src, "ui"), Root + "/Resources/Kit", "icon_*.png", topOnly: true);
            n += CopyDir(Path.Combine(src, "ui"), Root + "/Resources/Kit", "v3i_*.png", topOnly: true); // UI v3 ikonları
            // v1.1 ses (tools/audio/build_audio.py): Kenney CC0 efektleri + özgün müzik, konuşma, ortam
            foreach (var sub in new[] { "sfx", "music", "voice", "amb" })
                n += CopyDir(Path.Combine(src, "audio", sub), AudioDir + "/" + sub, "*.ogg", topOnly: true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuildFonts();
            // USS/UXML, görseller ve fontlar hazır olduktan sonra yeniden içe aktarılsın (yollar çözülsün)
            foreach (var guid in AssetDatabase.FindAssets("", new[] { Root + "/UI" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith(".uss") || p.EndsWith(".uxml")) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            }
            Debug.Log($"[Taverna] {n} dosya kopyalandı/güncellendi.");
        }

        // ------------------------------------------------------------------ fontlar
        public const string FontsDir = Root + "/UI/Fonts";

        /// <summary>
        /// UI Toolkit font varlıkları: Rubik Black (başlık, UI v3) ve Nunito Black (metin).
        /// Nunito'nun Türkçe harfleri (ş ğ ı İ) ayrı dosyada → yedek (fallback) olarak bağlanır.
        /// Rubik Türkçeyi kendisi tam içerir. Lilita One (UI v2) eski ekranlar için duruyor;
        /// İ/Ş/Ğ harfleri onda yok, Nunito'dan düşer.
        /// </summary>
        /// <summary>v1.7 CJK yazı tiplerinin örnekleme boyutu (px): 1024'lük atlasta ~300 karakter sığar; arayüzün en büyük yazısı (32px) için yeterli.</summary>
        public const int CjkSamplingSize = 48;
        /// <summary>v1.7 CJK atlas dolgusu (örnekleme boyutunun ~%10'u, Latin yazı tipleriyle aynı oran).</summary>
        public const int CjkPadding = 5;
        /// <summary>Latin yazı tiplerinin örnekleme boyutu ve dolgusu (v0.x'ten beri).</summary>
        public const int LatinSamplingSize = 90, LatinPadding = 9;
        /// <summary>Çalışma anında CJK sırasını değiştiren varlık (bkz. LocFontSet).</summary>
        public const string LocFontsPath = Root + "/Resources/Loc/LocFonts.asset";

        /// <summary>
        /// v1.7 yedek zinciri (döngüsüz): Nunito → [NunitoExt, Rubik]; Rubik → [NunitoExt (₺), SC, JP, KR]; LilitaOne → [Nunito].
        /// Rusça gövde metni Nunito'da yok → Rubik'ten düşer. CJK sırası çalışırken etkin dile göre değişir (LocFonts.Apply).
        /// CJK yazı tipleri dinamik ve çoklu atlaslı; derlemede atlas verisi temizlenir (yalnızca kullanılan karakterler çizilir).
        /// </summary>
        public static void BuildFonts()
        {
            var ext = MakeFontAsset(FontsDir + "/Nunito-Black-LatinExt.ttf", FontsDir + "/NunitoExt.asset", null);
            var sc = MakeFontAsset(FontsDir + "/NotoSansSC-Bold.otf", FontsDir + "/NotoSansSC.asset", null, CjkSamplingSize, CjkPadding);
            var jp = MakeFontAsset(FontsDir + "/NotoSansJP-Bold.otf", FontsDir + "/NotoSansJP.asset", null, CjkSamplingSize, CjkPadding);
            var kr = MakeFontAsset(FontsDir + "/NotoSansKR-Bold.otf", FontsDir + "/NotoSansKR.asset", null, CjkSamplingSize, CjkPadding);
            var rubik = MakeFontAsset(FontsDir + "/Rubik-Black.ttf", FontsDir + "/Rubik.asset", null);
            if (rubik)
            {
                rubik.fallbackFontAssetTable = new System.Collections.Generic.List<UnityEngine.TextCore.Text.FontAsset>();
                foreach (var f in new[] { ext, sc, jp, kr }) if (f) rubik.fallbackFontAssetTable.Add(f);
                EditorUtility.SetDirty(rubik);
            }
            var nunito = MakeFontAsset(FontsDir + "/Nunito-Black-Latin.ttf", FontsDir + "/Nunito.asset", ext);
            if (nunito && rubik) { nunito.fallbackFontAssetTable.Add(rubik); EditorUtility.SetDirty(nunito); }
            MakeFontAsset(FontsDir + "/LilitaOne-Regular.ttf", FontsDir + "/LilitaOne.asset", nunito);

            // Çalışma anı: CJK sırası etkin dile göre (Resources'tan yüklenir)
            var set = AssetDatabase.LoadAssetAtPath<LocFontSet>(LocFontsPath);
            if (!set)
            {
                Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..", Root + "/Resources/Loc")));
                set = ScriptableObject.CreateInstance<LocFontSet>();
                AssetDatabase.CreateAsset(set, LocFontsPath);
            }
            set.host = rubik;
            set.baseFallbacks = ext ? new System.Collections.Generic.List<UnityEngine.TextCore.Text.FontAsset> { ext } : new();
            set.sc = sc; set.jp = jp; set.kr = kr;
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
        }

        static UnityEngine.TextCore.Text.FontAsset MakeFontAsset(string ttf, string outPath, UnityEngine.TextCore.Text.FontAsset fallback,
            int samplingSize = LatinSamplingSize, int padding = LatinPadding)
        {
            var fa = AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>(outPath);
            if (!fa)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
                if (!font) { Debug.LogWarning("[Taverna] Font bulunamadı: " + ttf); return null; }
                fa = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(font, samplingSize, padding,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                    UnityEngine.TextCore.Text.AtlasPopulationMode.Dynamic, true);
                fa.name = Path.GetFileNameWithoutExtension(outPath);
                AssetDatabase.CreateAsset(fa, outPath);
                if (fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0])
                {
                    fa.atlasTextures[0].name = fa.name + " Atlas";
                    AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
                }
                if (fa.material)
                {
                    fa.material.name = fa.name + " Material";
                    AssetDatabase.AddObjectToAsset(fa.material, fa);
                }
            }
            fa.fallbackFontAssetTable = fallback ? new System.Collections.Generic.List<UnityEngine.TextCore.Text.FontAsset> { fallback } : new();
            EditorUtility.SetDirty(fa);
            return fa;
        }

        static int CopyDir(string from, string toAsset, string pattern, bool topOnly)
        {
            if (!Directory.Exists(from)) return 0;
            int n = 0;
            foreach (var f in Directory.GetFiles(from, pattern, topOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories))
                n += CopyFile(f, $"{toAsset}/{Path.GetFileName(f)}");
            return n;
        }

        static int CopyFile(string from, string toAsset)
        {
            if (!File.Exists(from)) return 0;
            string dst = Path.GetFullPath(Path.Combine(Application.dataPath, "..", toAsset));
            Directory.CreateDirectory(Path.GetDirectoryName(dst));
            if (File.Exists(dst) && File.GetLastWriteTimeUtc(dst) >= File.GetLastWriteTimeUtc(from) &&
                new FileInfo(dst).Length == new FileInfo(from).Length) return 0;
            File.Copy(from, dst, true);
            return 1;
        }

        // ------------------------------------------------------------------ materyaller
        public static Material PaletteMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (!mat)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, MatPath);
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/T_Palette_Main.png");
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.12f);
            mat.SetFloat("_SpecularHighlights", 0f);
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.SetFloat("_EnvironmentReflections", 0f);
            mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material ColorMaterial(string path, Color c)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", c);
            mat.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ prefablar
        /// <summary>Her FBX için: kök nesne + doğru yöne çevrilmiş model + palet materyali (+ isteğe bağlı çarpıştırıcı).</summary>
        public static void BuildPrefabs()
        {
            var mat = PaletteMaterial();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelsDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!fbx) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                bool isChar = name.StartsWith("CH_");
                string sub = isChar ? "Characters" : "Props";
                string outPath = $"{PrefabsDir}/{sub}/{name}.prefab";
                Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..", PrefabsDir, sub)));

                var root = new GameObject(name);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                model.transform.SetParent(root.transform, false);
                // Karakterler oyunda +Z'ye (ileri) bakar; eşyaların ön yüzü kameraya (−Z) bakar.
                model.transform.localRotation = Quaternion.Euler(0f, FrontYaw + (isChar ? 180f : 0f), 0f) * Quaternion.Euler(UpFixPitch, 0f, 0f);
                foreach (var r in model.GetComponentsInChildren<MeshRenderer>())
                {
                    r.sharedMaterial = mat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
                if (NeedsCollider(name)) AddBoundsCollider(root);
                PrefabUtility.SaveAsPrefabAsset(root, outPath);
                Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
        }

        static bool NeedsCollider(string n)
        {
            if (n.StartsWith("CH_") || n.StartsWith("PR_Drink_")) return false;
            if (n.StartsWith("PR_Station_") || n.StartsWith("PR_Furniture_")) return !n.Contains("Tabure") && !n.Contains("Sandalye");
            if (n.StartsWith("PR_Building_")) return !n.Contains("Zemin") && !n.Contains("Odeme") && !n.Contains("Kilitli") && !n.Contains("Cim") && !n.Contains("Kaldirim");
            return n.Contains("Somine") || n.Contains("FiciYigini") || n.Contains("Sahne") || n.Contains("Saksi") || n.Contains("Flama");
        }

        static void AddBoundsCollider(GameObject root)
        {
            var rs = root.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            var c = root.AddComponent<BoxCollider>();
            c.center = root.transform.InverseTransformPoint(b.center);
            c.size = b.size;
        }

        public static GameObject Prefab(string name)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Characters/{name}.prefab")
                    ?? AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Props/{name}.prefab");
            if (!p) Debug.LogWarning("[Taverna] Prefab bulunamadı: " + name);
            return p;
        }

        public static Sprite Icon(string key) => AssetDatabase.LoadAssetAtPath<Sprite>($"{IconsDir}/ICO_{key}.png");
    }
}
