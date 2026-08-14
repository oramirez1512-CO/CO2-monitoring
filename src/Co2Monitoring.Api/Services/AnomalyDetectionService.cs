using Co2Monitoring.Api.Data;
using Co2Monitoring.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Co2Monitoring.Api.Services;

/// <summary>Runs all IAnomalyRule implementations and merges severity + reasons.</summary>
public class AnomalyDetectionService
{
    private readonly AppDbContext _db;
    private readonly SiteStatsCalculator _statsCalculator;
    private readonly IEnumerable<IAnomalyRule> _rules;
    private readonly AnomalyDetectionOptions _options;

    public AnomalyDetectionService(
        AppDbContext db,
        SiteStatsCalculator statsCalculator,
        IEnumerable<IAnomalyRule> rules,
        IOptionsSnapshot<AnomalyDetectionOptions> options)
    {
        _db = db;
        _statsCalculator = statsCalculator;
        _rules = rules;
        _options = options.Value;
    }

    public async Task<AnomalyAssessment> AssessAsync(int recordId, CancellationToken ct = default)
    {
        var record = await _db.ConsumptionRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == recordId, ct)
            ?? throw new KeyNotFoundException($"Consumption record {recordId} was not found.");

        var history = await _db.ConsumptionRecords.AsNoTracking()
            .Where(x => x.Site == record.Site)
            .OrderBy(x => x.Month)
            .ToListAsync(ct);

        return AssessRecord(record, history);
    }

    public async Task<IReadOnlyList<AnomalyAssessment>> AssessAllAsync(CancellationToken ct = default)
    {
        var all = await _db.ConsumptionRecords.AsNoTracking()
            .OrderBy(x => x.Site)
            .ThenBy(x => x.Month)
            .ToListAsync(ct);

        var results = new List<AnomalyAssessment>();

        foreach (var group in all.GroupBy(r => r.Site))
        {
            var siteHistory = group.ToList();
            foreach (var record in siteHistory)
            {
                results.Add(AssessRecord(record, siteHistory));
            }
        }

        return results;
    }

    public AnomalyAssessment AssessRecord(
        ConsumptionRecord record,
        IReadOnlyList<ConsumptionRecord> siteHistory)
    {
        var stats = _statsCalculator.Calculate(
            record,
            siteHistory,
            _options.Stats.LookbackMonths);

        var hits = _rules
            .Select(rule => rule.Evaluate(record, stats, _options))
            .Where(result => result is not null)
            .Cast<AnomalyRuleResult>()
            .ToList();

        if (hits.Count == 0)
        {
            return new AnomalyAssessment
            {
                RecordId = record.Id,
                RequiresReview = false,
                Reason = string.Empty,
                Severity = Severity.None
            };
        }

        var maxSeverity = hits.Max(h => h.Severity);
        var reason = string.Join("; ", hits
            .OrderByDescending(h => h.Severity)
            .Select(h => h.Reason));

        return new AnomalyAssessment
        {
            RecordId = record.Id,
            RequiresReview = true,
            Reason = reason,
            Severity = maxSeverity
        };
    }
}
