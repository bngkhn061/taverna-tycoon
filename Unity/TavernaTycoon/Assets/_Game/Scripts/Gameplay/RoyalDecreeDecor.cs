using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.4 Kraliyet Fermanı dekoru (kralın hediyesi): Meyhane'nin arka duvarında, tezgâhın üstünde. Kayıtta hediye varsa görünür
    /// (<see cref="RoyalGift"/>). Son sahne hediye anına kadar gizler, sonra <see cref="Reveal"/> ile yaylı belirir.
    /// </summary>
    public class RoyalDecreeDecor : MonoBehaviour
    {
        public static RoyalDecreeDecor I { get; private set; }
        public GameObject visual;
        /// <summary>Son sahne: hediye anına kadar gizli.</summary>
        public bool forceHidden;
        float _pop = -1f;
        Vector3 _scale = Vector3.one;

        void Awake() { I = this; if (visual) _scale = visual.transform.localScale; }
        void OnDestroy() { if (I == this) I = null; }
        void OnEnable() { GameState.Changed += Refresh; RoyalGift.Changed += Refresh; Refresh(); }
        void OnDisable() { GameState.Changed -= Refresh; RoyalGift.Changed -= Refresh; }

        public void Refresh()
        {
            if (visual) visual.SetActive(RoyalGift.Has && !forceHidden);
        }

        /// <summary>Son sahne: ferman duvarda yaylı büyüyerek belirir.</summary>
        public void Reveal()
        {
            forceHidden = false;
            Refresh();
            _pop = 0f;
        }

        void Update()
        {
            if (_pop < 0f || !visual) return;
            _pop += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_pop / 0.6f);
            float s = k < 1f ? 1f + Mathf.Sin(k * Mathf.PI) * 0.35f * (1f - k) : 1f;
            visual.transform.localScale = _scale * (k < 0.15f ? k / 0.15f : s);
            if (k >= 1f) { visual.transform.localScale = _scale; _pop = -1f; }
        }

        /// <summary>Kamera hedefi (dekorun ortası).</summary>
        public Vector3 Center => transform.position + Vector3.up * 0.5f;
    }
}
