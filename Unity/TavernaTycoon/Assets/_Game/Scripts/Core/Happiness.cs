using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Mutluluk (v1.4, GAME_DESIGN.md §7.2). Son müşterilerin memnuniyetinin üstel hareketli ortalaması:
    /// her sonuçlanan müşteri bir puan getirir (servis anındaki sabıra göre 100 / 65 / 35, kızgın giden 0) ve
    /// ortalama <c>M ← M + α·(puan − M)</c> ile güncellenir. Kirli masalar yalnızca gösterilen (etkin) değeri düşürür,
    /// ortalamayı bozmaz. Boşta dönüş yok. Etkin değer müşteri gelişini çarpar (<see cref="SpawnMultiplier"/>).
    /// ★ yıldızlar ayrı: ambiyans → zengin müşteri ve bahşiş.
    /// </summary>
    public static class Happiness
    {
        /// <summary>Yeni oyunda ortalamanın başlangıcı.</summary>
        public const float Start = 60f;
        /// <summary>Her müşterinin ortalamayı kendi puanına çekme oranı (α).</summary>
        public const float Alpha = 0.08f;
        /// <summary>Servis anındaki sabır oranı eşikleri: ≥ Fast çabuk, ≥ Normal zamanında, altı geç.</summary>
        public const float FastThreshold = 0.66f, NormalThreshold = 0.33f;
        /// <summary>Müşteri puanları.</summary>
        public const float FastScore = 100f, NormalScore = 65f, SlowScore = 35f, AngryScore = 0f;
        /// <summary>Oyuncunun servis ettiği "Barlo'ya özel" sipariş: bu puanla bu kadar kez sayılır.</summary>
        public const float SpecialScore = 100f;
        public const int SpecialRepeats = 2;
        /// <summary>Kirli masa başına etkin değerden düşüş ve üst sınırı.</summary>
        public const float DirtyPenaltyPerTable = 4f, MaxDirtyPenalty = 20f;
        /// <summary>Müşteri gelişi çarpanının uçları: %0 → Min, %50 → 1, %100 → Max.</summary>
        public const float MinSpawn = 0.6f, MaxSpawn = 1.5f;

        /// <summary>Etkin değer değişti: (fark, dünya konumu). HUD uçan "+3 mutluluk" yazısı için.</summary>
        public static event Action<float, Vector3> Changed;

        /// <summary>Müşteri puanlarının hareketli ortalaması (kayıtta saklanır).</summary>
        public static float Average
        {
            get => GameState.Data.happiness < 0f ? Start : GameState.Data.happiness;
            private set => GameState.Data.happiness = Mathf.Clamp(value, 0f, 100f);
        }

        /// <summary>Şu an kirli olan masa sayısı (Tick günceller).</summary>
        public static int DirtyTables { get; private set; }

        /// <summary>Kirli masaların etkin değerden düşüşü (en fazla <see cref="MaxDirtyPenalty"/>).</summary>
        public static float DirtyPenalty => Mathf.Min(MaxDirtyPenalty, DirtyTables * DirtyPenaltyPerTable);

        /// <summary>Gösterilen / etkin mutluluk (0–100): ortalama eksi kirli masa düşüşü.</summary>
        public static float Value => Mathf.Clamp(Average - DirtyPenalty, 0f, 100f);

        public static float Value01 => Value / 100f;

        /// <summary>Test günlüğü: puanlanan müşteri sayısı.</summary>
        public static int Samples { get; private set; }

        /// <summary>Müşteri gelişi çarpanı (etkin değerden): %0 → ×0.6, %50 → ×1.0, %100 → ×1.5.</summary>
        public static float SpawnMultiplier => Value < 50f ? Mathf.Lerp(MinSpawn, 1f, Value / 50f) : Mathf.Lerp(1f, MaxSpawn, (Value - 50f) / 50f);

        /// <summary>-1 (kızgın) .. 1 (çok mutlu): yüz çizimi için.</summary>
        public static float Mood => Value / 50f - 1f;

        /// <summary>Servis anındaki sabır oranına göre müşteri puanı.</summary>
        public static float ScoreFor(float patience01) =>
            patience01 >= FastThreshold ? FastScore : patience01 >= NormalThreshold ? NormalScore : SlowScore;

        /// <summary>Müşteri servis edildi. Oyuncunun servis ettiği özel sipariş 100 puanla iki kez sayılır.</summary>
        public static void OnServed(float patience01, Vector3 at, bool special = false)
        {
            if (special) Record(SpecialScore, SpecialRepeats, at);
            else Record(ScoreFor(patience01), 1, at);
        }

        /// <summary>Müşteri (ya da han misafiri) bekleyip kızgın gitti: 0 puan.</summary>
        public static void OnAngry(Vector3 at) => Record(AngryScore, 1, at);

        static void Record(float score, int times, Vector3 at)
        {
            float before = Value;
            float m = Average;
            for (int i = 0; i < times; i++) m += Alpha * (score - m);
            Average = m;
            Samples += times;
            Changed?.Invoke(Value - before, at);
        }

        /// <summary>Her kare (GameBootstrap): kirli masa sayısı. Boşta dönüş yok.</summary>
        public static void Tick(float dt)
        {
            int dirty = 0;
            foreach (var t in Table.All) if (t && t.Dirty) dirty++;
            DirtyTables = dirty;
        }
    }
}
