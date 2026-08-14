using Co2Monitoring.Application.Abstractions;
using Co2Monitoring.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Co2Monitoring.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ISiteStatsCalculator, SiteStatsCalculator>();
        services.AddScoped<IAnomalyDetectionService, AnomalyDetectionService>();
        // IAnomalyRule implementations are registered in Infrastructure / a Rules module later.
        return services;
    }
}
