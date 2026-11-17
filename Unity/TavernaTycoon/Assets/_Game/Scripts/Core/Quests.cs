using System;
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>Görev türleri. Kayıtta sayı olarak saklanır: yeni türler yalnızca sona eklenir.</summary>
    public enum QuestType { ServeDrink, ServeCustomers, EarnCoins, RushHour, Upgrade, Unlock, SpecialOrder, SpecialOrderBig, HireStaff, OwnMachine, UnlockZone }

    [Serializable]
    public class QuestSave
    {
        public QuestType type;
        public DrinkId drink;
        public int target, progress, rewardCoins, rewardGems;
        public bool claimed;
        /// <summary>v1.9 durum görevlerinin hedefi: görevli kimliği (HireStaff) ya da alan kimliği (UnlockZone).</summary>
        public string key = "";
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
            RefreshState();   // v1.9: kayıttan gelen durum görevleri (oyuncu yokken yapılmış olabilir)
            if (_hooked) return;
            _hooked = true;
            GameState.DrinkServed += d => Bump(q => q.type == QuestType.ServeCustomers || (q.type == QuestType.ServeDrink && q.drink == d), 1);
            GameState.CoinsGainedAt += (a, _) => { if (!_claiming) Bump(q => q.type == QuestType.EarnCoins, (int)a); };
            // GameState.Unlock alan, makine ve personel için ortak; eski "Yeni bir alan aç" sayacı yalnızca ödeme karesiyle ilerler
            GameState.ZoneUnlocked += id => { if (IsZoneId(id)) Bump(q => q.type == QuestType.Unlock, 1); RefreshState(); };
            Management.Changed += RefreshState;
        }

        static bool IsZoneId(string id) => PayZone.Find(id);

        /// <summary>v1.9: durum görevi mi (oyunun o anki durumuna bakılır, sayaç değil)?</summary>
        public static bool IsStateQuest(QuestType t) => t is QuestType.HireStaff or QuestType.OwnMachine or QuestType.UnlockZone;

        /// <summary>Durum görevinin koşulu şu an tutuyor mu?</summary>
        public static bool StateHolds(QuestSave q) => q.type switch
        {
            QuestType.HireStaff => Management.Hired(q.key),
            QuestType.OwnMachine => Management.OwnsMachine(q.drink),
            QuestType.UnlockZone => GameState.IsUnlocked(q.key),
            _ => false,
        };

        /// <summary>
        /// v1.9 (oyuncu testi "görev önceden yapıldıysa otomatik tamamlansın"): durum görevlerini oyunun mevcut durumuyla günceller.
        /// Koşul tutuyorsa görev tamamlanır (ödül alınabilir). Görev oluşturulunca, oyun açılınca ve her satın alma / açılışta çağrılır.
        /// Sayaç görevleri (X bira sat) etkilenmez, sıfırdan sayar.
        /// </summary>
        public static void RefreshState()
        {
            bool any = false;
            foreach (var q in List) any |= ApplyState(q);
            if (any) Changed?.Invoke();
        }

        static bool ApplyState(QuestSave q)
        {
            if (q == null || q.claimed || q.Done || !IsStateQuest(q.type) || !StateHolds(q)) return false;
            q.progress = q.target;
            return true;
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

            // v1.9: "Yeni bir alan aç" yerine belirli alan / görevli / makine hedefleyen durum görevleri (adayı olan türler)
            var zone = ZoneCandidate();
            var staff = StaffCandidate(rnd);
            var machine = MachineCandidate(rnd);
            var types = new List<QuestType> { QuestType.ServeDrink, QuestType.ServeCustomers, QuestType.EarnCoins, QuestType.Upgrade, QuestType.RushHour };
            if (zone != null) types.Add(QuestType.UnlockZone);
            if (staff != null) types.Add(QuestType.HireStaff);
            if (machine.HasValue) types.Add(QuestType.OwnMachine);
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
            var t = types[rnd.Next(types.Count)];
            return t switch
            {
                QuestType.UnlockZone => MakeState(t, zone),
                QuestType.HireStaff => MakeState(t, staff),
                QuestType.OwnMachine => MakeState(t, "", machine.Value),
                _ => Make(t, drinks[rnd.Next(drinks.Count)]),
            };
        }

        /// <summary>Durum görevi için alan: sıradaki hedef kare (seviyesi yetiyorsa).</summary>
        static string ZoneCandidate()
        {
            var z = PayZone.NextGoal();
            return z && z.Available ? z.id : null;
        }

        /// <summary>Durum görevi için görevli: seviyesi ve bölgesi uygun, henüz işe alınmamış (Hazine hariç).</summary>
        static string StaffCandidate(System.Random rnd)
        {
            var c = new List<string>();
            foreach (var s in Catalog.StaffList)
            {
                var r = Management.CanHire(s);
                if (r is Management.BuyResult.Ok or Management.BuyResult.NoMoney) c.Add(s.id);
            }
            return c.Count > 0 ? c[rnd.Next(c.Count)] : null;
        }

        /// <summary>Durum görevi için makine: seviyesi ve bölgesi uygun, henüz alınmamış.</summary>
        static DrinkId? MachineCandidate(System.Random rnd)
        {
            if (!GameDatabase.I) return null;
            var c = new List<DrinkId>();
            foreach (var m in Catalog.Machines)
            {
                var r = Management.CanBuyMachine(m);
                if (r is Management.BuyResult.Ok or Management.BuyResult.NoMoney) c.Add(m.drink);
            }
            return c.Count > 0 ? c[rnd.Next(c.Count)] : null;
        }

        /// <summary>
        /// v1.9: durum görevi kurar (işe al / makine al / alan aç). Koşul şimdiden tutuyorsa görev tamamlanmış gelir.
        /// key: görevli ya da alan kimliği; drink: makine.
        /// </summary>
        public static QuestSave MakeState(QuestType t, string key, DrinkId drink = DrinkId.Bira)
        {
            var q = Make(t, drink);
            q.key = key ?? "";
            q.drink = drink;
            ApplyState(q);
            return q;
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
                case QuestType.Unlock: case QuestType.HireStaff: case QuestType.OwnMachine: case QuestType.UnlockZone: q.target = 1; break;
                case QuestType.SpecialOrder: q.target = Balance.SpecialQuestSmall; break;
                case QuestType.SpecialOrderBig: q.target = Balance.SpecialQuestBig; break;
            }
            q.rewardCoins = Mathf.RoundToInt(25 * scale * scale);
            q.rewardGems = QuestGems(lv);   // v1.9: 1 + Sv/4 → 1 + Sv/8 (ilk saatte toplanan elmas yarıya; GAME_DESIGN.md §8.2)
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
            QuestType.ServeDrink => Loc.F("{0} {1} sat", q.target, DrinkName(q.drink)),
            QuestType.ServeCustomers => Loc.F("{0} müşteriye servis yap", q.target),
            QuestType.EarnCoins => Loc.F("{0} altın kazan", q.target),
            QuestType.Upgrade => Loc.F("{0} yükseltme yap", q.target),
            QuestType.RushHour => Loc.T("Bir Yoğun Saat atlat"),
            QuestType.SpecialOrder or QuestType.SpecialOrderBig => Loc.F("{0} özel sipariş yetiştir", q.target),
            QuestType.HireStaff => Loc.F("İşe al: {0}", StaffName(q.key)),
            QuestType.OwnMachine => Loc.F("Makine al: {0}", DrinkName(q.drink)),
            QuestType.UnlockZone => Loc.F("Alanı aç: {0}", ZoneName(q.key)),
            _ => Loc.T("Yeni bir alan aç"),
        };

        /// <summary>v1.9: görev başına elmas ödülü (seviyeyle yavaş artar).</summary>
        public static int QuestGems(int level) => 1 + Mathf.Max(0, level) / QuestGemLevelStep;
        /// <summary>Görev elmasının kaç seviyede bir arttığı (v1.8: 4).</summary>
        public const int QuestGemLevelStep = 8;

        static string StaffName(string id)
        {
            foreach (var s in Catalog.StaffList) if (s.id == id) return s.name;
            return id;
        }

        static string ZoneName(string id)
        {
            var z = PayZone.Find(id);
            return z ? z.DisplayLabel : Loc.T("Yeni bir alan aç");
        }

        static string DrinkName(DrinkId id)
        {
            var d = GameDatabase.I ? GameDatabase.I.Drink(id) : null;
            return d ? Loc.T(d.displayName) : id.ToString();
        }
    }
}
