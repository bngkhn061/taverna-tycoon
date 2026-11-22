using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.4: sinemaların (giriş ve son) çalışma anında kullandığı modeller ve sahne parçaları. TavernaSceneBuilder kurar:
    /// prefab başvuruları (Kral, Barlo, muhafız, el arabası, ferman rulosu, merdiven) ve uzakta kapalı duran giriş sahnesi
    /// (yol, harap han, saray köşesi; <see cref="stage"/>) ile içindeki noktalar.
    /// </summary>
    public class CinemaProps : MonoBehaviour
    {
        public static CinemaProps I { get; private set; }

        [Header("Prefablar")]
        public GameObject king, barlo, guard, cart, scroll, ladder;
        [Header("Giriş sahnesi (kapalı başlar)")]
        public GameObject stage;
        public Transform roadStart, ruinStop, ruinFocus, carpetStart, bowSpot, kingSpot, guardL, guardR, palaceFocus;

        void Awake() => I = this;
        void OnDestroy() { if (I == this) I = null; }
    }
}
