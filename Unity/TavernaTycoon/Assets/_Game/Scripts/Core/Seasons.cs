using System;
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>v2.2 sezon kimliği. Kayıtta yazı anahtarı tutulur (<see cref="Seasons.Def.key"/>), sayı değil.</summary>
    public enum SeasonId { None, Harvest, Winter, Spring }

    /// <summary>v2.2 sezon görevi kaydı: tür (<see cref="Seasons.QuestKind"/>), ilerleme, ödül alındı mı.</summary>
    [Serializable]
    public class SeasonQuestSave
    {
        public int kind, progress;
        public bool claimed;
    }

    /// <summary>
    /// v2.2 sezonluk etkinlikler (GAME_DESIGN.md §16, Paket 4): cihaz takvimine bağlı üç süreli sezon — Hasat Şenliği (Eylül–Ekim),
    /// Kış Festivali (Aralık–Ocak), Bahar Bayramı (Nisan–Mayıs). Sezon dışında hiçbir sezon öğesi yoktur.
    /// Her sezonda: sezon tezgâhı ve içeceği, kendiliğinden görünen süsleme, sezon müşterisi, sezon jetonu, 4 sezon görevi,
    /// jetonla alınan kalıcı dekor ve kostüm. Jetonlar sezona bağlıdır: sezon bitince sıfırlanır, sonraki sezona taşınmaz.
    /// Saf hesaplar (<see cref="Find"/>, <see cref="NextStart"/>, <see cref="Sync"/>) tarih parametresi alır ve birim testlidir.
    /// Test: "-autotest-season harvest|winter|spring|none" geliştirme derlemesinde takvimi o sezonun içine (ya da dışına) taşır.
    /// </summary>
    public static class Seasons
    {
        /// <summary>Sezon tanımı.</summary>
        public struct Def
        {
            public SeasonId id;
            /// <summary>Kayıt ve ölçüm anahtarı: harvest / winter / spring.</summary>
            public string key;
            public string name, customerName;
            /// <summary>Başlangıç ayı (1–12); sezon <see cref="LengthMonths"/> ay sürer (ay başından ay sonuna).</summary>
            public int startMonth;
            public DrinkId drink;
            /// <summary>Kalıcı sezon dekoru (Catalog.DecorList kimlikleri) ve sezon jetonuyla alınan kostüm.</summary>
            public string[] decor;
            public string costume;
            /// <summary>Sahne nesnelerinin ve ikonların eki: Hasat / Kis / Bahar (PR_Season_Hasat_Yigin, ICO_Season_Hasat).</summary>
            public string set;
            public Color color;
        }

        /// <summary>Bir sezonun süresi (ay).</summary>
        public const int LengthMonths = 2;
        /// <summary>Sezon tezgâhı, müşterisi ve görevleri bu taverna seviyesinden itibaren (etkinliklerle aynı, rehber bittikten sonra).</summary>
        public const int MinLevel = 2;
        /// <summary>Sezon içeceği satışı başına jeton.</summary>
        public const int TokensPerServe = 1;
        /// <summary>Gelen müşterinin sezon müşterisi olma olasılığı.</summary>
        public const float CustomerChance = 0.15f;
        /// <summary>Sıradan müşterinin siparişinin sezon içeceği olma olasılığı (tezgâh aynı kattaysa).</summary>
        public const float OrderChance = 0.12f;
        /// <summary>Sezon başlangıç bildiriminin saati (yerel; sessiz saatlerin dışında).</summary>
        public const int NotifyHour = 10;

        public static readonly Def[] List =
        {
            new() { id = SeasonId.Harvest, key = "harvest", name = Loc.N("Hasat Şenliği"), customerName = Loc.N("Hasat Köylüsü"), startMonth = 9,
                    drink = DrinkId.ElmaSarabi, decor = new[] { "dec_kabakfener", "dec_basak" }, costume = "barlo_hasat", set = "Hasat",
                    color = new Color32(206, 112, 34, 255) },
            new() { id = SeasonId.Winter, key = "winter", name = Loc.N("Kış Festivali"), customerName = Loc.N("Kış Gezgini"), startMonth = 12,
                    drink = DrinkId.SicakSarap, decor = new[] { "dec_kardanadam", "dec_suslucam" }, costume = "barlo_kis", set = "Kis",
                    color = new Color32(62, 108, 178, 255) },
            new() { id = SeasonId.Spring, key = "spring", name = Loc.N("Bahar Bayramı"), customerName = Loc.N("Bahar Çiçekçisi"), startMonth = 4,
                    drink = DrinkId.CicekSerbeti, decor = new[] { "dec_cicekaraba", "dec_kusevi" }, costume = "nana_bahar", set = "Bahar",
                    color = new Color32(196, 92, 140, 255) },
        };

        // ------------------------------------------------------------------ takvim (saf)
        /// <summary>Tarih bir sezonun içindeyse o sezonu, başlangıç ve bitiş gününü (dahil) verir.</summary>
        public static bool Find(DateTime date, out Def def, out DateTime start, out DateTime end)
        {
            foreach (var d in List)
            {
                int year = date.Month >= d.startMonth ? date.Year : date.Year - 1;
                var s = new DateTime(year, d.startMonth, 1);
                var e = s.AddMonths(LengthMonths).AddDays(-1);
                if (date.Date >= s && date.Date <= e) { def = d; start = s; end = e; return true; }
            }
            def = default; start = end = default;
            return false;
        }

        /// <summary>Sezonun kayıt anahtarı: "winter-2026" (Aralık 2026 – Ocak 2027). Jetonlar bu anahtara bağlıdır.</summary>
        public static string KeyFor(Def d, DateTime start) => $"{d.key}-{start.Year}";

        /// <summary>Tarihteki sezon anahtarı ("" = sezon dışı).</summary>
        public static string KeyAt(DateTime date) => Find(date, out var d, out var s, out _) ? KeyFor(d, s) : "";

        /// <summary>Kalan gün (bugün dahil): son gün 1.</summary>
        public static int DaysLeft(DateTime date, DateTime end) => Mathf.Max(0, (end.Date - date.Date).Days + 1);

        /// <summary>Tarihten sonra başlayacak ilk sezonun başlangıç günü (00:00) ve tanımı.</summary>
        public static DateTime NextStart(DateTime after, out Def def)
        {
            DateTime best = DateTime.MaxValue;
            def = List[0];
            foreach (var d in List)
            {
                var s = new DateTime(after.Year, d.startMonth, 1);
                if (s <= after.Date) s = s.AddYears(1);
                if (s < best) { best = s; def = d; }
            }
            return best;
        }

        public static Def Get(SeasonId id) => Array.Find(List, d => d.id == id);
        public static bool IsSeasonDrink(DrinkId id) => id == DrinkId.ElmaSarabi || id == DrinkId.SicakSarap || id == DrinkId.CicekSerbeti;

        // ------------------------------------------------------------------ saat (test için kaydırılabilir)
        /// <summary>Test / bot: takvimi bu güne sabitler (saat gerçek). null = cihaz tarihi.</summary>
        public static DateTime? DebugDate;
        static bool _argsRead;

        /// <summary>Şimdiki yerel zaman (zorlanmışsa o günün içinde).</summary>
        public static DateTime Now
        {
            get
            {
                ReadArgs();
                return DebugDate.HasValue ? DebugDate.Value.Date + DateTime.Now.TimeOfDay : DateTime.Now;
            }
        }

        /// <summary>"-autotest-season harvest|winter|spring|none" (yalnızca geliştirme derlemesinde otomatik test).</summary>
        static void ReadArgs()
        {
            if (_argsRead) return;
            _argsRead = true;
            if (!AutoTest.Enabled) return;
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-autotest-season");
            if (i < 0 || i + 1 >= args.Length) return;
            DebugDate = ForcedDate(args[i + 1], DateTime.Now.Year);
            Debug.Log($"[Playtest] v2.2 sezon zorlandı: {args[i + 1]} → {DebugDate:yyyy-MM-dd}");
        }

        /// <summary>Zorlanan sezonun temsilî günü: sezonun 20. günü (yaz ortası = sezon dışı).</summary>
        public static DateTime ForcedDate(string key, int year) => key switch
        {
            "harvest" => new DateTime(year, 9, 20),
            "winter" => new DateTime(year, 12, 20),
            "spring" => new DateTime(year, 4, 20),
            _ => new DateTime(year, 7, 15),
        };

        // ------------------------------------------------------------------ durum
        /// <summary>Şu an süren sezon var mı (takvim)? Süsleme bununla görünür.</summary>
        public static bool Active => Find(Now, out _, out _, out _);

        /// <summary>Süren sezon (yoksa id = None).</summary>
        public static Def Current => Find(Now, out var d, out _, out _) ? d : default;

        /// <summary>Süren sezonun kalan günü (bugün dahil; sezon yoksa 0).</summary>
        public static int DaysLeftNow => Find(Now, out _, out _, out var e) ? DaysLeft(Now, e) : 0;

        /// <summary>Süren sezonun bitiş günü.</summary>
        public static DateTime EndNow => Find(Now, out _, out _, out var e) ? e : default;

        /// <summary>Sezon oynanabilir mi: tezgâh, müşteri, jeton ve görevler (Sv2, rehber bittikten sonra)?</summary>
        public static bool Playable => Active && PlayableFor(GameState.Level, Tutorial.Active);

        /// <summary>Saf: seviye ve rehber durumuna göre sezon içeriği açık mı?</summary>
        public static bool PlayableFor(int level, bool tutorial) => level >= MinLevel && !tutorial;

        public static int Tokens => GameState.Data.seasonTokens;

        /// <summary>Jeton, görev, dekor ya da kostüm değişti.</summary>
        public static event Action Changed;
        /// <summary>Yeni bir sezon başladı (anahtar): ölçüm ve HUD duyurusu.</summary>
        public static event Action<string> Started;

        /// <summary>
        /// Saf: kaydı tarihe göre günceller. Sezon anahtarı değiştiyse önceki sezonun jetonları sona erer
        /// (<see cref="SaveData.seasonExpiredTokens"/> oyuncuya bir kez söylenir), görevler yeni sezon için kurulur. Değiştiyse true.
        /// </summary>
        public static bool Sync(SaveData d, DateTime now)
        {
            string key = KeyAt(now);
            if (d.seasonKey == key)
            {
                if (key != "" && d.seasonQuests.Count == 0) NewQuests(d);
                return false;
            }
            if (!string.IsNullOrEmpty(d.seasonKey) && d.seasonTokens > 0)
            {
                d.seasonExpiredTokens = d.seasonTokens;
                d.seasonExpiredKey = d.seasonKey;
            }
            d.seasonTokens = 0;
            d.seasonKey = key;
            d.seasonQuests.Clear();
            if (key != "") NewQuests(d);
            return true;
        }

        static float _syncT;
        /// <summary>Takvim yeniden kaç saniyede bir okunur (gece yarısı sezon değişimi).</summary>
        const float SyncSeconds = 20f;

        /// <summary>Çalışma anı: açılışta ve düzenli aralıkla kaydı takvimle eşler; yeni sezonda season_start ölçümü.</summary>
        public static void Tick(float unscaledDt, bool force = false)
        {
            _syncT -= unscaledDt;
            if (!force && _syncT > 0f) return;
            _syncT = SyncSeconds;
            Hook();
            var d = GameState.Data;
            if (!Sync(d, Now)) return;
            if (d.seasonKey != "")
            {
                Services.Track("season_start", ("sezon", d.seasonKey), ("seviye", GameState.Level), ("sube", d.branch));
                Started?.Invoke(d.seasonKey);
            }
            GameState.Save();
            Changed?.Invoke();
        }

        static bool _hooked;
        static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            DayCycle.DayEnded += _ => { if (Playable) Bump(QuestKind.Days, 1); };
        }

        // ------------------------------------------------------------------ jeton
        /// <summary>Sezon jetonu ekler (süren sezon yoksa eklenmez). Ölçüm: season_token.</summary>
        public static void AddTokens(int n, string source)
        {
            if (n <= 0 || !Active) return;
            var d = GameState.Data;
            d.seasonTokens += n;
            Services.Track("season_token", ("sezon", d.seasonKey), ("miktar", n), ("kaynak", source), ("toplam", d.seasonTokens));
            Changed?.Invoke();
        }

        /// <summary>Jeton harcar; yetmiyorsa false.</summary>
        public static bool TrySpend(int n)
        {
            var d = GameState.Data;
            if (n < 0 || d.seasonTokens < n) return false;
            d.seasonTokens -= n;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Müşteriye servis yapıldı (Customer.Serve): sezon içeceği jetonu ve görev ilerlemesi.</summary>
        public static void OnServed(CustomerDef customer, DrinkId drink)
        {
            if (!Playable) return;
            Bump(QuestKind.Serve, 1);
            if (IsSeasonDrink(drink) && drink == Current.drink)
            {
                AddTokens(TokensPerServe, "servis");
                Bump(QuestKind.SeasonDrink, 1);
            }
            if (customer && customer.seasonal) Bump(QuestKind.SeasonGuest, 1);
        }

        // ------------------------------------------------------------------ görevler
        /// <summary>Sezon görevi türleri (kayıtta sayı: yalnızca sona ekle).</summary>
        public enum QuestKind { SeasonDrink, SeasonGuest, Serve, Days }

        /// <summary>Sezon görevleri: tür, hedef, jeton ödülü (toplam 100 jeton; GAME_DESIGN.md §16.3).</summary>
        public static readonly (QuestKind kind, int target, int reward)[] QuestDefs =
        {
            (QuestKind.SeasonDrink, 25, 30), (QuestKind.SeasonGuest, 10, 25), (QuestKind.Serve, 150, 25), (QuestKind.Days, 3, 20),
        };

        static void NewQuests(SaveData d)
        {
            d.seasonQuests.Clear();
            foreach (var q in QuestDefs) d.seasonQuests.Add(new SeasonQuestSave { kind = (int)q.kind });
        }

        public static List<SeasonQuestSave> QuestList => GameState.Data.seasonQuests;
        public static int Target(SeasonQuestSave q) => QuestDefs[Mathf.Clamp(q.kind, 0, QuestDefs.Length - 1)].target;
        public static int Reward(SeasonQuestSave q) => QuestDefs[Mathf.Clamp(q.kind, 0, QuestDefs.Length - 1)].reward;
        public static bool Done(SeasonQuestSave q) => q.progress >= Target(q);

        /// <summary>Toplanmayı bekleyen sezon görevi sayısı (HUD rozeti).</summary>
        public static int ReadyCount
        {
            get { int n = 0; foreach (var q in QuestList) if (Done(q) && !q.claimed) n++; return n; }
        }

        static void Bump(QuestKind k, int n)
        {
            bool any = false;
            foreach (var q in QuestList)
                if (q.kind == (int)k && !q.claimed && !Done(q)) { q.progress = Mathf.Min(Target(q), q.progress + n); any = true; }
            if (any) Changed?.Invoke();
        }

        /// <summary>Sezon görevinin ödülünü (jeton) alır.</summary>
        public static bool Claim(int index)
        {
            if (index < 0 || index >= QuestList.Count || !Active) return false;
            int reward = ClaimReward(QuestList[index]);
            if (reward <= 0) return false;
            AddTokens(reward, "gorev");
            GameState.Save();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Saf: görev bittiyse ve alınmadıysa alındı işaretler ve jeton ödülünü döner (yoksa 0).</summary>
        public static int ClaimReward(SeasonQuestSave q)
        {
            if (q == null || !Done(q) || q.claimed) return 0;
            q.claimed = true;
            return Reward(q);
        }

        public static string QuestText(SeasonQuestSave q)
        {
            int t = Target(q);
            return (QuestKind)q.kind switch
            {
                QuestKind.SeasonDrink => Loc.F("{0} {1} sat", t, DrinkName(Current.drink)),
                QuestKind.SeasonGuest => Loc.F("{0} kez sezon müşterisine servis yap", t),
                QuestKind.Serve => Loc.F("{0} müşteriye servis yap", t),
                _ => Loc.F("Sezonda {0} gün tamamla", t),
            };
        }

        public static string DrinkName(DrinkId id)
        {
            var def = GameDatabase.I ? GameDatabase.I.Drink(id) : null;
            return def ? Loc.T(def.displayName) : id.ToString();
        }

        // ------------------------------------------------------------------ kalıcı sezon dekoru
        /// <summary>Sezon dükkânı sonucu.</summary>
        public enum Buy { Ok, NoTokens, OutOfSeason, Owned, NoSlot, Locked }

        /// <summary>Dekor hangi sezonun (yoksa None)?</summary>
        public static SeasonId SeasonOfDecor(string id)
        {
            foreach (var s in List) if (Array.IndexOf(s.decor, id) >= 0) return s.id;
            return SeasonId.None;
        }

        /// <summary>Kalıcı sezon dekorunun jeton bedeli (Catalog.Decor.price).</summary>
        public static int DecorPrice(string id) => (int)Catalog.FindDecor(id).price;

        public static Buy CanBuyDecor(string id)
        {
            if (Management.DecorOwned(id) > 0) return Buy.Owned;
            if (!Active || SeasonOfDecor(id) != Current.id) return Buy.OutOfSeason;
            if (!Playable) return Buy.Locked;
            if (Tokens < DecorPrice(id)) return Buy.NoTokens;
            if (Management.FreeDecorSlot(id) < 0) return Buy.NoSlot;
            return Buy.Ok;
        }

        /// <summary>Kalıcı sezon dekorunu jetonla alır ve yerine koyar (sezon bitince de kalır). Ölçüm: season_purchase.</summary>
        public static Buy BuyDecor(string id)
        {
            var r = CanBuyDecor(id);
            if (r != Buy.Ok) return r;
            int slot = Management.FreeDecorSlot(id);
            if (!TrySpend(DecorPrice(id))) return Buy.NoTokens;
            GameState.Data.decor.Add($"{id}:{slot}");
            if (TavernRegistry.I) TavernRegistry.I.PlaceDecor(id, slot, true);
            Services.Track("season_purchase", ("urun", id), ("tur", "dekor"), ("jeton", DecorPrice(id)), ("sezon", GameState.Data.seasonKey));
            GameState.Save();
            GameState.NotifyChanged();
            Changed?.Invoke();
            return Buy.Ok;
        }

        /// <summary>Sezon dükkânı sonucunun oyuncu metni.</summary>
        public static string BuyMessage(Buy r) => r switch
        {
            Buy.NoTokens => Loc.T("Yeterli sezon jetonu yok"),
            Buy.OutOfSeason => Loc.T("Yalnızca kendi sezonunda alınır"),
            Buy.Owned => Loc.T("Zaten alındı"),
            Buy.NoSlot => Loc.T("Yer kalmadı"),
            Buy.Locked => Loc.F("Seviye {0} gerekli", MinLevel),
            _ => "",
        };

        /// <summary>Sezonun takvim aralığı metni ("Eylül–Ekim").</summary>
        public static string Months(Def d) => d.id switch
        {
            SeasonId.Harvest => Loc.T("Eylül–Ekim"),
            SeasonId.Winter => Loc.T("Aralık–Ocak"),
            SeasonId.Spring => Loc.T("Nisan–Mayıs"),
            _ => "",
        };

        // ------------------------------------------------------------------ bildirim
        /// <summary>Sıradaki sezon başlangıç bildiriminin yerel zamanı ve metni (Notifications.Plan).</summary>
        public static DateTime NextNotifyLocal(DateTime now, out Def def)
        {
            var s = NextStart(now, out def);
            return s.AddHours(NotifyHour);
        }
    }
}
