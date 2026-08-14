using Co2Monitoring.Domain.Entities;

namespace Co2Monitoring.Application.Abstractions;

public interface IConsumptionRecordRepository
{
    Task<ConsumptionRecord?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ConsumptionRecord>> GetBySiteAsync(string site, CancellationToken ct = default);
    Task<IReadOnlyList<ConsumptionRecord>> GetAllAsync(CancellationToken ct = default);
    Task<ConsumptionRecord> AddAsync(ConsumptionRecord record, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<ConsumptionRecord> records, CancellationToken ct = default);
}
