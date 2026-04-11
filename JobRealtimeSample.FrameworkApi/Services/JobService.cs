using System;
using System.Collections.Concurrent;
using JobRealtimeSample.FrameworkApi.Models;

namespace JobRealtimeSample.FrameworkApi.Services
{
    public sealed class JobService
    {
        private readonly ConcurrentDictionary<string, JobInfo> _jobs = new ConcurrentDictionary<string, JobInfo>();

        public JobInfo CreateJob()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
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

        public JobInfo GetJob(string jobId)
        {
            if (!_jobs.TryGetValue(jobId, out JobInfo job))
            {
                return null;
            }

            lock (job)
            {
                return job.Snapshot();
            }
        }

        public JobStatusNotification UpdateStatus(string jobId, string status, string message)
        {
            if (!_jobs.TryGetValue(jobId, out JobInfo job))
            {
                return null;
            }

            var notification = new JobStatusNotification
            {
                JobId = jobId,
                Status = status,
                Message = message,
                Timestamp = DateTimeOffset.UtcNow
            };

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
}
