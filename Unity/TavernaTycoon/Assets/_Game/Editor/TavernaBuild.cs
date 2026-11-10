using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Taverna.EditorTools
{
    /// <summary>
    /// Derleme komutları. Windows test sürümü telefon oranında (540x960) pencerede açılır.
    /// Komut satırı: -executeMethod Taverna.EditorTools.TavernaBuild.WindowsDev
    /// </summary>
    public static class TavernaBuild
    {
        static string BuildDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds"));

        [MenuItem("Taverna/Derle/Windows Test Sürümü")]
        public static void WindowsDev()
        {
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1266;
            PlayerSettings.defaultScreenHeight = 585;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;   // otomatik test pencere odakta değilken de sürsün
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { TavernaSceneBuilder.MenuScene, TavernaSceneBuilder.GameScene },
                locationPathName = Path.Combine(BuildDir, "Windows", "TavernaTycoon.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            };
            Report(BuildPipeline.BuildPlayer(opts));
        }

        [MenuItem("Taverna/Derle/Android APK (test)")]
        public static void AndroidDev()
        {
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
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.iOS.buildNumber = "1";
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

        static void Report(BuildReport r)
        {
            Debug.Log($"[Taverna] Derleme: {r.summary.result} · {r.summary.totalSize / (1024 * 1024f):0.0} MB · {r.summary.totalTime} · {r.summary.outputPath}");
            if (Application.isBatchMode && r.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
