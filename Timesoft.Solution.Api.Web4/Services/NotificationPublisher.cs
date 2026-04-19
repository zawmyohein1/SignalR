using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Timesoft.Solution.Api.Web4.Models;
using Timesoft.Solution.Api.Web4.Options;
using Microsoft.Extensions.Options;

namespace Timesoft.Solution.Api.Web4.Services;

public sealed class NotificationPublisher(
    ServiceBusClient serviceBusClient,
    IOptions<ServiceBusOptions> options,
    ILogger<NotificationPublisher> logger)
{
    private readonly ServiceBusOptions _options = options.Value;

    public async Task<bool> NotifyLeaveCalculationAsync(
        LeaveCalculationStatusNotification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Serialize(notification);
            var message = new ServiceBusMessage(BinaryData.FromString(payload))
            {
                ContentType = "application/json",
                Subject = "leave-calculation-status"
            };

            await using var sender = serviceBusClient.CreateSender(_options.QueueName);
            await sender.SendMessageAsync(message, cancellationToken);

            return true;
        }
        catch (Exception ex) when (ex is ServiceBusException or TaskCanceledException)
        {
            logger.LogWarning(
                ex,
                "Could not enqueue realtime notification for leave calculation {CalculationId}.",
                notification.CalculationId);

            return false;
        }
    }
}
