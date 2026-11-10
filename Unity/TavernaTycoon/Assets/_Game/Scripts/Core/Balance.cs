using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Denge formülleri — GAME_DESIGN.md §3.1, §4.1, §7, §8 ile birebir.
    /// Bir sayı değişecekse önce belge, sonra burası güncellenir.
    /// </summary>
    public static class Balance
    {
        // ---- Ün / seviye (§3.1)
        /// <summary>
        /// Gereken Ün. Sv12'ye kadar ×1.32/seviye (denge kontrolü 2026-11-10: eski eğriyle Sv30 ≈ 87 sa;
        /// design/balance/balance-check-economy-2026-11-10.md).
        /// </summary>
        /// Tempo ölçümü (2026-11-10, bot 7 seviyede Ün/dk): Sv12–16 ×1.15, Sv16 sonrası ×1.08 → bot Sv21 ≈ 5 sa, Sv30 ≈ 16 sa
        /// (insan oyuncu ~×1.3 yavaş: hedef Sv21 ≈ 5–7 sa, Sv30 ≈ 20 sa).
        public static int UnForLevel(int level)
        {
            if (level <= UnCurveBreak) return Mathf.RoundToInt(20f * Mathf.Pow(1.32f, level - 1));
            float b = 20f * Mathf.Pow(1.32f, UnCurveBreak - 1);
            if (level <= UnCurveBreak2) return Mathf.RoundToInt(b * Mathf.Pow(1.15f, level - UnCurveBreak));
            return Mathf.RoundToInt(b * Mathf.Pow(1.15f, UnCurveBreak2 - UnCurveBreak) * Mathf.Pow(1.08f, level - UnCurveBreak2));
        }
        public const int UnCurveBreak = 12, UnCurveBreak2 = 16;
        public const int MaxLevel = 30;

        // ---- İstasyon yükseltmeleri (§4.1)
        public const int MaxUpgradeLevel = 10;
        public static float ProduceTime(DrinkDef d, int speedLv) => d.baseTime * Mathf.Pow(0.9f, speedLv);
        public static int MaxStock(int stockLv) => Mathf.Min(3 + stockLv, 12);
        public static int SellPrice(DrinkDef d, int qualityLv) => Mathf.Max(1, Mathf.RoundToInt(d.basePrice * Mathf.Pow(1.15f, qualityLv)));

        public static int UpgradeCost(DrinkDef d, UpgradeStat stat, int currentLv)
        {
            // v0.6: başlangıç daha zorlu (oyuncu testi "çok kolay") → tabanlar ~%30 yukarı
            float b = stat switch { UpgradeStat.Hiz => 20f, UpgradeStat.Stok => 14f, _ => 32f };
            // v1.0 denge: Sv5 sonrası üs 1.18 → 1.24 (para birikmesin)
            return Mathf.RoundToInt(b * d.upgradeCostMultiplier * Mathf.Pow(1.18f, Mathf.Min(currentLv, 5)) * Mathf.Pow(1.24f, Mathf.Max(0, currentLv - 5)));
        }

        // ---- Depo (v0.6): içecek başına malzeme rafı, sipariş + teslimat
        /// <summary>Bir sipariş paketi kaç birim (1 birim = 1 bardak).</summary>
        public const int SupplyPack = 10;
        /// <summary>Malzemenin birim fiyatı, kalite dahil SATIŞ fiyatının oranı (v1.0 denge: 0.4 × taban → 0.3 × satış; kalite marjı sınırsız büyümesin).</summary>
        public const float SupplyCostRatio = 0.3f;
        public static int SupplyUnitCost(DrinkDef d, int qualityLv) => Mathf.Max(1, Mathf.RoundToInt(SellPrice(d, qualityLv) * SupplyCostRatio));
        public static int SupplyPackCost(DrinkDef d, int qualityLv) => SupplyUnitCost(d, qualityLv) * SupplyPack;
        /// <summary>Raf kapasitesi (depo seviyesine göre).</summary>
        public static readonly int[] DepotCapacities = { 20, 30, 45, 65, 90 };
        public static int DepotCapacity(int lv) => DepotCapacities[Mathf.Clamp(lv, 0, DepotCapacities.Length - 1)];
        public static int MaxDepotLevel => DepotCapacities.Length - 1;
        public static int DepotUpgradeCost(DrinkDef d, int lv) => Mathf.RoundToInt(40f * Mathf.Pow(1.8f, lv) * Mathf.Max(1f, d.basePrice / 4f));
        /// <summary>Teslimat süresi (saniye), teslimat yükseltmesiyle kısalır.</summary>
        public static float DeliverySeconds(int lv) => 20f * Mathf.Pow(0.75f, Mathf.Clamp(lv, 0, MaxDeliveryLevel));
        public const int MaxDeliveryLevel = 4;
        public static int DeliveryUpgradeCost(int lv) => Mathf.RoundToInt(120f * Mathf.Pow(2f, lv));
        public const int AutoOrderLevel = 7;
        public const int AutoOrderPrice = 900;
        /// <summary>Otomatik sipariş eşiği (raf doluluğu).</summary>
        public const float AutoOrderThreshold = 0.2f;

        // ---- Mutfak (v0.7)
        /// <summary>Mutfak açıkken bir siparişin yemek olma ihtimali.</summary>
        public const float FoodOrderChance = 0.35f;

        // ---- Kızgın müşteri cezası (v0.6)
        /// <summary>Kızgın müşteri Ün götürmez (v1.0 tempo ölçümü: Sv18+ kızgın kaybı ilerlemeyi durduruyordu); ceza yıldızdan.</summary>
        public const int AngryUnLoss = 0;
        public const float AngryStarPenalty = 0.5f;
        public const float StarPenaltyDecayPerSec = 0.02f;
        /// <summary>Zengin (ödeme ★4+) müşteri servis başına bu kadar Ün verir (kademe ve dekor yatırımının ödülü).</summary>
        public const int RichUn = 2;
        public const float MaxStarPenalty = 2f;

        /// <summary>4. ve 8. seviyede görsel kademe değişir: 1 Yeni, 2 Gelişmiş, 3 Kusursuz (v1.1 adları).</summary>
        public static int VisualTier(int bestLv) => bestLv >= 8 ? 3 : bestLv >= 4 ? 2 : 1;

        // ---- Müşteriler (§7)
        public static float PatienceSeconds(int stars) => Mathf.Lerp(15f, 45f, (stars - 1) / 4f);
        /// <summary>v1.0 denge: Sv8'den sonra sabır seviye başına %2 kısalır, en az %70.</summary>
        public static float PatienceLevelScale(int level) => Mathf.Max(0.85f, 1f - 0.015f * Mathf.Max(0, level - 8));   // tempo ölçümü: 0.7 / %2 fazla serti
        /// <summary>v1.0 denge: müşteriye fazladan 1 sipariş ihtimali (Sv10+ %20, Sv18+ %35).</summary>
        public static float ExtraOrderChance(int level) => level >= 18 ? 0.25f : level >= 10 ? 0.2f : 0f;
        /// <summary>v1.2 (oyuncu testi "biraz daha fazla müşteri, tempo artsın"): 6 → 5.2 sn.</summary>
        public const float BaseSpawnInterval = 5.2f;

        /// <summary>
        /// v1.2 masa planı (21 → 40 masa): açık oturak arttıkça müşteri de artar. 12 oturağa kadar ×1,
        /// sonra karekökle büyür, en fazla ×2.2 (40 oturakta ≈ ×1.83).
        /// </summary>
        public static float SeatSpawnScale(int seats) => Mathf.Clamp(Mathf.Sqrt(seats / 12f), 1f, 2.2f);
        public const float DrinkDuration = 3f;
        public static float PayMultiplier(int payStars) => payStars <= 0 ? 0f : 0.8f + 0.2f * payStars;
        /// <summary>Sabır yeşil bölgedeyken servis → bahşiş (+%25). Sarıdayken bahşiş yok.</summary>
        public static float TipMultiplier(float patience01) => patience01 > 0.5f ? 1.25f : 1f;

        // ---- Tempo (v0.9, oyuncu testi "oyun temposu biraz daha yavaş olmalı")
        /// <summary>Bir gün (saniye): 5 dk → 7 dk.</summary>
        public const float DayLength = 420f;
        /// <summary>Ödeme karesi (alan) bedelleri ×1.4; 10'a yuvarlanır.</summary>
        public const float ZoneCostScale = 1.4f;
        public static long ZoneCost(long baseCost) => (long)(Mathf.Round(baseCost * ZoneCostScale / 10f) * 10f);
        /// <summary>
        /// Müşteri aralığı çarpanı: Sv1–5 biraz seyrek (×1.25, v0.9), Sv6'dan sonra seviye başına ×0.97 sıklaşır,
        /// en fazla ×1.5 kalabalık (≈ Sv20). Personel ve otomasyon olmadan yetişmek zorlaşır (v1.0 denge).
        /// </summary>
        public static float SpawnLevelScale(int level) => level <= 5 ? 1.25f : Mathf.Max(1f / 1.25f, Mathf.Pow(0.98f, level - 6));   // tempo ölçümü: en fazla ×1.25 kalabalık

        // ---- Maaş (v1.0 denge, ölçüm sonrası üs 1.12 → 1.15 (oran %9 → hedef %10–20): maaş seviyeyle ve görevli seviyesiyle büyür; ödenmezse ertesi gün personel küskün)
        public static long Wage(long baseWage, int hireLevel, int tavernLevel, int staffLv) =>
            Mathf.RoundToInt(baseWage * Mathf.Pow(1.15f, Mathf.Max(0, tavernLevel - hireLevel)) * (1f + 0.15f * staffLv));
        /// <summary>Maaşı ödenmeyen personelin ertesi günkü hız çarpanı.</summary>
        public const float SulkySpeed = 0.8f;

        // ---- Oyun salonu (v0.9): oyuncu başı tur ücreti
        public const int DiceFee = 6, CardFee = 10, ArmFee = 16;
        /// <summary>Otomasyonlu kasada para yığını bu kadar bekleyince kendiliğinden toplanır.</summary>
        public const float AutoCollectDelay = 1.2f;

        // ---- v1.3 "oyuncuya hep iş kalsın" (oyuncu testi: oyun kendi kendine oynuyor)
        /// <summary>"Barlo'ya özel" sipariş: Sv3'ten sonra yeni siparişlerin bu kadarı. Garsonlar almaz, sadece oyuncu servis eder.</summary>
        public static float SpecialOrderChance(int level) => level < 3 ? 0f : level < 10 ? 0.18f : 0.22f;
        /// <summary>Özel siparişin ödeme çarpanı ve fazladan mutluluk.</summary>
        public const float SpecialPayMultiplier = 2.5f, SpecialHappiness = 3f;
        /// <summary>Özel sipariş müşterisinin sabrı biraz uzun (oyuncu yetişebilsin).</summary>
        public const float SpecialPatienceMultiplier = 1.4f;
        /// <summary>Kasa otomasyonu (para kendiliğinden toplanır) son düzlükte açılır.</summary>
        public const int KasaLevel = 22;
        public const long KasaPrice = 25000;
        /// <summary>Otomasyonlu resepsiyonda misafir bu kadar bekleyince kendi giriş yapar.</summary>
        public const float AutoCheckInDelay = 1.5f;
        /// <summary>Taverna adını değiştirme bedeli (altın) ya da izlenecek reklam sayısı.</summary>
        public const int RenameCost = 500, RenameAds = 2;

        // ---- Oyuncu
        public const float PlayerSpeed = 3.4f;
        public const int StartCarryCapacity = 2;
        public const float InteractRadius = 1.35f;
    }
}
