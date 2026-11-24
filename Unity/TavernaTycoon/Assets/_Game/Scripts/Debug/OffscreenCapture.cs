#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v2.5 penceresiz test ("-autotest -autotest-offscreen", oyuncu "-batchmode" ile ve "-nographics" olmadan başlatılır): hiçbir
    /// pencere açılmaz, sahibin ekranına / faresine dokunulmaz. Sahne kameraları bir RenderTexture'a, UI Toolkit panelleri ayrı bir
    /// RenderTexture'a çizilir; ekran görüntüsü ve video karesi ikisinin birleşimidir (arayüz kameranın üstüne alfa ile bindirilir).
    /// Motor kamerayı kendisi çizmiyorsa (batchmode) kamera her karede elle çizilir (<see cref="LateUpdate"/>); arayüz motorun panel
    /// çizimiyle güncellenir.
    /// Boyut: "-autotest-offscreen-size 2532x1170" (varsayılan 1266x586). Ses: <see cref="VideoRecorder"/> AudioRenderer ile alır;
    /// kayıt dışında ana ses düzeyi 0 (hoparlöre bir şey gitmez).
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public class OffscreenCapture : MonoBehaviour
    {
        public static OffscreenCapture I { get; private set; }
        /// <summary>Penceresiz mod açık mı.</summary>
        public static bool Enabled { get; private set; }

        public int Width { get; private set; } = 1266;
        public int Height { get; private set; } = 586;
        RenderTexture _camRt, _uiRt;
        readonly List<Camera> _cams = new();
        readonly List<UIDocument> _docs = new();
        Texture2D _readCam, _readUi;
        /// <summary>Teşhis: elle çizilen kare sayısı, kamera sayısı, panel sayısı.</summary>
        public int Rendered { get; private set; }
        public string Info => $"penceresiz {Width}×{Height} · kamera {_cams.Count} · panel {_docs.Count} · elle çizilen {Rendered} · motor çiziyor={AutoRender} · ekran {Screen.width}×{Screen.height} · grafik {SystemInfo.graphicsDeviceType}";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (!AutoTest.Enabled) return;
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-autotest-offscreen") < 0) return;
            Enabled = true;
            HudController.RenderTargetMode = true;
            var go = new GameObject("OffscreenCapture");
            DontDestroyOnLoad(go);
            I = go.AddComponent<OffscreenCapture>();
            int i = Array.IndexOf(args, "-autotest-offscreen-size");
            if (i >= 0 && i + 1 < args.Length)
            {
                var p = args[i + 1].ToLowerInvariant().Split('x');
                if (p.Length == 2 && int.TryParse(p[0], out int w) && int.TryParse(p[1], out int h) && w > 64 && h > 64) { I.Width = w; I.Height = h; }
            }
            I.Create();
            int t = Array.IndexOf(args, "-autotest-timeout");
            if (t >= 0 && t + 1 < args.Length && float.TryParse(args[t + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float sec) && sec > 5f) I._deadline = sec;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Sfx.MasterVolume = 0f;          // kayıt dışında sessiz (VideoRecorder kayıtta 1 yapar, çıkışı AudioRenderer susturur)
            AudioListener.volume = 0f;
            AutoTest.ShotHook = path => I.SavePng(path);
            SceneManager.sceneLoaded += (_, _) => I.Hook();
            Debug.Log($"[Playtest] -autotest-offscreen: pencere yok, çizim RenderTexture'a ({I.Width}×{I.Height}) · batchmode={Application.isBatchMode} · grafik {SystemInfo.graphicsDeviceType}");
        }

        void Create()
        {
            _camRt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { name = "Offscreen_Camera" };
            _camRt.Create();
            _uiRt = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32) { name = "Offscreen_UI" };
            _uiRt.Create();
            _readCam = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            _readUi = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        }

        /// <summary>Sahnedeki kameraları ve arayüz panellerini dokulara yönlendirir (sahne yüklenince ve her karede denetlenir).</summary>
        void Hook()
        {
            _cams.Clear();
            foreach (var c in FindObjectsByType<Camera>())
            {
                if (!c || c.targetTexture != null && c.targetTexture != _camRt) continue;   // başka dokuya çizen kameralar (ör. ikon) kalır
                c.targetTexture = _camRt;
                _cams.Add(c);
            }
            _cams.Sort((a, b) => a.depth.CompareTo(b.depth));
            _docs.Clear();
            foreach (var d in FindObjectsByType<UIDocument>())
            {
                if (!d || !d.panelSettings) continue;
                var ps = d.panelSettings;
                if (ps.targetTexture != _uiRt)
                {
                    ps.targetTexture = _uiRt;
                    ps.clearColor = true;
                    ps.colorClearValue = new Color(0f, 0f, 0f, 0f);
                }
                _docs.Add(d);
            }
        }

        void LateUpdate()
        {
            bool stale = _cams.Count == 0 || _cams.Exists(c => !c);
            if (!stale) foreach (var d in _docs) if (!d || d.panelSettings && d.panelSettings.targetTexture != _uiRt) { stale = true; break; }
            if (!stale && Camera.main && Camera.main.targetTexture != _camRt) stale = true;
            if (stale || Time.frameCount % 30 == 0) Hook();
            // batchmode'da motor kamerayı kendisi çizmiyorsa (son karede çizim olayı yoksa) elle çizilir
            if (_autoFrames == 0)
            {
                _manual = true;
                foreach (var c in _cams) if (c && c.isActiveAndEnabled) c.Render();
                _manual = false;
                Rendered++;
            }
            _autoFrames = 0;
        }

        /// <summary>Güvenlik: süreç en geç bu kadar gerçek saniye sonra kendini kapatır ("-autotest-timeout", varsayılan 600).</summary>
        float _deadline = 600f;
        bool _quitting;

        void Update()
        {
            float now = Time.realtimeSinceStartup;
            if (!_quitting && now > _deadline) { _quitting = true; Debug.Log($"[Playtest] -autotest-timeout: {_deadline:0} sn doldu, çıkılıyor"); Application.Quit(); }
            if (now > _deadline + 15f) System.Diagnostics.Process.GetCurrentProcess().Kill();   // Quit takılırsa
        }

        int _autoFrames;
        bool _manual;
        /// <summary>Teşhis: motor kamerayı kendisi çiziyor mu.</summary>
        public bool AutoRender { get; private set; }

        void OnEnable() => UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += OnCamRendered;
        void OnDisable() => UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= OnCamRendered;
        void OnCamRendered(UnityEngine.Rendering.ScriptableRenderContext _, Camera c)
        {
            if (_manual || c.targetTexture != _camRt) return;
            _autoFrames++;
            AutoRender = true;
        }

        /// <summary>Kamera ve arayüz dokusunun birleşimi (RGBA32, alt satır önce: Unity dokusu düzeni).</summary>
        public byte[] Compose()
        {
            Read(_camRt, _readCam);
            Read(_uiRt, _readUi);
            var a = _readCam.GetRawTextureData<byte>();
            var b = _readUi.GetRawTextureData<byte>();
            var o = new byte[a.Length];
            ComposeInto(a, b, o);
            return o;
        }

        static void Read(RenderTexture rt, Texture2D t)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            RenderTexture.active = prev;
        }

        /// <summary>
        /// Arayüz dokusu saydam siyaha (0,0,0,0) çizilir: renk zaten alfa ile çarpılmış (önceden çarpılmış) kabul edilir:
        /// sonuç = arayüz + kamera × (1 − arayüz alfası).
        /// </summary>
        static void ComposeInto(NativeArray<byte> cam, NativeArray<byte> ui, byte[] o)
        {
            int n = Math.Min(cam.Length, ui.Length);
            for (int i = 0; i < n; i += 4)
            {
                int ia = 255 - ui[i + 3];
                o[i] = (byte)Math.Min(255, ui[i] + cam[i] * ia / 255);
                o[i + 1] = (byte)Math.Min(255, ui[i + 1] + cam[i + 1] * ia / 255);
                o[i + 2] = (byte)Math.Min(255, ui[i + 2] + cam[i + 2] * ia / 255);
                o[i + 3] = 255;
            }
        }

        /// <summary>Birleşik görüntüyü PNG olarak yazar (AutoTest.Shot bu yolu kullanır).</summary>
        public void SavePng(string path)
        {
            var px = Compose();
            var png = ImageConversion.EncodeArrayToPNG(px, GraphicsFormat.R8G8B8A8_UNorm, (uint)Width, (uint)Height);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, png);
            // "-autotest-offscreen-uilayer": arayüz dokusunu ham hâliyle (RGBA) ayrıca yazar (birleştirme hatalarını incelemek için)
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-autotest-offscreen-uilayer") >= 0)
                File.WriteAllBytes(Path.ChangeExtension(path, null) + "_uilayer.png",
                    ImageConversion.EncodeArrayToPNG(_readUi.GetRawTextureData<byte>().ToArray(), GraphicsFormat.R8G8B8A8_UNorm, (uint)Width, (uint)Height));
        }

        int _pending;
        /// <summary>Bekleyen (iş parçacığında kodlanan) kare sayısı.</summary>
        public int Pending => Volatile.Read(ref _pending);

        /// <summary>Birleşik görüntüyü iş parçacığında JPG olarak yazar (video karesi).</summary>
        public void SaveJpgAsync(string path, int quality)
        {
            var px = Compose();
            int w = Width, h = Height;
            Interlocked.Increment(ref _pending);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { File.WriteAllBytes(path, ImageConversion.EncodeArrayToJPG(px, GraphicsFormat.R8G8B8A8_UNorm, (uint)w, (uint)h, 0, quality)); }
                finally { Interlocked.Decrement(ref _pending); }
            });
        }
    }
}
#endif
