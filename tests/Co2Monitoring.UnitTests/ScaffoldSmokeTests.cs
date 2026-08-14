using Co2Monitoring.Api.Domain;

namespace Co2Monitoring.UnitTests;

public class ScaffoldSmokeTests
{
    [Fact]
    public void ConsumptionRecord_Intensity_IsNull_WhenEnergyIsZero()
    {
        var record = new ConsumptionRecord { EnergyKwh = 0, Co2Kg = 100 };
        Assert.Null(record.Intensity);
    }

    [Fact]
    public void ConsumptionRecord_Intensity_IsCo2OverEnergy()
    {
        var record = new ConsumptionRecord { EnergyKwh = 100, Co2Kg = 25 };
        Assert.Equal(0.25m, record.Intensity);
    }
}
