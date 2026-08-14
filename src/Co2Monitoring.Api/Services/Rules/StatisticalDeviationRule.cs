using Co2Monitoring.Api.Domain;

namespace Co2Monitoring.Api.Services.Rules;

/// <summary>R3 — MoM and/or vs-average spikes for energy or CO₂ (needs prior months).</summary>
public sealed class StatisticalDeviationRule : IAnomalyRule
{
    public string Code => "R3";

    public AnomalyRuleResult? Evaluate(
        ConsumptionRecord record,
        SiteStats stats,
        AnomalyDetectionOptions options)
    {
        // R4: no statistical anomaly without history.
        if (stats.PriorMonthCount == 0)
        {
            return null;
        }

        var energyMom = Exceeds(stats.EnergyMomChangePercent, options.Stats.EnergyMomChangePercent);
        var energyAvg = Exceeds(stats.EnergyVsAvgPercent, options.Stats.EnergyVsAvgPercent);
        var co2Mom = Exceeds(stats.Co2MomChangePercent, options.Stats.Co2MomChangePercent);
        var co2Avg = Exceeds(stats.Co2VsAvgPercent, options.Stats.Co2VsAvgPercent);

        var energyBoth = energyMom && energyAvg;
        var co2Both = co2Mom && co2Avg;
        var any = energyMom || energyAvg || co2Mom || co2Avg;

        if (!any)
        {
            return null;
        }

        var parts = new List<string>();
        if (energyMom)
        {
            parts.Add($"energy MoM {stats.EnergyMomChangePercent:0.#}% (threshold {options.Stats.EnergyMomChangePercent}%)");
        }

        if (energyAvg)
        {
            parts.Add($"energy vs avg {stats.EnergyVsAvgPercent:0.#}% (threshold {options.Stats.EnergyVsAvgPercent}%)");
        }

        if (co2Mom)
        {
            parts.Add($"CO₂ MoM {stats.Co2MomChangePercent:0.#}% (threshold {options.Stats.Co2MomChangePercent}%)");
        }

        if (co2Avg)
        {
            parts.Add($"CO₂ vs avg {stats.Co2VsAvgPercent:0.#}% (threshold {options.Stats.Co2VsAvgPercent}%)");
        }

        var severity = energyBoth || co2Both ? Severity.High : Severity.Medium;
        var headline = energyBoth || (energyMom || energyAvg)
            ? "Energy consumption significantly exceeds historical behavior for site"
            : "CO₂ emissions significantly exceed historical behavior for site";

        return new AnomalyRuleResult
        {
            RuleCode = Code,
            Reason = $"{headline}: {string.Join("; ", parts)}",
            Severity = severity
        };
    }

    private static bool Exceeds(decimal? value, decimal threshold) =>
        value is not null && value >= threshold;
}
