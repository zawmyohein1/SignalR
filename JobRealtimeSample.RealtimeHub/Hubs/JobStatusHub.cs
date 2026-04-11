using JobRealtimeSample.RealtimeHub.Services;
using Microsoft.AspNetCore.SignalR;

namespace JobRealtimeSample.RealtimeHub.Hubs;

public sealed class JobStatusHub(DemoHubTokenService tokenService) : Hub
{
    public static string JobGroupName(string jobId) => $"job:{jobId}";

    public static string CalculationGroupName(string companyCode, string loginUserId, string calculationId)
        => $"company:{companyCode}:user:{loginUserId}:calculation:{calculationId}";

    public Task JoinJobGroup(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new HubException("A jobId is required.");
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, JobGroupName(jobId.Trim()));
    }

    public Task LeaveJobGroup(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new HubException("A jobId is required.");
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, JobGroupName(jobId.Trim()));
    }

    public Task JoinCalculationGroup(string companyCode, string loginUserId, string calculationId)
    {
        ValidateCalculationAccess(companyCode, loginUserId, calculationId);

        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            CalculationGroupName(companyCode.Trim(), loginUserId.Trim(), calculationId.Trim()));
    }

    public Task LeaveCalculationGroup(string companyCode, string loginUserId, string calculationId)
    {
        ValidateCalculationAccess(companyCode, loginUserId, calculationId);

        return Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            CalculationGroupName(companyCode.Trim(), loginUserId.Trim(), calculationId.Trim()));
    }

    private void ValidateCalculationAccess(string companyCode, string loginUserId, string calculationId)
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

        var token = GetAccessToken();

        if (!tokenService.TryValidate(token, companyCode.Trim(), loginUserId.Trim(), calculationId.Trim(), out _))
        {
            throw new HubException("The hub access token is missing or invalid for this calculation.");
        }
    }

    private string? GetAccessToken()
    {
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
