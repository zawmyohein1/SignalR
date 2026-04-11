using System;
using System.Configuration;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using JobRealtimeSample.FrameworkApi.Models;

namespace JobRealtimeSample.FrameworkApi.Services
{
    public sealed class BackgroundJobRunner
    {
        private readonly JobService _jobService;
        private readonly RealtimeNotifier _realtimeNotifier;
        private readonly double _initialDelaySeconds;
        private readonly double _stepDelaySeconds;

        private const string StartedStatus = "Started";
        private const string ProcessingStep1Status = "Processing step 1";
        private const string ProcessingStep2Status = "Processing step 2";
        private const string ProcessingStep3Status = "Processing step 3";
        private const string CompletedStatus = "Completed";
        private const string FailedStatus = "Failed";

        private static readonly JobStep[] ProcessingSteps = new[]
        {
            new JobStep(ProcessingStep1Status, "Preparing input and allocating work."),
            new JobStep(ProcessingStep2Status, "Running the long calculation."),
            new JobStep(ProcessingStep3Status, "Finalizing the result."),
            new JobStep(CompletedStatus, "Heavy task completed successfully.")
        };

        public BackgroundJobRunner(JobService jobService, RealtimeNotifier realtimeNotifier)
        {
            _jobService = jobService;
            _realtimeNotifier = realtimeNotifier;
            _initialDelaySeconds = ReadSeconds("GenericJobInitialDelaySeconds", 1);
            _stepDelaySeconds = ReadSeconds("GenericJobStepDelaySeconds", 5);
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
                await DelayAsync(_initialDelaySeconds, cancellationToken);
                await PublishStatusAsync(jobId, StartedStatus, "Background worker picked up the heavy task.", cancellationToken);

                foreach (JobStep step in ProcessingSteps)
                {
                    await DelayAsync(_stepDelaySeconds, cancellationToken);
                    await PublishStatusAsync(jobId, step.Status, step.Message, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                await PublishStatusAsync(jobId, FailedStatus, "Background task was canceled.", CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Background job {0} failed. {1}", jobId, ex);
                await PublishStatusAsync(jobId, FailedStatus, "Background task failed. Check API logs for details.", CancellationToken.None);
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

        private static Task DelayAsync(double seconds, CancellationToken cancellationToken)
        {
            return Task.Delay(TimeSpan.FromSeconds(Math.Max(0, seconds)), cancellationToken);
        }

        private static double ReadSeconds(string key, double defaultValue)
        {
            double value;

            if (!double.TryParse(
                    ConfigurationManager.AppSettings[key],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value)
                || value < 0)
            {
                return defaultValue;
            }

            return value;
        }

        private sealed class JobStep
        {
            public JobStep(string status, string message)
            {
                Status = status;
                Message = message;
            }

            public string Status { get; }

            public string Message { get; }
        }
    }
}
