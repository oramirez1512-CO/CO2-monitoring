namespace Co2Monitoring.Api.Dtos;

public record CreateConsumptionRecordRequest(
    string Site,
    string Month,
    decimal EnergyKwh,
    decimal Co2Kg);

public record ConsumptionRecordDto(
    int Id,
    string Site,
    string Month,
    decimal EnergyKwh,
    decimal Co2Kg);

public record AnomalyAssessmentDto(
    int Id,
    bool RequiresReview,
    string Reason,
    string Severity);
