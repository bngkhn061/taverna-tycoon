using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Gece/gündüz kuralları (v0.6, oyuncu testi: "interaktif gece/gündüz"). Gün DayCycle.Progress01 ile akar:
    /// sabah–öğle → akşam (fenerler söner, yakılmalı) → gece. Karanlıkta yakılmamış odaya müşteri gelmez,
    /// oradaki müşterilerin sabrı hızlı biter; yakılmış odada bahşiş artar; gece fantastik müşteriler gelir ve fazla öder.
    /// </summary>
    public static class Night
    {
        public const float EveningAt = 0.6f;
        public const float NightAt = 0.72f;
        public const float LitTipBonus = 1.2f;
        public const float NightCreaturePay = 1.3f;
        public const float NightCreatureWeight = 3f;
        public const float DarkPatienceDrain = 1.6f;
        /// <summary>Fantastik müşteriler: Goblin (17) ve sonrası.</summary>
        public const int FirstNightCreature = 17;

        public static float Phase => DayCycle.Progress01;
        /// <summary>Akşam veya gece: fenerler gerekli.</summary>
        public static bool IsDark => Phase >= EveningAt;
        public static bool IsNight => Phase >= NightAt;

        /// <summary>Konumun bölgesi (büyük haritada: bahçe x &lt; −4.1, salon x &gt; 4.1, arası meyhane).</summary>
        public static string ZoneOf(Vector3 p) => Inn.Upstairs(p) ? "teras" : p.x < -15.1f ? "iskele" : p.x > 15.1f ? "avlu"
                                                  : p.x < -4.1f ? "bahce" : p.x > 4.1f ? "salon" : "";

        /// <summary>Bölge aydınlık mı? Gündüz hep; karanlıkta fenerlerinin en az yarısı yanıyorsa.</summary>
        public static bool ZoneLit(string zone)
        {
            if (!IsDark) return true;
            int n = 0, lit = 0;
            foreach (var l in Lantern.All)
            {
                if (l.zone != zone) continue;
                n++;
                if (l.lit) lit++;
            }
            return n == 0 || lit * 2 >= n;
        }

        /// <summary>Açık bölgelerdeki fenerler: (yanan, toplam).</summary>
        public static (int lit, int total) LanternCount()
        {
            int n = 0, lit = 0;
            foreach (var l in Lantern.All)
            {
                if (!l.ZoneOpen) continue;
                n++;
                if (l.lit) lit++;
            }
            return (lit, n);
        }

        public static bool IsNightCreature(CustomerDef d) => d && d.number >= FirstNightCreature;

        /// <summary>Ödeme çarpanı: karanlıkta aydınlık oda bahşişi + gece fantastik müşteri tarifesi.</summary>
        public static float PayMultiplier(CustomerDef d, Vector3 pos)
        {
            float m = 1f;
            if (IsDark && ZoneLit(ZoneOf(pos))) m *= LitTipBonus;
            if (IsNight && IsNightCreature(d)) m *= NightCreaturePay;
            return m;
        }
    }
}
