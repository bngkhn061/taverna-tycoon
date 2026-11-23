using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Taverna.EditorTools
{
    /// <summary>
    /// Derleme komutları. Windows test sürümü telefon oranında (540x960) pencerede açılır.
    /// Komut satırı: -executeMethod Taverna.EditorTools.TavernaBuild.WindowsDev
    /// v1.6: uygulama kimliği ve sürüm tek yerden (<see cref="ApplyIdentity"/>); her derleme aynısını yazar.
    /// </summary>
    public static class TavernaBuild
    {
        static string BuildDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds"));

        // ------------------------------------------------------------------ v1.6 uygulama kimliği (tek yer)
        /// <summary>Yayıncı adı. Değişirse Windows'ta PlayerPrefs / persistentDataPath klasörü de değişir.</summary>
        public const string CompanyName = "Bn Studio";
        public const string ProductName = "Taverna Tycoon";
        /// <summary>Android paket adı, iOS paket kimliği ve Windows kimliği (mağazaya çıktıktan sonra DEĞİŞTİRİLEMEZ).</summary>
        public const string ApplicationId = "com.bnstudio.tavernatycoon";
        /// <summary>Oyuncuya görünen sürüm (Application.version).</summary>
        public const string Version = "1.0.0";
        /// <summary>Android sürüm kodu: her mağaza yüklemesinde bir artırılmalı.</summary>
        public const int AndroidVersionCode = 1;
        /// <summary>iOS derleme numarası: her App Store yüklemesinde bir artırılmalı.</summary>
        public const string IosBuildNumber = "1";

        /// <summary>Şirket, ürün adı, uygulama kimliği (Android / iOS / Windows) ve sürümü yazar.</summary>
        public static void ApplyIdentity()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, ApplicationId);
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, ApplicationId);
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.Android.bundleVersionCode = AndroidVersionCode;
            PlayerSettings.iOS.buildNumber = IosBuildNumber;
        }

        [MenuItem("Taverna/Derle/Windows Test Sürümü")]
        public static void WindowsDev()
        {
            ApplyIdentity();
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1266;
            PlayerSettings.defaultScreenHeight = 585;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;   // otomatik test pencere odakta değilken de sürsün
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { TavernaSceneBuilder.MenuScene, TavernaSceneBuilder.GameScene },
                locationPathName = Path.Combine(BuildDir, WindowsFolder(), "TavernaTycoon.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            };
            Report(BuildPipeline.BuildPlayer(opts));
        }

        /// <summary>
        /// v2.5: Windows test sürümünün klasörü (Unity/Builds altında). Varsayılan "Windows"; komut satırında
        /// "-taverna-win-dir Ad" verilirse o klasöre derlenir (eski sürüm açıkken dosyaları kilitliyse yan klasöre derlemek için).
        /// </summary>
        static string WindowsFolder()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-taverna-win-dir");
            return i >= 0 && i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]) ? args[i + 1] : "Windows";
        }

        [MenuItem("Taverna/Derle/Android APK (test)")]
        public static void AndroidDev()
        {
            ApplyIdentity();
            EditorUserBuildSettings.buildAppBundle = false;
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { TavernaSceneBuilder.MenuScene, TavernaSceneBuilder.GameScene },
                locationPathName = Path.Combine(BuildDir, "Android", "TavernaTycoon.apk"),
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            };
            Report(BuildPipeline.BuildPlayer(opts));
        }

        /// <summary>
        /// iOS: Windows'ta sadece Xcode projesi üretilir; imzalama ve iPhone'a kurma Mac'te Xcode ile yapılır
        /// (rehber: docs/ios-kurulum-rehberi.md). Oyuncu sürümü (geliştirme değil): Hazine test satın alması kapalıdır.
        /// Komut satırı: -buildTarget iOS -executeMethod Taverna.EditorTools.TavernaBuild.IosXcode
        /// </summary>
        [MenuItem("Taverna/Derle/iOS Xcode projesi")]
        public static void IosXcode()
        {
            ApplyIdentity();
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;   // Xcode'da Apple ID ekibi seçilince kendisi imzalar
            PlayerSettings.iOS.appleDeveloperTeamID = "";
            PlayerSettings.iOS.requiresFullScreen = true;
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { TavernaSceneBuilder.MenuScene, TavernaSceneBuilder.GameScene },
                locationPathName = Path.Combine(BuildDir, "iOS", "TavernaTycoon_Xcode"),
                target = BuildTarget.iOS,
                options = BuildOptions.None,
            };
            Report(BuildPipeline.BuildPlayer(opts));
        }

        // ------------------------------------------------------------------ v1.6 Android yayın (Google Play AAB)
        /// <summary>İmzalama bilgilerinin okunduğu ortam değişkenleri (değerler hiçbir dosyaya yazılmaz).</summary>
        public const string EnvKeystore = "TAVERNA_KEYSTORE", EnvKeystorePass = "TAVERNA_KEYSTORE_PASS",
                            EnvKeyAlias = "TAVERNA_KEY_ALIAS", EnvKeyPass = "TAVERNA_KEY_PASS";

        /// <summary>
        /// Google Play yayın derlemesi: geliştirme derlemesi DEĞİL (test reklamı / test mağazası kapalı), IL2CPP, yalnızca ARM64,
        /// AAB çıktısı (Unity/Builds/Android/TavernaTycoon.aab). İmzalama anahtarı sahibinindir: yol ve parolalar
        /// TAVERNA_KEYSTORE, TAVERNA_KEYSTORE_PASS, TAVERNA_KEY_ALIAS, TAVERNA_KEY_PASS ortam değişkenlerinden okunur;
        /// biri eksikse ya da anahtar deposu yoksa derleme açık bir hatayla durur. İmza bilgileri derlemeden sonra
        /// proje ayarlarından geri silinir (ProjectSettings.asset'e yol / parola kalmaz).
        /// Komut satırı: -buildTarget Android -executeMethod Taverna.EditorTools.TavernaBuild.AndroidRelease
        /// </summary>
        [MenuItem("Taverna/Derle/Android AAB (yayın, imzalı)")]
        public static void AndroidRelease()
        {
            string keystore = Environment.GetEnvironmentVariable(EnvKeystore);
            string storePass = Environment.GetEnvironmentVariable(EnvKeystorePass);
            string alias = Environment.GetEnvironmentVariable(EnvKeyAlias);
            string keyPass = Environment.GetEnvironmentVariable(EnvKeyPass);
            var missing = new System.Collections.Generic.List<string>();
            if (string.IsNullOrEmpty(keystore)) missing.Add(EnvKeystore);
            if (string.IsNullOrEmpty(storePass)) missing.Add(EnvKeystorePass);
            if (string.IsNullOrEmpty(alias)) missing.Add(EnvKeyAlias);
            if (string.IsNullOrEmpty(keyPass)) missing.Add(EnvKeyPass);
            if (missing.Count > 0) { Fail($"Android yayın derlemesi durdu: imzalama ortam değişkenleri eksik: {string.Join(", ", missing)}. Ayrıntı: docs/release-checklist.md"); return; }
            if (!File.Exists(keystore)) { Fail($"Android yayın derlemesi durdu: anahtar deposu bulunamadı ({EnvKeystore} = {keystore})."); return; }

            ApplyIdentity();
            var android = UnityEditor.Build.NamedBuildTarget.Android;
            var prevBackend = PlayerSettings.GetScriptingBackend(android);
            var prevArch = PlayerSettings.Android.targetArchitectures;
            bool prevAab = EditorUserBuildSettings.buildAppBundle;
            try
            {
                PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                EditorUserBuildSettings.buildAppBundle = true;
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystore;
                PlayerSettings.Android.keystorePass = storePass;
                PlayerSettings.Android.keyaliasName = alias;
                PlayerSettings.Android.keyaliasPass = keyPass;
                var opts = new BuildPlayerOptions
                {
                    scenes = new[] { TavernaSceneBuilder.MenuScene, TavernaSceneBuilder.GameScene },
                    locationPathName = Path.Combine(BuildDir, "Android", "TavernaTycoon.aab"),
                    target = BuildTarget.Android,
                    options = BuildOptions.None,   // geliştirme derlemesi değil: Debug.isDebugBuild = false
                };
                Report(BuildPipeline.BuildPlayer(opts));
            }
            finally
            {
                // İmza bilgisi proje ayarlarında kalmasın; test APK derlemesi eski ayarlarla sürsün
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasPass = "";
                PlayerSettings.Android.keystoreName = "";
                PlayerSettings.Android.keyaliasName = "";
                PlayerSettings.Android.useCustomKeystore = false;
                EditorUserBuildSettings.buildAppBundle = prevAab;
                PlayerSettings.Android.targetArchitectures = prevArch;
                PlayerSettings.SetScriptingBackend(android, prevBackend);
            }
        }

        static void Fail(string message)
        {
            Debug.LogError("[Taverna] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }

        static void Report(BuildReport r)
        {
            Debug.Log($"[Taverna] Derleme: {r.summary.result} · {r.summary.totalSize / (1024 * 1024f):0.0} MB · {r.summary.totalTime} · {r.summary.outputPath}");
            if (Application.isBatchMode && r.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
