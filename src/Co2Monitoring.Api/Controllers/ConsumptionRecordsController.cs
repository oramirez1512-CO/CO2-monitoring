using Co2Monitoring.Api.Data;
using Co2Monitoring.Api.Domain;
using Co2Monitoring.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Co2Monitoring.Api.Controllers;

[ApiController]
[Route("api/v1/consumption-records")]
public class ConsumptionRecordsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ConsumptionRecordsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ConsumptionRecordDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ConsumptionRecordDto>>> GetAll(
        [FromQuery] string? site,
        CancellationToken ct)
    {
        var query = _db.ConsumptionRecords.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(site))
        {
            query = query.Where(x => x.Site == site);
        }

        var records = await query
            .OrderBy(x => x.Site)
            .ThenBy(x => x.Month)
            .ToListAsync(ct);

        return Ok(records.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ConsumptionRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsumptionRecordDto>> GetById(int id, CancellationToken ct)
    {
        var record = await _db.ConsumptionRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return record is null ? NotFound() : Ok(ToDto(record));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ConsumptionRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConsumptionRecordDto>> Create(
        [FromBody] CreateConsumptionRecordRequest request,
        CancellationToken ct)
    {
        if (!IsValidMonth(request.Month))
        {
            return BadRequest("month must use YYYY-MM format.");
        }

        var entity = new ConsumptionRecord
        {
            Site = request.Site.Trim(),
            Month = request.Month,
            EnergyKwh = request.EnergyKwh,
            Co2Kg = request.Co2Kg
        };

        _db.ConsumptionRecords.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
    }

    [HttpPost("bulk")]
    [ProducesResponseType(typeof(IEnumerable<ConsumptionRecordDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ConsumptionRecordDto>>> CreateBulk(
        [FromBody] IReadOnlyList<CreateConsumptionRecordRequest> requests,
        CancellationToken ct)
    {
        if (requests.Count == 0)
        {
            return BadRequest("At least one record is required.");
        }

        if (requests.Any(r => !IsValidMonth(r.Month)))
        {
            return BadRequest("All months must use YYYY-MM format.");
        }

        var entities = requests.Select(r => new ConsumptionRecord
        {
            Site = r.Site.Trim(),
            Month = r.Month,
            EnergyKwh = r.EnergyKwh,
            Co2Kg = r.Co2Kg
        }).ToList();

        await _db.ConsumptionRecords.AddRangeAsync(entities, ct);
        await _db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, entities.Select(ToDto));
    }

    private static ConsumptionRecordDto ToDto(ConsumptionRecord record) =>
        new(record.Id, record.Site, record.Month, record.EnergyKwh, record.Co2Kg);

    private static bool IsValidMonth(string month) =>
        DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out _);
}
