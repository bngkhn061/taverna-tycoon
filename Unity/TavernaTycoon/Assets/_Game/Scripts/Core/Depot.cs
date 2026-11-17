using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Depo ve sipariş (v0.6, oyuncu testi: "içecekler bitsin, sipariş verelim, depomuz olsun").
    /// Her içeceğin kendi malzeme rafı var (1 birim = 1 bardak). Makine üretirken rafı harcar;
    /// raf boşalınca üretim durur. Oyuncu paket sipariş eder, teslimat bir süre sonra gelir.
    /// Raf kapasitesi içecek başına, teslimat süresi genel olarak yükseltilir. Sayılar: Balance.
    /// </summary>
    public static class Depot
    {
        public enum OrderResult { Ok, NoMoney, Full, Locked }

        /// <summary>Bir teslimat geldiğinde (içecek, gelen birim).</summary>
        public static event Action<DrinkId, int> Delivered;
        /// <summary>Raf boşaldığında bir kez (makine durdu).</summary>
        public static event Action<DrinkId> RanOut;

        static StationSave S(DrinkId id)
        {
            var s = GameState.Station(id);
            if (!s.supplyInit)
            {
                s.supplyInit = true;
                s.supply = Balance.DepotCapacity(s.depotLv); // yeni makine dolu rafla gelir
            }
            return s;
        }

        public static int Supply(DrinkId id) => S(id).supply;
        public static int Capacity(DrinkId id) => Balance.DepotCapacity(S(id).depotLv);
        public static int Incoming(DrinkId id) => S(id).incoming;
        public static float Eta(DrinkId id) => S(id).eta;
        public static int DepotLevel(DrinkId id) => S(id).depotLv;
        public static float DeliverySeconds => Balance.DeliverySeconds(GameState.Data.deliveryLv);
        public static float Fill01(DrinkId id) => Capacity(id) > 0 ? Supply(id) / (float)Capacity(id) : 0f;

        /// <summary>Üretim için bir birim harcar; raf boşsa false.</summary>
        public static bool Consume(DrinkId id)
        {
            var s = S(id);
            if (s.supply <= 0) return false;
            s.supply--;
            if (s.supply == 0) RanOut?.Invoke(id);
            return true;
        }

        /// <summary>Raf + yoldaki sipariş bir paket daha alır mı?</summary>
        /// <summary>Bir paket malzemenin bedeli (kalite dahil satış fiyatına bağlı).</summary>
        public static int PackCost(DrinkDef def) => Mathf.RoundToInt(Balance.SupplyPackCost(def, S(def.id).quality) * Tiers.PriceMultiplier);

        public static bool HasRoom(DrinkId id) => Supply(id) + Incoming(id) + Balance.SupplyPack <= Capacity(id);

        /// <summary>Bir paket (Balance.SupplyPack birim) sipariş eder; para hemen düşer, mal teslimatla gelir.</summary>
        public static OrderResult Order(DrinkId id)
        {
            var def = GameDatabase.I ? GameDatabase.I.Drink(id) : null;
            if (!def) return OrderResult.Locked;
            var s = S(id);
            if (!HasRoom(id)) return OrderResult.Full;
            // v1.9: borçluyken malzeme siparişi borca yazılır (makineler durup oyun kilitlenmesin; malzeme satışın %30'u, sipariş hep kâr eder)
            if (GameState.InDebt) GameState.ForceSpend(PackCost(def));
            else if (!GameState.TrySpend(PackCost(def))) return OrderResult.NoMoney;
            s.incoming += Balance.SupplyPack;
            if (s.eta <= 0f) s.eta = DeliverySeconds;
            GameState.NotifyChanged();
            return OrderResult.Ok;
        }

        /// <summary>Yoldaki siparişi hemen getirir (reklam ödülü).</summary>
        public static void RushDelivery(DrinkId id)
        {
            var s = S(id);
            if (s.incoming > 0) Deliver(id, s);
        }

        public static bool CanUpgradeShelf(DrinkId id) => DepotLevel(id) < Balance.MaxDepotLevel;

        public static bool UpgradeShelf(DrinkId id)
        {
            var def = GameDatabase.I ? GameDatabase.I.Drink(id) : null;
            var s = S(id);
            if (!def || s.depotLv >= Balance.MaxDepotLevel) return false;
            if (!GameState.TrySpend(Balance.DepotUpgradeCost(def, s.depotLv))) return false;
            s.depotLv++;
            GameState.Save();
            GameState.NotifyChanged();
            return true;
        }

        public static bool UpgradeDelivery()
        {
            int lv = GameState.Data.deliveryLv;
            if (lv >= Balance.MaxDeliveryLevel) return false;
            if (!GameState.TrySpend(Balance.DeliveryUpgradeCost(lv))) return false;
            GameState.Data.deliveryLv++;
            GameState.Save();
            GameState.NotifyChanged();
            return true;
        }

        public static bool AutoOrderAvailable => GameState.Level >= Balance.AutoOrderLevel;

        public static bool BuyAutoOrder()
        {
            if (GameState.Data.autoOrder || !AutoOrderAvailable) return false;
            if (!GameState.TrySpend(Balance.AutoOrderPrice)) return false;
            GameState.Data.autoOrder = true;
            GameState.Save();
            GameState.NotifyChanged();
            return true;
        }

        /// <summary>Teslimat sayaçları ve otomatik sipariş. GameBootstrap her kare çağırır.</summary>
        public static void Tick(float dt)
        {
            foreach (var st in DrinkStation.All)
            {
                if (!st || !st.drink) continue;
                var s = S(st.drink.id);
                if (s.incoming > 0)
                {
                    s.eta -= dt;
                    if (s.eta <= 0f) Deliver(st.drink.id, s);
                }
                if (GameState.Data.autoOrder && s.incoming == 0 &&
                    s.supply <= Capacity(st.drink.id) * Balance.AutoOrderThreshold)
                    Order(st.drink.id);
            }
        }

        /// <summary>Gece verilen siparişler sabah hepsi birden gelir (DayCycle.NextDay).</summary>
        public static void CompleteAll()
        {
            foreach (var st in DrinkStation.All)
            {
                if (!st || !st.drink) continue;
                var s = S(st.drink.id);
                if (s.incoming > 0) Deliver(st.drink.id, s);
            }
        }

        static void Deliver(DrinkId id, StationSave s)
        {
            int n = Mathf.Min(s.incoming, Mathf.Max(0, Balance.DepotCapacity(s.depotLv) - s.supply));
            s.supply += n;
            s.incoming = 0;
            s.eta = 0f;
            Delivered?.Invoke(id, n);
            GameState.NotifyChanged();
        }
    }
}
