namespace Timesoft.Solution.Api.Web4.Models;

public sealed record LeaveCalculationResult(
    bool Accepted,
    LeaveCalculationResponse? Response,
    string? ValidationMessage = null);
