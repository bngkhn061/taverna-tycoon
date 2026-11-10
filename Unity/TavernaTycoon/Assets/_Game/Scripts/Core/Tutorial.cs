using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// İlk oyun rehberi v2 (v1.2, mockup design/ui/ui_v12_akis.html R1–R3; oyuncu testi: "oyun hemen akmamalı,
    /// görevleri, dükkânı inceletmeli, butonlar sonradan açılmalı"). 9 adım, her biri oyuncu yapınca geçer; atlanabilir.
    /// Rehber sürerken gün saati ve yoğun saat durur, ilk müşteri ancak bardak dolunca gelir ve en fazla 2 müşteri olur.
    /// Yan butonlar adım adım açılır (<see cref="Reveals"/>). Görünüm HudController (karartma, halka, Barlo kartı).
    /// </summary>
    public static class Tutorial
    {
        public static readonly string[] Texts =
        {
            "Bira fıçısına yürü, bardak dolsun!",
            "İlk müşterin geliyor! Fıçının önünde bekle, bardak dolsun",
            "Birayı bekleyen müşterine götür",
            "Masaya bırakılan parayı topla",
            "Görev panosu açıldı! Görevler altın ve elmas verir, bir bak",
            "Ödeme karesinin üstünde dur: altınla yeni masa aç",
            "Dükkân açıldı! Yeni makine = yeni içecek, daha çok müşteri",
            "Personel: görevliler sana yardım eder, her gün maaş alır",
            "Depo: makineler malzeme harcar, azalınca buradan sipariş ver",
        };
        public const int StepCount = 9;

        /// <summary>Adım → bu adımla açılan yan buton (HUD element adı).</summary>
        public const int StepQuests = 4, StepShop = 6, StepStaff = 7, StepDepot = 8;

        public static int Step { get; private set; } = -1;
        public static bool Active => Step >= 0 && Step < StepCount;
        public static event Action Changed;

        static bool _served, _coins, _quests, _shop, _staff, _depot;

        /// <summary>Yeni oyunda (Sv1, rehber görülmemiş) başlatır. GameBootstrap çağırır.</summary>
        public static void Begin()
        {
            _served = _coins = _quests = _shop = _staff = _depot = false;
            Step = !GameState.Data.tutorialDone && GameState.Level <= 1 ? 0 : -1;
            GameState.DrinkServed -= OnServed;
            GameState.CoinsGainedAt -= OnCoins;
            GameState.DrinkServed += OnServed;
            GameState.CoinsGainedAt += OnCoins;
            Changed?.Invoke();
        }

        static void OnServed(DrinkId _) => _served = true;
        static void OnCoins(long a, Vector3 _) { if (Step == 3) _coins = true; }
        public static void OnQuestsOpened() => _quests = true;
        public static void OnShopOpened() => _shop = true;
        public static void OnStaffOpened() => _staff = true;
        public static void OnDepotOpened() => _depot = true;

        public static void Skip() => Finish();

        static void Finish()
        {
            Step = StepCount;
            GameState.Data.tutorialDone = true;
            GameState.Save();
            Changed?.Invoke();
        }

        static void Next()
        {
            Step++;
            Sfx.Play(SfxId.Claim, 1.2f);
            if (Step >= StepCount) Finish();
            else Changed?.Invoke();
        }

        /// <summary>Bu adıma gelmeden buton gizli. Rehber yoksa (eski oyuncu) her şey açık.</summary>
        public static bool Reveals(int step) => !Active || Step >= step;

        /// <summary>Rehber sürerken oyun "akmaz": gün saati ve yoğun saat durur.</summary>
        public static bool HoldsTime => Active;

        /// <summary>Rehberde müşteri gelişi: oyuncu fıçıya varana kadar hiç, sonra en fazla 2 müşteri.</summary>
        public static bool AllowsSpawn => !Active || Step >= StepQuests || (Step >= 1 && Customer.All.Count < 2);   // fıçı ancak sipariş varken bardak verir

        static DrinkStation Beer => DrinkStation.All.Find(s => s && s.drink && s.drink.id == DrinkId.Bira);

        /// <summary>Adım bir HUD butonunu gösteriyorsa onun adı (yoksa null).</summary>
        public static string UiTarget => Step switch
        {
            StepQuests => "open-quests",
            StepShop => "open-shop",
            StepStaff => "open-staff",
            StepDepot => "open-depot",
            _ => null,
        };

        /// <summary>Adım hedefinin dünya konumu (yoksa null).</summary>
        public static Vector3? Target
        {
            get
            {
                switch (Step)
                {
                    case 0: case 1: return Beer ? Beer.ServePos : null;
                    case 2:
                        foreach (var c in Customer.All) if (c.state == Customer.State.Waiting) return c.transform.position;
                        return null;
                    case 3: return CoinPile.All.Count > 0 ? CoinPile.All[0].transform.position : null;
                    case 5: var z = PayZone.NextGoal(); return z ? z.transform.position : null;
                    default: return null;
                }
            }
        }

        /// <summary>Her kare (GameBootstrap): adım koşulu sağlandı mı?</summary>
        public static void Tick()
        {
            if (!Active) return;
            var p = PlayerController.I;
            if (!p) return;
            switch (Step)
            {
                case 0:
                    if (Beer && DrinkStation.FlatDistance(p.transform.position, Beer.ServePos) < Balance.InteractRadius) Next();
                    break;
                case 1:
                    if (p.carrier && p.carrier.Count > 0) Next();
                    break;
                case 2:
                    if (_served) Next();
                    break;
                case 3:
                    if (_coins) Next();
                    break;
                case StepQuests:
                    if (_quests) Next();
                    break;
                case 5:
                    var z = PayZone.NextGoal();
                    // Kare henüz kilitliyse (seviye) adım beklemez: oyun durmuş halde takılı kalmasın
                    if (!z || !z.Available || z.Paid > 0 || GameState.IsUnlocked(z.id)) Next();
                    break;
                case StepShop:
                    if (_shop) Next();
                    break;
                case StepStaff:
                    if (_staff) Next();
                    break;
                case StepDepot:
                    if (_depot) Next();
                    break;
            }
        }
    }
}
