using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Mutluluk (v1.2, oyuncu testi: "müşteri ne kadar az beklerse o kadar mutlu, mutluluk ne kadar fazlaysa o kadar müşteri gelir").
    /// 0–100 arası tek değer, kayıtta saklanır. Hızlı servis artırır, bekleyip kızgın giden ve kirli masa azaltır;
    /// boşta yavaşça 50'ye döner. Müşteri gelişini çarpar (SpawnMultiplier). ★ yıldızlar ayrı: ambiyans → zengin müşteri ve bahşiş.
    /// Formüller GAME_DESIGN.md §7.2.
    /// </summary>
    public static class Happiness
    {
        public const float Start = 60f;
        /// <summary>Servis anındaki sabır oranına göre artış: ≥%66 hızlı, ≥%33 normal, altı yavaş (0).</summary>
        public const float FastServe = 4f, NormalServe = 2f;
        public const float AngryLoss = 8f;
        /// <summary>Kirli masa başına saniyede kayıp.</summary>
        public const float DirtyPerSecond = 0.12f;
        /// <summary>Boşta 50'ye dönüş hızı (saniyede).</summary>
        public const float DriftPerSecond = 0.04f;

        /// <summary>Değer değişti: (fark, dünya konumu). HUD uçan "+3" yazısı için.</summary>
        public static event Action<float, Vector3> Changed;

        public static float Value
        {
            get => GameState.Data.happiness < 0f ? Start : GameState.Data.happiness;
            private set => GameState.Data.happiness = Mathf.Clamp(value, 0f, 100f);
        }

        public static float Value01 => Value / 100f;

        /// <summary>Müşteri gelişi çarpanı: %0 → ×0.6, %50 → ×1.0, %100 → ×1.5.</summary>
        public static float SpawnMultiplier => Value < 50f ? Mathf.Lerp(0.6f, 1f, Value / 50f) : Mathf.Lerp(1f, 1.5f, (Value - 50f) / 50f);

        /// <summary>-1 (kızgın) .. 1 (çok mutlu): yüz çizimi için.</summary>
        public static float Mood => Value / 50f - 1f;

        public static void OnServed(float patience01, Vector3 at)
        {
            float d = patience01 >= 0.66f ? FastServe : patience01 >= 0.33f ? NormalServe : 0f;
            if (d > 0f) Add(d, at);
        }

        public static void OnAngry(Vector3 at) => Add(-AngryLoss, at);

        /// <summary>v1.3: oyuncunun servis ettiği "Barlo'ya özel" sipariş için ek mutluluk.</summary>
        public static void Bonus(float amount, Vector3 at) => Add(amount, at);

        static void Add(float d, Vector3 at)
        {
            Value += d;
            Changed?.Invoke(d, at);
        }

        /// <summary>Her kare (GameBootstrap): kirli masalar yavaşça düşürür, boşta 50'ye döner.</summary>
        public static void Tick(float dt)
        {
            int dirty = 0;
            foreach (var t in Table.All) if (t && t.Dirty) dirty++;
            float v = Value - dirty * DirtyPerSecond * dt;
            v = Mathf.MoveTowards(v, 50f, DriftPerSecond * dt);
            Value = v;
        }
    }
}
