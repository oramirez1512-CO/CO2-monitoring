using Co2Monitoring.Api.Domain;

namespace Co2Monitoring.Api.Services.Rules;

/// <summary>R1 — energy/CO₂ below configured minimums (e.g. negatives).</summary>
public sealed class InvalidValueRule : IAnomalyRule
{
    public string Code => "R1";

    public AnomalyRuleResult? Evaluate(
        ConsumptionRecord record,
        SiteStats stats,
        AnomalyDetectionOptions options)
    {
        var minEnergy = options.Validation.MinEnergyKwh;
        var minCo2 = options.Validation.MinCo2Kg;
        var reasons = new List<string>();

        if (record.EnergyKwh < minEnergy)
        {
            reasons.Add($"energyKwh {record.EnergyKwh} is below minimum {minEnergy}");
        }

        if (record.Co2Kg < minCo2)
        {
            reasons.Add($"co2Kg {record.Co2Kg} is below minimum {minCo2}");
        }

        if (reasons.Count == 0)
        {
            return null;
        }

        return new AnomalyRuleResult
        {
            RuleCode = Code,
            Reason = "Invalid values: " + string.Join("; ", reasons),
            Severity = Severity.High
        };
    }
}
