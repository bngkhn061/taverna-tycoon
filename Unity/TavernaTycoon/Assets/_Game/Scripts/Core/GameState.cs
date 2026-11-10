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
        public string floorTheme = "", wallTheme = "";   // Themes (boş = varsayılan)
        public List<int> roomLvs = new();                 // han odalarının konfor kademesi (Inn)
        public string tavernName = "";                    // v0.9: tabela (boşsa ilk "OYNA"da sorulur)
        public bool tutorialDone;                         // v0.9: rehber görüldü / atlandı
        public int renameAds;                             // v0.9: ad değiştirmek için izlenen reklam sayacı
        public List<string> autos = new();                // v0.9: alınan otomasyonlar (Automation)
        public int ustaGelir, ustaMusteri;                // v0.9: Usta geliştirme kademeleri
        public int sulkyDay;
        public int tier;                                  // v1.0 "Tavernayı Büyüt": 0 Ahşap, 1 Taş Han, 2 Saray Hanı
        public List<string> premium = new();              // v1.0 Hazine: alınan gerçek para ürünleri (tek seferlik)
        public string vipUntilUtc = "", vipGemDate = "", starterShownUtc = "";                              // v1.0: maaşı ödenmeyen personelin küskün olduğu gün
        public string lastSeenUtc;
    }

    /// <summary>
    /// Oyunun tek kalıcı durumu: para, elmas, ün/seviye, istasyon seviyeleri, açılan alanlar.
    /// Değişiklikleri olaylarla bildirir; UI bu olaylara abone olur.
    /// </summary>
    public static class GameState
    {
        const string Key = "taverna_save_v1";
        public static SaveData Data { get; private set; } = new();

        public static event Action Changed;
        public static event Action<int> LeveledUp;
        public static event Action<long, Vector3> CoinsGainedAt;
        public static event Action<DrinkId> DrinkServed;
        public static event Action<string> ZoneUnlocked;
        public const string DefaultTavernName = "Barlo'nun Tavernası";
        public static string TavernName => string.IsNullOrWhiteSpace(Data.tavernName) ? DefaultTavernName : Data.tavernName;

        public static void RaiseServed(DrinkId id) { DrinkServed?.Invoke(id); Changed?.Invoke(); }

        public static long Coins => Data.coins;
        public static int Level => Data.level;
        public static float LevelProgress01 => Mathf.Clamp01(Data.un / (float)Balance.UnForLevel(Data.level));

        public static void Load()
        {
            var json = PlayerPrefs.GetString(Key, "");
            Data = string.IsNullOrEmpty(json) ? NewGame() : JsonUtility.FromJson<SaveData>(json) ?? NewGame();
            Changed?.Invoke();
        }

        static SaveData NewGame() => new SaveData { coins = 0, level = 1 };

        public static void Save()
        {
            Data.lastSeenUtc = DateTime.UtcNow.ToString("o");
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public static void ResetSave()
        {
            PlayerPrefs.DeleteKey(Key);
            Data = NewGame();
            Changed?.Invoke();
        }

        public static void AddCoins(long amount, Vector3 worldPos)
        {
            Data.coins += amount;
            CoinsGainedAt?.Invoke(amount, worldPos);
            Changed?.Invoke();
        }

        public static bool TrySpend(long amount)
        {
            if (Data.coins < amount) return false;
            Data.coins -= amount;
            Changed?.Invoke();
            return true;
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
            Data.un += amount;
            while (Data.level < Balance.MaxLevel && Data.un >= Balance.UnForLevel(Data.level))
            {
                Data.un -= Balance.UnForLevel(Data.level);
                Data.level++;
                Data.gems += Boosts.LevelGemReward(Data.level);
                LeveledUp?.Invoke(Data.level);
            }
            Changed?.Invoke();
        }

        public static StationSave Station(DrinkId id)
        {
            var s = Data.stations.Find(x => x.id == id);
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
