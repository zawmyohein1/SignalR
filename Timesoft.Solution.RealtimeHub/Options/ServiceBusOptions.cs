namespace Timesoft.Solution.RealtimeHub.Options;

public sealed class ServiceBusOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string QueueName { get; set; } = "leave-calculation-status";
}
