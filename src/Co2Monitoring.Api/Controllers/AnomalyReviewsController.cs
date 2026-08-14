using Co2Monitoring.Api.Domain;
using Co2Monitoring.Api.Dtos;
using Co2Monitoring.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Co2Monitoring.Api.Controllers;

[ApiController]
[Route("api/v1/anomaly-reviews")]
public class AnomalyReviewsController : ControllerBase
{
    private readonly AnomalyDetectionService _detectionService;

    public AnomalyReviewsController(AnomalyDetectionService detectionService)
    {
        _detectionService = detectionService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(IEnumerable<AnomalyAssessmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AnomalyAssessmentDto>>> AssessAll(CancellationToken ct)
    {
        var results = await _detectionService.AssessAllAsync(ct);
        return Ok(results.Select(ToDto));
    }

    [HttpPost("{id:int}")]
    [ProducesResponseType(typeof(AnomalyAssessmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnomalyAssessmentDto>> AssessById(int id, CancellationToken ct)
    {
        try
        {
            var result = await _detectionService.AssessAsync(id, ct);
            return Ok(ToDto(result));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private static AnomalyAssessmentDto ToDto(AnomalyAssessment assessment) =>
        new(
            assessment.RecordId,
            assessment.RequiresReview,
            assessment.Reason,
            assessment.Severity.ToString());
}
