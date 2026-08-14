using Co2Monitoring.Application.Abstractions;
using Co2Monitoring.Domain.Configuration;
using Co2Monitoring.Domain.Entities;
using Co2Monitoring.Domain.Enums;
using Co2Monitoring.Domain.Models;
using Co2Monitoring.Domain.Rules;
using Microsoft.Extensions.Options;

namespace Co2Monitoring.Application.Services;

/// <summary>
/// Scaffold: orchestrates rules once R1–R3 are implemented.
/// </summary>
public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly IConsumptionRecordRepository _repository;
    private readonly ISiteStatsCalculator _statsCalculator;
    private readonly IEnumerable<IAnomalyRule> _rules;
    private readonly AnomalyDetectionOptions _options;

    public AnomalyDetectionService(
        IConsumptionRecordRepository repository,
        ISiteStatsCalculator statsCalculator,
        IEnumerable<IAnomalyRule> rules,
        IOptions<AnomalyDetectionOptions> options)
    {
        _repository = repository;
        _statsCalculator = statsCalculator;
        _rules = rules;
        _options = options.Value;
    }

    public async Task<AnomalyAssessment> AssessAsync(int recordId, CancellationToken ct = default)
    {
        var record = await _repository.GetByIdAsync(recordId, ct)
            ?? throw new KeyNotFoundException($"Consumption record {recordId} was not found.");

        var history = await _repository.GetBySiteAsync(record.Site, ct);
        return AssessRecord(record, history);
    }

    public async Task<IReadOnlyList<AnomalyAssessment>> AssessAllAsync(CancellationToken ct = default)
    {
        var all = await _repository.GetAllAsync(ct);
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
