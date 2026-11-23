#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Taverna
{
    /// <summary>
    /// v2.5 test aracı (yalnızca geliştirme derlemesi, "-autotest-v25 rec-*"): oyunu gerçek hızda oynatırken her ~1/30 sn'de bir
    /// ekranı (arayüz dahil) JPG kare olarak, aynı anda dinleyicinin karışımını (<see cref="AudioCapture"/>, hoparlöre gitmez)
    /// WAV olarak yazar. Görüntü ve ses aynı gerçek saatle başlar; her karenin gerçek zamanı <c>frames.txt</c>'ye yazılır,
    /// birleştirme (ffmpeg) kareleri bu zamanlarla yerleştirir: ses ile görüntü kaymaz.
    /// Kare okuma GPU'dan eşzamansız (AsyncGPUReadback), JPG kodlama iş parçacığında: oyun yavaşlamaz.
    /// </summary>
    /// <remarks>
    /// v2.5 penceresiz kip (<see cref="OffscreenCapture"/>): oyun zamanı kareye bağlanır (Time.captureFramerate = 30), her karede
    /// birleşik görüntü ve AudioRenderer'ın o kareye düşen sesi alınır; gerçek zaman önemsizdir, görüntü ve ses örnek örnek eşleşir.
    /// AudioRenderer çalışırken Unity hoparlöre sessizlik verir.
    /// </remarks>
    [DefaultExecutionOrder(32001)]
    public class VideoRecorder : MonoBehaviour
    {
        /// <summary>Hedef kare aralığı (sn).</summary>
        public const float FrameInterval = 1f / 30f;
        /// <summary>JPG kalitesi (H.264'e girmeden önceki ara kare).</summary>
        const int JpgQuality = 92;

        string _dir;
        bool _on;
        double _t0, _next;
        int _frame, _pending, _written;
        RenderTexture _rt;
        readonly StringBuilder _times = new();
        AudioCapture _cap;

        /// <summary>Kayıt sürüyor mu.</summary>
        public bool Recording => _on;
        /// <summary>Yazılan ve bekleyen kare sayısı (test günlüğü).</summary>
        public int Frames => _frame;
        public int Pending => Volatile.Read(ref _pending);
        /// <summary>Kaydın başından beri geçen gerçek süre (sn).</summary>
        public double Elapsed => !_on ? 0 : _offline ? _frame * (double)FrameInterval : Time.realtimeSinceStartupAsDouble - _t0;

        // ---- penceresiz kip
        bool _offline, _audioOn;
        int _channels = 2;
        readonly System.Collections.Generic.List<float> _pcm = new();
        string _audioNote = "";

        /// <summary>Kaydı başlatır: <paramref name="dir"/> klasörüne kare ve ses (en çok <paramref name="maxSeconds"/> sn).</summary>
        public static VideoRecorder Begin(string dir, float maxSeconds, AudioCapture cap)
        {
            Directory.CreateDirectory(dir);
            var go = new GameObject("VideoRecorder");
            DontDestroyOnLoad(go);
            var r = go.AddComponent<VideoRecorder>();
            r._dir = dir;
            if (OffscreenCapture.Enabled)
            {
                r._offline = true;
                r._on = true;
                Time.captureFramerate = 30;
                Application.targetFrameRate = 30;   // gerçek saat de oyun saatine yakın kalsın (gerçek zamanlı arayüz animasyonları)
                r._u0 = Time.unscaledTime; r._t0 = Time.realtimeSinceStartupAsDouble;
                Sfx.MasterVolume = 1f;
                AudioListener.volume = 1f;
                r._channels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
                try { r._audioOn = AudioRenderer.Start(); r._audioNote = r._audioOn ? "AudioRenderer" : "AudioRenderer.Start false"; }
                catch (System.Exception e) { r._audioNote = "AudioRenderer hata: " + e.Message; }
                return r;
            }
            r._cap = cap;
            r._rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            r._rt.Create();
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            r._t0 = Time.realtimeSinceStartupAsDouble;
            r._next = 0;
            r._on = true;
            if (cap) cap.Begin(maxSeconds);
            r.StartCoroutine(r.Loop());
            return r;
        }

        void LateUpdate()
        {
            if (!_offline || !_on || OffscreenCapture.I == null) return;
            int idx = _frame++;
            _times.Append(idx.ToString("00000")).Append(' ').AppendLine((idx * (double)FrameInterval).ToString("0.00000", CultureInfo.InvariantCulture));
            OffscreenCapture.I.SaveJpgAsync(Path.Combine(_dir, $"f{idx:00000}.jpg"), JpgQuality);
            if (!_audioOn) return;
            int n = AudioRenderer.GetSampleCountForCaptureFrame();
            if (n <= 0) { _zeroAudioFrames++; return; }
            var buf = new Unity.Collections.NativeArray<float>(n * _channels, Unity.Collections.Allocator.Temp);
            AudioRenderer.Render(buf);
            for (int i = 0; i < buf.Length; i++) _pcm.Add(buf[i]);
            buf.Dispose();
        }

        int _zeroAudioFrames;
        float _u0;

        IEnumerator Loop()
        {
            var eof = new WaitForEndOfFrame();
            while (_on)
            {
                yield return eof;
                if (!_on) break;
                double t = Time.realtimeSinceStartupAsDouble - _t0;
                if (t + 0.004 < _next) continue;
                _next = Mathf.Max((float)_next + FrameInterval, (float)t);   // geride kalırsa yetişmeye çalışmaz, sıradaki aralığa geçer
                int idx = _frame++;
                _times.Append(idx.ToString("00000")).Append(' ').AppendLine(t.ToString("0.00000", CultureInfo.InvariantCulture));
                ScreenCapture.CaptureScreenshotIntoRenderTexture(_rt);
                int w = _rt.width, h = _rt.height;
                Interlocked.Increment(ref _pending);
                AsyncGPUReadback.Request(_rt, 0, TextureFormat.RGBA32, req =>
                {
                    if (req.hasError) { Interlocked.Decrement(ref _pending); return; }
                    var bytes = req.GetData<byte>().ToArray();
                    string path = Path.Combine(_dir, $"f{idx:00000}.jpg");
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            var jpg = ImageConversion.EncodeArrayToJPG(bytes, GraphicsFormat.R8G8B8A8_UNorm, (uint)w, (uint)h, 0, JpgQuality);
                            File.WriteAllBytes(path, jpg);
                            Interlocked.Increment(ref _written);
                        }
                        finally { Interlocked.Decrement(ref _pending); }
                    });
                });
            }
        }

        /// <summary>Kaydı durdurur; bütün kareler yazılınca ses ve zaman dosyasını yazar, <paramref name="done"/>'ı çağırır.</summary>
        public void End(System.Action<string> done)
        {
            if (!_on) return;
            if (_offline) { _on = false; StartCoroutine(FinishOffline(done)); return; }
            _on = false;
            double dur = Time.realtimeSinceStartupAsDouble - _t0;
            StartCoroutine(Finish(dur, done));
        }

        IEnumerator FinishOffline(System.Action<string> done)
        {
            double dur = _frame * (double)FrameInterval;
            if (_audioOn) AudioRenderer.Stop();
            Time.captureFramerate = 0;
            Application.targetFrameRate = 60;
            string clocks = $"ölçeksiz saat {Time.unscaledTime - _u0:0.00} sn · gerçek {Time.realtimeSinceStartupAsDouble - _t0:0.00} sn";
            Sfx.MasterVolume = 0f;
            AudioListener.volume = 0f;
            float wait = 0f;
            while (OffscreenCapture.I.Pending > 0 && wait < 60f) { wait += Time.unscaledDeltaTime; yield return null; }
            _times.Append("end ").AppendLine(dur.ToString("0.00000", CultureInfo.InvariantCulture));
            File.WriteAllText(Path.Combine(_dir, "frames.txt"), _times.ToString());
            int rate = AudioSettings.outputSampleRate;
            float peak = 0f; double sum = 0;
            foreach (var v in _pcm) { peak = Mathf.Max(peak, Mathf.Abs(v)); sum += v * v; }
            float rms = _pcm.Count > 0 ? Mathf.Sqrt((float)(sum / _pcm.Count)) : 0f;
            float aSec = _pcm.Count / (float)(rate * _channels);
            string audio = "-";
            if (_pcm.Count > 0) { audio = Path.Combine(_dir, "audio.wav"); WriteWav(audio, _pcm, _channels, rate); }
            done?.Invoke($"penceresiz · kare {_frame} · görüntü {dur:0.00} sn · ses {_audioNote}: {aSec:0.00} sn, {rate} Hz × {_channels}, tepe {peak:0.0000} ({20f * Mathf.Log10(peak + 1e-9f):0.0} dBFS), RMS {rms:0.00000} · sessiz kare {_zeroAudioFrames} · fark {aSec - dur:+0.000;-0.000} sn · {clocks}");
            Destroy(gameObject);
        }

        static void WriteWav(string path, System.Collections.Generic.List<float> s, int ch, int rate)
        {
            using var f = new BinaryWriter(File.Create(path));
            int bytes = s.Count * 4;
            f.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            f.Write(36 + bytes);
            f.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E', (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            f.Write(16); f.Write((short)3); f.Write((short)ch); f.Write(rate); f.Write(rate * ch * 4); f.Write((short)(ch * 4)); f.Write((short)32);
            f.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            f.Write(bytes);
            foreach (var v in s) f.Write(v);
        }

        IEnumerator Finish(double dur, System.Action<string> done)
        {
            float wait = 0f;
            while (Volatile.Read(ref _pending) > 0 && wait < 30f) { wait += Time.unscaledDeltaTime; yield return null; }
            _times.Append("end ").AppendLine(dur.ToString("0.00000", CultureInfo.InvariantCulture));
            File.WriteAllText(Path.Combine(_dir, "frames.txt"), _times.ToString());
            string audio = "-";
            float aSec = 0f;
            if (_cap) { audio = _cap.End(Path.Combine(_dir, "audio.wav")); aSec = _cap.Seconds; }
            if (_rt) { _rt.Release(); Destroy(_rt); }
            done?.Invoke($"kare {_frame} (yazılan {_written}, bekleyen {Volatile.Read(ref _pending)}) · görüntü {dur:0.00} sn · ses {aSec:0.00} sn ({audio}) · fark {aSec - dur:+0.000;-0.000} sn · ortalama {(_frame > 0 ? _frame / dur : 0):0.0} kare/sn");
            Destroy(gameObject);
        }
    }
}
#endif
