namespace Timesoft.Solution.RealtimeHub.Models;

public sealed record HubAccessTokenClaims(
    string CompanyCode,
    string LoginUserId,
    string CalculationId,
    DateTimeOffset ExpiresAt);
