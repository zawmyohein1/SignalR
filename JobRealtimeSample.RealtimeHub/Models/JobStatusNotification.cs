namespace JobRealtimeSample.RealtimeHub.Models;

public sealed record JobStatusNotification(
    string JobId,
    string Status,
    string Message,
    DateTimeOffset Timestamp);
