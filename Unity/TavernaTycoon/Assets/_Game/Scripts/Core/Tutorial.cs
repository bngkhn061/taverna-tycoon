using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// İlk oyun rehberi (v0.9, mockup E): 6 adım, her biri oyuncu yapınca geçer; her an atlanabilir.
    /// Adımlar ve hedefleri burada; görünüm HudController (karartma, ok, Barlo balonu).
    /// </summary>
    public static class Tutorial
    {
        public static readonly string[] Texts =
        {
            "Bira fıçısına yürü, bardak dolsun!",
            "Bardağı al: fıçının önünde biraz bekle",
            "Birayı bekleyen müşteriye götür",
            "Masaya bırakılan parayı topla",
            "Ödeme karesinin üstünde dur: yeni masa!",
            "Dükkânı aç: makine, personel, dekor",
        };
        public const int StepCount = 6;

        public static int Step { get; private set; } = -1;
        public static bool Active => Step >= 0 && Step < StepCount;
        public static event Action Changed;

        static bool _served, _coins, _shop;

        /// <summary>Yeni oyunda (Sv1, rehber görülmemiş) başlatır. GameBootstrap çağırır.</summary>
        public static void Begin()
        {
            _served = _coins = _shop = false;
            Step = !GameState.Data.tutorialDone && GameState.Level <= 1 ? 0 : -1;
            GameState.DrinkServed -= OnServed;
            GameState.CoinsGainedAt -= OnCoins;
            GameState.DrinkServed += OnServed;
            GameState.CoinsGainedAt += OnCoins;
            Changed?.Invoke();
        }

        static void OnServed(DrinkId _) => _served = true;
        static void OnCoins(long a, Vector3 _) { if (Step == 3) _coins = true; }
        public static void OnShopOpened() => _shop = true;

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

        static DrinkStation Beer => DrinkStation.All.Find(s => s && s.drink && s.drink.id == DrinkId.Bira);

        /// <summary>Adım hedefinin dünya konumu (yoksa null: HUD hedefi gösterir ya da sadece balon).</summary>
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
                    case 4: var z = PayZone.NextGoal(); return z ? z.transform.position : null;
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
                case 4:
                    var z = PayZone.NextGoal();
                    if (!z || z.Paid > 0 || GameState.IsUnlocked(z.id)) Next();
                    break;
                case 5:
                    if (_shop) Next();
                    break;
            }
        }
    }
}
