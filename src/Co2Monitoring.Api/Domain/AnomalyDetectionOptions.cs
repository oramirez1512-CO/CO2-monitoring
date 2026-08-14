namespace Co2Monitoring.Api.Domain;

/// <summary>Thresholds from BUSINESS_RULES.md — bound from appsettings.json.</summary>
public class AnomalyDetectionOptions
{
    public const string SectionName = "AnomalyDetection";

    public ValidationOptions Validation { get; set; } = new();
    public IntensityOptions Intensity { get; set; } = new();
    public StatsOptions Stats { get; set; } = new();
}

public class ValidationOptions
{
    public decimal MinEnergyKwh { get; set; }
    public decimal MinCo2Kg { get; set; }
}

public class IntensityOptions
{
    public decimal MinKgPerKwh { get; set; } = 0.05m;
    public decimal MaxKgPerKwh { get; set; } = 0.50m;
}

public class StatsOptions
{
    public int LookbackMonths { get; set; } = 3;
    public decimal EnergyMomChangePercent { get; set; } = 50m;
    public decimal EnergyVsAvgPercent { get; set; } = 80m;
    public decimal Co2MomChangePercent { get; set; } = 50m;
    public decimal Co2VsAvgPercent { get; set; } = 80m;
    public decimal IntensityVsAvgPercent { get; set; } = 40m;
}
