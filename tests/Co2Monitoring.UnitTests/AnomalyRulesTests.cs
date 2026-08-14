using Co2Monitoring.Api.Domain;
using Co2Monitoring.Api.Services;
using Co2Monitoring.Api.Services.Rules;
using Microsoft.Extensions.Options;

namespace Co2Monitoring.UnitTests;

internal static class AnomalyTestHelpers
{
    public static AnomalyDetectionOptions DefaultOptions() => new()
    {
        Validation = new ValidationOptions { MinEnergyKwh = 0, MinCo2Kg = 0 },
        Intensity = new IntensityOptions { MinKgPerKwh = 0.05m, MaxKgPerKwh = 0.50m },
        Stats = new StatsOptions
        {
            LookbackMonths = 3,
            EnergyMomChangePercent = 50,
            EnergyVsAvgPercent = 80,
            Co2MomChangePercent = 50,
            Co2VsAvgPercent = 80,
            IntensityVsAvgPercent = 40
        }
    };

    public static SiteStats EmptyStats() => new() { PriorMonthCount = 0 };

    public static ConsumptionRecord Rec(
        int id, string site, string month, decimal energy, decimal co2) =>
        new()
        {
            Id = id,
            Site = site,
            Month = month,
            EnergyKwh = energy,
            Co2Kg = co2
        };
}

public class SiteStatsCalculatorTests
{
    private readonly SiteStatsCalculator _sut = new();

    [Fact]
    public void Calculate_NoPriorMonths_LeavesStatisticalFieldsNull()
    {
        var record = AnomalyTestHelpers.Rec(1, "Madrid", "2026-01", 12000, 2800);
        var stats = _sut.Calculate(record, [record], lookbackMonths: 3);

        Assert.Equal(0, stats.PriorMonthCount);
        Assert.Null(stats.PreviousMonth);
        Assert.Null(stats.AvgEnergyKwh);
        Assert.Null(stats.EnergyMomChangePercent);
    }

    [Fact]
    public void Calculate_ComputesMomAndAverages_FromPriorMonthsOnly()
    {
        var history = new List<ConsumptionRecord>
        {
            AnomalyTestHelpers.Rec(1, "Madrid", "2026-01", 10000, 2000),
            AnomalyTestHelpers.Rec(2, "Madrid", "2026-02", 10000, 2000),
            AnomalyTestHelpers.Rec(3, "Madrid", "2026-03", 10000, 2000),
            AnomalyTestHelpers.Rec(4, "Madrid", "2026-04", 20000, 4000)
        };

        var stats = _sut.Calculate(history[3], history, lookbackMonths: 3);

        Assert.Equal(3, stats.PriorMonthCount);
        Assert.Equal(10000m, stats.AvgEnergyKwh);
        Assert.Equal(2000m, stats.AvgCo2Kg);
        Assert.Equal(0.2m, stats.AvgIntensity);
        Assert.Equal(100m, stats.EnergyMomChangePercent);
        Assert.Equal(100m, stats.EnergyVsAvgPercent);
        Assert.Equal(0m, stats.IntensityVsAvgPercent);
    }
}

public class InvalidValueRuleTests
{
    private readonly InvalidValueRule _sut = new();
    private readonly AnomalyDetectionOptions _options = AnomalyTestHelpers.DefaultOptions();

    [Fact]
    public void Evaluate_NegativeValues_ReturnsHigh()
    {
        var record = AnomalyTestHelpers.Rec(7, "Barcelona", "2026-03", -900, -210);
        var result = _sut.Evaluate(record, AnomalyTestHelpers.EmptyStats(), _options);

        Assert.NotNull(result);
        Assert.Equal("R1", result!.RuleCode);
        Assert.Equal(Severity.High, result.Severity);
    }

    [Fact]
    public void Evaluate_ValidValues_ReturnsNull()
    {
        var record = AnomalyTestHelpers.Rec(1, "Madrid", "2026-01", 12000, 2800);
        Assert.Null(_sut.Evaluate(record, AnomalyTestHelpers.EmptyStats(), _options));
    }
}

public class IntensityRuleTests
{
    private readonly IntensityRule _sut = new();
    private readonly AnomalyDetectionOptions _options = AnomalyTestHelpers.DefaultOptions();

    [Fact]
    public void Evaluate_OutOfRange_ReturnsHigh()
    {
        var record = AnomalyTestHelpers.Rec(8, "Barcelona", "2026-04", 8800, 8500);
        var result = _sut.Evaluate(record, AnomalyTestHelpers.EmptyStats(), _options);

        Assert.NotNull(result);
        Assert.Equal(Severity.High, result!.Severity);
        Assert.Equal("R2", result.RuleCode);
    }

    [Fact]
    public void Evaluate_OnlyFarFromAverage_ReturnsMedium()
    {
        var record = AnomalyTestHelpers.Rec(2, "Site", "2026-02", 10000, 3000);
        var stats = new SiteStats
        {
            PriorMonthCount = 2,
            AvgIntensity = 0.20m,
            IntensityVsAvgPercent = 50m
        };

        var result = _sut.Evaluate(record, stats, _options);

        Assert.NotNull(result);
        Assert.Equal(Severity.Medium, result!.Severity);
    }
}

public class StatisticalDeviationRuleTests
{
    private readonly StatisticalDeviationRule _sut = new();
    private readonly AnomalyDetectionOptions _options = AnomalyTestHelpers.DefaultOptions();

    [Fact]
    public void Evaluate_NoHistory_ReturnsNull_R4()
    {
        var record = AnomalyTestHelpers.Rec(1, "Valencia", "2026-01", 5000, 1150);
        Assert.Null(_sut.Evaluate(record, AnomalyTestHelpers.EmptyStats(), _options));
    }

    [Fact]
    public void Evaluate_MomAndAvg_ReturnsHigh()
    {
        var record = AnomalyTestHelpers.Rec(4, "Madrid", "2026-04", 79000, 18500);
        var stats = new SiteStats
        {
            PriorMonthCount = 3,
            EnergyMomChangePercent = 558m,
            EnergyVsAvgPercent = 558m,
            Co2MomChangePercent = 558m,
            Co2VsAvgPercent = 558m
        };

        var result = _sut.Evaluate(record, stats, _options);

        Assert.NotNull(result);
        Assert.Equal(Severity.High, result!.Severity);
        Assert.Equal("R3", result.RuleCode);
    }

    [Fact]
    public void Evaluate_OnlyMom_ReturnsMedium()
    {
        var stats = new SiteStats
        {
            PriorMonthCount = 2,
            EnergyMomChangePercent = 60m,
            EnergyVsAvgPercent = 10m
        };

        var result = _sut.Evaluate(
            AnomalyTestHelpers.Rec(2, "X", "2026-02", 1, 1),
            stats,
            _options);

        Assert.NotNull(result);
        Assert.Equal(Severity.Medium, result!.Severity);
    }
}

public class SampleDatasetDetectionTests
{
    [Fact]
    public void SampleDataset_Flags_Ids_4_7_8_AsHighReview()
    {
        var records = SampleRecords();
        var service = CreateService();

        var results = records
            .GroupBy(r => r.Site)
            .SelectMany(g => g.Select(r => service.AssessRecord(r, g.ToList())))
            .ToDictionary(r => r.RecordId);

        Assert.False(results[1].RequiresReview);
        Assert.False(results[2].RequiresReview);
        Assert.False(results[3].RequiresReview);

        Assert.True(results[4].RequiresReview);
        Assert.Equal(Severity.High, results[4].Severity);

        Assert.False(results[5].RequiresReview);
        Assert.False(results[6].RequiresReview);

        Assert.True(results[7].RequiresReview);
        Assert.Equal(Severity.High, results[7].Severity);

        Assert.True(results[8].RequiresReview);
        Assert.Equal(Severity.High, results[8].Severity);

        Assert.False(results[9].RequiresReview);
        Assert.False(results[10].RequiresReview);
    }

    private static AnomalyDetectionService CreateService() =>
        new(
            db: null!,
            statsCalculator: new SiteStatsCalculator(),
            rules:
            [
                new InvalidValueRule(),
                new IntensityRule(),
                new StatisticalDeviationRule()
            ],
            options: new OptionsSnapshot(AnomalyTestHelpers.DefaultOptions()));

    private sealed class OptionsSnapshot(AnomalyDetectionOptions value)
        : IOptionsSnapshot<AnomalyDetectionOptions>
    {
        public AnomalyDetectionOptions Value { get; } = value;
        public AnomalyDetectionOptions Get(string? name) => Value;
    }

    private static List<ConsumptionRecord> SampleRecords() =>
    [
        AnomalyTestHelpers.Rec(1, "Madrid", "2026-01", 12000, 2800),
        AnomalyTestHelpers.Rec(2, "Madrid", "2026-02", 12100, 2850),
        AnomalyTestHelpers.Rec(3, "Madrid", "2026-03", 11900, 2780),
        AnomalyTestHelpers.Rec(4, "Madrid", "2026-04", 79000, 18500),
        AnomalyTestHelpers.Rec(5, "Barcelona", "2026-01", 8800, 2000),
        AnomalyTestHelpers.Rec(6, "Barcelona", "2026-02", 8900, 2050),
        AnomalyTestHelpers.Rec(7, "Barcelona", "2026-03", -900, -210),
        AnomalyTestHelpers.Rec(8, "Barcelona", "2026-04", 8800, 8500),
        AnomalyTestHelpers.Rec(9, "Valencia", "2026-01", 5000, 1150),
        AnomalyTestHelpers.Rec(10, "Valencia", "2026-02", 5100, 1180)
    ];
}
