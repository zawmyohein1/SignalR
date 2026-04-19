using Microsoft.AspNetCore.SignalR;
using Timesoft.Solution.RealtimeHub.Hubs;
using Timesoft.Solution.RealtimeHub.Models;

namespace Timesoft.Solution.RealtimeHub.Services;

public sealed class JobStatusNotifier(
    IHubContext<JobStatusHub> hubContext,
    ILogger<JobStatusNotifier> logger)
{
    public async Task SendLeaveCalculationStatusAsync(LeaveCalculationStatusNotification notification)
    {
        var groupName = JobStatusHub.CalculationGroupName(
            notification.CompanyCode.Trim(),
            notification.LoginUserId.Trim(),
            notification.CalculationId.Trim());

        // Push only to browsers that joined this exact calculation group.
        await hubContext.Clients
            .Group(groupName)
            .SendAsync("LeaveCalculationStatusUpdated", notification);

        logger.LogInformation(
            "Pushed {Status} for calculation {CalculationId} to {GroupName}.",
            notification.Status,
            notification.CalculationId,
            groupName);
    }
}
