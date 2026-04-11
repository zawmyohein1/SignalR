using System;
using System.Collections.Generic;
using System.Linq;

namespace JobRealtimeSample.FrameworkApi.Models
{
    public sealed class JobInfo
    {
        public string JobId { get; set; }

        public string Status { get; set; }

        public string Message { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public List<JobStatusNotification> History { get; set; } = new List<JobStatusNotification>();

        public JobInfo Snapshot()
        {
            return new JobInfo
            {
                JobId = JobId,
                Status = Status,
                Message = Message,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                History = History.ToList()
            };
        }
    }
}
