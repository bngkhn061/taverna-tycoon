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
        /// Gereken Ün (seviye → bir sonraki seviye). v1.8: üstel eğri yerine ölçülmüş tablo — her seviyenin Ün'ü,
        /// o seviyedeki iyi oyuncunun (bot) Ün/dk hızı × hedef insan süresi ÷ 1.3 (design/balance/balance-check-final-2026-11-12.md).
        /// Hedef insan süresi: Sv1–5 2.4–3.8 dk, Sv6–12 5–8.6 dk, Sv13–20 9.4–13.4 dk, Sv21–29 14.2–19 dk; toplam ≈ 5 sa.
        /// Eski eğri (20 × 1.32^…, Hazine'li botla ölçülmüştü) Hazine'siz oyuncuda Sv15'te seviyeyi ~60 dk'ya çıkarıyordu.
        /// </summary>
        public static int UnForLevel(int level) => UnTable[Mathf.Clamp(level, 1, UnTable.Length) - 1];

        /// <summary>v1.8: seviye başına gereken Ün (indeks 0 = Sv1). Sv30'da tablo sonu yalnızca gösterim içindir (Ün birikmez).</summary>
        public static readonly int[] UnTable =
        {
            12, 16, 25, 35, 50, 75, 95, 125, 155, 170,
            190, 205, 230, 240, 260, 290, 320, 350, 380, 410,
            460, 480, 490, 500, 510, 520, 530, 540, 550, 560,
        };
        public const int MaxLevel = 30;

        // ---- İstasyon yükseltmeleri (§4.1)
        public const int MaxUpgradeLevel = 10;
        /// <summary>
        /// Üretim süresi. v1.8: taban süreler ×<see cref="ProduceTimeScale"/> — ölçümde hiçbir makine sipariş beklerken boş kalmıyordu,
        /// hız / stok yükseltmesi ve barmen / aşçı etkisizdi (design/balance/balance-check-final-2026-11-12.md).
        /// </summary>
        public static float ProduceTime(DrinkDef d, int speedLv) => d.baseTime * ProduceTimeScale * Mathf.Pow(SpeedUpgradeStep, speedLv);
        /// <summary>v1.8: tüm makinelerin taban üretim süresi çarpanı (1 → 2).</summary>
        public const float ProduceTimeScale = 2f;
        /// <summary>Hız yükseltmesinin seviye başına süre çarpanı.</summary>
        public const float SpeedUpgradeStep = 0.9f;
        /// <summary>
        /// Makinenin bekleyen bardak kapasitesi. v1.9 (oyuncu testi "3 → 3"): üst sınır 12 kalktı, her stok seviyesi +1
        /// (Sv0 3 → Sv10 13); eskiden Sv9 → Sv10 "12 → 12" gösteriyordu.
        /// </summary>
        public static int MaxStock(int stockLv) => BaseStock + Mathf.Max(0, stockLv);
        /// <summary>Stok yükseltmesi olmadan makinede bekleyebilen bardak.</summary>
        public const int BaseStock = 3;
        /// <summary>Kalite yükseltmesinin seviye başına fiyat çarpanı.</summary>
        public const float QualityStep = 1.15f;

        /// <summary>
        /// Satış fiyatı (kalite seviyesiyle). v1.9 (oyuncu testi Bira kalitesinde "3 → 3"): ucuz içeceklerde ×1.15 yuvarlanınca
        /// fiyat artmıyordu; her kalite seviyesi fiyatı en az 1 altın artırır.
        /// </summary>
        public static int SellPrice(DrinkDef d, int qualityLv) => SellPrice(d.basePrice, qualityLv);

        /// <summary><see cref="SellPrice(DrinkDef,int)"/>'in taban fiyatla hali (birim testi).</summary>
        public static int SellPrice(float basePrice, int qualityLv)
        {
            int p = Mathf.Max(1, Mathf.RoundToInt(basePrice));
            for (int lv = 1; lv <= qualityLv; lv++)
                p = Mathf.Max(p + 1, Mathf.RoundToInt(basePrice * Mathf.Pow(QualityStep, lv)));
            return p;
        }

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

        /// <summary>v1.8: Kral Şerbeti çeşmesi aynı kattaysa her içecek siparişinin bu kadarı Kral Şerbeti olur (imza içecek).</summary>
        public const float SignatureOrderChance = 0.12f;   // v1.8: ölçüm 0.09 ile %5.9–8.2 → 0.12

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
        /// <summary>Sabır (sn): ★1 → <see cref="PatienceMin"/>, ★5 → <see cref="PatienceMax"/>.</summary>
        public static float PatienceSeconds(int stars) => Mathf.Lerp(PatienceMin, PatienceMax, (stars - 1) / 4f);
        /// <summary>v1.8: 15 / 45 → 20 / 50 sn (büyük haritada garson yolu uzadı; ölçüm Sv15'te %60 kızgın).</summary>
        public const float PatienceMin = 20f, PatienceMax = 50f;
        /// <summary>v1.0 denge: Sv8'den sonra sabır seviye başına %2 kısalır, en az %70.</summary>
        public static float PatienceLevelScale(int level) => Mathf.Max(PatienceLevelFloor, 1f - 0.015f * Mathf.Max(0, level - 8));
        /// <summary>v1.8: seviyeyle kısalan sabrın alt sınırı 0.85 → 0.9 (ölçüm: Sv22+ kızgın %20–26).</summary>
        public const float PatienceLevelFloor = 0.9f;
        /// <summary>v1.0 denge: müşteriye fazladan 1 sipariş ihtimali (Sv10+ %20, Sv18+ %35).</summary>
        public static float ExtraOrderChance(int level) => level >= 18 ? 0.25f : level >= 10 ? 0.2f : 0f;
        /// <summary>v1.2 (oyuncu testi "biraz daha fazla müşteri, tempo artsın"): 6 → 5.2 sn.</summary>
        public const float BaseSpawnInterval = 5.2f;

        /// <summary>
        /// v1.2 masa planı (21 → 40 masa): açık oturak arttıkça müşteri de artar. 12 oturağa kadar ×1,
        /// sonra karekökle büyür, en fazla ×2.2 (40 oturakta ≈ ×1.83).
        /// </summary>
        public static float SeatSpawnScale(int seats) => Mathf.Clamp(Mathf.Sqrt(seats / 12f), 1f, SeatSpawnMax);
        /// <summary>v1.8: oturak ölçeğinin üst sınırı 2.2 → 1.7 (servis kapasitesi oturakla büyümüyor; garson sayısı sabit, yol uzuyor).</summary>
        public const float SeatSpawnMax = 1.7f;
        /// <summary>
        /// v1.8: içme süresi sabit 3 sn → seviyeyle 7 → 22 sn (Sv1 → Sv20). Müşteri masada daha uzun kalır; oturak doluluğu sipariş
        /// hızını artırmadan yükselir (ölçüm: normal saatte doluluk %30–40, hedef %55–80).
        /// </summary>
        public static float DrinkDuration(int level) => Mathf.Lerp(DrinkDurationEarly, DrinkDurationLate, (level - 1) / (float)(DrinkDurationLateLevel - 1));
        public const float DrinkDurationEarly = 7f, DrinkDurationLate = 22f;
        public const int DrinkDurationLateLevel = 20;
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
        public static float SpawnLevelScale(int level) => level <= 5 ? EarlySpawnScale : Mathf.Max(1f / MaxCrowd, Mathf.Pow(0.98f, level - 6));
        /// <summary>v1.8: seviyeyle artan kalabalığın üst sınırı ×1.25 → ×1.15 (geç oyunda servis kapasitesi büyümüyor).</summary>
        public const float MaxCrowd = 1.15f;
        /// <summary>v1.8: Sv1–5 aralık çarpanı 1.25 → 1.1 (oyuncu testi "biraz daha fazla müşteri"; ölçümde Sv1–4 kızgın %1).</summary>
        public const float EarlySpawnScale = 1.1f;

        // ---- Maaş (v1.0 denge, ölçüm sonrası üs 1.12 → 1.15 (oran %9 → hedef %10–20): maaş seviyeyle ve görevli seviyesiyle büyür; ödenmezse ertesi gün personel küskün)
        /// v1.8: × <see cref="WageScale"/> (ölçüm: Sv9–22 maaş / gelir %9–11, hedef %15–35).
        public static long Wage(long baseWage, int hireLevel, int tavernLevel, int staffLv) =>
            Mathf.RoundToInt(baseWage * WageScale * Mathf.Pow(WageLevelGrowth, Mathf.Max(0, tavernLevel - hireLevel)) * (1f + 0.15f * staffLv));
        /// <summary>v1.8: tüm maaşların çarpanı (1 → 2).</summary>
        public const float WageScale = 2f;
        /// <summary>v1.8: maaşın taverna seviyesi başına büyümesi 1.15 → 1.12 (ölçüm: Sv29'da maaş / gelir %59).</summary>
        public const float WageLevelGrowth = 1.12f;
        /// <summary>Maaşı ödenmeyen personelin ertesi günkü hız çarpanı.</summary>
        public const float SulkySpeed = 0.8f;

        // ---- Oyun salonu (v0.9): oyuncu başı tur ücreti
        public const int DiceFee = 6, CardFee = 10, ArmFee = 16;
        /// <summary>Otomasyonlu kasada para yığını bu kadar bekleyince kendiliğinden toplanır.</summary>
        public const float AutoCollectDelay = 1.2f;

        // ---- v1.3 "oyuncuya hep iş kalsın" (oyuncu testi: oyun kendi kendine oynuyor)
        /// <summary>"Barlo'ya özel" sipariş: Sv3'ten sonra yeni siparişlerin bu kadarı. Garsonlar almaz, sadece oyuncu servis eder.</summary>
        /// v1.8: %18 / %22 → %10 / %7 (ölçüm: iyi oynayan bot Sv15'te özel siparişlerin %20–34'ünü yetiştirebildi; hedef %70–85.
        /// Geç oyunda oyuncunun para toplama, han ve teras işi de var).
        public static float SpecialOrderChance(int level) => level < 3 ? 0f : level < 10 ? SpecialChanceEarly : SpecialChanceLate;
        public const float SpecialChanceEarly = 0.10f, SpecialChanceLate = 0.07f;
        /// <summary>Özel siparişin ödeme çarpanı (mutluluğa katkısı v1.4'te Happiness.SpecialScore / SpecialRepeats).</summary>
        public const float SpecialPayMultiplier = 2.5f;
        /// <summary>Özel sipariş müşterisinin sabrı biraz uzun (oyuncu yetişebilsin).</summary>
        public const float SpecialPatienceMultiplier = 2f;   // v1.8: 1.4 → 2

        /// <summary>v1.8: yıldız başına bahşiş artışı (Management.TipFromStars) %5 → %10.</summary>
        public const float TipPerStar = 0.10f;

        /// <summary>
        /// v1.8: etkinlik fiyatlarının dayanağı — o seviyede iyi bir oyuncunun 3 dakikalık geliri (ölçüm: Sv5 ≈ 1.100, Sv9 ≈ 2.000,
        /// Sv15 ≈ 6.000, Sv29 ≈ 21.000 altın). Etkinlik fiyatı = pay × bu değer (TavernEvents.Def.share).
        /// </summary>
        public static float EventIncomeRef(int level) => EventIncomeBase * Mathf.Pow(EventIncomeGrowth, Mathf.Max(0, level - 1));
        public const float EventIncomeBase = 660f, EventIncomeGrowth = 1.135f;

        /// <summary>v1.8: garsonların yürüme hızı çarpanı (servis kapasitesi büyük haritada çok düşüktü).</summary>
        public const float WaiterSpeedScale = 1.3f;
        /// <summary>Kasa otomasyonu (para kendiliğinden toplanır) son düzlükte açılır.</summary>
        public const int KasaLevel = 22;
        public const long KasaPrice = 25000;
        /// <summary>Otomasyonlu resepsiyonda misafir bu kadar bekleyince kendi giriş yapar.</summary>
        public const float AutoCheckInDelay = 1.5f;
        /// <summary>Taverna adını değiştirme bedeli (altın) ya da izlenecek reklam sayısı.</summary>
        public const int RenameCost = 500, RenameAds = 2;
        /// <summary>v1.9: taverna adını elmasla değiştirme bedeli (kozmetik; ilk ad menüde ücretsiz). İlk saatte ≈ 25 elmas toplanır.</summary>
        public const int RenameGems = 20;

        // ---- v1.4 (Paket A, production/project-stage-report.md A bölümü)
        /// <summary>
        /// Hazine (premium) olmadan elde edilebilecek toplam ambiyans (kod okunarak, 2026-11-10):
        /// masalar 217 (43 masa: ana 14, salon 15, bahçe 14, ön sıra 13×3 = 39, oyun salonu 21, mahzen 24, teras 32, avlu 30, iskele 28),
        /// dekor yerleri 127 (saksı 7×2, mumluk 7×2, halı 4×4, flama 3×4, fıçı 3×3, kalkan 5, boynuz 2×5, mühür 15, avize 4×8),
        /// ozan sahnesi 20, en iyi zemin + duvar teması 6 + 6 = 12, Saray Hanı kademesi 60 → 436.
        /// v2.0: + bahçe uzun masası 8, oyun salonu zar 2 / kart 2 masaları 6 + 7, taş yol 4×3, bahçe feneri 3×6 → 487
        /// (yıldız eşikleri değişmedi: 5★ toplamın ≈ %72'si).
        /// </summary>
        public const float AmbianceNoPremiumTotal = 487f;

        /// <summary>
        /// Yıldız eşikleri (ambiyans): indeks i → (i+1)★. 2★ ≈ %8, 3★ ≈ %25, 4★ ≈ %50, 5★ ≈ %80 (premium'suz toplamın).
        /// Aradaki yarım yıldızlar doğrusal.
        /// </summary>
        public static readonly float[] StarThresholds = { 0f, 35f, 110f, 220f, 350f };

        /// <summary>Ambiyanstan yarım yıldıza yuvarlanmış (aşağı) puan, 1–5.</summary>
        public static float StarsForAmbiance(float ambiance)
        {
            var t = StarThresholds;
            if (ambiance >= t[t.Length - 1]) return t.Length;
            for (int i = t.Length - 2; i >= 0; i--)
                if (ambiance >= t[i])
                {
                    float s = 1f + i + (ambiance - t[i]) / (t[i + 1] - t[i]);
                    return Mathf.Floor(s * 2f) / 2f;
                }
            return 1f;
        }

        /// <summary>Bir yıldız değerine (yarımlı) ulaşmak için gereken ambiyans.</summary>
        public static float AmbianceForStars(float stars)
        {
            var t = StarThresholds;
            if (stars <= 1f) return 0f;
            if (stars >= t.Length) return t[t.Length - 1];
            int i = Mathf.FloorToInt(stars) - 1;
            return t[i] + (stars - 1f - i) * (t[i + 1] - t[i]);
        }

        /// <summary>
        /// Geçici / kazanılan müşteri çarpanlarının (mutluluk × etkinlik × yoğun saat × Usta) çarpımının üst sınırı.
        /// Oturak ve seviye ölçeği bu sınırın dışında.
        /// </summary>
        public const float SpawnBoostCap = 3.0f;
        /// <summary>Yoğun saatte müşteri gelişi çarpanı.</summary>
        public const float RushSpawnMultiplier = 2.2f;

        /// <summary>İşe alım ücreti en az görevlinin şu anki günlük maaşının bu katı (kovup sabah geri almak kâr etmesin).</summary>
        public const int HireWageMultiplier = 2;

        /// <summary>v1.4 işe alım ücreti: katalog ücreti ya da günlük maaşın <see cref="HireWageMultiplier"/> katı (büyük olan).</summary>
        public static long HireCost(long catalogHire, long currentWage) => System.Math.Max(catalogHire, HireWageMultiplier * currentWage);

        /// <summary>Geçici müşteri çarpanlarının çarpımı, <see cref="SpawnBoostCap"/> ile sınırlı.</summary>
        public static float CapSpawnBoost(float raw) => Mathf.Min(SpawnBoostCap, raw);

        /// <summary>Özel sipariş görevleri: açılış seviyesi, hedefler ve büyük görevin ödül çarpanları.</summary>
        public const int SpecialQuestLevel = 3, SpecialQuestSmall = 5, SpecialQuestBig = 12;
        public const int SpecialQuestBigCoinMultiplier = 2, SpecialQuestBigExtraGems = 1;

        // ---- v2.0 gün sonu (oyuncu testi madde 15)
        /// <summary>Günün bu kısmından sonra yeni müşteri gelmez, oturanlar yeni sipariş vermez ("son siparişler").</summary>
        public const float LastCallPhase = 0.9f;
        /// <summary>Gün bitince kapanış: müşteriler ve personel çıkarken en fazla bu kadar oyun saniyesi beklenir, sonra plan ekranı.</summary>
        public const float ClosingMaxSeconds = 12f;
        /// <summary>Personelin işe gelip giderken yürüme hızı (m/sn).</summary>
        public const float CommuteSpeed = 3.2f;

        // ---- Oyuncu
        public const float PlayerSpeed = 3.4f;
        public const int StartCarryCapacity = 2;
        public const float InteractRadius = 1.35f;
    }
}
