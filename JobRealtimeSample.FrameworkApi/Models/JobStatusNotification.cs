using System;

namespace JobRealtimeSample.FrameworkApi.Models
{
    public sealed class JobStatusNotification
    {
        public string JobId { get; set; }

        public string Status { get; set; }

        public string Message { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}
