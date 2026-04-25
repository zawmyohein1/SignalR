using Microsoft.AspNetCore.SignalR;
using Timesoft.Solution.RealtimeHub.Services;

namespace Timesoft.Solution.RealtimeHub.Hubs;

public sealed class NotificationHub(HubTokenService tokenService) : Hub
{
    public static string GroupName(string companyCode, string loginUserId, string calculationId)
        => $"company:{companyCode}:user:{loginUserId}:calculation:{calculationId}";

    public Task JoinCalculationGroup(string companyCode, string loginUserId, string calculationId, string hubAccessToken)
    {
        var normalized = NormalizeCalculationContext(companyCode, loginUserId, calculationId);
        // Browser must prove it owns this calculation before joining.
        ValidateCalculationAccess(normalized, hubAccessToken);

        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            GroupName(normalized.CompanyCode, normalized.LoginUserId, normalized.CalculationId));
    }

    private void ValidateCalculationAccess(
        CalculationContext calculationContext,
        string? hubAccessToken)
    {
        var token = string.IsNullOrWhiteSpace(hubAccessToken)
            ? GetAccessToken()
            : hubAccessToken.Trim();

        if (!tokenService.TryValidate(
                token,
                calculationContext.CompanyCode,
                calculationContext.LoginUserId,
                calculationContext.CalculationId,
                out _))
        {
            throw new HubException("The hub access token is missing or invalid for this calculation.");
        }
    }

    private static CalculationContext NormalizeCalculationContext(
        string companyCode,
        string loginUserId,
        string calculationId)
    {
        if (string.IsNullOrWhiteSpace(companyCode))
        {
            throw new HubException("A companyCode is required.");
        }

        if (string.IsNullOrWhiteSpace(loginUserId))
        {
            throw new HubException("A loginUserId is required.");
        }

        if (string.IsNullOrWhiteSpace(calculationId))
        {
            throw new HubException("A calculationId is required.");
        }

        return new CalculationContext(companyCode.Trim(), loginUserId.Trim(), calculationId.Trim());
    }

    private string? GetAccessToken()
    {
        // SignalR JavaScript client sends bearer token as query string.
        var request = Context.GetHttpContext()?.Request;

        if (request is null)
        {
            return null;
        }

        if (request.Query.TryGetValue("access_token", out var queryToken))
        {
            return queryToken.FirstOrDefault();
        }

        var authorization = request.Headers.Authorization.ToString();

        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..]
            : null;
    }

    private sealed record CalculationContext(string CompanyCode, string LoginUserId, string CalculationId);
}
