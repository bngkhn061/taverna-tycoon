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
    /// <item>Ortam (v2.4): kısık oda tonu + müşteri sayısıyla büyüyen yumuşak kalabalık uğultusu (v2.3'e kadar: ocak çıtırtısı, kuşlar, cırcır böcekleri).</item>
    /// <item>Konuşma: oturan müşteriler ara sıra kendi ses tiplerinde gevezelik eder, güler, sipariş verir, söylenir.</item>
    /// </list>
    /// Ses düzeyleri GameSettings.Music / GameSettings.Sfx'ten.
    /// <para>
    /// v2.4 (oyuncu testi 2026-11-22 madde 2, "oyuna girince cızırtı, tiz, titreme"): kök neden oyun sahnesinde hiç AudioListener
    /// olmamasıydı. Dinleyici yalnızca ana menü kamerasındaydı; oyun sahnesi yüklenince yok oluyordu ve oyun boyunca Unity sesi
    /// dinleyicisiz (desteklenmeyen durumda) karıştırıyordu (v1.9 ölçeri de bu yüzden hiç örnek görmemişti: "dinleyici yok", −120 dBFS).
    /// Artık tek dinleyici bu kalıcı ses nesnesinde (sahne değişse de yaşar, hareket etmez: Doppler / konum etkisi yok); sahnelerdeki
    /// başka dinleyiciler kapatılır. Bütün kaynaklar 2B (spatialBlend 0) ve dopplerLevel 0. Ortam sesi sadeleşti: ocak çıtırtısı,
    /// kuş ve cırcır böceği (tiz, 1000 çıtırtı/dk) kalktı; yerine yumuşak oda tonu ve alçak geçirgen kalabalık uğultusu
    /// (tools/audio/build_amb_v24.py; 2 kHz üstü yok). Ölçüm: production/qa/evidence/2026-11-22-v24-audio.md.
    /// </para>
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        // v1.9 karışım tavanı (oyuncu testi 2026-11-17 madde 2 "ses cızırtılı"): v1.8'de müzik 0.6, ortam toplamı akşam 1.67'ye
        // (ocak 0.7), 10 efekt + 4 konuşma kaynağı aynı anda; yoğun sahnede çıkış tepesi −0.2 dBFS'e dayanıyordu (tools/audio ölçümü).
        // Efekt kaynağı doluyken çalan klip kesiliyordu (tık). Şimdi: müzik ≤ 0.45, ortam toplamı ≤ 0.35, en fazla 6 efekt,
        // aynı efekt üst üste binmez, aynı anda çalan efektlerin toplam düzeyi ≤ FxBudget, boş kaynak yoksa yeni efekt atlanır.
        const int FxVoices = 6, TalkVoices = 3;
        const float MusicBase = 0.45f, CrossFade = 2.5f, TalkBase = 0.4f;
        /// <summary>v1.9: bütün ortam döngülerinin toplam düzey tavanı (ayar çarpılmadan önce).</summary>
        public const float AmbientCap = 0.35f;
        /// <summary>v2.4: oda tonunun gündüz / akşam düzeyi ve kalabalık uğultusunun en yüksek düzeyi (ayar çarpılmadan önce).</summary>
        public const float RoomDay = 0.12f, RoomNight = 0.16f, MurmurMax = 0.26f;
        /// <summary>v1.9: aynı anda çalan efektlerin toplam düzey tavanı ve efektlerin genel çarpanı.</summary>
        public const float FxBudget = 1f, FxScale = 0.8f;
        /// <summary>v2.4: art arda para sesinin perde adımı ve en fazla adım (en çok +%12).</summary>
        public const float CoinStreakStep = 0.03f;
        public const int CoinStreakMax = 4;
        /// <summary>Bütçeden kalan düzey bundan azsa efekt hiç çalınmaz.</summary>
        const float FxMinVolume = 0.08f;

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
        readonly SfxId[] _fxId = new SfxId[FxVoices];
        readonly float[] _fxVol = new float[FxVoices];
        /// <summary>v1.9 (test günlüğü): kaynak / bütçe dolu ya da aynı efekt çaldığı için atlanan efekt sayısı.</summary>
        public static int SkippedFx { get; private set; }
        int _fxNext, _coinStreak;
        AudioSource _musA, _musB;
        AudioClip _day, _evening, _rush;
        AudioListener _listener;
        AudioSource _room, _murmur;   // v2.4: oda tonu + kalabalık uğultusu (eski: crowd_low/high, fire, day, night)
        float _lastCoin, _duck = 1f, _duckUntil, _duckLevel = 1f, _chatT = 2f;
        bool _inGame;

        /// <summary>v2.1 son sahne: en coşkulu parça (yoğun saat, "Market Day") zorla çalar.</summary>
        public static bool ForceFestive;
        /// <summary>
        /// v2.4 ses teşhisi (otomatik test): bütün kaynaklar susar, aynı ses nesnesinde 1 kHz saf ton çalar. Dinleyici kaydında tonun
        /// perdesi ölçülür: kamera (dinleyici) hareket ederken perde oynuyorsa Doppler etkisi vardır (tools/audio/analyze_capture.py).
        /// </summary>
        public static bool ProbeMode;
        /// <summary>v2.4: ana ses düzeyi (oyunda 1; gizli otomatik testte −60 dB, AudioCapture).</summary>
        public static float MasterVolume = 1f;
        /// <summary>v2.4: deneme tonunun frekansı (Hz).</summary>
        public const int ProbeHz = 1000;
        AudioSource _probe;
        /// <summary>Çalan müzik parçasının adı (test günlüğü için).</summary>
        public static string MusicNow => _i && _i._musA && _i._musA.clip ? _i._musA.clip.name : "-";
        /// <summary>Yüklenen efekt + konuşma grubu sayısı (test günlüğü için).</summary>
        public static int LoadedGroups => _i ? _i._fx.Count + _i._talk.Count : 0;
        /// <summary>Kalabalık uğultusunun toplam düzeyi (test günlüğü için).</summary>
        public static float CrowdLevel => _i ? _i.Raw(_i._murmur) : 0f;
        /// <summary>Ortam kaynaklarının durumu (test günlüğü için).</summary>
        public static string AmbientInfo => !_i ? "-" : string.Join(" ", System.Array.ConvertAll(
            new[] { _i._room, _i._murmur },
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

            _room = Ambient("room");
            _murmur = Ambient("murmur");

            // v2.4: oyunun tek dinleyicisi bu kalıcı nesnenin çocuğunda (oyun sahnesinde dinleyici yoktu). Kaynaklarla aynı nesnede
            // değil: dinleyiciye takılan süzgeçler (OnAudioFilterRead) kaynak yerine bütün karışıma uygulansın.
            _listener = new GameObject("AudioListener").AddComponent<AudioListener>();
            _listener.transform.SetParent(transform, false);
            SceneManager.sceneLoaded += OnScene;
            OnScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void OnDestroy() => SceneManager.sceneLoaded -= OnScene;

        void OnScene(Scene s, LoadSceneMode m)
        {
            _inGame = FindAnyObjectByType<GameBootstrap>() != null;
            // v2.4: sahnede başka dinleyici varsa kapat (iki dinleyici Unity'de uyarı + belirsiz karışım)
            foreach (var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (l != _listener) l.enabled = false;
        }

        /// <summary>v2.4 (test günlüğü): etkin dinleyici sayısı ve bu ses nesnesinde mi.</summary>
        public static string ListenerInfo
        {
            get
            {
                int n = 0;
                foreach (var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) if (l.enabled && l.gameObject.activeInHierarchy) n++;
                return $"etkin dinleyici {n} · ses nesnesinde={(_i && _i._listener && _i._listener.enabled)}";
            }
        }

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
            s.spatialBlend = 0f;   // v2.4: hepsi 2B
            s.dopplerLevel = 0f;   // v2.4: Doppler kapalı (proje ayarında da Doppler Factor 0)
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
            AudioListener.volume = MasterVolume;
            bool open = _inGame && !DayCycle.Ended;
            bool dark = _inGame && (Night.IsDark || DayCycle.Ended);

            // Müzik seçimi + çapraz geçiş
            var want = ForceFestive && _inGame ? _rush : !_inGame ? _day : RushHour.Active && open ? _rush : dark ? _evening : _day;
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

            // Ortam (v2.4): oyunda hep kısık oda tonu; kalabalık uğultusu içerideki müşteri sayısıyla büyür (12 müşteride tam)
            int n = 0;
            if (open) foreach (var c in Customer.All) if (c && c.state != Customer.State.Leaving) n++;
            float sfx = GameSettings.Sfx;
            float room = _inGame ? (dark ? RoomNight : RoomDay) : 0f;
            float murmur = open ? Mathf.Clamp01(n / 12f) * MurmurMax : 0f;
            if (open && RushHour.Active) murmur = Mathf.Max(murmur, MurmurMax * 0.7f);
            float sum = room + murmur;
            float k = sum > AmbientCap ? AmbientCap / sum : 1f;
            Fade(_room, room * k, sfx, dt);
            Fade(_murmur, murmur * k, sfx, dt);

            if (ProbeMode) { ApplyProbe(); return; }
            if (_probe && _probe.isPlaying) _probe.Stop();
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

        /// <summary>v2.4 teşhis: diğer bütün kaynakları sustur, 1 kHz deneme tonunu çal (aynı nesnede, aynı ayarlarla).</summary>
        void ApplyProbe()
        {
            if (!_probe)
            {
                int rate = 44100;
                var data = new float[rate];   // 1 sn = tam 1000 devir: döngü ekinde tık yok
                for (int k = 0; k < rate; k++) data[k] = Mathf.Sin(2f * Mathf.PI * ProbeHz * k / rate);
                var clip = AudioClip.Create("probe_1k", rate, 1, rate, false);
                clip.SetData(data, 0);
                _probe = NewSource(true);
                _probe.clip = clip;
            }
            foreach (var s in GetComponents<AudioSource>()) if (s != _probe) s.volume = 0f;
            _probe.volume = 0.3f;
            if (!_probe.isPlaying) _probe.Play();
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
            if (ProbeMode) return;
            var (key, vol, jitter, gap) = Table[(int)id];
            float now = Time.unscaledTime;
            if (_last.TryGetValue(id, out var lt) && now - lt < gap) return;
            _last[id] = now;
            if (!_fx.TryGetValue(key, out var clips) || clips.Count == 0) return;
            if (id == SfxId.Coin)
            {
                // Arka arkaya toplanan paralar yükselen perdeyle çalsın (tatmin hissi)
                // v2.4: en çok +%12 (v1.1–v2.3: +%28; seri boyunca giderek tizleşiyordu)
                _coinStreak = now - _lastCoin < 0.45f ? Mathf.Min(_coinStreak + 1, CoinStreakMax) : 0;
                _lastCoin = now;
                pitch *= 1f + CoinStreakStep * _coinStreak;
            }
            // v1.9: boş kaynak seç; aynı efekt zaten çalıyorsa ya da bütçe doluysa atla (çalan klibi kesmek tık sesi yapar).
            // Büyük anlar (seviye, yoğun saat, alan açma, şafak, gece) her durumda çalar: en eski kaynağın yerine geçer.
            bool big = id is SfxId.LevelUp or SfxId.Rush or SfxId.Unlock or SfxId.Dawn or SfxId.Nightfall;
            AudioSource src = null;
            float used = 0f;
            for (int k = 0; k < FxVoices; k++)
            {
                var s = _fxSrc[k];
                if (!s.isPlaying) { if (!src) src = s; continue; }
                if (_fxId[k] == id && !big) { SkippedFx++; return; }
                used += _fxVol[k];
            }
            float v = vol * FxScale;
            if (!src)
            {
                if (!big) { SkippedFx++; return; }
                _fxNext = (_fxNext + 1) % FxVoices;
                src = _fxSrc[_fxNext];
            }
            else if (!big)
            {
                v = Mathf.Min(v, FxBudget - used);
                if (v < FxMinVolume) { SkippedFx++; return; }
            }
            int slot = System.Array.IndexOf(_fxSrc, src);
            _fxId[slot] = id;
            _fxVol[slot] = v;
            src.clip = clips[Random.Range(0, clips.Count)];
            src.pitch = pitch * (1f + Random.Range(-jitter, jitter));
            src.volume = v * GameSettings.Sfx;
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
            if (!_i || ProbeMode) return;
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
