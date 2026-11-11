namespace Taverna
{
    /// <summary>İçecek kimlikleri. Sıra GAME_DESIGN.md §4 ile aynıdır, araya ekleme yapma.</summary>
    // v0.7: yemekler de aynı sırayla üretilip servis edilir (DrinkDef.isFood)
    public enum DrinkId { Bira, Sira, BalSarabi, Sarap, Sut, KaraBira, Cay, Grog, ElfCiyi, EjderAtesi, Ekmek, Guvec, KozEt, Turta, YillanmisSarap, BalLikoru, GunBatimiPuncu, IzgaraBalik, KralSerbeti, SolenTabagi }   // v1.5: Sv27 Kral Şerbeti, Sv29 Şölen Tabağı (sona eklendi, kayıt uyumu)

    public enum UpgradeStat { Hiz, Stok, Kalite }
}
