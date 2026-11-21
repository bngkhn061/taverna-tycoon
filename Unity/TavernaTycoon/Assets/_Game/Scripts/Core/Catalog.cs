namespace Taverna
{
    /// <summary>
    /// Mağaza kataloğu (GAME_DESIGN.md §3.2.4): makineler, personel, dekor.
    /// Fiyat/seviye/maaş değişirse önce belge, sonra burası.
    /// </summary>
    public static class Catalog
    {
        // ------------------------------------------------------------------ makineler
        public struct Machine
        {
            public DrinkId drink;
            public long price;
            public string zone;       // "" = Meyhane, "salon", "bahce"
            public string zoneName;
        }

        /// <summary>Açılış seviyesi içeceğin kendi unlockLevel'ından gelir (DrinkDef).</summary>
        public static readonly Machine[] Machines =
        {
            new() { drink = DrinkId.Bira, price = 0, zone = "" },
            new() { drink = DrinkId.Sira, price = 40, zone = "" },
            new() { drink = DrinkId.BalSarabi, price = 220, zone = "" },
            new() { drink = DrinkId.Sarap, price = 600, zone = "salon", zoneName = Loc.N("Ana Salon") },
            new() { drink = DrinkId.Sut, price = 650, zone = "salon", zoneName = Loc.N("Ana Salon") },
            new() { drink = DrinkId.KaraBira, price = 1500, zone = "salon", zoneName = Loc.N("Ana Salon") },
            new() { drink = DrinkId.Cay, price = 2000, zone = "bahce", zoneName = Loc.N("Bahçe") },
            new() { drink = DrinkId.ElfCiyi, price = 6500, zone = "salon", zoneName = Loc.N("Ana Salon") },
            new() { drink = DrinkId.Grog, price = 4400, zone = "bahce", zoneName = Loc.N("Bahçe") },
            new() { drink = DrinkId.EjderAtesi, price = 12000, zone = "bahce", zoneName = Loc.N("Bahçe") },
            // v0.7 mutfak makineleri
            new() { drink = DrinkId.Ekmek, price = 500, zone = "mutfak", zoneName = Loc.N("Mutfak") },
            new() { drink = DrinkId.Guvec, price = 1300, zone = "mutfak", zoneName = Loc.N("Mutfak") },
            new() { drink = DrinkId.KozEt, price = 2600, zone = "mutfak", zoneName = Loc.N("Mutfak") },
            new() { drink = DrinkId.Turta, price = 4200, zone = "mutfak", zoneName = Loc.N("Mutfak") },
            new() { drink = DrinkId.YillanmisSarap, price = 9000, zone = "mahzen", zoneName = Loc.N("Şarap Mahzeni") },
            new() { drink = DrinkId.BalLikoru, price = 7000, zone = "mahzen", zoneName = Loc.N("Şarap Mahzeni") },
            new() { drink = DrinkId.GunBatimiPuncu, price = 11000, zone = "teras", zoneName = Loc.N("Çatı Terası") },
            new() { drink = DrinkId.IzgaraBalik, price = 12000, zone = "iskele", zoneName = Loc.N("İskele") },
            // v1.5 oyun sonu makineleri: en pahalı makineden (12.000) bir kademe yukarı
            new() { drink = DrinkId.KralSerbeti, price = 14000, zone = "salon", zoneName = Loc.N("Ana Salon") },
            new() { drink = DrinkId.SolenTabagi, price = 16000, zone = "mutfak", zoneName = Loc.N("Mutfak") },
        };

        public static string MachineKey(DrinkId d) => "mac_" + d;

        // ------------------------------------------------------------------ personel
        public enum Role { Garson, Barmen, Temizlikci, Ozan, Fenerci, Asci, Hizmetci }

        public struct Staff
        {
            public string id, name, roleName, icon, desc;
            public Role role;
            public int level;
            public long hire, wage;
            /// <summary>v1.0: sadece Hazine'den (gerçek para) alınır, maaşı yok.</summary>
            public bool premium;
            /// <summary>v1.1: çalışacağı bölge ("" = her yer). Bölge açılmadan işe alınamaz; Hazine'den alınmışsa bölge açılınca gelir.</summary>
            public string zone, zoneName;
        }

        public static readonly Staff[] StaffList =
        {
            new() { id = "staff_nana", name = "Nana", role = Role.Temizlikci, roleName = Loc.N("Temizlikçi"), icon = "Staff_Temizlikci", level = 2, hire = 80, wage = 12, desc = Loc.N("Kirli masaları temizler") },
            new() { id = "staff_pip", name = "Pip", role = Role.Garson, roleName = Loc.N("Garson"), icon = "Main_Pip", level = 3, hire = 150, wage = 18, desc = Loc.N("Sipariş taşır, para toplar") },
            new() { id = "staff_gus", name = "Gus", role = Role.Barmen, roleName = Loc.N("Barmen"), icon = "Staff_Barmen", level = 5, hire = 500, wage = 45, desc = Loc.N("Tüm makineler +%40 hız") },
            new() { id = "staff_mina", name = "Mina", role = Role.Garson, roleName = Loc.N("Garson"), icon = "Staff_GarsonMina", level = 6, hire = 650, wage = 40, desc = Loc.N("Sipariş taşır, para toplar"), zone = "salon", zoneName = Loc.N("Ana Salon") },
            new() { id = "staff_tobi", name = "Tobi", role = Role.Garson, roleName = Loc.N("Garson"), icon = "Staff_GarsonTobi", level = 8, hire = 1300, wage = 70, desc = Loc.N("Hızlı garson, 2 bardak taşır"), zone = "salon", zoneName = Loc.N("Ana Salon") },
            new() { id = "staff_ayla", name = "Ayla", role = Role.Asci, roleName = Loc.N("Aşçı"), icon = "Staff_Asci", level = 7, hire = 800, wage = 45, desc = Loc.N("Yemekler +%40 hızlı pişer"), zone = "mutfak", zoneName = Loc.N("Mutfak") },
            new() { id = "staff_brak", name = "Brak", role = Role.Fenerci, roleName = Loc.N("Fenerci"), icon = "Staff_Fedai", level = 7, hire = 900, wage = 50, desc = Loc.N("Akşam fenerleri yakar") },
            new() { id = "staff_sema", name = "Sema", role = Role.Hizmetci, roleName = Loc.N("Hizmetçi"), icon = "Staff_Hizmetci", level = 11, hire = 1400, wage = 60, desc = Loc.N("Han odalarını toplar"), zone = "merdiven", zoneName = Loc.N("Han") },
            new() { id = "staff_lirael", name = "Lirael", role = Role.Garson, roleName = Loc.N("Elf Garson"), icon = "Staff_Lirael", level = 1, hire = 0, wage = 0, desc = Loc.N("Çok hızlı · 4 bardak"), premium = true },
            new() { id = "staff_ignis", name = "Ignis", role = Role.Asci, roleName = Loc.N("Ejder Aşçı"), icon = "Staff_Ignis", level = 1, hire = 0, wage = 0, desc = Loc.N("Yemekler ×2 hızlı"), premium = true, zone = "mutfak", zoneName = Loc.N("Mutfak") },
            new() { id = "staff_lir", name = "Lir", role = Role.Ozan, roleName = Loc.N("Ozan"), icon = "Staff_OzanSahne", level = 9, hire = 1500, wage = 80, desc = Loc.N("Herkesin sabrı +%25") },
        };

        // ------------------------------------------------------------------ dekor
        public struct Decor
        {
            public string id, name, icon, prefab;
            public int level;
            public long price;
            public float ambiance;
            /// <summary>v1.0: sadece Hazine'den (gerçek para) alınır.</summary>
            public bool premium;
        }

        /// v1.8: flama 120→80, fıçı 150→80, kalkan 220→150, boynuz 260→170, avize 420→250, mühür 3.000→1.500 (geri ödeme tahmini 20–33 gündü).
        public static readonly Decor[] DecorList =
        {
            new() { id = "dec_saksi", name = Loc.N("Saksı"), icon = "DEC_Saksi", prefab = "PR_Decor_Saksi", level = 1, price = 40, ambiance = 2 },
            new() { id = "dec_mum", name = Loc.N("Mumluk"), icon = "DEC_Mum", prefab = "PR_Decor_Mum", level = 1, price = 30, ambiance = 2 },
            new() { id = "dec_hali", name = Loc.N("Halı"), icon = "DEC_Hali", prefab = "PR_Decor_Hali", level = 2, price = 90, ambiance = 4 },
            new() { id = "dec_flama", name = Loc.N("Flama"), icon = "DEC_Flama", prefab = "PR_Decor_Flama", level = 3, price = 80, ambiance = 4 },
            new() { id = "dec_fici", name = Loc.N("Fıçı Yığını"), icon = "DEC_FiciYigini", prefab = "PR_Decor_FiciYigini", level = 3, price = 80, ambiance = 3 },
            new() { id = "dec_kalkan", name = Loc.N("Kalkan & Kılıç"), icon = "DEC_Kalkan", prefab = "PR_Decor_Kalkan", level = 4, price = 150, ambiance = 5 },
            new() { id = "dec_boynuz", name = Loc.N("Geyik Boynuzu"), icon = "DEC_Boynuz", prefab = "PR_Decor_Boynuz", level = 5, price = 170, ambiance = 5 },
            new() { id = "dec_avize", name = Loc.N("Avize"), icon = "DEC_Avize", prefab = "PR_Decor_Avize", level = 6, price = 250, ambiance = 8 },
            new() { id = "dec_kristal", name = Loc.N("Kristal Avize"), icon = "DEC_KristalAvize", prefab = "PR_Decor_KristalAvize", level = 1, price = 0, ambiance = 25, premium = true },
            new() { id = "dec_ejder", name = Loc.N("Ejder Heykeli"), icon = "DEC_EjderHeykeli", prefab = "PR_Decor_EjderHeykeli", level = 1, price = 0, ambiance = 30, premium = true },
            new() { id = "dec_altinfici", name = Loc.N("Altın Fıçı"), icon = "DEC_AltinFici", prefab = "PR_Decor_AltinFici", level = 1, price = 0, ambiance = 8, premium = true },
            new() { id = "dec_muhur", name = Loc.N("Kraliyet Mührü"), icon = "DEC_Muhur", prefab = "PR_Decor_Muhur", level = 12, price = 1500, ambiance = 15 },
            // v2.0 (oyuncu testi madde 21): bahçe dekoru — taş patika (ambiyans) ve gece ışık veren bahçe feneri (yakılması gerekmez)
            new() { id = "dec_tasyol", name = Loc.N("Taş Yol"), icon = "DEC_TasYol", prefab = "PR_Decor_TasYol", level = 8, price = 120, ambiance = 3 },
            new() { id = "dec_bahcefener", name = Loc.N("Bahçe Feneri"), icon = "DEC_BahceFeneri", prefab = "PR_Decor_BahceFeneri", level = 9, price = 300, ambiance = 6 },
        };

        public static Decor FindDecor(string id) => System.Array.Find(DecorList, d => d.id == id);
    }
}
