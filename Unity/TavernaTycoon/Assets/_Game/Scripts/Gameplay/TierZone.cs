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
            // v1.5: kademeler bitince aynı kare "Yeni Şube" (prestij) kartını açar
            bool branch = Tiers.Next == null && Prestige.Visible;
            bool vis = (Tiers.Visible || branch) && !Ending.Playing;   // v2.1: son sahnede kare gizli
            if (visual && visual.activeSelf != vis) visual.SetActive(vis);
            var p = PlayerController.I;
            if (!vis || !p) { _inside = false; return; }
            bool inside = DrinkStation.FlatDistance(p.transform.position, transform.position) < radius;
            // v2.1: 3. tavernada Sv30 + Saray Hanı ve son görülmediyse kare "Krala Davet" kartını açar (prestij kartının yerine)
            if (inside && !_inside && !Ending.Playing)
            {
                if (branch && Ending.Available) Ending.RaiseZoneEntered();
                else if (branch) Prestige.RaiseZoneEntered();
                else Tiers.RaiseZoneEntered();
            }
            _inside = inside;
        }
    }
}
