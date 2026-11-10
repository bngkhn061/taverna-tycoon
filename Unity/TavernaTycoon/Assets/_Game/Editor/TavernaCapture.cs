using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Taverna.EditorTools
{
    /// <summary>
    /// Sahnenin kamerasından PNG önizleme alır (görsel doğrulama için, oyun moduna girmeden).
    /// Komut satırı: -executeMethod Taverna.EditorTools.TavernaCapture.CaptureGame
    /// Çıktı: Unity/captures/*.png
    /// </summary>
    public static class TavernaCapture
    {
        static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../captures"));

        [MenuItem("Taverna/Önizleme Görüntüsü Al")]
        public static void CaptureGame()
        {
            EditorSceneManager.OpenScene(TavernaSceneBuilder.GameScene, OpenSceneMode.Single);
            var follow = Object.FindAnyObjectByType<CameraFollow>();
            var cam = follow.GetComponent<Camera>();
            follow.SnapNow();
            Shot(cam, "game_portrait.png", 1080, 1920);

            // Bütün odayı gören geniş açı
            var savedPos = cam.transform.position;
            var savedRot = cam.transform.rotation;
            var savedFov = cam.fieldOfView;
            cam.transform.SetPositionAndRotation(new Vector3(0f, 15.5f, -13.2f), Quaternion.Euler(50f, 0f, 0f));
            cam.fieldOfView = 52f;
            Shot(cam, "game_overview.png", 1920, 1080);

            // Tüm kilitli nesneleri açıp tekrar (tam aşama 1 görünümü)
            foreach (var pz in Object.FindObjectsByType<PayZone>(FindObjectsInactive.Include))
            {
                foreach (var t in pz.unlockTargets) if (t) t.SetActive(true);
                foreach (var h in pz.hideOnUnlock) if (h) h.SetActive(false);
                pz.gameObject.SetActive(false);
            }
            Shot(cam, "game_overview_unlocked.png", 1920, 1080);
            cam.transform.SetPositionAndRotation(savedPos, savedRot);
            cam.fieldOfView = savedFov;
            EditorSceneManager.OpenScene(TavernaSceneBuilder.GameScene, OpenSceneMode.Single); // değişiklikleri at
            Debug.Log("[Taverna] Önizlemeler: " + OutDir);
        }

        static void Shot(Camera cam, string file, int w, int h)
        {
            Directory.CreateDirectory(OutDir);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, file), tex.EncodeToPNG());
            cam.targetTexture = prev;
            RenderTexture.active = null;
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
