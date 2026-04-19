using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Timesoft.Solution.RealtimeHub.Models;
using Timesoft.Solution.RealtimeHub.Options;

namespace Timesoft.Solution.RealtimeHub.Services;

public sealed class NotificationConsumer(
    ServiceBusClient serviceBusClient,
    IOptions<ServiceBusOptions> options,
    NotificationPublisher notificationPublisher,
    ILogger<NotificationConsumer> logger) : BackgroundService
{
    // One processor listens to the configured queue and feeds updates into SignalR.
    private readonly ServiceBusProcessor _processor = serviceBusClient.CreateProcessor(
        options.Value.QueueName,
        new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 8,
            PrefetchCount = 16
        });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wire handlers before starting the queue processor.
        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        // Keep the hosted service alive until shutdown is requested.
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop receiving messages before releasing the processor.
        await _processor.StopProcessingAsync(cancellationToken);
        await _processor.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            // Service Bus payload is the same notification model used by the API.
            var notification = JsonSerializer.Deserialize<LeaveCalculationStatusNotification>(
                args.Message.Body.ToString(),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (notification is null)
            {
                logger.LogWarning("Received an invalid leave calculation notification.");
                await args.DeadLetterMessageAsync(args.Message, "InvalidPayload", "Could not deserialize the notification body.");
                return;
            }

            // Forward the notification to the existing SignalR push path.
            await notificationPublisher.PublishAsync(notification);
            // Mark the queue message done only after SignalR forwarding succeeds.
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process a leave calculation notification message.");
            // Let Service Bus retry the message later.
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(
            args.Exception,
            "Service Bus error on entity {EntityPath} during {ErrorSource}.",
            args.EntityPath,
            args.ErrorSource);

        return Task.CompletedTask;
    }
}
