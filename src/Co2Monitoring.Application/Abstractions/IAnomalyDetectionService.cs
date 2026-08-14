using Co2Monitoring.Domain.Entities;
using Co2Monitoring.Domain.Models;

namespace Co2Monitoring.Application.Abstractions;

public interface IAnomalyDetectionService
{
    Task<AnomalyAssessment> AssessAsync(int recordId, CancellationToken ct = default);
    Task<IReadOnlyList<AnomalyAssessment>> AssessAllAsync(CancellationToken ct = default);
    AnomalyAssessment AssessRecord(ConsumptionRecord record, IReadOnlyList<ConsumptionRecord> siteHistory);
}
