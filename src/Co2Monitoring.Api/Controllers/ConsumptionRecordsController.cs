using Co2Monitoring.Application.Abstractions;
using Co2Monitoring.Application.Dtos;
using Co2Monitoring.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Co2Monitoring.Api.Controllers;

[ApiController]
[Route("api/v1/consumption-records")]
public class ConsumptionRecordsController : ControllerBase
{
    private readonly IConsumptionRecordRepository _repository;

    public ConsumptionRecordsController(IConsumptionRecordRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ConsumptionRecordDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ConsumptionRecordDto>>> GetAll(
        [FromQuery] string? site,
        CancellationToken ct)
    {
        var records = string.IsNullOrWhiteSpace(site)
            ? await _repository.GetAllAsync(ct)
            : await _repository.GetBySiteAsync(site, ct);

        return Ok(records.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ConsumptionRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsumptionRecordDto>> GetById(int id, CancellationToken ct)
    {
        var record = await _repository.GetByIdAsync(id, ct);
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

        var created = await _repository.AddAsync(new ConsumptionRecord
        {
            Site = request.Site.Trim(),
            Month = request.Month,
            EnergyKwh = request.EnergyKwh,
            Co2Kg = request.Co2Kg
        }, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
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

        await _repository.AddRangeAsync(entities, ct);
        return StatusCode(StatusCodes.Status201Created, entities.Select(ToDto));
    }

    private static ConsumptionRecordDto ToDto(ConsumptionRecord record) =>
        new(record.Id, record.Site, record.Month, record.EnergyKwh, record.Co2Kg);

    private static bool IsValidMonth(string month) =>
        DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out _);
}
