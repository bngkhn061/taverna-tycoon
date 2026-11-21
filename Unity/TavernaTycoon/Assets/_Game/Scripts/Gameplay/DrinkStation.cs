using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Üretim istasyonu (GAME_DESIGN.md §4): kendi kendine üretir, stokta tutar,
    /// yanına gelen taşıyıcıya (oyuncu/garson) içecek verir. Seviyeleri GameState'te saklanır.
    /// </summary>
    public class DrinkStation : MonoBehaviour
    {
        public static readonly List<DrinkStation> All = new();

        public DrinkDef drink;
        [Tooltip("Ahşap / Pirinç / Altın görünümleri (Balance.VisualTier).")]
        public GameObject[] tierVisuals = new GameObject[3];
        public Transform servePoint;
        public float transferInterval = 0.22f;

        public int Stock { get; private set; }
        public float Progress01 => _t / ProduceTime;
        public float ProduceTime => Balance.ProduceTime(drink, Save.speed) / Management.SpeedMultiplierFor(drink);
        public int MaxStock => Balance.MaxStock(Save.stock);
        /// <summary>Satış fiyatı: kalite × taverna kademesi (v1.0 "Tavernayı Büyüt").</summary>
        public int Price => Mathf.RoundToInt(Balance.SellPrice(drink, Save.quality) * Tiers.PriceMultiplier * TavernEvents.PriceMultiplier);
        public StationSave Save => GameState.Station(drink.id);
        /// <summary>Depoda bu içeceğin malzemesi bitti mi? (üretim durur)</summary>
        public bool OutOfSupply => !Seasonal && Depot.Supply(drink.id) <= 0;
        /// <summary>v2.2 sezon tezgâhı: depodan malzeme harcamaz (sezon boyunca kendiliğinden açılır, Seasons).</summary>
        public bool Seasonal => drink && Seasons.IsSeasonDrink(drink.id);
        public Vector3 ServePos => servePoint ? servePoint.position : transform.position + transform.forward * 0.6f;

        float _t, _transferT;
        int _shownTier = -1;

        void OnEnable() { All.Add(this); Stock = 1; }
        void OnDisable() => All.Remove(this);

        void Update()
        {
            // Üretim depodaki malzemeyi harcar; malzeme yoksa makine bekler (v0.6 depo)
            if (Stock < MaxStock && !OutOfSupply)
            {
                _t += Time.deltaTime;
                if (_t >= ProduceTime && (Seasonal || Depot.Consume(drink.id))) { _t = 0f; Stock++; }
            }
            else if (Stock >= MaxStock) _t = 0f;

            // Yakındaki oyuncuyla alışveriş
            var p = PlayerController.I;
            _transferT -= Time.deltaTime;
            if (p && p.carrier && _transferT <= 0f && FlatDistance(p.transform.position, ServePos) < BarloSkills.Reach)
            {
                int carried = p.carrier.CountOf(drink.id);
                int demand = Demand(drink.id);
                if (carried < demand && TryGiveTo(p.carrier))
                {
                    // Akıllı alma: sadece bekleyen bir sipariş varsa ver (tepsi istenmeyen içecekle tıkanmasın)
                    _transferT = transferInterval;
                    Sfx.Play(SfxId.Pickup, 1f + carried * 0.06f);
                }
                else if (carried > demand && Stock < MaxStock && p.carrier.Take(drink.id))
                {
                    // Kimsenin istemediği içecek geri stoğa döner
                    Stock++;
                    _transferT = transferInterval;
                }
            }

            int tier = Balance.VisualTier(Mathf.Max(Save.speed, Mathf.Max(Save.stock, Save.quality)));
            if (tier != _shownTier) ShowTier(tier);
        }

        void ShowTier(int tier)
        {
            for (int i = 0; i < tierVisuals.Length; i++)
                if (tierVisuals[i]) tierVisuals[i].SetActive(i == tier - 1);
            if (_shownTier > 0) { var a = GetComponentInChildren<ToyAnimator>(); if (a) a.Pop(); }
            _shownTier = tier;
        }

        public void FillToMax() { Stock = MaxStock; _t = 0f; }

        /// <summary>Taşıyıcıya (oyuncu/garson) bir bardak verir.</summary>
        public bool TryGiveTo(Carrier c)
        {
            if (Stock <= 0 || !c || !c.HasSpace) return false;
            c.Add(drink);
            Stock--;
            return true;
        }

        public bool TryUpgrade(UpgradeStat stat)
        {
            var s = Save;
            int lv = s.Get(stat);
            if (lv >= Balance.MaxUpgradeLevel) return false;
            if (!GameState.TrySpend(Balance.UpgradeCost(drink, stat, lv))) return false;
            s.Inc(stat);
            Quests.OnUpgrade();
            Sfx.Play(SfxId.Buy);
            GameState.Save();
            GameState.NotifyChanged();
            return true;
        }

        /// <summary>Şu an bu içeceği bekleyen müşteri sayısı.</summary>
        public static int Demand(DrinkId id)
        {
            int n = 0;
            foreach (var c in Customer.All) if (c.state == Customer.State.Waiting && c.wanted == id && !c.reservedBy) n++;
            return n;
        }

        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0; b.y = 0;
            return Vector3.Distance(a, b);
        }
    }
}
