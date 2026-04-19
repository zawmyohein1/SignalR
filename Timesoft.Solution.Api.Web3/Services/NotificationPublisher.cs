using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Timesoft.Solution.Api.Web3.Models;
using Newtonsoft.Json;

namespace Timesoft.Solution.Api.Web3.Services
{
    public sealed class NotificationPublisher : IDisposable
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly string _queueName;

        public NotificationPublisher()
        {
            var connectionString =
                AppSettings.Read(
                    "ServiceBus-ConnectionString")
                ?? string.Empty;

            _queueName =
                AppSettings.Read(
                    "ServiceBus-QueueName")
                ?? "leave-calculation-status";

            _serviceBusClient = new ServiceBusClient(connectionString);
        }

        public async Task<bool> NotifyLeaveCalculationAsync(
            LeaveCalculationStatusNotification notification,
            CancellationToken cancellationToken)
        {
            try
            {
                var json = JsonConvert.SerializeObject(notification);
                var message = new ServiceBusMessage(BinaryData.FromString(json))
                {
                    ContentType = "application/json",
                    Subject = "leave-calculation-status"
                };

                var sender = _serviceBusClient.CreateSender(_queueName);

                try
                {
                    await sender.SendMessageAsync(message, cancellationToken);
                    return true;
                }
                finally
                {
                    await sender.DisposeAsync();
                }
            }
            catch (Exception ex) when (ex is ServiceBusException || ex is TaskCanceledException)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "Could not enqueue realtime notification for leave calculation {0}. {1}",
                    notification.CalculationId,
                    ex.Message);

                return false;
            }
        }

        public void Dispose()
        {
            _serviceBusClient.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
