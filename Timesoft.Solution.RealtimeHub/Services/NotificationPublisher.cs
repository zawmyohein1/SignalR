using Microsoft.AspNetCore.SignalR;
using Timesoft.Solution.RealtimeHub.Hubs;
using Timesoft.Solution.RealtimeHub.Models;

namespace Timesoft.Solution.RealtimeHub.Services;

public sealed class NotificationPublisher(
    IHubContext<NotificationHub> hubContext,
    ILogger<NotificationPublisher> logger)
{
    public async Task PublishAsync(LeaveCalculationStatusNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var groupName = NotificationHub.GroupName(
            Required(notification.CompanyCode, nameof(notification.CompanyCode)),
            Required(notification.LoginUserId, nameof(notification.LoginUserId)),
            Required(notification.CalculationId, nameof(notification.CalculationId)));

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

    private static string Required(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Notification {propertyName} is required.", propertyName);
        }

        return value.Trim();
    }
}
