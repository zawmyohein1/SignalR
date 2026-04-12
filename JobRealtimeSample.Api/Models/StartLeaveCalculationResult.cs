namespace JobRealtimeSample.Api.Models;

public sealed record StartLeaveCalculationResult(
    bool Accepted,
    StartLeaveCalculationResponse Response);
