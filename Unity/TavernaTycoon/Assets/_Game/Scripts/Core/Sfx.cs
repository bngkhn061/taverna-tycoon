using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Taverna
{
    public enum SfxId
    {
        Coin, Pop, Serve, Buy, LevelUp, Rush, Tick, Claim, Angry,
        Pickup, Dice, Door, Step, Wipe, Clink, Lantern, Unlock, Dawn, Nightfall, Error
    }

    /// <summary>Müşterilerin oyuncak-dil konuşma türleri (Resources/Audio/voice/&lt;ses&gt;_&lt;tür&gt;_N).</summary>
    public enum VoiceKind { Chat, Laugh, Happy, Grumble, Order }

    /// <summary>
    /// Oyunun tüm sesi (v1.1). Klipler Resources/Audio altından yüklenir; üretimi tools/audio/build_audio.py.
    /// <list type="bullet">
    /// <item>Efektler: Kenney CC0 kayıtları + kendi katmanlamamız; her olayın birkaç çeşidi, perde oynaması ve tekrar sınırı var.</item>
    /// <item>Müzik (RandomMind, CC0): gündüz Minstrel Dance, akşam The Old Tower Inn, yoğun saat Market Day. Duruma göre çapraz geçer, dönülen parça kaldığı yerden sürer.</item>
    /// <item>Ortam: kalabalık uğultusu müşteri sayısıyla büyür; ocak çıtırtısı, gündüz kuşlar, akşam cırcır böcekleri.</item>
    /// <item>Konuşma: oturan müşteriler ara sıra kendi ses tiplerinde gevezelik eder, güler, sipariş verir, söylenir.</item>
    /// </list>
    /// Ses düzeyleri GameSettings.Music / GameSettings.Sfx'ten.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const int FxVoices = 10, TalkVoices = 4;
        const float MusicBase = 0.6f, CrossFade = 2.5f, TalkBase = 0.5f;

        /// <summary>SfxId sırasıyla: klip anahtarı, ses düzeyi, perde oynaması (±), aynı efektin en kısa tekrar aralığı (sn).</summary>
        static readonly (string key, float vol, float jitter, float gap)[] Table =
        {
            ("coin", 0.55f, 0.03f, 0.035f), ("pop", 0.6f, 0.04f, 0.03f), ("serve", 0.75f, 0.05f, 0.05f),
            ("buy", 0.75f, 0.03f, 0.05f), ("levelup", 1f, 0f, 0.5f), ("rush", 0.9f, 0f, 1f),
            ("tick", 0.35f, 0.02f, 0.05f), ("claim", 0.8f, 0.03f, 0.08f), ("angry", 0.8f, 0.04f, 0.3f),
            ("pickup", 0.55f, 0.06f, 0.06f), ("dice", 0.7f, 0.05f, 0.1f), ("door", 0.7f, 0.03f, 0.3f),
            ("step", 0.6f, 0.05f, 0.2f), ("wipe", 0.6f, 0.06f, 0.08f), ("clink", 0.8f, 0.03f, 0.1f),
            ("lantern", 0.6f, 0.06f, 0.1f), ("unlock", 0.9f, 0f, 0.3f), ("dawn", 0.8f, 0f, 1f),
            ("nightfall", 0.8f, 0f, 1f), ("error", 0.6f, 0f, 0.2f),
        };

        static Sfx _i;
        readonly Dictionary<string, List<AudioClip>> _fx = new(), _talk = new();
        readonly Dictionary<SfxId, float> _last = new();
        readonly Dictionary<AudioClip, float> _resume = new();
        readonly Dictionary<AudioSource, float> _raw = new();
        AudioSource[] _fxSrc, _talkSrc;
        int _fxNext, _coinStreak;
        AudioSource _musA, _musB;
        AudioClip _day, _evening, _rush;
        AudioSource _crowdLo, _crowdHi, _fire, _birds, _crickets;
        float _lastCoin, _duck = 1f, _duckUntil, _duckLevel = 1f, _chatT = 2f;
        bool _inGame;

        /// <summary>Çalan müzik parçasının adı (test günlüğü için).</summary>
        public static string MusicNow => _i && _i._musA && _i._musA.clip ? _i._musA.clip.name : "-";
        /// <summary>Yüklenen efekt + konuşma grubu sayısı (test günlüğü için).</summary>
        public static int LoadedGroups => _i ? _i._fx.Count + _i._talk.Count : 0;
        /// <summary>Kalabalık uğultusunun toplam düzeyi (test günlüğü için).</summary>
        public static float CrowdLevel => _i ? _i.Raw(_i._crowdLo) + _i.Raw(_i._crowdHi) : 0f;
        /// <summary>Ortam kaynaklarının durumu (test günlüğü için).</summary>
        public static string AmbientInfo => !_i ? "-" : string.Join(" ", System.Array.ConvertAll(
            new[] { _i._crowdLo, _i._crowdHi, _i._fire, _i._birds, _i._crickets },
            s => s && s.clip ? $"{s.clip.name}:{_i.Raw(s):0.00}{(s.isPlaying ? "" : "(durdu)")}" : "yok")) + $" oyun={_i._inGame} ayar=m{GameSettings.Music:0.00}/e{GameSettings.Sfx:0.00}";

        float Raw(AudioSource s) => s && _raw.TryGetValue(s, out var v) ? v : 0f;
        /// <summary>Şu ana kadar çalınan konuşma sayısı (test günlüğü için).</summary>
        public static int TalkCount { get; private set; }

        public static void Ensure()
        {
            if (_i) return;
            var go = new GameObject("Sfx");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<Sfx>();
        }

        void Awake()
        {
            foreach (var c in Resources.LoadAll<AudioClip>("Audio/sfx")) Group(_fx, c);
            foreach (var c in Resources.LoadAll<AudioClip>("Audio/voice")) Group(_talk, c);
            if (_fx.Count == 0) Debug.LogWarning("Sfx: Resources/Audio/sfx boş (TavernaImport.CopySources çalıştı mı?)");

            _fxSrc = new AudioSource[FxVoices];
            for (int k = 0; k < FxVoices; k++) _fxSrc[k] = NewSource(false);
            _talkSrc = new AudioSource[TalkVoices];
            for (int k = 0; k < TalkVoices; k++) _talkSrc[k] = NewSource(false);

            _day = Resources.Load<AudioClip>("Audio/music/day");
            _evening = Resources.Load<AudioClip>("Audio/music/evening");
            _rush = Resources.Load<AudioClip>("Audio/music/rush");
            _musA = NewSource(true);
            _musB = NewSource(true);

            _crowdLo = Ambient("crowd_low");
            _crowdHi = Ambient("crowd_high");
            _fire = Ambient("fire");
            _birds = Ambient("day");
            _crickets = Ambient("night");

            SceneManager.sceneLoaded += OnScene;
            OnScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void OnDestroy() => SceneManager.sceneLoaded -= OnScene;

        void OnScene(Scene s, LoadSceneMode m) => _inGame = FindAnyObjectByType<GameBootstrap>() != null;

        static void Group(Dictionary<string, List<AudioClip>> d, AudioClip c)
        {
            // "coin_2" → "coin", "male_chat_3" → "male_chat", "levelup" → "levelup"
            string n = c.name;
            int u = n.LastIndexOf('_');
            string key = u > 0 && int.TryParse(n.Substring(u + 1), out _) ? n.Substring(0, u) : n;
            if (!d.TryGetValue(key, out var list)) d[key] = list = new List<AudioClip>();
            list.Add(c);
        }

        AudioSource NewSource(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.volume = 0f;
            return s;
        }

        AudioSource Ambient(string name)
        {
            var clip = Resources.Load<AudioClip>("Audio/amb/" + name);
            var s = NewSource(true);
            if (!clip) return s;
            s.clip = clip;
            s.Play();
            s.time = Random.Range(0f, clip.length * 0.9f);   // döngüler aynı anda başa sarmasın
            return s;
        }

        // ------------------------------------------------------------------ her kare: müzik, ortam, gevezelik
        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            AudioListener.volume = 1f;
            bool open = _inGame && !DayCycle.Ended;
            bool dark = _inGame && (Night.IsDark || DayCycle.Ended);

            // Müzik seçimi + çapraz geçiş
            var want = !_inGame ? _day : RushHour.Active && open ? _rush : dark ? _evening : _day;
            if (want && _musA.clip != want)
            {
                if (_musA.clip) _resume[_musA.clip] = _musA.time;
                (_musA, _musB) = (_musB, _musA);
                _musA.clip = want;
                _musA.volume = 0f;
                _musA.time = want == _rush ? 0f : _resume.TryGetValue(want, out var at) ? at : 0f;
                _musA.Play();
            }
            _duck = Mathf.MoveTowards(_duck, Time.unscaledTime < _duckUntil ? _duckLevel : 1f, dt * 2f);
            float fade = MusicBase / CrossFade * dt;
            float target = GameSettings.Music * MusicBase * _duck;
            _musA.volume = Mathf.Abs(_musA.volume - target) > 0.05f && _musA.volume < target ? _musA.volume + fade : target;
            _musB.volume = Mathf.MoveTowards(_musB.volume, 0f, fade);
            if (_musB.isPlaying && _musB.volume <= 0f) _musB.Stop();

            // Ortam: kalabalık müşteri sayısıyla büyür
            int n = 0;
            if (open) foreach (var c in Customer.All) if (c && c.state != Customer.State.Leaving) n++;
            float sfx = GameSettings.Sfx;
            float lo = open ? Mathf.Clamp01(n / 4f) * 0.32f : 0f;
            float hi = open ? Mathf.Clamp01((n - 4) / 10f) * 0.45f : 0f;
            if (open && RushHour.Active) hi = Mathf.Max(hi, 0.18f);
            Fade(_crowdLo, lo, sfx, dt);
            Fade(_crowdHi, hi, sfx, dt);
            Fade(_fire, _inGame ? (dark ? 0.7f : 0.45f) : 0f, sfx, dt);
            Fade(_birds, _inGame && !dark ? 0.16f : 0f, sfx, dt);
            Fade(_crickets, _inGame && dark ? 0.2f : 0f, sfx, dt);

            // Gevezelik: oturan müşterilerden rastgele biri konuşur ya da güler (kalabalıkta daha sık)
            if (open && n >= 2)
            {
                _chatT -= dt;
                if (_chatT <= 0f)
                {
                    _chatT = Random.Range(1.0f, 3.2f) * Mathf.Lerp(1.4f, 0.45f, Mathf.Clamp01(n / 14f));
                    var c = RandomDrinker();
                    if (c) Voice(c.def, Random.value < 0.22f ? VoiceKind.Laugh : VoiceKind.Chat, c.transform.position, 0.8f);
                }
            }
        }

        /// <summary>Ortam düzeyi ayardan bağımsız yavaşça hedefe gider, ayar en son çarpılır (sürgü anında etki eder).</summary>
        void Fade(AudioSource s, float target, float setting, float dt)
        {
            if (!s || !s.clip) return;
            _raw.TryGetValue(s, out var v);
            _raw[s] = v = Mathf.MoveTowards(v, target, dt * 0.25f);
            s.volume = v * setting;
        }

        static Customer RandomDrinker()
        {
            int count = 0;
            Customer pick = null;
            foreach (var c in Customer.All)
                if (c && c.state == Customer.State.Drinking && Random.Range(0, ++count) == 0) pick = c;   // rezervuar örnekleme
            return pick;
        }

        // ------------------------------------------------------------------ efekt
        public static void Play(SfxId id, float pitch = 1f)
        {
            Ensure();
            _i.PlayFx(id, pitch);
        }

        void PlayFx(SfxId id, float pitch)
        {
            var (key, vol, jitter, gap) = Table[(int)id];
            float now = Time.unscaledTime;
            if (_last.TryGetValue(id, out var lt) && now - lt < gap) return;
            _last[id] = now;
            if (!_fx.TryGetValue(key, out var clips) || clips.Count == 0) return;
            if (id == SfxId.Coin)
            {
                // Arka arkaya toplanan paralar yükselen perdeyle çalsın (tatmin hissi)
                _coinStreak = now - _lastCoin < 0.45f ? Mathf.Min(_coinStreak + 1, 8) : 0;
                _lastCoin = now;
                pitch *= 1f + 0.035f * _coinStreak;
            }
            var src = _fxSrc[_fxNext];
            _fxNext = (_fxNext + 1) % FxVoices;
            src.clip = clips[Random.Range(0, clips.Count)];
            src.pitch = pitch * (1f + Random.Range(-jitter, jitter));
            src.volume = vol * GameSettings.Sfx;
            src.Play();
            if (id is SfxId.LevelUp or SfxId.Rush or SfxId.Unlock or SfxId.Dawn or SfxId.Nightfall)
                Duck(0.35f, src.clip.length * 0.9f);
        }

        /// <summary>Büyük anlarda müziği kısa süre kıs (efekt öne çıksın).</summary>
        public static void Duck(float level, float seconds)
        {
            if (!_i) return;
            _i._duckLevel = level;
            _i._duckUntil = Time.unscaledTime + seconds;
        }

        // ------------------------------------------------------------------ konuşma
        /// <summary>
        /// Müşteri tipinin ses kümesinden bir konuşma çalar. Ekran dışındaysa kısık, ekrandaki yerine göre sağ/sol.
        /// Aynı anda en fazla <see cref="TalkVoices"/> konuşma; doluysa gevezelik atlanır, tepkiler en eskinin yerine geçer.
        /// </summary>
        public static void Voice(CustomerDef def, VoiceKind kind, Vector3 pos, float vol = 1f)
        {
            if (!_i) return;
            string key = BankOf(def ? def.number : 0) + "_" + kind.ToString().ToLowerInvariant();
            if (!_i._talk.TryGetValue(key, out var clips) || clips.Count == 0) return;
            float pan = 0f, att = 1f;
            var cam = Camera.main;
            if (cam)
            {
                var vp = cam.WorldToViewportPoint(pos);
                if (vp.z < 0f) return;
                bool onScreen = vp.x > -0.1f && vp.x < 1.1f && vp.y > -0.1f && vp.y < 1.1f;
                att = onScreen ? 1f : 0.35f;
                pan = Mathf.Clamp((vp.x - 0.5f) * 1.4f, -0.8f, 0.8f);
            }
            AudioSource src = null;
            foreach (var s in _i._talkSrc) if (!s.isPlaying) { src = s; break; }
            if (!src)
            {
                if (kind is VoiceKind.Chat or VoiceKind.Laugh) return;
                src = _i._talkSrc[Random.Range(0, TalkVoices)];
            }
            src.clip = clips[Random.Range(0, clips.Count)];
            src.pitch = VoicePitch(def) * Random.Range(0.97f, 1.03f);
            src.panStereo = pan;
            src.volume = GameSettings.Sfx * TalkBase * vol * att;
            src.Play();
            TalkCount++;
        }

        /// <summary>Müşteri numarasından ses tipi (CHARACTERS.md karakterleri): kalın, erkek, kadın, minik, yaşlı, kibar.</summary>
        public static string BankOf(int number) => number switch
        {
            3 or 8 or 10 or 12 or 16 or 18 => "deep",
            6 or 19 or 21 or 22 => "female",
            13 or 17 => "tiny",
            11 or 20 => "old",
            9 or 14 => "posh",
            _ => "male",
        };

        /// <summary>Aynı ses kümesindeki tipler birbirinden ayrılsın: tipe özgü sabit perde (0.93–1.05).</summary>
        static float VoicePitch(CustomerDef d) => d ? 0.93f + (d.number * 37 % 13) / 100f : 1f;
    }
}
