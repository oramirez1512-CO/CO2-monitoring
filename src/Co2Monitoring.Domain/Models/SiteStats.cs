using Co2Monitoring.Domain.Entities;

namespace Co2Monitoring.Domain.Models;

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
    public decimal? IntensityVsAvgPercent { get; set; }
}
