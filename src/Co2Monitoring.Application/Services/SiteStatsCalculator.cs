using Co2Monitoring.Domain.Entities;
using Co2Monitoring.Domain.Models;

namespace Co2Monitoring.Application.Services;

/// <summary>
/// Scaffold: logic lands in the next implementation phase.
/// </summary>
public class SiteStatsCalculator : ISiteStatsCalculator
{
    public SiteStats Calculate(
        ConsumptionRecord record,
        IReadOnlyList<ConsumptionRecord> siteHistory,
        int lookbackMonths)
    {
        // TODO: media móvil, MoM e intensidad vs histórico (BUSINESS_RULES.md)
        return new SiteStats
        {
            LookbackMonths = lookbackMonths,
            PriorMonthCount = 0
        };
    }
}
