#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Taverna
{
    /// <summary>
    /// "-autotest -autotest-hidden" (yalnızca Windows geliştirme derlemesi): otomatik test oyunu sahibin ekranındaki
    /// tam ekran oyunun önüne geçmesin, odağı ve fareyi çalmasın. Pencere hiç etkinleşmeden ekran dışına ve en alta
    /// taşınır (ilk saniyelerde yeniden uygulanır), arka planda çalışır; gerçek fare ve klavye kapatılır (bot kendi
    /// girdisini üretir). Ekran görüntüsü pencere ekran dışındayken de çizilir.
    /// </summary>
    public class AutoTestWindow : MonoBehaviour
    {
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
        delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);

        const int SwShowNoActivate = 4;
        const uint SwpNoSize = 0x0001, SwpNoActivate = 0x0010;
        static readonly IntPtr HwndBottom = new(1);
        const int OffscreenX = -4000, OffscreenY = -4000;
        /// <summary>Pencere ilk bu kadar gerçek saniye boyunca tekrar tekrar ekran dışına itilir (Unity açılışta yeniden konumlayabilir).</summary>
        const float EnforceSeconds = 12f, EnforceEvery = 0.5f;

        float _t, _next;

        /// <summary>Bayrak varsa sahne yüklenmeden önce kurulur.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (!AutoTest.Enabled || Array.IndexOf(Environment.GetCommandLineArgs(), "-autotest-hidden") < 0) return;
            Application.runInBackground = true;
            var go = new GameObject("AutoTestWindow");
            DontDestroyOnLoad(go);
            go.AddComponent<AutoTestWindow>();
            Hide();
            DisableRealInput();
            Debug.Log("[Playtest] -autotest-hidden: pencere ekran dışında, gerçek fare / klavye kapalı");
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (_t > EnforceSeconds || _t < _next) return;
            _next = _t + EnforceEvery;
            Hide();
            DisableRealInput();
        }

        /// <summary>Bu sürecin görünür üst pencerelerini etkinleştirmeden ekran dışına ve z sırasının en altına taşır.</summary>
        static void Hide()
        {
            _self = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            EnumWindows(HideOne, IntPtr.Zero);
        }

        static uint _self;

        /// <summary>EnumWindows geri çağrısı (IL2CPP'de de çalışsın diye statik).</summary>
        [AOT.MonoPInvokeCallback(typeof(EnumProc))]
        static bool HideOne(IntPtr h, IntPtr _)
        {
            GetWindowThreadProcessId(h, out uint pid);
            if (pid != _self || !IsWindowVisible(h)) return true;
            ShowWindow(h, SwShowNoActivate);
            SetWindowPos(h, HwndBottom, OffscreenX, OffscreenY, 0, 0, SwpNoSize | SwpNoActivate);
            return true;
        }

        /// <summary>Kullanıcının tıklamaları / tuşları oyuna düşmesin.</summary>
        static void DisableRealInput()
        {
            if (Mouse.current != null && Mouse.current.enabled) InputSystem.DisableDevice(Mouse.current);
            if (Keyboard.current != null && Keyboard.current.enabled) InputSystem.DisableDevice(Keyboard.current);
        }
    }
}
#endif
