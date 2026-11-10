using System;
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>Görev türleri. Kayıtta sayı olarak saklanır: yeni türler yalnızca sona eklenir.</summary>
    public enum QuestType { ServeDrink, ServeCustomers, EarnCoins, RushHour, Upgrade, Unlock, SpecialOrder, SpecialOrderBig }

    [Serializable]
    public class QuestSave
    {
        public QuestType type;
        public DrinkId drink;
        public int target, progress, rewardCoins, rewardGems;
        public bool claimed;
        public bool Done => progress >= target;
    }

    /// <summary>
    /// Günlük görevler (GAME_DESIGN.md §8.3): her gün 3 yeni görev, ödüller seviyeyle ölçeklenir.
    /// Bir görev toplanınca yerine hemen yenisi gelir — oyuncunun elinde hep bir kısa hedef olsun.
    /// </summary>
    public static class Quests
    {
        public static event Action Changed;
        static bool _hooked, _claiming;

        public static List<QuestSave> List => GameState.Data.quests;

        public static void Init()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (GameState.Data.questDay != today || List.Count == 0)
            {
                GameState.Data.questDay = today;
                List.Clear();
                for (int i = 0; i < 3; i++) List.Add(Generate(i));
            }
            if (_hooked) return;
            _hooked = true;
            GameState.DrinkServed += d => Bump(q => q.type == QuestType.ServeCustomers || (q.type == QuestType.ServeDrink && q.drink == d), 1);
            GameState.CoinsGainedAt += (a, _) => { if (!_claiming) Bump(q => q.type == QuestType.EarnCoins, (int)a); };
            GameState.ZoneUnlocked += _ => Bump(q => q.type == QuestType.Unlock, 1);
        }

        public static void OnRushCompleted() => Bump(q => q.type == QuestType.RushHour, 1);
        public static void OnUpgrade() => Bump(q => q.type == QuestType.Upgrade, 1);

        /// <summary>v1.4: oyuncu bir "Barlo'ya özel" siparişi servis etti (Customer.Serve, özel dal).</summary>
        public static void OnSpecialServed() => Bump(IsSpecial, 1);

        static bool IsSpecial(QuestSave q) => q.type == QuestType.SpecialOrder || q.type == QuestType.SpecialOrderBig;

        static void Bump(Predicate<QuestSave> match, int amount)
        {
            bool any = false;
            foreach (var q in List)
                if (!q.claimed && !q.Done && match(q)) { q.progress = Mathf.Min(q.target, q.progress + amount); any = true; }
            if (any) Changed?.Invoke();
        }

        public static int ReadyCount
        {
            get { int n = 0; foreach (var q in List) if (q.Done && !q.claimed) n++; return n; }
        }

        public static bool Claim(int index)
        {
            var q = List[index];
            if (!q.Done || q.claimed) return false;
            q.claimed = true;
            GameState.Data.gems += q.rewardGems;
            GameState.Data.questsClaimed++;
            _claiming = true;
            GameState.AddCoins(q.rewardCoins, PlayerController.I ? PlayerController.I.transform.position : Vector3.zero);
            _claiming = false;
            List[index] = Generate(GameState.Data.questsClaimed + index);
            GameState.Save();
            Changed?.Invoke();
            return true;
        }

        static QuestSave Generate(int seed)
        {
            int lv = GameState.Level;
            var rnd = new System.Random(seed * 7919 + lv * 31 + DateTime.Now.DayOfYear);
            // Sadece istasyonu açık içecekler
            var drinks = new List<DrinkId>();
            foreach (var s in DrinkStation.All) drinks.Add(s.drink.id);
            if (drinks.Count == 0) drinks.Add(DrinkId.Bira);

            var types = new List<QuestType> { QuestType.ServeDrink, QuestType.ServeCustomers, QuestType.EarnCoins, QuestType.Upgrade, QuestType.RushHour, QuestType.Unlock };
            // v1.4: özel sipariş görevleri Sv3'ten (özel siparişlerin başladığı seviye) itibaren
            if (lv >= Balance.SpecialQuestLevel) { types.Add(QuestType.SpecialOrder); types.Add(QuestType.SpecialOrderBig); }
            // Listede zaten olan türleri tekrar verme (üç görev hep farklı olsun; iki özel sipariş görevi aynı anda çıkmaz)
            foreach (var q0 in List)
                if (!q0.claimed)
                {
                    types.Remove(q0.type);
                    if (IsSpecial(q0)) { types.Remove(QuestType.SpecialOrder); types.Remove(QuestType.SpecialOrderBig); }
                }
            if (types.Count == 0) types.Add(QuestType.ServeCustomers);
            return Make(types[rnd.Next(types.Count)], drinks[rnd.Next(drinks.Count)]);
        }

        /// <summary>Bir görevi türüne ve seviyeye göre kurar (hedef ve ödül). Test botu da kullanır.</summary>
        public static QuestSave Make(QuestType t, DrinkId drink = DrinkId.Bira)
        {
            int lv = GameState.Level;
            float scale = 1f + lv * 0.35f;
            var q = new QuestSave { type = t };
            switch (t)
            {
                case QuestType.ServeDrink: q.drink = drink; q.target = 8 + lv * 2; break;
                case QuestType.ServeCustomers: q.target = 12 + lv * 3; break;
                case QuestType.EarnCoins: q.target = Mathf.RoundToInt(60 * scale * scale); break;
                case QuestType.Upgrade: q.target = 2 + lv / 3; break;
                case QuestType.RushHour: q.target = 1; break;
                case QuestType.Unlock: q.target = 1; break;
                case QuestType.SpecialOrder: q.target = Balance.SpecialQuestSmall; break;
                case QuestType.SpecialOrderBig: q.target = Balance.SpecialQuestBig; break;
            }
            q.rewardCoins = Mathf.RoundToInt(25 * scale * scale);
            q.rewardGems = 1 + lv / 4;
            // 12 özel sipariş ≈ 65 sipariş (özel oranı %18–22): normal görevin iki katı emek → iki kat altın, +1 elmas
            if (t == QuestType.SpecialOrderBig)
            {
                q.rewardCoins *= Balance.SpecialQuestBigCoinMultiplier;
                q.rewardGems += Balance.SpecialQuestBigExtraGems;
            }
            return q;
        }

        public static string Text(QuestSave q) => q.type switch
        {
            QuestType.ServeDrink => $"{q.target} {DrinkName(q.drink)} sat",
            QuestType.ServeCustomers => $"{q.target} müşteriye servis yap",
            QuestType.EarnCoins => $"{q.target} altın kazan",
            QuestType.Upgrade => $"{q.target} yükseltme yap",
            QuestType.RushHour => "Bir Yoğun Saat atlat",
            QuestType.SpecialOrder or QuestType.SpecialOrderBig => $"{q.target} özel sipariş yetiştir",
            _ => "Yeni bir alan aç",
        };

        static string DrinkName(DrinkId id)
        {
            var d = GameDatabase.I ? GameDatabase.I.Drink(id) : null;
            return d ? d.displayName : id.ToString();
        }
    }
}
