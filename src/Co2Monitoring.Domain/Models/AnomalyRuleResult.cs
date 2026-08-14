using Co2Monitoring.Domain.Enums;

namespace Co2Monitoring.Domain.Models;

public class AnomalyRuleResult
{
    public string RuleCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Severity Severity { get; set; }
}
