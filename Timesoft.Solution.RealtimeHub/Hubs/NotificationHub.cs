using Microsoft.AspNetCore.SignalR;
using Timesoft.Solution.RealtimeHub.Services;

namespace Timesoft.Solution.RealtimeHub.Hubs;

public sealed class NotificationHub(HubTokenService tokenService) : Hub
{
    public static string GroupName(string companyCode, string loginUserId, string calculationId)
        => $"company:{companyCode}:user:{loginUserId}:calculation:{calculationId}";

    public Task JoinCalculationGroup(string companyCode, string loginUserId, string calculationId, string hubAccessToken)
    {
        // Browser must prove it owns this calculation before joining.
        ValidateCalculationAccess(companyCode, loginUserId, calculationId, hubAccessToken);

        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            GroupName(companyCode.Trim(), loginUserId.Trim(), calculationId.Trim()));
    }

    private void ValidateCalculationAccess(string companyCode, string loginUserId, string calculationId, string? hubAccessToken)
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

        var token = string.IsNullOrWhiteSpace(hubAccessToken)
            ? GetAccessToken()
            : hubAccessToken.Trim();

        if (!tokenService.TryValidate(token, companyCode.Trim(), loginUserId.Trim(), calculationId.Trim(), out _))
        {
            throw new HubException("The hub access token is missing or invalid for this calculation.");
        }
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
}
