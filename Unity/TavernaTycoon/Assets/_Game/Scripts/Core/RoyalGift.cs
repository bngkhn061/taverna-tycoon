using System;

namespace Taverna
{
    /// <summary>
    /// v2.4 kralın hediyesi (GAME_DESIGN.md §18.3, §15.2): "Krala Davet" sonunda Kral, izin verdiği tavernaya Kraliyet Fermanı'nı
    /// hediye eder. Ferman Meyhane'nin arka duvarına, tezgâhın üstüne asılır (<see cref="RoyalDecreeDecor"/>) ve kalıcı
    /// <see cref="IncomeBonus"/> gelir verir; prestijte (yeni şubede) kalır. "Kral'ın Dostu" kostümü ve "Efsanevi Han" unvanı da korunur.
    /// </summary>
    public static class RoyalGift
    {
        /// <summary>Kalıcı gelir bonusu (+%5).</summary>
        public const float IncomeBonus = 0.05f;

        public static event Action Changed;

        public static bool Owned(SaveData d) => d != null && d.royalDecree;
        public static bool Has => Owned(GameState.Data);

        /// <summary>Kayda göre gelir çarpanı (1 ya da 1.05).</summary>
        public static float IncomeMultiplierFor(SaveData d) => Owned(d) ? 1f + IncomeBonus : 1f;
        public static float IncomeMultiplier => IncomeMultiplierFor(GameState.Data);

        /// <summary>Hediyeyi verir; ilk kez verildiyse true.</summary>
        public static bool Grant(SaveData d)
        {
            if (d == null || d.royalDecree) return false;
            d.royalDecree = true;
            Changed?.Invoke();
            return true;
        }

        /// <summary>v2.4 öncesi kayıt: son sahneyi görmüş oyuncu hediyeyi de almış sayılır.</summary>
        public static void Migrate(SaveData d)
        {
            if (d != null && d.endingSeen) d.royalDecree = true;
        }
    }
}
