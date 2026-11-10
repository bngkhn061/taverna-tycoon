using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// "Tavernayı Büyüt" hedef alanı (v1.0, mockup M): sıradaki kademenin seviyesine gelince görünür.
    /// Barlo üstüne basınca para hemen düşmez; kademe kartı açılır (yanlışlıkla büyük harcama olmasın).
    /// Kart bir kez açılır, Barlo alandan çıkıp tekrar girince yeniden açılır.
    /// </summary>
    public class TierZone : MonoBehaviour
    {
        public GameObject visual;
        public float radius = 0.8f;
        bool _inside;

        void Update()
        {
            bool vis = Tiers.Visible;
            if (visual && visual.activeSelf != vis) visual.SetActive(vis);
            var p = PlayerController.I;
            if (!vis || !p) { _inside = false; return; }
            bool inside = DrinkStation.FlatDistance(p.transform.position, transform.position) < radius;
            if (inside && !_inside) Tiers.RaiseZoneEntered();
            _inside = inside;
        }
    }
}
