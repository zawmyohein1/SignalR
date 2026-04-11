namespace JobRealtimeSample.Api.Models;

public sealed class JobInfo
{
    public required string JobId { get; init; }

    public string Status { get; set; } = "Accepted";

    public string Message { get; set; } = "Heavy task started in background";

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<JobStatusNotification> History { get; set; } = [];

    public JobInfo Snapshot()
    {
        return new JobInfo
        {
            JobId = JobId,
            Status = Status,
            Message = Message,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            History = [.. History]
        };
    }
}
