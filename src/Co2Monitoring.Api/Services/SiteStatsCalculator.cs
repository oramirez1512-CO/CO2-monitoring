using Co2Monitoring.Api.Domain;

namespace Co2Monitoring.Api.Services;

/// <summary>Builds lookback averages / MoM for R2–R3. Logic lands next.</summary>
public class SiteStatsCalculator
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
