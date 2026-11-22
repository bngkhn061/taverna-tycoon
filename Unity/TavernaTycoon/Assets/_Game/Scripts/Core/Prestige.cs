using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Taverna
{
    /// <summary>
    /// v1.5 Prestij "Yeni Şube" (GAME_DESIGN.md §9): Sv30 + Saray Hanı + 150.000 altın olunca taverna sıfırdan
    /// yeni bir şubede başlar. Kalıcı kazanım: şube başına gelir +%25 ve Ün +%35 (v1.8); yeni şube 500 × şube no altınla açılır.
    /// Elmas, Hazine ürünleri, koleksiyon, ad, ayarlar, rehber bilgisi, reklam sayaçları ve kozmetikler kalır.
    /// Harita aynıdır (yeni şube haritaları lansman sonrası).
    /// </summary>
    public static class Prestige
    {
        /// <summary>Prestij şartı: seviye (en üst seviye).</summary>
        public const int RequiredLevel = Balance.MaxLevel;
        /// <summary>Prestij şartı: kasadaki altın (harcanır, sonra zaten sıfırlanır).</summary>
        public const long RequiredCoins = 150000;
        /// <summary>Şube başına kalıcı gelir artışı (Boosts.IncomeMultiplier zincirinde).</summary>
        public const float IncomePerBranch = 0.25f;
        /// <summary>Şube başına Ün kazanımı artışı.</summary>
        /// v1.8: 0.15 → 0.35 (hedef: ikinci şube toplam sürede %25–35 kısa; içerik atlanmaz, yalnızca Ün hızlanır).
        public const float UnPerBranch = 0.35f;
        /// <summary>Yeni şubenin başlangıç altını = bu × şube numarası.</summary>
        public const long StartCoinsPerBranch = 500;

        static readonly string[] Names = { Loc.N("Köy Meyhanesi"), Loc.N("Liman Kenti"), Loc.N("Dağ Geçidi") };

        /// <summary>Barlo "Tavernayı Büyüt" karesine bastı ve kademeler bitmiş (HUD şube kartını açar).</summary>
        public static event Action ZoneEntered;

        /// <summary>Prestijten sonra sahne yeni yüklendi (HUD şube şeridini gösterir, bir kez).</summary>
        public static bool JustBranched;

        /// <summary>Şube sayısı (0 = ilk taverna).</summary>
        public static int Branch => Mathf.Max(0, GameState.Data.branch);

        /// <summary>Şube adı: 0 Köy Meyhanesi, 1 Liman Kenti, 2 Dağ Geçidi, sonrası "N. Şube".</summary>
        public static string BranchName(int b) => b >= 0 && b < Names.Length ? Loc.T(Names[b]) : Loc.F("{0}. Şube", b);
        public static string CurrentName => BranchName(Branch);

        /// <summary>Kalıcı gelir çarpanı: 1 + 0.25 × şube.</summary>
        public static float IncomeMultiplier => 1f + IncomePerBranch * Branch;
        public static float IncomeMultiplierFor(int b) => 1f + IncomePerBranch * b;
        /// <summary>Ün kazanım çarpanı: 1 + 0.35 × şube (v1.8).</summary>
        public static float UnMultiplier => 1f + UnPerBranch * Branch;
        public static float UnMultiplierFor(int b) => 1f + UnPerBranch * b;
        public static long StartCoins(int b) => StartCoinsPerBranch * b;

        static float _unCarry;

        /// <summary>Ün kazancını şube çarpanıyla ölçekler (küsurat sonraki kazanca taşınır).</summary>
        public static int ScaleUn(int amount)
        {
            if (amount <= 0 || Branch == 0) return amount;
            float v = amount * UnMultiplier + _unCarry;
            int n = Mathf.FloorToInt(v);
            _unCarry = v - n;
            return n;
        }

        /// <summary>Hedef karesi ve hedef kutusu: Sv30'a gelindi (şartların kalanı kartta).</summary>
        public static bool Visible => GameState.Level >= RequiredLevel;

        static bool TopTier => Tiers.Next == null;

        public static List<(string label, bool ok)> Requirements()
        {
            var top = Tiers.List[Tiers.List.Length - 1];
            return new List<(string, bool)>
            {
                (Loc.F("Seviye {0}", RequiredLevel), GameState.Level >= RequiredLevel),
                (TopTier ? Loc.F("{0} kademesi", Loc.T(top.name)) : Loc.F("{0} kademesi gerekli", Loc.T(top.name)), TopTier),
                (Loc.F("Altın {0} / {1}", HudController.FormatNum(Math.Min(GameState.Coins, RequiredCoins)), HudController.FormatNum(RequiredCoins)), GameState.Coins >= RequiredCoins),
            };
        }

        public static bool CanPrestige => GameState.Level >= RequiredLevel && TopTier && GameState.Coins >= RequiredCoins;

        public static void RaiseZoneEntered() => ZoneEntered?.Invoke();

        /// <summary>Kayıt üzerinde sıfırlama (sahneyi yüklemez; test edilebilir). Şart tutmazsa false.</summary>
        public static bool Apply()
        {
            if (!CanPrestige) return false;
            var d = GameState.Data;
            int b = Branch + 1;
            ResetForBranch(d, b);
            _unCarry = 0f;
            TavernEvents.StopAll();
            Services.Track("prestige", ("sube", b), ("gelir_carpani", IncomeMultiplierFor(b)));
            GameState.Save();
            return true;
        }

        /// <summary>
        /// v2.4 (birim testi): kaydı yeni şube için sıfırlar (yalnızca veri; sahne, ölçüm ve kayıt yok). Kalanlar aşağıdaki listede;
        /// v2.4: giriş sineması işareti (openingSeen) ve kralın hediyesi (royalDecree) de kalır.
        /// </summary>
        public static void ResetForBranch(SaveData d, int b)
        {
            d.branch = b;
            // --- sıfırlanan
            d.coins = StartCoins(b);
            d.level = 1;
            d.un = 0;
            d.tier = 0;
            d.day = 1;
            d.happiness = Happiness.Start;
            d.eventDay = 0;
            d.eventsUsed.Clear();
            d.stations.Clear();              // makine yükseltmeleri ve depo
            d.deliveryLv = 0;
            d.autoOrder = false;
            d.autos.Clear();                 // otomasyon
            d.ustaGelir = 0; d.ustaMusteri = 0;
            d.roomLvs.Clear();               // han odası konforu
            d.zoneIds.Clear(); d.zonePaid.Clear();
            d.sulkyDay = 0;
            d.quests.Clear(); d.questDay = "";   // görevler yeni şubenin seviyesine göre yeniden üretilir
            BarloSkills.ResetAll(d);         // v2.1: Barlo'nun yetenekleri de sıfırlanır (şube çarpanı yeniden almayı hızlandırır)
            // Personel seviyeleri: normal görevlilerinki sıfırlanır; Hazine görevlilerininki kalır
            for (int i = d.staffIds.Count - 1; i >= 0; i--)
                if (!IsPremiumStaff(d.staffIds[i])) { d.staffIds.RemoveAt(i); if (i < d.staffLvs.Count) d.staffLvs.RemoveAt(i); }
            // Açılanlar: alanlar, masalar, makineler, işe alımlar gider; temalar (thm_) ve Hazine görevlileri kalır
            d.unlocked.RemoveAll(id => !(id.StartsWith("thm_") || IsPremiumStaff(id)));
            // --- kalanlar: gems, premium, benched, vip*, seenCustomers, tavernName, tutorialDone, eventsSeen,
            //     renameAds, staffAds, decor (bölgesi açılınca görünür), floorTheme/wallTheme, lastDaily/dailyStreak, customersServed,
            //     v2.2: costumes / wornCostumes (giyilen kostüm) ve sezon jetonu / görevleri (sezona bağlı, şubeye değil)
            //     v2.4: openingSeen, royalDecree (Kraliyet Fermanı ve +%5 gelir)
        }

        /// <summary>Şubeyi aç: kaydı sıfırla ve oyun sahnesini yeniden yükle (rehber tekrar başlamaz).</summary>
        public static bool Execute()
        {
            if (!Apply()) return false;
            JustBranched = true;
            PlayerController.JoystickInput = Vector2.zero;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return true;
        }

        static bool IsPremiumStaff(string id) => Array.Exists(Catalog.StaffList, s => s.premium && s.id == id);
    }
}
