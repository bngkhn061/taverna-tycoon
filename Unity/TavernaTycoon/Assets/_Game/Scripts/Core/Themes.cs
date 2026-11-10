using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Zemin ve duvar temaları (v0.6, oyuncu testi: "duvarlar, zeminler mağazadan satın alıp dükkanımızı güzelleştirebilelim").
    /// Bir kez satın alınan tema istendiğinde ücretsiz seçilir. Seçili temalar ambiyansa katkı verir.
    /// Sahnede her tema varyantı hazır durur (ThemeSet); seçim sadece hangisinin görüneceğini değiştirir.
    /// </summary>
    public static class Themes
    {
        public enum Kind { Floor, Wall }

        public struct Theme
        {
            public string id, name;
            public Kind kind;
            /// <summary>Prefab son eki: "" = varsayılan (PR_Building_Zemin / Duvar / Pencere).</summary>
            public string suffix;
            public int level, price;
            public float ambiance;
            /// <summary>v1.0: sadece Hazine'den (gerçek para) alınır.</summary>
            public bool premium;
        }

        public static readonly Theme[] List =
        {
            new() { id = "zemin_mese", name = "Meşe Tahta", kind = Kind.Floor, suffix = "", level = 1, price = 0, ambiance = 0 },
            new() { id = "zemin_parke", name = "Koyu Parke", kind = Kind.Floor, suffix = "_Parke", level = 3, price = 300, ambiance = 2 },
            new() { id = "zemin_tas", name = "Taş Döşeme", kind = Kind.Floor, suffix = "_Tas", level = 5, price = 600, ambiance = 4 },
            new() { id = "zemin_dama", name = "Dama Karo", kind = Kind.Floor, suffix = "_Dama", level = 8, price = 1200, ambiance = 6 },
            new() { id = "duvar_badana", name = "Krem Badana", kind = Kind.Wall, suffix = "", level = 1, price = 0, ambiance = 0 },
            new() { id = "duvar_lambri", name = "Ahşap Lambri", kind = Kind.Wall, suffix = "_Lambri", level = 4, price = 400, ambiance = 2 },
            new() { id = "duvar_tas", name = "Taş Duvar", kind = Kind.Wall, suffix = "_Tas", level = 6, price = 800, ambiance = 4 },
            new() { id = "duvar_siva", name = "Kırmızı Sıva", kind = Kind.Wall, suffix = "_Siva", level = 9, price = 1500, ambiance = 6 },
            new() { id = "zemin_altin", name = "Altın Mermer", kind = Kind.Floor, suffix = "_Altin", level = 1, price = 0, ambiance = 20, premium = true },
            new() { id = "duvar_altin", name = "Altın Duvar", kind = Kind.Wall, suffix = "_Altin", level = 1, price = 0, ambiance = 20, premium = true },
        };

        public static event Action Changed;

        public static Theme Find(string id) => Array.Find(List, t => t.id == id);
        static string Default(Kind k) => k == Kind.Floor ? "zemin_mese" : "duvar_badana";

        public static string Current(Kind k)
        {
            var id = k == Kind.Floor ? GameState.Data.floorTheme : GameState.Data.wallTheme;
            return string.IsNullOrEmpty(id) ? Default(k) : id;
        }

        public static bool Owned(Theme t) => (t.price == 0 && !t.premium) || GameState.Data.unlocked.Contains("thm_" + t.id);

        public static Management.BuyResult CanBuy(Theme t)
        {
            if (Owned(t)) return Management.BuyResult.Owned;
            if (t.premium) return Management.BuyResult.Premium;
            if (GameState.Level < t.level) return Management.BuyResult.LevelLow;
            return GameState.Coins >= t.price ? Management.BuyResult.Ok : Management.BuyResult.NoMoney;
        }

        /// <summary>Satın alır (sahip değilse) ve seçer.</summary>
        public static Management.BuyResult BuyOrSelect(Theme t)
        {
            if (!Owned(t))
            {
                var r = CanBuy(t);
                if (r != Management.BuyResult.Ok) return r;
                GameState.TrySpend(t.price);
                GameState.Data.unlocked.Add("thm_" + t.id); // Unlock() değil: "yeni alan açıldı" kutlaması çıkmasın
            }
            if (t.kind == Kind.Floor) GameState.Data.floorTheme = t.id; else GameState.Data.wallTheme = t.id;
            GameState.Save();
            Changed?.Invoke();
            GameState.NotifyChanged();
            return Management.BuyResult.Ok;
        }

        /// <summary>Seçili zemin + duvar temasının ambiyans katkısı.</summary>
        public static float Ambiance => Find(Current(Kind.Floor)).ambiance + Find(Current(Kind.Wall)).ambiance;
    }
}
