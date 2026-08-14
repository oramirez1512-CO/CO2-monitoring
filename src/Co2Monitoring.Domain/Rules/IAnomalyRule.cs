using Co2Monitoring.Domain.Configuration;
using Co2Monitoring.Domain.Entities;
using Co2Monitoring.Domain.Models;

namespace Co2Monitoring.Domain.Rules;

public interface IAnomalyRule
{
    string Code { get; }

    AnomalyRuleResult? Evaluate(
        ConsumptionRecord record,
        SiteStats stats,
        AnomalyDetectionOptions options);
}
