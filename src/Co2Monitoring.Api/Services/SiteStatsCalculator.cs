using Co2Monitoring.Api.Domain;

namespace Co2Monitoring.Api.Services;

/// <summary>Builds lookback averages / MoM for R2–R3 from prior months of the same site.</summary>
public class SiteStatsCalculator
{
    public SiteStats Calculate(
        ConsumptionRecord record,
        IReadOnlyList<ConsumptionRecord> siteHistory,
        int lookbackMonths)
    {
        var prior = siteHistory
            .Where(r => string.CompareOrdinal(r.Month, record.Month) < 0)
            .OrderBy(r => r.Month)
            .ToList();

        var lookback = prior
            .TakeLast(Math.Max(lookbackMonths, 0))
            .ToList();

        var previous = prior.LastOrDefault();

        decimal? avgEnergy = null;
        decimal? avgCo2 = null;
        decimal? avgIntensity = null;

        if (lookback.Count > 0)
        {
            avgEnergy = lookback.Average(r => r.EnergyKwh);
            avgCo2 = lookback.Average(r => r.Co2Kg);

            var intensities = lookback
                .Where(r => r.EnergyKwh > 0)
                .Select(r => r.Co2Kg / r.EnergyKwh)
                .ToList();

            if (intensities.Count > 0)
            {
                avgIntensity = intensities.Average();
            }
        }

        var currentIntensity = record.EnergyKwh > 0
            ? record.Co2Kg / record.EnergyKwh
            : (decimal?)null;

        return new SiteStats
        {
            LookbackMonths = lookbackMonths,
            PriorMonthCount = prior.Count,
            AvgEnergyKwh = avgEnergy,
            AvgCo2Kg = avgCo2,
            AvgIntensity = avgIntensity,
            PreviousMonth = previous,
            EnergyMomChangePercent = previous is null
                ? null
                : PercentDeviation(record.EnergyKwh, previous.EnergyKwh),
            Co2MomChangePercent = previous is null
                ? null
                : PercentDeviation(record.Co2Kg, previous.Co2Kg),
            EnergyVsAvgPercent = avgEnergy is null
                ? null
                : PercentDeviation(record.EnergyKwh, avgEnergy.Value),
            Co2VsAvgPercent = avgCo2 is null
                ? null
                : PercentDeviation(record.Co2Kg, avgCo2.Value),
            IntensityVsAvgPercent = currentIntensity is null || avgIntensity is null
                ? null
                : PercentDeviation(currentIntensity.Value, avgIntensity.Value)
        };
    }

    /// <summary>Absolute percent deviation of <paramref name="current"/> vs <paramref name="baseline"/>.</summary>
    internal static decimal? PercentDeviation(decimal current, decimal baseline)
    {
        if (baseline == 0m)
        {
            return null;
        }

        return Math.Abs((current - baseline) / Math.Abs(baseline) * 100m);
    }
}
