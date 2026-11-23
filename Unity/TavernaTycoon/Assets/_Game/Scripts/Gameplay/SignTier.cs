using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v1.2 (oyuncu testi: "her seviye atladığımızda tabela biraz daha şaşaalı olsun"): taverna tabelası
    /// Sv1 / 5 / 10 / 15 / 20'de kademe atlar (mockup S, modeller tools/blender/build_signs.py).
    /// Kademe değişince yeni tabela zıplayarak gelir; HUD yazının rengini <see cref="Current"/>'a göre değiştirir.
    /// </summary>
    public class SignTier : MonoBehaviour
    {
        public static readonly int[] Levels = { 1, 5, 10, 15, 20 };
        public static readonly string[] Names = { Loc.N("Tahta levha"), Loc.N("Boyalı tabela"), Loc.N("Demir çerçeve"), Loc.N("Yaldızlı tabela"), Loc.N("Görkemli tabela") };

        /// <summary>Şu an görünen kademe (0..4).</summary>
        public static int Current { get; private set; }
        /// <summary>Oyun içinde kademe yükseldi (HUD kutlaması için).</summary>
        public static event System.Action<int> Upgraded;

        public GameObject[] variants;
        int _shown = -1;
        static SignTier _i;

        void Awake() => _i = this;
        void OnDestroy() { if (_i == this) _i = null; }

        /// <summary>
        /// v2.5: görünen tabelanın bütün parçaları çiziliyor mu (son sahne tabelayı kameranın önünden kaldırınca
        /// HUD'daki ad yazısı da gizlensin, havada asılı kalmasın).
        /// </summary>
        public static bool BoardVisible
        {
            get
            {
                if (!_i || _i._shown < 0 || _i._shown >= _i.variants.Length) return false;
                var v = _i.variants[_i._shown];
                if (!v || !v.activeInHierarchy) return false;
                foreach (var r in v.GetComponentsInChildren<Renderer>()) if (!r.enabled) return false;
                return true;
            }
        }

        public static int TierFor(int level)
        {
            int t = 0;
            for (int i = 0; i < Levels.Length; i++) if (level >= Levels[i]) t = i;
            return t;
        }

        void Update()
        {
            int t = TierFor(GameState.Level);
            if (t == _shown) return;
            bool upgrade = _shown >= 0 && t > _shown;
            _shown = t;
            Current = t;
            for (int i = 0; i < variants.Length; i++) if (variants[i]) variants[i].SetActive(i == t);
            if (!upgrade || !variants[t]) return;
            variants[t].AddComponent<PopIn>();
            Upgraded?.Invoke(t);
        }
    }
}
