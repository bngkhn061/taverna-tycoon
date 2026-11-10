using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    public enum SfxId { Coin, Pop, Serve, Buy, LevelUp, Rush, Tick, Claim, Angry }

    /// <summary>
    /// Kodla sentezlenen ses efektleri ve döngü müziği (dış ses dosyası gerekmez).
    /// Gerçek ses tasarımı gelene kadar oyunun "his"ini taşır. Ses ayarları GameSettings'ten.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const int Rate = 44100;
        static Sfx _i;
        readonly Dictionary<SfxId, AudioClip> _clips = new();
        AudioSource _fx, _music;
        float _lastCoin;

        public static void Ensure()
        {
            if (_i) return;
            var go = new GameObject("Sfx");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<Sfx>();
        }

        void Awake()
        {
            _fx = gameObject.AddComponent<AudioSource>();
            _fx.playOnAwake = false;
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.clip = MakeMusic();
            _music.volume = GameSettings.Music * 0.35f;
            _music.Play();

            _clips[SfxId.Coin] = Tone(new[] { 1318f, 1975f }, 0.14f, 0.35f, decay: 18f);
            _clips[SfxId.Pop] = Sweep(380f, 900f, 0.09f, 0.4f);
            _clips[SfxId.Serve] = Tone(new[] { 660f, 990f }, 0.12f, 0.3f, decay: 14f, tri: true);
            _clips[SfxId.Buy] = Arp(new[] { 523f, 659f, 784f, 1046f }, 0.07f, 0.35f);
            _clips[SfxId.LevelUp] = Arp(new[] { 392f, 523f, 659f, 784f, 1046f, 1318f }, 0.09f, 0.4f, tail: 0.4f);
            _clips[SfxId.Rush] = Arp(new[] { 392f, 523f, 392f, 523f, 784f }, 0.11f, 0.35f, square: true);
            _clips[SfxId.Tick] = Tone(new[] { 1046f }, 0.035f, 0.2f, decay: 60f);
            _clips[SfxId.Claim] = Arp(new[] { 784f, 1046f, 1568f }, 0.06f, 0.35f, tail: 0.2f);
            _clips[SfxId.Angry] = Sweep(330f, 180f, 0.25f, 0.3f);
        }

        void Update()
        {
            _music.volume = GameSettings.Music * 0.35f;
            AudioListener.volume = 1f;
            _fx.volume = GameSettings.Sfx;
        }

        public static void Play(SfxId id, float pitch = 1f)
        {
            Ensure();
            if (id == SfxId.Coin)
            {
                // Arka arkaya paralar yükselen perdeyle çalsın (tatmin hissi)
                float since = Time.unscaledTime - _i._lastCoin;
                _i._lastCoin = Time.unscaledTime;
                pitch *= since < 0.4f ? Random.Range(1.05f, 1.2f) : 1f;
            }
            _i._fx.pitch = pitch;
            _i._fx.PlayOneShot(_i._clips[id]);
        }

        // ------------------------------------------------------------------ sentez
        static AudioClip Make(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        static float Osc(float phase, bool tri, bool square)
        {
            if (square) return Mathf.Sign(Mathf.Sin(phase)) * 0.5f;
            if (tri) return Mathf.PingPong(phase / Mathf.PI, 2f) - 1f;
            return Mathf.Sin(phase);
        }

        static AudioClip Tone(float[] freqs, float dur, float vol, float decay = 10f, bool tri = false)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, env = Mathf.Exp(-decay * t) * Mathf.Min(1f, t * 400f);
                float s = 0;
                for (int k = 0; k < freqs.Length; k++) s += Osc(2 * Mathf.PI * freqs[k] * t, tri, false) / (k + 1);
                d[i] = s * env * vol;
            }
            return Make("tone", d);
        }

        static AudioClip Sweep(float f0, float f1, float dur, float vol)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            float ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                ph += 2 * Mathf.PI * Mathf.Lerp(f0, f1, t) / Rate;
                d[i] = Mathf.Sin(ph) * (1f - t) * vol;
            }
            return Make("sweep", d);
        }

        static AudioClip Arp(float[] notes, float step, float vol, float tail = 0.1f, bool square = false)
        {
            int n = (int)((notes.Length * step + tail) * Rate);
            var d = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                int start = (int)(k * step * Rate);
                for (int i = start; i < n; i++)
                {
                    float t = (i - start) / (float)Rate;
                    d[i] += Osc(2 * Mathf.PI * notes[k] * t, !square, square) * Mathf.Exp(-9f * t) * vol * 0.6f;
                }
            }
            return Make("arp", d);
        }

        /// <summary>Lavta benzeri, 8 ölçülük sakin taverna döngüsü (D dorian, 100 BPM).</summary>
        static AudioClip MakeMusic()
        {
            const float bpm = 100f;
            float beat = 60f / bpm;
            int bars = 8, n = (int)(bars * 4 * beat * Rate);
            var d = new float[n];
            // D dorian: D E F G A B C
            float[] scale = { 293.7f, 329.6f, 349.2f, 392f, 440f, 493.9f, 523.3f, 587.3f };
            int[] melody = { 0, 2, 4, 2, 3, 4, 5, 4, 2, 1, 0, 1, 2, 4, 3, 2, 4, 5, 7, 5, 4, 2, 3, 4, 2, 1, 0, 2, 1, -1, 0, -1 };
            int[] bass = { 0, 0, 3, 3, 4, 4, 0, 0 };
            for (int i = 0; i < melody.Length; i++)
            {
                if (melody[i] < 0) continue;
                Pluck(d, (int)(i * beat * Rate), scale[melody[i]], 0.13f, beat * 1.6f);
            }
            for (int b = 0; b < bars; b++)
            {
                int start = (int)(b * 4 * beat * Rate);
                Pluck(d, start, scale[bass[b]] / 2f, 0.12f, beat * 3.5f);
                Pluck(d, start + (int)(2 * beat * Rate), scale[(bass[b] + 4) % 7] / 2f, 0.07f, beat * 1.8f);
            }
            return Make("music", d);
        }

        static void Pluck(float[] d, int start, float f, float vol, float dur)
        {
            int n = Mathf.Min(d.Length - start, (int)(dur * Rate));
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = Mathf.Sin(2 * Mathf.PI * f * t) + 0.35f * Mathf.Sin(4 * Mathf.PI * f * t) + 0.15f * Mathf.Sin(6 * Mathf.PI * f * t);
                d[start + i] += s * Mathf.Exp(-3.2f * t) * Mathf.Min(1f, t * 300f) * vol;
            }
        }
    }
}
