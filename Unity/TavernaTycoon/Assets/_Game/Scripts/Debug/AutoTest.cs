using System;
using System.IO;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// "-autotest" komut satırı parametresiyle açılan geliştirme sürümlerinde otomatik test modu.
    /// Normal oyuncu sürümünde hiçbir etkisi yoktur.
    /// </summary>
    public static class AutoTest
    {
        public static bool Enabled
        {
            get
            {
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
                return Array.IndexOf(Environment.GetCommandLineArgs(), "-autotest") >= 0;
#else
                return false;
#endif
            }
        }

        public static string CaptureDir
        {
            get
            {
                // Build klasörünün yanında: Unity/captures
                var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../captures"));
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static void Shot(string file) => ScreenCapture.CaptureScreenshot(Path.Combine(CaptureDir, file), 2);
    }
}
