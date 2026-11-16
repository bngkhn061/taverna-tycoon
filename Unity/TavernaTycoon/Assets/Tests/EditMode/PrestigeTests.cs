using NUnit.Framework;

namespace Taverna.Tests
{
    /// <summary>Prestij (Yeni Şube) çarpanları, başlangıç altını ve şube adları.</summary>
    public class PrestigeTests
    {
        const float Eps = 0.0001f;

        [Test]
        public void IncomeAndUnMultipliers_GrowPerBranch()
        {
            Assert.AreEqual(1f, Prestige.IncomeMultiplierFor(0), Eps);
            Assert.AreEqual(1f + 2 * Prestige.IncomePerBranch, Prestige.IncomeMultiplierFor(2), Eps);
            Assert.AreEqual(1f + 3 * Prestige.UnPerBranch, Prestige.UnMultiplierFor(3), Eps);
            Assert.AreEqual(1.5f, Prestige.IncomeMultiplierFor(2), Eps);
            Assert.AreEqual(2.05f, Prestige.UnMultiplierFor(3), Eps);   // v1.8: şube başına +%35
        }

        [Test]
        public void StartCoinsAndNames_FollowBranchNumber()
        {
            Assert.AreEqual(0L, Prestige.StartCoins(0));
            Assert.AreEqual(2 * Prestige.StartCoinsPerBranch, Prestige.StartCoins(2));
            Assert.AreEqual("Köy Meyhanesi", Prestige.BranchName(0));
            Assert.AreEqual("5. Şube", Prestige.BranchName(5));
            Assert.AreEqual(Balance.MaxLevel, Prestige.RequiredLevel);
        }
    }
}
