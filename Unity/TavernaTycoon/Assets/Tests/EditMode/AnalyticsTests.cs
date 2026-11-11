using System.Collections.Generic;
using NUnit.Framework;

namespace Taverna.Tests
{
    /// <summary>Yerel ölçüm: olay satırı JSON biçimi ve rehber sayaçları.</summary>
    public class AnalyticsTests
    {
        [Test]
        public void ToJsonLine_EscapesStringsAndKeepsTypes()
        {
            var line = LocalAnalyticsService.ToJsonLine(new System.DateTime(2026, 11, 11, 0, 0, 0, System.DateTimeKind.Utc), "s1", "store_request",
                new Dictionary<string, object> { { "urun", "a\"b" }, { "acik", false }, { "n", 3 }, { "f", 1.5f } });
            StringAssert.Contains("\"olay\":\"store_request\"", line);
            StringAssert.Contains("\"urun\":\"a\\\"b\"", line);
            StringAssert.Contains("\"acik\":false", line);
            StringAssert.Contains("\"n\":3", line);
            StringAssert.Contains("\"f\":1.5", line);
        }

        [Test]
        public void SlowestStep_ReturnsHighestAverage()
        {
            var c = new AnalyticsCounters();
            c.stepSeconds[0] = 10f; c.stepSamples[0] = 2;   // ort. 5
            c.stepSeconds[4] = 60f; c.stepSamples[4] = 2;   // ort. 30
            var (step, avg) = c.SlowestStep();
            Assert.AreEqual(5, step);
            Assert.AreEqual(30f, avg, 0.001f);
        }

        [Test]
        public void MostSkippedStep_NoSkips_ReturnsZero()
        {
            var c = new AnalyticsCounters();
            Assert.AreEqual(0, c.MostSkippedStep());
            c.skipAtStep[2] = 3;
            Assert.AreEqual(3, c.MostSkippedStep());
        }
    }
}
