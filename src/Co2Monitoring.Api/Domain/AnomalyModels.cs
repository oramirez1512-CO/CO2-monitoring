namespace Co2Monitoring.Api.Domain;

public class AnomalyAssessment
{
    public int RecordId { get; set; }
    public bool RequiresReview { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Severity Severity { get; set; } = Severity.None;
}

public class AnomalyRuleResult
{
    public string RuleCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Severity Severity { get; set; }
}

public class SiteStats
{
    public int LookbackMonths { get; set; }
    public int PriorMonthCount { get; set; }
    public decimal? AvgEnergyKwh { get; set; }
    public decimal? AvgCo2Kg { get; set; }
    public decimal? AvgIntensity { get; set; }
    public ConsumptionRecord? PreviousMonth { get; set; }
    public decimal? EnergyMomChangePercent { get; set; }
    public decimal? Co2MomChangePercent { get; set; }
    public decimal? EnergyVsAvgPercent { get; set; }
    public decimal? Co2VsAvgPercent { get; set; }
    public decimal? IntensityVsAvgPercent { get; set; }
}

/// <summary>R1 / R2 / R3 implement this. New rules = new class, no changes to the orchestrator.</summary>
public interface IAnomalyRule
{
    string Code { get; }

    AnomalyRuleResult? Evaluate(
        ConsumptionRecord record,
        SiteStats stats,
        AnomalyDetectionOptions options);
}
