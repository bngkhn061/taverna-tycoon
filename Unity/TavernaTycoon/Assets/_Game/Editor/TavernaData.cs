using System.IO;
using UnityEditor;
using UnityEngine;

namespace Taverna.EditorTools
{
    /// <summary>
    /// İçecek ve müşteri tanımlarını tasarım belgelerinden (GAME_DESIGN.md §4, CHARACTERS.md §B) üretir.
    /// Bu tablolar belgelerle aynı kalmalı; denge değişirse önce belge, sonra burası.
    /// </summary>
    public static class TavernaData
    {
        const string DataDir = TavernaImport.Root + "/Data";

        // id, dosya anahtarı, ad, fiyat, süre, açılış seviyesi, yükseltme çarpanı
        static readonly (DrinkId id, string key, string name, int price, float time, int unlock, float mult)[] Drinks =
        {
            (DrinkId.Bira, "Bira", "Bira", 3, 2f, 1, 1f),
            (DrinkId.Sira, "Sira", "Şıra", 4, 3f, 1, 1f),
            (DrinkId.BalSarabi, "BalSarabi", "Bal Şarabı", 7, 4f, 3, 1f),
            (DrinkId.Sarap, "Sarap", "Kırmızı Şarap", 10, 5f, 5, 4f),
            (DrinkId.Sut, "Sut", "Süt", 6, 2f, 6, 1f),
            (DrinkId.KaraBira, "KaraBira", "Kara Bira", 14, 6f, 8, 4f),
            (DrinkId.Cay, "Cay", "Bitki Çayı", 12, 4f, 10, 4f),
            (DrinkId.Grog, "Grog", "Goblin Grogu", 22, 6f, 14, 10f),
            (DrinkId.ElfCiyi, "ElfCiyi", "Elf Çiyi", 35, 7f, 18, 10f),
            (DrinkId.EjderAtesi, "EjderAtesi", "Ejder Ateşi", 60, 9f, 24, 10f),
            // v0.7 mutfak: yemekler (isFood) — fiyat yüksek, pişme uzun
            (DrinkId.Ekmek, "Ekmek", "Sıcak Ekmek", 9, 7f, 6, 1f),
            (DrinkId.Guvec, "Guvec", "Güveç", 18, 10f, 9, 4f),
            (DrinkId.KozEt, "KozEt", "Köz Et", 30, 12f, 12, 4f),
            (DrinkId.Turta, "Turta", "Elmalı Turta", 45, 14f, 15, 10f),
            // v1.0 yeni bölümler: Şarap Mahzeni (Sv17–18) ve Çatı Terası (Sv20) — pahalı, yavaş
            (DrinkId.YillanmisSarap, "YillanmisSarap", "Yıllanmış Şarap", 80, 12f, 17, 10f),
            (DrinkId.BalLikoru, "BalLikoru", "Bal Likörü", 55, 9f, 18, 10f),
            (DrinkId.GunBatimiPuncu, "GunBatimiPuncu", "Gün Batımı Punçu", 70, 10f, 20, 10f),
            // v1.0 İskele (Sv26): yemek
            (DrinkId.IzgaraBalik, "IzgaraBalik", "Izgara Balık", 50, 11f, 26, 10f),
            // v1.5 oyun sonu: en pahalı içecekten (Yıllanmış Şarap 80 / 12 sn) ve yemekten (Balık 50, Turta 14 sn) bir kademe yukarı
            (DrinkId.KralSerbeti, "KralSerbeti", "Kral Şerbeti", 95, 13f, 27, 10f),
            (DrinkId.SolenTabagi, "SolenTabagi", "Şölen Tabağı", 110, 15f, 29, 10f),
        };

        public static readonly System.Collections.Generic.Dictionary<DrinkId, string> StationKey = new()
        {
            { DrinkId.Bira, "Bira" }, { DrinkId.Sira, "Pres" }, { DrinkId.BalSarabi, "Bal" }, { DrinkId.Sarap, "Sarap" },
            { DrinkId.Sut, "Sut" }, { DrinkId.KaraBira, "KaraBira" }, { DrinkId.Cay, "Cay" }, { DrinkId.Grog, "Grog" },
            { DrinkId.ElfCiyi, "Kristal" }, { DrinkId.EjderAtesi, "Ejder" },
            { DrinkId.Ekmek, "Firin" }, { DrinkId.Guvec, "Kazan" }, { DrinkId.KozEt, "Mangal" }, { DrinkId.Turta, "Tatli" },
            { DrinkId.YillanmisSarap, "Yillanmis" }, { DrinkId.BalLikoru, "Likor" }, { DrinkId.GunBatimiPuncu, "Punc" }, { DrinkId.IzgaraBalik, "Balik" },
            { DrinkId.KralSerbeti, "Serbet" }, { DrinkId.SolenTabagi, "Solen" },
        };

        static readonly DrinkId[] AllDrinks =
            { DrinkId.Bira, DrinkId.Sira, DrinkId.BalSarabi, DrinkId.Sarap, DrinkId.Sut, DrinkId.KaraBira, DrinkId.Cay, DrinkId.Grog, DrinkId.ElfCiyi, DrinkId.EjderAtesi, DrinkId.YillanmisSarap, DrinkId.BalLikoru };

        /// <summary>v1.5: Gizli Kral her içeceği ister, Kral Şerbeti dahil.</summary>
        static readonly DrinkId[] KingDrinks = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Append(AllDrinks, DrinkId.KralSerbeti));

        // no, dosya anahtarı, ad, içecekler, sabır★, ödeme💰, açılış, sipariş sayısı
        static readonly (int no, string key, string name, DrinkId[] drinks, int pat, int pay, int unlock, int orders)[] Customers =
        {
            (1, "Ciftci", "Çiftçi", new[] { DrinkId.Bira }, 4, 1, 1, 1),
            (2, "Balikci", "Balıkçı", new[] { DrinkId.Sira }, 4, 1, 1, 1),
            (3, "Oduncu", "Oduncu", new[] { DrinkId.Bira }, 3, 2, 2, 1),
            (4, "Ozan", "Ozan", new[] { DrinkId.BalSarabi }, 4, 1, 3, 1),
            (5, "Firinci", "Fırıncı", new[] { DrinkId.Sira }, 3, 2, 4, 1),
            (6, "CobanKiz", "Çoban Kız", new[] { DrinkId.Sut }, 5, 1, 6, 1),
            (7, "Kunduraci", "Kunduracı", new[] { DrinkId.Bira }, 4, 1, 7, 1),
            (8, "Demirci", "Demirci", new[] { DrinkId.KaraBira }, 3, 2, 8, 1),
            (9, "Tuccar", "Tüccar", new[] { DrinkId.Sarap }, 2, 4, 5, 1),
            (10, "Muhafiz", "Muhafız", new[] { DrinkId.Bira }, 3, 2, 9, 1),
            (11, "Kesis", "Keşiş", new[] { DrinkId.Cay }, 5, 2, 10, 1),
            (12, "CuceMadenci", "Cüce Madenci", new[] { DrinkId.BalSarabi, DrinkId.BalLikoru }, 3, 3, 11, 2),
            (13, "Bucukluk", "Buçukluk", new[] { DrinkId.Sira, DrinkId.Sut }, 4, 2, 12, 2),
            (14, "VergiMemuru", "Vergi Memuru", new[] { DrinkId.Sarap, DrinkId.YillanmisSarap }, 1, 0, 13, 1),
            (15, "Yaver", "Yaver", new[] { DrinkId.Sut }, 4, 1, 9, 1),
            (16, "Sovalye", "Şövalye", new[] { DrinkId.BalSarabi }, 3, 3, 14, 1),
            (17, "Goblin", "Goblin", new[] { DrinkId.Grog }, 2, 2, 14, 1),
            (18, "Ork", "Ork Savaşçı", new[] { DrinkId.KaraBira }, 2, 3, 15, 2),
            (19, "ElfOkcu", "Elf Okçu", new[] { DrinkId.ElfCiyi, DrinkId.BalLikoru }, 4, 3, 18, 1),
            (20, "Buyucu", "Büyücü", AllDrinks, 3, 4, 16, 1),
            (21, "Cadi", "Cadı", new[] { DrinkId.Cay }, 3, 3, 17, 1),
            (22, "Simyaci", "Simyacı", new[] { DrinkId.ElfCiyi }, 3, 3, 19, 1),
            (23, "Avci", "Avcı", new[] { DrinkId.BalSarabi }, 4, 2, 15, 1),
            (24, "Kervanci", "Kervancı", new[] { DrinkId.Cay, DrinkId.GunBatimiPuncu }, 4, 3, 20, 1),
            (25, "KorsanKaptan", "Korsan Kaptan", new[] { DrinkId.Bira, DrinkId.Grog, DrinkId.GunBatimiPuncu, DrinkId.IzgaraBalik }, 2, 4, 21, 1),
            (26, "Trol", "Trol", new[] { DrinkId.Grog }, 4, 4, 22, 3),
            (27, "EjderhaAvcisi", "Ejderha Avcısı", new[] { DrinkId.EjderAtesi, DrinkId.KralSerbeti }, 3, 5, 24, 1),
            (28, "VampirKont", "Vampir Kont", new[] { DrinkId.Sarap, DrinkId.YillanmisSarap, DrinkId.KralSerbeti }, 3, 5, 25, 1),
            (29, "HayaletSovalye", "Hayalet Şövalye", new[] { DrinkId.Sarap }, 5, 4, 26, 1),
            (30, "KilikKral", "Gizli Kral", KingDrinks, 2, 5, 28, 1),
        };

        public static GameDatabase Build()
        {
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..", DataDir, "Drinks")));
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..", DataDir, "Customers")));
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..", TavernaImport.Root, "Resources")));

            var drinks = new DrinkDef[Drinks.Length];
            for (int i = 0; i < Drinks.Length; i++)
            {
                var d = Drinks[i];
                var a = LoadOrCreate<DrinkDef>($"{DataDir}/Drinks/{(int)d.id:00}_{d.key}.asset");
                a.id = d.id; a.displayName = d.name; a.basePrice = d.price; a.baseTime = d.time;
                a.unlockLevel = d.unlock; a.upgradeCostMultiplier = d.mult;
                a.icon = TavernaImport.Icon(d.key);
                a.cupPrefab = TavernaImport.Prefab($"PR_Drink_{d.key}");
                // Yemekler: mutfak (Ekmek..Turta) + iskele balığı. (v1.0 düzeltme: mahzen/teras içecekleri yemek sayılıyordu)
                // v1.5: Şölen Tabağı (mutfak, Sv29) de yemek
                a.isFood = (d.id >= DrinkId.Ekmek && d.id <= DrinkId.Turta) || d.id == DrinkId.IzgaraBalik || d.id == DrinkId.SolenTabagi;
                EditorUtility.SetDirty(a);
                drinks[i] = a;
            }

            var customers = new CustomerDef[Customers.Length];
            for (int i = 0; i < Customers.Length; i++)
            {
                var c = Customers[i];
                var a = LoadOrCreate<CustomerDef>($"{DataDir}/Customers/{c.no:00}_{c.key}.asset");
                a.number = c.no; a.displayName = c.name; a.drinks = c.drinks; a.patienceStars = c.pat;
                a.payStars = c.pay; a.unlockLevel = c.unlock; a.ordersPerVisit = c.orders;
                a.spawnWeight = c.no == 30 ? 0.05f : c.unlock >= 24 ? 0.3f : 1f;
                a.modelPrefab = TavernaImport.Prefab($"CH_Cust_{c.no:00}_{c.key}");
                EditorUtility.SetDirty(a);
                customers[i] = a;
            }

            var db = LoadOrCreate<GameDatabase>($"{TavernaImport.Root}/Resources/GameDatabase.asset");
            db.drinks = drinks;
            db.customers = customers;
            db.coinPrefab = TavernaImport.Prefab("PR_Drink_Para");
            db.coinIcon = TavernaImport.Icon("Para");
            db.gemIcon = TavernaImport.Icon("Elmas");
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            return db;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }
    }
}
