namespace JobRealtimeSample.Api.Options;

public sealed class RealtimeHubOptions
{
    public string NotificationEndpoint { get; set; } = "https://localhost:5003/api/notifications/job-status";

    public string LeaveCalculationNotificationEndpoint { get; set; } =
        "https://localhost:5003/api/notifications/leave-calculation-status";

    public string NotificationUsername { get; set; } = "sample-api";

    public string NotificationPassword { get; set; } = "sample-secret";
}

