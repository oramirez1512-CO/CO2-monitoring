using Co2Monitoring.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Co2Monitoring.Api.Data;

/// <summary>
/// Example dataset from BUSINESS_RULES.md expectations:
/// ids 4, 7, 8 → review; rest OK.
/// </summary>
public static class SampleDataSeeder
{
    public static async Task SeedIfEmptyAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.ConsumptionRecords.AnyAsync(ct))
        {
            return;
        }

        db.ConsumptionRecords.AddRange(
            // Madrid — stable then spike (id 4 → R3)
            Rec(1, "Madrid", "2026-01", 12000, 2800),
            Rec(2, "Madrid", "2026-02", 12100, 2850),
            Rec(3, "Madrid", "2026-03", 11900, 2780),
            Rec(4, "Madrid", "2026-04", 79000, 18500),

            // Barcelona — normal, then invalid (7 → R1), then intensity spike (8 → R2)
            Rec(5, "Barcelona", "2026-01", 8800, 2000),
            Rec(6, "Barcelona", "2026-02", 8900, 2050),
            Rec(7, "Barcelona", "2026-03", -900, -210),
            Rec(8, "Barcelona", "2026-04", 8800, 8500),

            // Valencia — short series, values OK (R4: no false statistical flags)
            Rec(9, "Valencia", "2026-01", 5000, 1150),
            Rec(10, "Valencia", "2026-02", 5100, 1180));

        await db.SaveChangesAsync(ct);
    }

    private static ConsumptionRecord Rec(
        int id, string site, string month, decimal energyKwh, decimal co2Kg) =>
        new()
        {
            Id = id,
            Site = site,
            Month = month,
            EnergyKwh = energyKwh,
            Co2Kg = co2Kg
        };
}
