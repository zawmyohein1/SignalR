using System;
using System.Threading;
using System.Threading.Tasks;
using JobRealtimeSample.FrameworkApi.Models;

namespace JobRealtimeSample.FrameworkApi.Services
{
    public sealed class BackgroundJobRunner
    {
        private readonly JobService _jobService;
        private readonly RealtimeNotifier _realtimeNotifier;

        public BackgroundJobRunner(JobService jobService, RealtimeNotifier realtimeNotifier)
        {
            _jobService = jobService;
            _realtimeNotifier = realtimeNotifier;
        }

        public void RunInBackground(string jobId)
        {
            // The Web API request returns immediately. The heavy work continues
            // in-process so the browser does not wait on a timeout-prone request.
            _ = Task.Run(() => RunJobAsync(jobId, CancellationToken.None));
        }

        private async Task RunJobAsync(string jobId, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                await PublishStatusAsync(jobId, "Started", "Background worker picked up the heavy task.", cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                await PublishStatusAsync(jobId, "Processing step 1", "Preparing input and allocating work.", cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                await PublishStatusAsync(jobId, "Processing step 2", "Running the long calculation.", cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                await PublishStatusAsync(jobId, "Processing step 3", "Finalizing the result.", cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                await PublishStatusAsync(jobId, "Completed", "Heavy task completed successfully.", cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await PublishStatusAsync(jobId, "Failed", "Background task was canceled.", CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Background job {0} failed. {1}", jobId, ex);
                await PublishStatusAsync(jobId, "Failed", "Background task failed. Check API logs for details.", CancellationToken.None);
            }
        }

        private async Task PublishStatusAsync(string jobId, string status, string message, CancellationToken cancellationToken)
        {
            JobStatusNotification notification = _jobService.UpdateStatus(jobId, status, message);

            if (notification == null)
            {
                System.Diagnostics.Trace.TraceWarning("Could not update missing job {0}.", jobId);
                return;
            }

            bool wasSent = await _realtimeNotifier.NotifyAsync(notification, cancellationToken);

            if (!wasSent)
            {
                System.Diagnostics.Trace.TraceWarning("Job {0} status {1} was saved but not delivered to the realtime hub.", jobId, status);
            }
        }
    }
}
