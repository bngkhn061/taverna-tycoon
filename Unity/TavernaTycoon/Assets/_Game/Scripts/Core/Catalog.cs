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
            new() { drink = DrinkId.Sarap, price = 600, zone = "salon", zoneName = "Ana Salon" },
            new() { drink = DrinkId.Sut, price = 650, zone = "salon", zoneName = "Ana Salon" },
            new() { drink = DrinkId.KaraBira, price = 1500, zone = "salon", zoneName = "Ana Salon" },
            new() { drink = DrinkId.Cay, price = 2000, zone = "bahce", zoneName = "Bahçe" },
            new() { drink = DrinkId.ElfCiyi, price = 6500, zone = "salon", zoneName = "Ana Salon" },
            new() { drink = DrinkId.Grog, price = 4400, zone = "bahce", zoneName = "Bahçe" },
            new() { drink = DrinkId.EjderAtesi, price = 12000, zone = "bahce", zoneName = "Bahçe" },
            // v0.7 mutfak makineleri
            new() { drink = DrinkId.Ekmek, price = 500, zone = "mutfak", zoneName = "Mutfak" },
            new() { drink = DrinkId.Guvec, price = 1300, zone = "mutfak", zoneName = "Mutfak" },
            new() { drink = DrinkId.KozEt, price = 2600, zone = "mutfak", zoneName = "Mutfak" },
            new() { drink = DrinkId.Turta, price = 4200, zone = "mutfak", zoneName = "Mutfak" },
            new() { drink = DrinkId.YillanmisSarap, price = 9000, zone = "mahzen", zoneName = "Şarap Mahzeni" },
            new() { drink = DrinkId.BalLikoru, price = 7000, zone = "mahzen", zoneName = "Şarap Mahzeni" },
            new() { drink = DrinkId.GunBatimiPuncu, price = 11000, zone = "teras", zoneName = "Çatı Terası" },
            new() { drink = DrinkId.IzgaraBalik, price = 12000, zone = "iskele", zoneName = "İskele" },
            // v1.5 oyun sonu makineleri: en pahalı makineden (12.000) bir kademe yukarı
            new() { drink = DrinkId.KralSerbeti, price = 14000, zone = "salon", zoneName = "Ana Salon" },
            new() { drink = DrinkId.SolenTabagi, price = 16000, zone = "mutfak", zoneName = "Mutfak" },
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
            new() { id = "staff_nana", name = "Nana", role = Role.Temizlikci, roleName = "Temizlikçi", icon = "Staff_Temizlikci", level = 2, hire = 80, wage = 12, desc = "Kirli masaları temizler" },
            new() { id = "staff_pip", name = "Pip", role = Role.Garson, roleName = "Garson", icon = "Main_Pip", level = 3, hire = 150, wage = 18, desc = "Sipariş taşır, para toplar" },
            new() { id = "staff_gus", name = "Gus", role = Role.Barmen, roleName = "Barmen", icon = "Staff_Barmen", level = 5, hire = 500, wage = 45, desc = "Tüm makineler +%40 hız" },
            new() { id = "staff_mina", name = "Mina", role = Role.Garson, roleName = "Garson", icon = "Staff_GarsonMina", level = 6, hire = 650, wage = 40, desc = "Sipariş taşır, para toplar" },
            new() { id = "staff_tobi", name = "Tobi", role = Role.Garson, roleName = "Garson", icon = "Staff_GarsonTobi", level = 8, hire = 1300, wage = 70, desc = "Hızlı garson, 2 bardak taşır" },
            new() { id = "staff_ayla", name = "Ayla", role = Role.Asci, roleName = "Aşçı", icon = "Staff_Asci", level = 7, hire = 800, wage = 45, desc = "Yemekler +%40 hızlı pişer", zone = "mutfak", zoneName = "Mutfak" },
            new() { id = "staff_brak", name = "Brak", role = Role.Fenerci, roleName = "Fenerci", icon = "Staff_Fedai", level = 7, hire = 900, wage = 50, desc = "Akşam fenerleri yakar" },
            new() { id = "staff_sema", name = "Sema", role = Role.Hizmetci, roleName = "Hizmetçi", icon = "Staff_Hizmetci", level = 11, hire = 1400, wage = 60, desc = "Han odalarını toplar", zone = "merdiven", zoneName = "Han" },
            new() { id = "staff_lirael", name = "Lirael", role = Role.Garson, roleName = "Elf Garson", icon = "Staff_Lirael", level = 1, hire = 0, wage = 0, desc = "Çok hızlı · 4 bardak", premium = true },
            new() { id = "staff_ignis", name = "Ignis", role = Role.Asci, roleName = "Ejder Aşçı", icon = "Staff_Ignis", level = 1, hire = 0, wage = 0, desc = "Yemekler ×2 hızlı", premium = true, zone = "mutfak", zoneName = "Mutfak" },
            new() { id = "staff_lir", name = "Lir", role = Role.Ozan, roleName = "Ozan", icon = "Staff_OzanSahne", level = 9, hire = 1500, wage = 80, desc = "Herkesin sabrı +%25" },
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

        public static readonly Decor[] DecorList =
        {
            new() { id = "dec_saksi", name = "Saksı", icon = "DEC_Saksi", prefab = "PR_Decor_Saksi", level = 1, price = 40, ambiance = 2 },
            new() { id = "dec_mum", name = "Mumluk", icon = "DEC_Mum", prefab = "PR_Decor_Mum", level = 1, price = 30, ambiance = 2 },
            new() { id = "dec_hali", name = "Halı", icon = "DEC_Hali", prefab = "PR_Decor_Hali", level = 2, price = 90, ambiance = 4 },
            new() { id = "dec_flama", name = "Flama", icon = "DEC_Flama", prefab = "PR_Decor_Flama", level = 3, price = 120, ambiance = 4 },
            new() { id = "dec_fici", name = "Fıçı Yığını", icon = "DEC_FiciYigini", prefab = "PR_Decor_FiciYigini", level = 3, price = 150, ambiance = 3 },
            new() { id = "dec_kalkan", name = "Kalkan & Kılıç", icon = "DEC_Kalkan", prefab = "PR_Decor_Kalkan", level = 4, price = 220, ambiance = 5 },
            new() { id = "dec_boynuz", name = "Geyik Boynuzu", icon = "DEC_Boynuz", prefab = "PR_Decor_Boynuz", level = 5, price = 260, ambiance = 5 },
            new() { id = "dec_avize", name = "Avize", icon = "DEC_Avize", prefab = "PR_Decor_Avize", level = 6, price = 420, ambiance = 8 },
            new() { id = "dec_kristal", name = "Kristal Avize", icon = "DEC_KristalAvize", prefab = "PR_Decor_KristalAvize", level = 1, price = 0, ambiance = 25, premium = true },
            new() { id = "dec_ejder", name = "Ejder Heykeli", icon = "DEC_EjderHeykeli", prefab = "PR_Decor_EjderHeykeli", level = 1, price = 0, ambiance = 30, premium = true },
            new() { id = "dec_altinfici", name = "Altın Fıçı", icon = "DEC_AltinFici", prefab = "PR_Decor_AltinFici", level = 1, price = 0, ambiance = 8, premium = true },
            new() { id = "dec_muhur", name = "Kraliyet Mührü", icon = "DEC_Muhur", prefab = "PR_Decor_Muhur", level = 12, price = 3000, ambiance = 15 },
        };

        public static Decor FindDecor(string id) => System.Array.Find(DecorList, d => d.id == id);
    }
}
