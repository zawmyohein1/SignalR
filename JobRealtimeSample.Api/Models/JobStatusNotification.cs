namespace JobRealtimeSample.Api.Models;

public sealed record JobStatusNotification(
    string JobId,
    string Status,
    string Message,
    DateTimeOffset Timestamp);
