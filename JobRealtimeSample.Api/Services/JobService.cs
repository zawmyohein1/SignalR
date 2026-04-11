using System.Collections.Concurrent;
using JobRealtimeSample.Api.Models;

namespace JobRealtimeSample.Api.Services;

public sealed class JobService
{
    private readonly ConcurrentDictionary<string, JobInfo> _jobs = new();

    public JobInfo CreateJob()
    {
        var now = DateTimeOffset.UtcNow;
        var job = new JobInfo
        {
            JobId = Guid.NewGuid().ToString("N"),
            Status = "Accepted",
            Message = "Heavy task started in background",
            CreatedAt = now,
            UpdatedAt = now
        };

        _jobs[job.JobId] = job;

        return job.Snapshot();
    }

    public JobInfo? GetJob(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return null;
        }

        lock (job)
        {
            return job.Snapshot();
        }
    }

    public JobStatusNotification? UpdateStatus(string jobId, string status, string message)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return null;
        }

        var notification = new JobStatusNotification(jobId, status, message, DateTimeOffset.UtcNow);

        lock (job)
        {
            job.Status = status;
            job.Message = message;
            job.UpdatedAt = notification.Timestamp;
            job.History.Add(notification);
        }

        return notification;
    }
}
