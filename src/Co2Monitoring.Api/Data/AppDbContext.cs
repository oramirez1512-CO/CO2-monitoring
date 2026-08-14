using Co2Monitoring.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Co2Monitoring.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ConsumptionRecord> ConsumptionRecords => Set<ConsumptionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ConsumptionRecord>();

        entity.ToTable("consumption_records");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Site).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Month).HasMaxLength(7).IsRequired();
        entity.Property(x => x.EnergyKwh).HasPrecision(18, 2);
        entity.Property(x => x.Co2Kg).HasPrecision(18, 2);
        entity.Ignore(x => x.Intensity);
        entity.HasIndex(x => new { x.Site, x.Month }).IsUnique();
    }
}
