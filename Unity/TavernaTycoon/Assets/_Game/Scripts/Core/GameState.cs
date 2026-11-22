using System;
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    [Serializable]
    public class StationSave
    {
        public DrinkId id;
        public int speed, stock, quality;
        // Depo (v0.6): makinenin malzeme stoğu. supplyInit=false → ilk erişimde rafı doldur (eski kayıtlar da)
        public bool supplyInit;
        public int supply, depotLv, incoming;
        public float eta;
        public int Get(UpgradeStat s) => s switch { UpgradeStat.Hiz => speed, UpgradeStat.Stok => stock, _ => quality };
        public void Inc(UpgradeStat s) { if (s == UpgradeStat.Hiz) speed++; else if (s == UpgradeStat.Stok) stock++; else quality++; }
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        /// <summary>v1.6: kayıt biçiminin sürümü (0 = v1.6 öncesi PlayerPrefs kaydı). Yazarken <see cref="GameState.CurrentSaveVersion"/>.</summary>
        public int saveVersion;
        public long coins;
        public int gems;
        public int level = 1;
        public int un;
        public int customersServed;
        public List<StationSave> stations = new();
        public List<string> unlocked = new();
        public List<int> seenCustomers = new();
        public List<string> zoneIds = new();
        public List<long> zonePaid = new();
        public List<QuestSave> quests = new();
        public string questDay = "";
        public int questsClaimed;
        public string lastDaily = "";
        public int dailyStreak;
        public List<string> decor = new();
        public int day = 1;
        public int deliveryLv;      // teslimat süresi yükseltmesi (Depot)
        public bool autoOrder;      // "Tedarikçi anlaşması": stok azalınca kendiliğinden sipariş
        public List<string> staffIds = new();   // görevli seviyeleri (StaffUpgrades)
        public List<int> staffLvs = new();
        public List<string> staffAds = new();
        public List<string> benched = new();     // v1.3: izne gönderilen (Hazine) görevliler   // v1.1: reklamla verilen her görevli seviyesi (görevli başına en fazla 2)
        public string floorTheme = "", wallTheme = "";   // Themes (boş = varsayılan)
        public List<int> roomLvs = new();                 // han odalarının konfor kademesi (Inn)
        public string tavernName = "";                    // v0.9: tabela (boşsa ilk "OYNA"da sorulur)
        public bool tutorialDone;
        public bool eventsSeen;                           // v1.2: etkinlik butonundaki "YENİ!" bir kez                         // v0.9: rehber görüldü / atlandı
        public int renameAds;                             // v0.9: ad değiştirmek için izlenen reklam sayacı
        public List<string> autos = new();                // v0.9: alınan otomasyonlar (Automation)
        public int ustaGelir, ustaMusteri;                // v0.9: Usta geliştirme kademeleri
        public int sulkyDay;
        public int eventDay;                              // v1.2 etkinlikler: bugün kullanılanlar
        public List<string> eventsUsed = new();
        public float happiness = -1f;                    // v1.2 Mutluluk (0–100, -1 = yeni oyun → Happiness.Start)
        public int tier;                                  // v1.0 "Tavernayı Büyüt": 0 Ahşap, 1 Taş Han, 2 Saray Hanı
        public int branch;                                // v1.5 prestij: açılan şube sayısı (0 = ilk taverna, Prestige)
        public List<string> premium = new();              // v1.0 Hazine: alınan gerçek para ürünleri (tek seferlik)
        public string vipUntilUtc = "", vipGemDate = "", starterShownUtc = "";                              // v1.0: maaşı ödenmeyen personelin küskün olduğu gün
        public string lastSeenUtc;
        /// <summary>v1.9 "YENİ!" rozeti: dükkânda görülmüş ürün anahtarları (makine "mac_X", görevli, dekor, tema, otomasyon "auto_X").</summary>
        public List<string> seenItems = new();
        /// <summary>v1.9: bu seviyeye kadar açılan ürünler görülmüş sayılır (eski kayıtta yükseltme anındaki seviye; -1 = ayarlanmadı).</summary>
        public int seenLevel = -1;
        /// <summary>v2.1 Barlo'nun yetenek seviyeleri (0–5): koşu hızı, taşıma, servis menzili, para menzili (BarloSkills). Prestijte sıfırlanır.</summary>
        public int barloSpeed, barloCarry, barloServe, barloCoin;
        /// <summary>v2.1 son sahne ("Krala Davet") görüldü mü (Ending). Görüldükten sonra serbest oyun, "Efsanevi Han" rozeti.</summary>
        public bool endingSeen;
        /// <summary>v2.1: oyun sahnesinde geçen toplam gerçek süre (sn) ve biten toplam gün (prestijte sıfırlanmaz; game_complete ölçümü).</summary>
        public float playSeconds;
        public int daysTotal;
        /// <summary>v2.2 sezon: jetonların bağlı olduğu sezon anahtarı ("harvest-2026"; "" = sezon dışı), jeton ve sezon görevleri (Seasons).</summary>
        public string seasonKey = "";
        public int seasonTokens;
        public List<SeasonQuestSave> seasonQuests = new();
        /// <summary>v2.2: başlangıç duyurusunun gösterildiği sezon anahtarı (HUD bir kez).</summary>
        public string seasonSeenKey = "";
        /// <summary>v2.2: sona eren sezonun kullanılmamış jetonları (oyuncuya bir kez söylenir, sonra 0).</summary>
        public int seasonExpiredTokens;
        public string seasonExpiredKey = "";
        /// <summary>v2.2 kostümler: edinilen (jeton, ödül, Hazine) ve giyilen ("sahip=kostüm"). Prestijte sıfırlanmaz (Costumes).</summary>
        public List<string> costumes = new();
        public List<string> wornCostumes = new();
        /// <summary>v2.3 düzenleme modu: taşınmış masa ve dekorların yerleşimi ("anahtar|x|z|çeyrek", <see cref="Layout"/>). Prestijte silinmez.</summary>
        public List<string> layout = new();
        /// <summary>v2.3: yerleşim biçimi sürümü (0 = alan yok, eski kayıt → varsayılan düzen).</summary>
        public int layoutVersion;
        /// <summary>v2.3: düzenleme modunun ilk ipucu gösterildi mi.</summary>
        public bool editTipShown;
        /// <summary>v2.3 bulut kaydı: oyun boyunca kazanılan toplam altın (prestijte sıfırlanmaz; çakışma kuralı, <see cref="CloudSave"/>).</summary>
        public long totalEarned;
    }

    /// <summary>
    /// Oyunun tek kalıcı durumu: para, elmas, ün/seviye, istasyon seviyeleri, açılan alanlar.
    /// Değişiklikleri olaylarla bildirir; UI bu olaylara abone olur.
    /// </summary>
    public static class GameState
    {
        /// <summary>v1.5 ve öncesinin PlayerPrefs kayıt anahtarı (v1.6'da ilk açılışta dosyaya taşınır).</summary>
        public const string LegacyPrefsKey = "taverna_save_v1";
        /// <summary>Kayıt biçimi sürümü: 2 = v1.6 imzalı dosya.</summary>
        public const int CurrentSaveVersion = 4;   // v2.3: yerleşim (Layout) ve toplam kazanç; v2.0: han konforu 3 → 5 seviye (Inn.MigrateComfortV2)
        public static SaveData Data { get; private set; } = new();
        /// <summary>v2.3: kayıt diskten yüklendi ya da yeni oyun kuruldu (boş varsayılan kayıt buluta gönderilmesin).</summary>
        public static bool IsLoaded { get; private set; }

        /// <summary>Son açılışta kayıt nereden geldi (test günlüğü, bilgi mesajı).</summary>
        public static SaveLoadSource LoadSource { get; private set; }
        /// <summary>Oyuncuya bir kez gösterilecek kayıt bilgisi (yedekten açıldı / bozuk kayıt kenara alındı); HUD gösterip siler.</summary>
        public static string LoadNotice { get; set; }
        /// <summary>Ölçüm: kayıt sayısı, son kaydın karesi, ana iş parçacığındaki süresi (ms; JSON üretimi + kuyruğa alma).</summary>
        public static int SaveCount { get; private set; }
        public static int LastSaveFrame { get; private set; } = -1;
        public static double LastSaveMainMs { get; private set; }
        public static double MaxSaveMainMs { get; private set; }

        public static event Action Changed;
        public static event Action<int> LeveledUp;
        public static event Action<long, Vector3> CoinsGainedAt;
        public static event Action<DrinkId> DrinkServed;
        public static event Action<string> ZoneUnlocked;
        public static readonly string DefaultTavernName = Loc.N("Barlo'nun Tavernası");
        public static string TavernName => string.IsNullOrWhiteSpace(Data.tavernName) ? Loc.T(DefaultTavernName) : Data.tavernName;   // v1.7: varsayılan ad çevrilir, oyuncunun yazdığı çevrilmez

        public static void RaiseServed(DrinkId id) { DrinkServed?.Invoke(id); Changed?.Invoke(); }

        public static long Coins => Data.coins;
        public static int Level => Data.level;
        public static float LevelProgress01 => Mathf.Clamp01(Data.un / (float)Balance.UnForLevel(Data.level));

        /// <summary>Kayıt klasörü ve imza anahtarı (cihaz kimliğinden) hazır değilse kurar.</summary>
        static void EnsureStore()
        {
            if (SaveStore.Ready) return;
            SaveStore.Init(Application.persistentDataPath, SaveCodec.DeriveKey(SystemInfo.deviceUniqueIdentifier));
        }

        /// <summary>
        /// v1.6 kaydı yükler: save.dat → (imza tutmazsa) save.bak → (hiç dosya yoksa) eski PlayerPrefs kaydı taşınır → yeni oyun.
        /// İkisi de bozuksa dosyalar kenara alınır, oyuncuya <see cref="LoadNotice"/> ile bilgi verilir ve yeni oyun başlar
        /// (bozuk kayıt hiçbir zaman üzerine yazılmaz).
        /// </summary>
        public static void Load()
        {
            EnsureStore();
            var src = SaveStore.Read(out var json);
            SaveData d = src is SaveLoadSource.Main or SaveLoadSource.Backup ? Parse(json) : null;
            if (src == SaveLoadSource.None)
            {
                var legacy = PlayerPrefs.GetString(LegacyPrefsKey, "");
                d = string.IsNullOrEmpty(legacy) ? null : Parse(legacy);
                if (d != null)
                {
                    src = SaveLoadSource.Legacy;
                    Data = Upgrade(d);
                    SaveSync();
                    if (SaveStore.LastError == null) { PlayerPrefs.DeleteKey(LegacyPrefsKey); PlayerPrefs.Save(); }
                    Debug.Log($"[Kayıt] eski PlayerPrefs kaydı dosyaya taşındı: {SaveStore.MainPath}");
                }
            }
            if (src == SaveLoadSource.Backup)
            {
                LoadNotice = Loc.T("Kayıt dosyası bozuktu, bir önceki yedekten açıldı.");
                Debug.LogWarning("[Kayıt] save.dat imzası tutmadı, save.bak açıldı.");
            }
            if (src == SaveLoadSource.Corrupt)
            {
                LoadNotice = Loc.F("Kayıt dosyası okunamadı (bozuk). Eski dosya saklandı: {0}. Yeni oyun başladı.", SaveStore.LastCorruptName);
                Debug.LogWarning($"[Kayıt] save.dat ve save.bak imzası tutmadı, kenara alındı ({SaveStore.LastCorruptName}).");
            }
            LoadSource = src;
            Data = Upgrade(d ?? NewGame());
            IsLoaded = true;
            Changed?.Invoke();
        }

        /// <summary>v2.3 (bulut kaydı, birim testi): JSON'u okuyup güncel biçime yükseltir; okunamazsa null.</summary>
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var d = Parse(json);
            return d == null ? null : Upgrade(d);
        }

        /// <summary>v2.3: bellekteki kaydın JSON'u (bulut yedeği; diske yazmaz).</summary>
        public static string SnapshotJson() => Snapshot();

        /// <summary>v2.3 (buluttan yükle): bellekteki kaydın yerine geçer. Çağıran hemen <see cref="SaveSync"/> yapar.</summary>
        public static void ReplaceData(SaveData d)
        {
            if (d == null) return;
            Data = d;
            IsLoaded = true;
            Changed?.Invoke();
        }

        static SaveData Parse(string json)
        {
            try { return JsonUtility.FromJson<SaveData>(json); }
            catch (Exception e) { Debug.LogWarning("[Kayıt] JSON çözülemedi: " + e.Message); return null; }
        }

        /// <summary>Eski sürüm kaydını güncel biçime getirir (şimdilik yalnızca sürüm numarası; alan eklemeleri JsonUtility ile varsayılan alır).</summary>
        /// <summary>Han konforunun 5 seviyeye geçtiği kayıt sürümü.</summary>
        const int ComfortV2SaveVersion = 3;

        static SaveData Upgrade(SaveData d)
        {
            if (d.saveVersion < ComfortV2SaveVersion && d.roomLvs != null)
                for (int i = 0; i < d.roomLvs.Count; i++) d.roomLvs[i] = Inn.MigrateComfortV2(d.roomLvs[i]);
            if (d.saveVersion < CurrentSaveVersion) d.saveVersion = CurrentSaveVersion;
            if (d.seenLevel < 0) d.seenLevel = Mathf.Max(1, d.level);   // v1.9: eski kayıtta mevcut ürünler "YENİ!" çıkmasın
            Layout.Migrate(d);   // v2.3: yerleşimi olmayan eski kayıt varsayılan düzenle açılır
            return d;
        }

        static SaveData NewGame() => new SaveData { coins = 0, level = 1, saveVersion = CurrentSaveVersion, seenLevel = 1, layoutVersion = Layout.Version };

        static string Snapshot()
        {
            Data.lastSeenUtc = DateTime.UtcNow.ToString("o");
            Data.saveVersion = CurrentSaveVersion;
            return JsonUtility.ToJson(Data);
        }

        /// <summary>
        /// Kaydeder: JSON ana iş parçacığında üretilir, imzalama ve diske yazma arka planda (<see cref="SaveStore.WriteAsync"/>).
        /// Önemli olaylarda (satın alma, seviye, gün sonu, alan açma) ve 30 sn'de bir çağrılır.
        /// </summary>
        public static void Save()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            EnsureStore();
            SaveStore.WriteAsync(Snapshot());
            Measure(sw);
        }

        /// <summary>Hemen ve eşzamanlı kaydeder (uygulama arka plana geçerken / kapanırken).</summary>
        public static void SaveSync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            EnsureStore();
            SaveStore.WriteSync(Snapshot());
            Measure(sw);
        }

        static void Measure(System.Diagnostics.Stopwatch sw)
        {
            LastSaveMainMs = sw.Elapsed.TotalMilliseconds;
            if (LastSaveMainMs > MaxSaveMainMs) MaxSaveMainMs = LastSaveMainMs;
            SaveCount++;
            LastSaveFrame = Time.frameCount;
        }

        /// <summary>Kaydı siler ve yeni oyun açar (Ayarlar › Kaydı Sıfırla, otomatik test). Eski PlayerPrefs kaydı da silinir.</summary>
        public static void ResetSave()
        {
            EnsureStore();
            SaveStore.DeleteAll();
            PlayerPrefs.DeleteKey(LegacyPrefsKey);
            Data = NewGame();
            IsLoaded = true;
            Changed?.Invoke();
        }

        /// <summary>Test botu: verilen kaydı belleğe koyar (kayıt senaryosu sonunda oyunu eski hâline döndürmek için).</summary>
        public static void DebugReplace(SaveData d) { Data = d ?? NewGame(); Changed?.Invoke(); }

        public static void AddCoins(long amount, Vector3 worldPos)
        {
            Data.coins += amount;
            if (amount > 0) Data.totalEarned += amount;   // v2.3: bulut çakışma kuralı için toplam kazanç
            CoinsGainedAt?.Invoke(amount, worldPos);
            Changed?.Invoke();
        }

        /// <summary>Altın harcar; kasada yetmiyorsa (ya da borçluysa) false. Satın almalar ve ödeme kareleri bunu kullanır.</summary>
        public static bool TrySpend(long amount)
        {
            if (Data.coins < amount) return false;
            Data.coins -= amount;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// v1.9 (oyuncu testi madde 7): bakiye eksi mi? Borçluyken satın alma ve ödeme kareleri kapalı ("Önce borcu öde");
        /// kazanılan altın önce borcu kapatır.
        /// </summary>
        public static bool InDebt => Data.coins < 0;

        /// <summary>
        /// v1.9: zorunlu gider — bakiye yetmese de düşer, eksiye inebilir (gün sonu maaşı, borçluyken malzeme siparişi).
        /// Dönen değer: borca yazılan kısım (harcamadan önce kasada olmayan altın).
        /// </summary>
        public static long ForceSpend(long amount)
        {
            if (amount <= 0) return 0;
            long covered = System.Math.Max(0, System.Math.Min(amount, Data.coins));
            Data.coins -= amount;
            Changed?.Invoke();
            return amount - covered;
        }

        /// <summary>Ün kaybı (kızgın müşteri). Seviye düşmez, ün 0'ın altına inmez.</summary>
        public static void LoseUn(int amount)
        {
            Data.un = Mathf.Max(0, Data.un - amount);
            Changed?.Invoke();
        }

        public static void AddUn(int amount)
        {
            if (Data.level >= Balance.MaxLevel) return;
            Data.un += Prestige.ScaleUn(amount);   // v1.5: şube başına Ün +%15
            bool leveled = false;
            while (Data.level < Balance.MaxLevel && Data.un >= Balance.UnForLevel(Data.level))
            {
                Data.un -= Balance.UnForLevel(Data.level);
                Data.level++;
                Data.gems += Boosts.LevelGemReward(Data.level);
                leveled = true;
                Services.Track("level_up", ("seviye", Data.level), ("gun", Data.day), ("servis", Data.customersServed));
                LeveledUp?.Invoke(Data.level);
            }
            if (leveled) Save();   // v1.6: seviye önemli olay, hemen kaydet
            Changed?.Invoke();
        }

        public static StationSave Station(DrinkId id)
        {
            // v1.8: lambda yerine döngü — makineler her kare birkaç kez çağırıyor, her çağrı bir kapanış nesnesi ayırıyordu (GC)
            StationSave s = null;
            var list = Data.stations;
            for (int i = 0; i < list.Count; i++) if (list[i].id == id) { s = list[i]; break; }
            if (s == null) { s = new StationSave { id = id }; Data.stations.Add(s); }
            return s;
        }

        public static bool IsUnlocked(string id) => Data.unlocked.Contains(id);
        public static void Unlock(string id)
        {
            if (!Data.unlocked.Contains(id)) Data.unlocked.Add(id);
            Save();
            ZoneUnlocked?.Invoke(id);
            Changed?.Invoke();
        }

        public static void MarkSeen(int customerNumber)
        {
            if (!Data.seenCustomers.Contains(customerNumber)) Data.seenCustomers.Add(customerNumber);
        }

        public static long ZonePaid(string id)
        {
            int i = Data.zoneIds.IndexOf(id);
            return i >= 0 ? Data.zonePaid[i] : 0;
        }

        public static void SetZonePaid(string id, long paid)
        {
            int i = Data.zoneIds.IndexOf(id);
            if (i >= 0) Data.zonePaid[i] = paid;
            else { Data.zoneIds.Add(id); Data.zonePaid.Add(paid); }
        }

        public static void NotifyChanged() => Changed?.Invoke();
    }
}
