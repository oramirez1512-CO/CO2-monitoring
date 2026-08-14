using Co2Monitoring.Api.Domain;

namespace Co2Monitoring.Api.Services.Rules;

/// <summary>R2 — intensity outside absolute range and/or far from site average.</summary>
public sealed class IntensityRule : IAnomalyRule
{
    public string Code => "R2";

    public AnomalyRuleResult? Evaluate(
        ConsumptionRecord record,
        SiteStats stats,
        AnomalyDetectionOptions options)
    {
        if (record.EnergyKwh <= 0)
        {
            return null;
        }

        var intensity = record.Co2Kg / record.EnergyKwh;
        var min = options.Intensity.MinKgPerKwh;
        var max = options.Intensity.MaxKgPerKwh;
        var outOfRange = intensity < min || intensity > max;

        var vsAvg = stats.IntensityVsAvgPercent;
        var farFromAvg = stats.PriorMonthCount > 0
            && vsAvg is not null
            && vsAvg >= options.Stats.IntensityVsAvgPercent;

        if (!outOfRange && !farFromAvg)
        {
            return null;
        }

        if (outOfRange)
        {
            return new AnomalyRuleResult
            {
                RuleCode = Code,
                Reason =
                    $"Energy–CO₂ intensity {intensity:0.###} kg/kWh is outside configured range [{min}, {max}]",
                Severity = Severity.High
            };
        }

        return new AnomalyRuleResult
        {
            RuleCode = Code,
            Reason =
                $"Energy–CO₂ intensity {intensity:0.###} kg/kWh deviates {vsAvg:0.#}% from site average {stats.AvgIntensity:0.###} (threshold {options.Stats.IntensityVsAvgPercent}%)",
            Severity = Severity.Medium
        };
    }
}
