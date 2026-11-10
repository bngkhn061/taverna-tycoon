using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Yönetim katmanı (GAME_DESIGN.md §3.2.4): makine / personel / dekor satın alma, taverna puanı, maaşlar.
    /// Sahnedeki karşılıkları TavernRegistry'de durur.
    /// </summary>
    public static class Management
    {
        public static event Action Changed;

        // ------------------------------------------------------------------ makineler
        public static int MachineLevel(DrinkId d) { var def = GameDatabase.I.Drink(d); return def ? def.unlockLevel : 1; }
        public static bool OwnsMachine(DrinkId d) => d == DrinkId.Bira || GameState.IsUnlocked(Catalog.MachineKey(d));
        public static bool ZoneOpen(string zone) => string.IsNullOrEmpty(zone) || GameState.IsUnlocked(zone);

        public enum BuyResult { Ok, NoMoney, LevelLow, ZoneLocked, Owned, NoSlot, Premium }

        public static BuyResult CanBuyMachine(Catalog.Machine m)
        {
            if (OwnsMachine(m.drink)) return BuyResult.Owned;
            if (GameState.Level < MachineLevel(m.drink)) return BuyResult.LevelLow;
            if (!ZoneOpen(m.zone)) return BuyResult.ZoneLocked;
            return GameState.Coins >= m.price ? BuyResult.Ok : BuyResult.NoMoney;
        }

        public static BuyResult BuyMachine(Catalog.Machine m)
        {
            var r = CanBuyMachine(m);
            if (r != BuyResult.Ok) return r;
            GameState.TrySpend(m.price);
            GameState.Unlock(Catalog.MachineKey(m.drink));
            if (TavernRegistry.I) TavernRegistry.I.ApplyMachine(m.drink, true);
            Changed?.Invoke();
            return r;
        }

        // ------------------------------------------------------------------ personel
        public static bool Hired(string id) => GameState.IsUnlocked(id);

        public static BuyResult CanHire(Catalog.Staff s)
        {
            if (Hired(s.id)) return BuyResult.Owned;
            if (s.premium) return BuyResult.Premium;
            if (GameState.Level < s.level) return BuyResult.LevelLow;
            return GameState.Coins >= s.hire ? BuyResult.Ok : BuyResult.NoMoney;
        }

        public static BuyResult Hire(Catalog.Staff s)
        {
            var r = CanHire(s);
            if (r != BuyResult.Ok) return r;
            GameState.TrySpend(s.hire);
            GameState.Unlock(s.id);
            if (TavernRegistry.I) TavernRegistry.I.ApplyStaff(s.id, true);
            Changed?.Invoke();
            return r;
        }

        public static long DailyWages
        {
            get
            {
                long w = 0;
                foreach (var s in Catalog.StaffList)
                    if (Hired(s.id)) w += WageOf(s);
                return w;
            }
        }

        /// <summary>Görevlinin bugünkü maaşı (tavernanın seviyesi ve görevlinin seviyesiyle büyür).</summary>
        public static long WageOf(Catalog.Staff s) => Balance.Wage(s.wage, s.level, GameState.Level, StaffUpgrades.Level(s.id));

        /// <summary>Dün maaşı ödenemedi: bugün personel küskün (yavaş).</summary>
        public static bool Sulky => GameState.Data.sulkyDay > 0 && GameState.Data.sulkyDay == GameState.Data.day;
        public static float MoraleMultiplier => Sulky ? Balance.SulkySpeed : 1f;

        public static bool HasRole(Catalog.Role role)
        {
            foreach (var s in Catalog.StaffList) if (s.role == role && Hired(s.id)) return true;
            return false;
        }

        // Rol bonusları görevli seviyesiyle büyür (StaffUpgrades, v0.6)
        public static float StationSpeedMultiplier => HasRole(Catalog.Role.Barmen) ? 1f + StaffUpgrades.BarmenBonus(RoleStaffId(Catalog.Role.Barmen)) : 1f;
        /// <summary>Mutfak makinelerinin hızı: Aşçı (v0.7), Barmen'le aynı eğri.</summary>
        public static float KitchenSpeedMultiplier => (HasRole(Catalog.Role.Asci) ? 1f + StaffUpgrades.BarmenBonus(RoleStaffId(Catalog.Role.Asci)) : 1f)
                                                     * (Hired("staff_ignis") ? 2f : 1f);   // v1.0 Hazine: Ejder aşçı Ignis yemekleri ×2 hızlandırır
        public static float SpeedMultiplierFor(DrinkDef d) => d && d.isFood ? KitchenSpeedMultiplier : StationSpeedMultiplier;
        public static float PatienceMultiplier => HasRole(Catalog.Role.Ozan) ? 1f + StaffUpgrades.BardBonus(RoleStaffId(Catalog.Role.Ozan)) : 1f;

        /// <summary>Bu rolde işe alınmış ilk görevlinin kimliği (yoksa boş).</summary>
        static string RoleStaffId(Catalog.Role role)
        {
            foreach (var s in Catalog.StaffList) if (s.role == role && Hired(s.id)) return s.id;
            return "";
        }

        // ------------------------------------------------------------------ dekor
        public static int DecorOwned(string id)
        {
            int n = 0;
            foreach (var e in GameState.Data.decor) if (e.StartsWith(id + ":")) n++;
            return n;
        }

        public static int FreeDecorSlot(string id) => TavernRegistry.I ? TavernRegistry.I.FreeSlot(id) : -1;

        public static BuyResult CanBuyDecor(Catalog.Decor d)
        {
            if (d.premium) return DecorOwned(d.id) > 0 ? BuyResult.Owned : BuyResult.Premium;
            if (GameState.Level < d.level) return BuyResult.LevelLow;
            if (FreeDecorSlot(d.id) < 0) return BuyResult.NoSlot;
            return GameState.Coins >= d.price ? BuyResult.Ok : BuyResult.NoMoney;
        }

        public static BuyResult BuyDecor(Catalog.Decor d)
        {
            var r = CanBuyDecor(d);
            if (r != BuyResult.Ok) return r;
            int slot = FreeDecorSlot(d.id);
            GameState.TrySpend(d.price);
            GameState.Data.decor.Add($"{d.id}:{slot}");
            GameState.Save();
            TavernRegistry.I.PlaceDecor(d.id, slot, true);
            Changed?.Invoke();
            GameState.NotifyChanged();
            return r;
        }

        // ------------------------------------------------------------------ taverna puanı
        /// <summary>1–5 yıldız (yarımlı): ambiyans yükseltir, kirli masalar düşürür.</summary>
        public static float Stars
        {
            get
            {
                int dirty = 0, total = 0;
                foreach (var t in Table.All) { total++; if (t.Dirty) dirty++; }
                float s = 1f + AmbianceSource.Total / 30f - (total > 0 ? 2f * dirty / total : 0f) - AngryPenalty;
                return Mathf.Clamp(Mathf.Round(s * 2f) / 2f, 1f, 5f);
            }
        }

        /// <summary>Kızgın giden müşterilerin geçici yıldız cezası (zamanla söner).</summary>
        public static float AngryPenalty { get; private set; }

        /// <summary>Kızgın müşteri: biraz ün ve geçici yıldız kaybı (para kaybı yok).</summary>
        public static void OnAngryCustomer()
        {
            AngryPenalty = Mathf.Min(Balance.MaxStarPenalty, AngryPenalty + Balance.AngryStarPenalty);
            GameState.LoseUn(Balance.AngryUnLoss);
        }

        public static void TickPenalty(float dt) => AngryPenalty = Mathf.Max(0f, AngryPenalty - Balance.StarPenaltyDecayPerSec * dt);

        public static float TipFromStars => 1f + 0.05f * (Stars - 1f);
        public static float SpawnFromStars => 0.7f + 0.15f * Stars;

        public static string BuyMessage(BuyResult r, int level = 0, string zone = "") => r switch
        {
            BuyResult.NoMoney => "Yeterli altın yok",
            BuyResult.LevelLow => $"Seviye {level} gerekli",
            BuyResult.ZoneLocked => $"Önce {zone} açılmalı",
            BuyResult.Owned => "Zaten var",
            BuyResult.NoSlot => "Boş yer kalmadı",
            BuyResult.Premium => "Hazine'de satılıyor",
            _ => "",
        };
    }
}
