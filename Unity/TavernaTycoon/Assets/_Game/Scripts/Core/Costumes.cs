using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.2 kostümler (GAME_DESIGN.md §16.5): Barlo ve personel için aynı gövde üzerine kıyafet / şapka varyantı. Yalnızca görünüm —
    /// oyunu kolaylaştırmaz. Edinim: Hazine (gerçek para, <see cref="Premium"/>; yayında mağaza yokken "Yakında"), sezon jetonu
    /// (yalnızca kendi sezonunda) ya da ödül (son sahne görülünce "Kral'ın Dostu"). Giyilen kostüm kayıtta tutulur, prestijde sıfırlanmaz.
    /// Sahnede <see cref="CostumeWearer"/> giyileni gösterir (oyun, son sahne, ana menü).
    /// </summary>
    public static class Costumes
    {
        /// <summary>Kostüm nereden edinilir.</summary>
        public enum Source { Store, Season, Reward }

        public struct Costume
        {
            public string id;
            /// <summary>Kimin kostümü: "barlo" ya da personel kimliği (staff_pip, staff_gus, staff_ayla, staff_nana).</summary>
            public string owner;
            public string name, desc;
            /// <summary>Model ve ikon eki: CH_Costume_&lt;key&gt;, ICO_Cos_&lt;key&gt;.</summary>
            public string key;
            public Source source;
            /// <summary>Hazine ürün kimliği (Source.Store).</summary>
            public string product;
            /// <summary>Sezon (Source.Season) ve jeton bedeli.</summary>
            public SeasonId season;
            public int tokens;
        }

        /// <summary>Kostüm sahiplerinin sırası (Gardırop).</summary>
        public static readonly string[] Owners = { "barlo", "staff_pip", "staff_gus", "staff_ayla", "staff_nana" };

        public static readonly Costume[] List =
        {
            new() { id = "barlo_korsan", owner = "barlo", name = Loc.N("Korsan"), desc = Loc.N("Üç köşeli şapka, göz bandı, kaptan ceketi"), key = "Barlo_Korsan", source = Source.Store, product = "cos_barlo_korsan" },
            new() { id = "barlo_sovalye", owner = "barlo", name = Loc.N("Şövalye"), desc = Loc.N("Sorguçlu miğfer ve parlak zırh"), key = "Barlo_Sovalye", source = Source.Store, product = "cos_barlo_sovalye" },
            new() { id = "barlo_kis", owner = "barlo", name = Loc.N("Kış Festivali"), desc = Loc.N("Ponponlu kış şapkası ve atkı"), key = "Barlo_Kis", source = Source.Season, season = SeasonId.Winter, tokens = 120 },
            new() { id = "barlo_hasat", owner = "barlo", name = Loc.N("Hasat Çiftçisi"), desc = Loc.N("Hasır şapka, tulum, ağızda başak"), key = "Barlo_Hasat", source = Source.Season, season = SeasonId.Harvest, tokens = 120 },
            new() { id = "barlo_kral", owner = "barlo", name = Loc.N("Kral'ın Dostu"), desc = Loc.N("Mor bere, pelerin ve kraliyet madalyonu"), key = "Barlo_Kral", source = Source.Reward },
            new() { id = "pip_yelek", owner = "staff_pip", name = Loc.N("Zarif Yelek"), desc = Loc.N("Bordo yelek, papyon, taranmış saç"), key = "Pip_Yelek", source = Source.Store, product = "cos_pip_yelek" },
            new() { id = "gus_usta", owner = "staff_gus", name = Loc.N("Şık Barmen"), desc = Loc.N("Silindir şapka ve tek gözlük"), key = "Gus_Usta", source = Source.Store, product = "cos_gus_usta" },
            new() { id = "ayla_sef", owner = "staff_ayla", name = Loc.N("Büyük Şef"), desc = Loc.N("Kocaman şef şapkası, altın rozet"), key = "Ayla_Sef", source = Source.Store, product = "cos_ayla_sef" },
            new() { id = "nana_bahar", owner = "staff_nana", name = Loc.N("Bahar Çiçeği"), desc = Loc.N("Çiçek tacı ve pembe şal"), key = "Nana_Bahar", source = Source.Season, season = SeasonId.Spring, tokens = 100 },
        };

        /// <summary>Giyme / çıkarma / edinme.</summary>
        public static event Action Changed;

        public static Costume Find(string id) => Array.Find(List, c => c.id == id);
        public static Costume FindByProduct(string product) => Array.Find(List, c => c.product == product);
        public static string Prefab(Costume c) => "CH_Costume_" + c.key;
        public static string Icon(Costume c) => "Cos_" + c.key;

        /// <summary>Sahibin görünen adı ("Barlo", "Pip · Garson").</summary>
        public static string OwnerName(string owner)
        {
            if (owner == "barlo") return "Barlo";
            foreach (var s in Catalog.StaffList) if (s.id == owner) return $"{s.name} · {Loc.T(s.roleName)}";
            return owner;
        }

        // ------------------------------------------------------------------ sahiplik (saf: kayıt üzerinden)
        /// <summary>Kostüm edinildi mi? Ödül kostümü şartı tutunca (son sahne görüldü) kendiliğinden sahiplidir.</summary>
        public static bool Owned(SaveData d, Costume c) => !string.IsNullOrEmpty(c.id) && (d.costumes.Contains(c.id) || (c.source == Source.Reward && d.endingSeen));
        public static bool Owned(string id) => Owned(GameState.Data, Find(id));

        /// <summary>Sahibin giydiği kostüm kimliği ("" = kendi kıyafeti).</summary>
        public static string Worn(SaveData d, string owner)
        {
            string prefix = owner + "=";
            foreach (var w in d.wornCostumes) if (w.StartsWith(prefix)) return w.Substring(prefix.Length);
            return "";
        }
        public static string Worn(string owner) => Worn(GameState.Data, owner);

        /// <summary>Saf: kostümü giydirir (sahiplik şart; aynı sahibin önceki kostümü çıkar). Başarılıysa true.</summary>
        public static bool Equip(SaveData d, string id)
        {
            var c = Find(id);
            if (string.IsNullOrEmpty(c.id) || !Owned(d, c)) return false;
            Unequip(d, c.owner);
            d.wornCostumes.Add($"{c.owner}={c.id}");
            return true;
        }

        /// <summary>Saf: sahibin kostümünü çıkarır (kendi kıyafetine döner).</summary>
        public static bool Unequip(SaveData d, string owner) => d.wornCostumes.RemoveAll(w => w.StartsWith(owner + "=")) > 0;

        /// <summary>Saf: kostümü kayda sahipli olarak ekler.</summary>
        public static void GrantTo(SaveData d, string id)
        {
            if (!d.costumes.Contains(id)) d.costumes.Add(id);
        }

        // ------------------------------------------------------------------ çalışma anı
        /// <summary>Giydirir, kaydeder; ölçüm: costume_equip.</summary>
        public static bool Wear(string id)
        {
            if (!Equip(GameState.Data, id)) return false;
            var c = Find(id);
            Services.Track("costume_equip", ("kostum", id), ("sahip", c.owner), ("kaynak", SourceId(c.source)));
            GameState.Save();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Kostümü çıkarır, kaydeder; ölçüm: costume_equip (kostum = "").</summary>
        public static void TakeOff(string owner)
        {
            if (!Unequip(GameState.Data, owner)) return;
            Services.Track("costume_equip", ("kostum", ""), ("sahip", owner), ("kaynak", "cikar"));
            GameState.Save();
            Changed?.Invoke();
        }

        /// <summary>Hazine ödemesi onaylandı (Premium.Grant): kostüm verilir ve hemen giyilir. Ölçüm: costume_purchase.</summary>
        public static void GrantFromStore(string productId)
        {
            var c = FindByProduct(productId);
            if (string.IsNullOrEmpty(c.id)) return;
            GrantTo(GameState.Data, c.id);
            Equip(GameState.Data, c.id);
            Services.Track("costume_purchase", ("kostum", c.id), ("kaynak", "hazine"));
            Services.Track("costume_equip", ("kostum", c.id), ("sahip", c.owner), ("kaynak", "hazine"));
            Changed?.Invoke();
        }

        /// <summary>Sezon kostümü alınabilir mi (kendi sezonunda, jeton yetiyorsa)?</summary>
        public static Seasons.Buy CanBuyWithTokens(Costume c)
        {
            if (Owned(GameState.Data, c)) return Seasons.Buy.Owned;
            if (c.source != Source.Season || !Seasons.Active || Seasons.Current.id != c.season) return Seasons.Buy.OutOfSeason;
            if (!Seasons.Playable) return Seasons.Buy.Locked;
            return Seasons.Tokens >= c.tokens ? Seasons.Buy.Ok : Seasons.Buy.NoTokens;
        }

        /// <summary>Sezon kostümünü jetonla alır ve giydirir. Ölçüm: costume_purchase ve season_purchase.</summary>
        public static Seasons.Buy BuyWithTokens(string id)
        {
            var c = Find(id);
            var r = CanBuyWithTokens(c);
            if (r != Seasons.Buy.Ok) return r;
            if (!Seasons.TrySpend(c.tokens)) return Seasons.Buy.NoTokens;
            GrantTo(GameState.Data, c.id);
            Equip(GameState.Data, c.id);
            Services.Track("costume_purchase", ("kostum", c.id), ("kaynak", "jeton"), ("jeton", c.tokens));
            Services.Track("costume_equip", ("kostum", c.id), ("sahip", c.owner), ("kaynak", "jeton"));
            Services.Track("season_purchase", ("urun", c.id), ("tur", "kostum"), ("jeton", c.tokens), ("sezon", GameState.Data.seasonKey));
            GameState.Save();
            Changed?.Invoke();
            return Seasons.Buy.Ok;
        }

        /// <summary>Kostüm edinme yolunun metni (Gardırop kartı).</summary>
        public static string SourceText(Costume c) => c.source switch
        {
            Source.Store => Loc.T("Hazine"),
            Source.Season => Loc.F("{0} · {1}", Loc.T(Seasons.Get(c.season).name), Seasons.Months(Seasons.Get(c.season))),
            _ => Loc.T("Ödül: Efsanevi Han (son sahne)"),
        };

        static string SourceId(Source s) => s switch { Source.Store => "hazine", Source.Season => "jeton", _ => "odul" };

        /// <summary>Kaydın kostüm sayıları (test günlüğü).</summary>
        public static string DebugInfo => $"sahip [{string.Join(",", GameState.Data.costumes)}] · giyilen [{string.Join(",", GameState.Data.wornCostumes)}]";

    }
}
