using System.Collections.Generic;

namespace Taverna
{
    /// <summary>
    /// Ölçüm (analitik) hizmeti (v1.6 yayın altyapısı). Oyun olayları <see cref="Services.Track"/> ile buraya gelir.
    /// Gerçek hizmet (Firebase, GameAnalytics, Unity Analytics…) bağlanınca bu arayüzü uygulayan bir sınıf
    /// <see cref="Services.Register(IAnalyticsService)"/> ile takılır. Olay listesi: docs/services-integration.md.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>Test / yerel uygulama mı (veri cihazdan çıkmaz)?</summary>
        bool IsTest { get; }

        /// <summary>Bir olay gönderir. Parametre değerleri string, bool, int, long, float ya da double olur.</summary>
        void Track(string eventName, IReadOnlyDictionary<string, object> parameters);

        /// <summary>Bekleyen olayları yollar / diske yazar. <paramref name="blocking"/> true ise bitene kadar bekler (kapanış).</summary>
        void Flush(bool blocking);
    }
}
