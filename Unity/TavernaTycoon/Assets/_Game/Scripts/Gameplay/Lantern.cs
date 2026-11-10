using System;
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Duvar feneri (v0.6 gece/gündüz). Gündüz loş yanar. Akşam olunca söner; Barlo (veya Fenerci) yanına
    /// gelince yanar ve gece boyunca parlak kalır. Sabah sıfırlanır.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class Lantern : MonoBehaviour
    {
        public static readonly List<Lantern> All = new();
        public static event Action<Lantern> LitEvent;

        /// <summary>Bölge kimliği: "" meyhane, "salon", "bahce".</summary>
        public string zone = "";
        public float dayIntensity = 1.6f;
        public float nightIntensity = 2.6f;
        public float lightRadius = 1.8f;

        public bool lit { get; private set; }
        public bool ZoneOpen => Management.ZoneOpen(zone);
        Light _light;
        bool _wasDark;

        void OnEnable() { All.Add(this); _light = GetComponent<Light>(); }
        void OnDisable() => All.Remove(this);

        void Update()
        {
            bool dark = Night.IsDark;
            if (dark != _wasDark) { lit = false; _wasDark = dark; } // akşam: söner · sabah: sıfırlanır
            if (dark && !lit && ZoneOpen)
            {
                var p = PlayerController.I;
                if (p && DrinkStation.FlatDistance(p.transform.position, transform.position) < lightRadius) Ignite();
            }
            _light.enabled = !dark || lit;
            _light.intensity = dark ? nightIntensity : dayIntensity;
        }

        /// <summary>Feneri yakar (oyuncu ya da Fenerci).</summary>
        public void Ignite()
        {
            if (lit || !Night.IsDark) return;
            lit = true;
            Sfx.Play(SfxId.Lantern);
            LitEvent?.Invoke(this);
        }
    }
}
