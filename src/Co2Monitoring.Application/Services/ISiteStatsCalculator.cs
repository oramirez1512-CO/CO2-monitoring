using Co2Monitoring.Domain.Entities;
using Co2Monitoring.Domain.Models;

namespace Co2Monitoring.Application.Services;

public interface ISiteStatsCalculator
{
    SiteStats Calculate(
        ConsumptionRecord record,
        IReadOnlyList<ConsumptionRecord> siteHistory,
        int lookbackMonths);
}
