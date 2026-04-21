using Azure.Messaging.ServiceBus;

namespace Timesoft.Solution.Api.Web4.Configuration;

internal static class ServiceBusTransport
{
    public static ServiceBusTransportType Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            string.Equals(value, "AmqpTcp", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "Amqp", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceBusTransportType.AmqpTcp;
        }

        if (string.Equals(value, "AmqpWebSockets", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "WebSockets", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceBusTransportType.AmqpWebSockets;
        }

        throw new InvalidOperationException(
            $"Invalid ServiceBus:TransportType '{value}'. Expected AmqpTcp or AmqpWebSockets.");
    }
}
