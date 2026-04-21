namespace Timesoft.Solution.Api.Web4.Options;

public sealed class ServiceBusOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string QueueName { get; set; } = "leave-calculation-status";

    public string TransportType { get; set; } = "AmqpTcp";
}
