namespace Co2Monitoring.Domain.Entities;

public class ConsumptionRecord
{
    public int Id { get; set; }
    public string Site { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty; // YYYY-MM
    public decimal EnergyKwh { get; set; }
    public decimal Co2Kg { get; set; }

    public decimal? Intensity =>
        EnergyKwh > 0 ? Co2Kg / EnergyKwh : null;
}
