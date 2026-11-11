using NUnit.Framework;

namespace Taverna.Tests
{
    /// <summary>Mutluluk hareketli ortalaması ve müşteri çarpanı (saf işlevler).</summary>
    public class HappinessTests
    {
        const float Eps = 0.0001f;

        [Test]
        public void Ema_MovesTowardScoreByAlpha()
        {
            Assert.AreEqual(63.2f, Happiness.Ema(60f, 100f, 1, 0.08f), Eps);
            Assert.AreEqual(55.2f, Happiness.Ema(60f, 0f, 1, 0.08f), Eps);
            Assert.AreEqual(66.144f, Happiness.Ema(60f, 100f, 2, 0.08f), 0.001f);
        }

        [Test]
        public void Ema_SpecialOrderCountsTwice()
        {
            float once = Happiness.Ema(Happiness.Start, Happiness.SpecialScore, 1, Happiness.Alpha);
            float special = Happiness.Ema(Happiness.Start, Happiness.SpecialScore, Happiness.SpecialRepeats, Happiness.Alpha);
            Assert.Greater(special, once);
            Assert.AreEqual(Happiness.Ema(once, Happiness.SpecialScore, 1, Happiness.Alpha), special, Eps);
        }

        [Test]
        public void Ema_StaysWithinZeroAndHundred()
        {
            Assert.AreEqual(100f, Happiness.Ema(100f, 100f, 50, Happiness.Alpha), Eps);
            Assert.AreEqual(0f, Happiness.Ema(0f, 0f, 50, Happiness.Alpha), Eps);
            Assert.LessOrEqual(Happiness.Ema(99f, 100f, 1, 1.5f), 100f);
        }

        [Test]
        public void ScoreFor_UsesPatienceThresholds()
        {
            Assert.AreEqual(Happiness.FastScore, Happiness.ScoreFor(1f));
            Assert.AreEqual(Happiness.FastScore, Happiness.ScoreFor(Happiness.FastThreshold));
            Assert.AreEqual(Happiness.NormalScore, Happiness.ScoreFor(0.5f));
            Assert.AreEqual(Happiness.SlowScore, Happiness.ScoreFor(0.1f));
        }

        [Test]
        public void EffectiveValue_DirtyTablesPenaltyIsCapped()
        {
            Assert.AreEqual(60f, Happiness.EffectiveValue(60f, 0), Eps);
            Assert.AreEqual(60f - 2 * Happiness.DirtyPenaltyPerTable, Happiness.EffectiveValue(60f, 2), Eps);
            Assert.AreEqual(60f - Happiness.MaxDirtyPenalty, Happiness.EffectiveValue(60f, 100), Eps);
            Assert.AreEqual(0f, Happiness.EffectiveValue(5f, 5), Eps);
        }

        [Test]
        public void SpawnMultiplierFor_PiecewiseLinear()
        {
            Assert.AreEqual(Happiness.MinSpawn, Happiness.SpawnMultiplierFor(0f), Eps);
            Assert.AreEqual(1f, Happiness.SpawnMultiplierFor(50f), Eps);
            Assert.AreEqual(Happiness.MaxSpawn, Happiness.SpawnMultiplierFor(100f), Eps);
            Assert.AreEqual((Happiness.MinSpawn + 1f) / 2f, Happiness.SpawnMultiplierFor(25f), Eps);
        }
    }
}
