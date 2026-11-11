using UnityEngine;

namespace Taverna
{
    /// <summary>Oyun sahnesinin başlatıcısı: kaydı yükler, ayarları uygular, görevleri ve çevrimdışı kazancı hazırlar.</summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        public long startingCoins = 10;
        float _saveT;

        void Awake()
        {
            GameSettings.Apply();
            Sfx.Ensure();
            // v1.5: test kaydı süreç başına bir kez sıfırlanır (prestij sahneyi yeniden yükleyince şube kaydı kalsın)
            bool firstBoot = !_booted;
            _booted = true;
            if (AutoTest.Enabled && firstBoot) { GameState.ResetSave(); Application.runInBackground = true; }
            GameState.Load();
            if (!firstBoot && AutoTest.Enabled) return;   // test bayrakları yalnızca ilk açılışta
            if (GameState.Data.customersServed == 0 && GameState.Coins == 0 && GameState.Data.unlocked.Count == 0)
                GameState.Data.coins = startingCoins;
            // Test: "-autotest-start-level N" → yeni içeriği (örn. mutfak, Sv6) uzun oynatmadan denemek için
            int startLv = AutoTestArg("-autotest-start-level");
            if (AutoTest.Enabled && startLv > 1)
            {
                GameState.Data.level = startLv;
                GameState.Data.coins = 3000L * startLv;
            }
            // Test: "-autotest-tier N" → taverna kademesi hazır (tempo ölçümünde yapay başlangıç harcaması olmasın)
            int tierArg = AutoTestArg("-autotest-tier");
            if (AutoTest.Enabled && tierArg > 0) GameState.Data.tier = tierArg;
            // Test: "-autotest-unlock mutfak,salon" → bu alanlar açık başlasın (yeni alanı doğrudan denemek için)
            var args = System.Environment.GetCommandLineArgs();
            int ui = System.Array.IndexOf(args, "-autotest-unlock");
            if (AutoTest.Enabled && ui >= 0 && ui + 1 < args.Length)
                foreach (var id in args[ui + 1].Split(','))
                    if (!GameState.Data.unlocked.Contains(id)) GameState.Data.unlocked.Add(id);
        }

        static bool _booted;

        static int AutoTestArg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var v) ? v : 0;
        }

        void Start()
        {
            // İstasyonlar etkinleştikten sonra: görevler açık içeceklere göre üretilir
            Quests.Init();
            if (AutoTest.Enabled && string.IsNullOrEmpty(GameState.Data.tavernName)) GameState.Data.tavernName = "Test Tavernası";
            if (AutoTest.Enabled && GameState.Level > 1) GameState.Data.tutorialDone = true;
            Tutorial.Begin();
            int waiters = 0;
            if (GameState.IsUnlocked("pip")) waiters++;
            if (GameState.IsUnlocked("garson2")) waiters++;
            Offline.Compute(waiters);
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
            if (AutoTest.Enabled && !FindAnyObjectByType<PlaytestBot>())   // v1.5: prestijten sonra aynı bot sürer
            {
                var bot = new GameObject("PlaytestBot").AddComponent<PlaytestBot>();
                DontDestroyOnLoad(bot.gameObject);
                bot.gameSeconds = AutoTestSeconds();
                bot.timeScale = 4f;
                bot.onFinished = _ => bot.StartCoroutine(QuitSoon());   // v1.5: bot sahne değişse de yaşar, bu başlatıcı yaşamayabilir
            }
#endif
        }

        static float AutoTestSeconds()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-autotest-seconds");
            return i >= 0 && i + 1 < args.Length && float.TryParse(args[i + 1], out var s) ? s : 300f;
        }

        static System.Collections.IEnumerator QuitSoon()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            Application.Quit();
        }

        void Update()
        {
            _saveT += Time.unscaledDeltaTime;
            if (_saveT > 10f) { _saveT = 0f; GameState.Save(); }
            Depot.Tick(Time.deltaTime);
            Campaigns.Tick();
            Inn.Tick(Time.deltaTime);
            Tutorial.Tick();
            Happiness.Tick(Time.deltaTime);
            Premium.TickDaily();
            Management.TickPenalty(Time.deltaTime);
        }

        void OnApplicationPause(bool paused) { if (paused) GameState.Save(); }
        void OnApplicationQuit() => GameState.Save();
    }
}
