using Co2Monitoring.Api.Data;
using Co2Monitoring.Api.Domain;
using Co2Monitoring.Api.Services;
using Co2Monitoring.Api.Services.Rules;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Thresholds live in appsettings.json → "AnomalyDetection".
// IOptionsSnapshot picks up file changes on the next request (reloadOnChange).
builder.Services.Configure<AnomalyDetectionOptions>(
    builder.Configuration.GetSection(AnomalyDetectionOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=co2monitoring.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddSingleton<SiteStatsCalculator>();
builder.Services.AddScoped<AnomalyDetectionService>();

builder.Services.AddSingleton<IAnomalyRule, InvalidValueRule>();
builder.Services.AddSingleton<IAnomalyRule, IntensityRule>();
builder.Services.AddSingleton<IAnomalyRule, StatisticalDeviationRule>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SampleDataSeeder.SeedIfEmptyAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/api/v1/health");

app.Run();

public partial class Program;
