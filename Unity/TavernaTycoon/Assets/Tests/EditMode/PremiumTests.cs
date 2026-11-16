using NUnit.Framework;

namespace Taverna.Tests
{
    /// <summary>v1.8: başlangıç paketi teklifi yalnızca erken oyunda kendiliğinden açılır (geç seviyede pencerelerin yerini almasın).</summary>
    public class PremiumTests
    {
        SaveData _saved;

        [SetUp]
        public void Keep() => _saved = GameState.Data;

        [TearDown]
        public void Restore() => GameState.DebugReplace(_saved);

        static void AtLevel(int level, string shownUtc = "") =>
            GameState.DebugReplace(new SaveData { level = level, starterShownUtc = shownUtc });

        [Test]
        public void ShouldOfferStarter_InsideEarlyWindow_True()
        {
            AtLevel(Premium.StarterLevel);
            Assert.IsTrue(Premium.ShouldOfferStarter);
            AtLevel(Premium.StarterOfferMaxLevel);
            Assert.IsTrue(Premium.ShouldOfferStarter);
        }

        [Test]
        public void ShouldOfferStarter_LateLevel_False()
        {
            AtLevel(Premium.StarterOfferMaxLevel + 1);
            Assert.IsFalse(Premium.ShouldOfferStarter);
            AtLevel(29);
            Assert.IsFalse(Premium.ShouldOfferStarter);
        }

        [Test]
        public void ShouldOfferStarter_BeforeStarterLevelOrAlreadyShown_False()
        {
            AtLevel(Premium.StarterLevel - 1);
            Assert.IsFalse(Premium.ShouldOfferStarter);
            AtLevel(Premium.StarterLevel, "2026-11-12T10:00:00.0000000Z");
            Assert.IsFalse(Premium.ShouldOfferStarter);
        }
    }

    /// <summary>v1.8: Ayarlar'daki geliştirici "Ölçüm özeti" satırı yayın derlemesinde kurulmaz.</summary>
    public class SettingsPanelTests
    {
        [Test]
        public void ShowMetricsRow_ReleaseBuild_False()
        {
            Assert.IsFalse(SettingsPanel.ShowMetricsRow(isDebugBuild: false, releaseSimulation: false));
        }

        [Test]
        public void ShowMetricsRow_DevelopmentBuildWithReleaseSimulation_False()
        {
            Assert.IsFalse(SettingsPanel.ShowMetricsRow(isDebugBuild: true, releaseSimulation: true));
        }

        [Test]
        public void ShowMetricsRow_DevelopmentBuild_True()
        {
            Assert.IsTrue(SettingsPanel.ShowMetricsRow(isDebugBuild: true, releaseSimulation: false));
        }
    }
}
