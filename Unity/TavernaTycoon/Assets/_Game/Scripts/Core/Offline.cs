using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Çevrimdışı kazanç (GAME_DESIGN.md §8.2): oyuncu yokken personelin ürettiğinin %30'u.
    /// Personel yoksa küçük bir "taban" kazanç verilir ki ilk dönüş de ödüllendirsin. Sınır: 2 saat.
    /// </summary>
    public static class Offline
    {
        public static long Pending { get; private set; }
        public static int Minutes { get; private set; }
        public const int CapMinutes = 120;

        public static void Compute(int waiterCount)
        {
            Pending = 0;
            Minutes = 0;
            if (string.IsNullOrEmpty(GameState.Data.lastSeenUtc)) return;
            if (!DateTime.TryParse(GameState.Data.lastSeenUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var last)) return;
            int min = (int)Math.Min(CapMinutes, (DateTime.UtcNow - last).TotalMinutes);
            if (min < 2) return;
            Minutes = min;
            // Dakika başı: (taban + garson katkısı) × seviye ölçeği × %30
            float perMin = (1.5f + waiterCount * 6f) * (2f + GameState.Level) * 0.3f;
            Pending = Math.Max(1, (long)(perMin * min));
        }

        public static void Collect(float multiplier = 1f)
        {
            if (Pending <= 0) return;
            GameState.AddCoins((long)(Pending * multiplier), PlayerController.I ? PlayerController.I.transform.position : Vector3.zero);
            Pending = 0;
        }
    }
}
