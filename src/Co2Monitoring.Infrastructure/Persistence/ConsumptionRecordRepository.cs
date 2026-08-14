using Co2Monitoring.Application.Abstractions;
using Co2Monitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Co2Monitoring.Infrastructure.Persistence;

public class ConsumptionRecordRepository : IConsumptionRecordRepository
{
    private readonly AppDbContext _db;

    public ConsumptionRecordRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ConsumptionRecord?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.ConsumptionRecords.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<ConsumptionRecord>> GetBySiteAsync(string site, CancellationToken ct = default)
    {
        return await _db.ConsumptionRecords
            .AsNoTracking()
            .Where(x => x.Site == site)
            .OrderBy(x => x.Month)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ConsumptionRecord>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.ConsumptionRecords
            .AsNoTracking()
            .OrderBy(x => x.Site)
            .ThenBy(x => x.Month)
            .ToListAsync(ct);
    }

    public async Task<ConsumptionRecord> AddAsync(ConsumptionRecord record, CancellationToken ct = default)
    {
        _db.ConsumptionRecords.Add(record);
        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task AddRangeAsync(IEnumerable<ConsumptionRecord> records, CancellationToken ct = default)
    {
        await _db.ConsumptionRecords.AddRangeAsync(records, ct);
        await _db.SaveChangesAsync(ct);
    }
}
