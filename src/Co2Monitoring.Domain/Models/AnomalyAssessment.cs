using Co2Monitoring.Domain.Enums;

namespace Co2Monitoring.Domain.Models;

public class AnomalyAssessment
{
    public int RecordId { get; set; }
    public bool RequiresReview { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Severity Severity { get; set; } = Severity.None;
}
