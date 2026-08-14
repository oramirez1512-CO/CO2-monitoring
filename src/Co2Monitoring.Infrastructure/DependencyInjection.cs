using Co2Monitoring.Application.Abstractions;
using Co2Monitoring.Domain.Configuration;
using Co2Monitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Co2Monitoring.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AnomalyDetectionOptions>(
            configuration.GetSection(AnomalyDetectionOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=co2monitoring.db";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IConsumptionRecordRepository, ConsumptionRecordRepository>();

        return services;
    }
}
