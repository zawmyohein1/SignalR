using JobRealtimeSample.Api.Models;
using JobRealtimeSample.Api.Options;
using Microsoft.Extensions.Options;

namespace JobRealtimeSample.Api.Services;

public sealed class BackgroundJobRunner(
    JobService jobService,
    RealtimeNotifier realtimeNotifier,
    IOptions<GenericJobOptions> options,
    ILogger<BackgroundJobRunner> logger)
{
    private readonly GenericJobOptions _options = options.Value;

    private const string StartedStatus = "Started";
    private const string ProcessingStep1Status = "Processing step 1";
    private const string ProcessingStep2Status = "Processing step 2";
    private const string ProcessingStep3Status = "Processing step 3";
    private const string CompletedStatus = "Completed";
    private const string FailedStatus = "Failed";

    private static readonly JobStep[] ProcessingSteps =
    [
        new(ProcessingStep1Status, "Preparing input and allocating work."),
        new(ProcessingStep2Status, "Running the long calculation."),
        new(ProcessingStep3Status, "Finalizing the result."),
        new(CompletedStatus, "Heavy task completed successfully.")
    ];

    public void RunInBackground(string jobId)
    {
        // The controller deliberately does not await this work. A real heavy task
        // can exceed browser, reverse proxy, or hosting timeout limits when held
        // inside one synchronous HTTP request.
        _ = Task.Run(() => RunJobAsync(jobId, CancellationToken.None));
    }

    private async Task RunJobAsync(string jobId, CancellationToken cancellationToken)
    {
        try
        {
            await DelayAsync(_options.InitialDelaySeconds, cancellationToken);
            await PublishStatusAsync(jobId, StartedStatus, "Background worker picked up the heavy task.", cancellationToken);

            foreach (var step in ProcessingSteps)
            {
                await DelayAsync(_options.StepDelaySeconds, cancellationToken);
                await PublishStatusAsync(jobId, step.Status, step.Message, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            await PublishStatusAsync(jobId, FailedStatus, "Background task was canceled.", CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Background job {JobId} failed.", jobId);
            await PublishStatusAsync(jobId, FailedStatus, "Background task failed. Check API logs for details.", CancellationToken.None);
        }
    }

    private async Task PublishStatusAsync(string jobId, string status, string message, CancellationToken cancellationToken)
    {
        JobStatusNotification? notification = jobService.UpdateStatus(jobId, status, message);

        if (notification is null)
        {
            logger.LogWarning("Could not update missing job {JobId}.", jobId);
            return;
        }

        var wasSent = await realtimeNotifier.NotifyAsync(notification, cancellationToken);

        if (!wasSent)
        {
            logger.LogWarning("Job {JobId} status {Status} was saved but not delivered to the realtime hub.", jobId, status);
        }
    }

    private static Task DelayAsync(double seconds, CancellationToken cancellationToken)
    {
        return Task.Delay(TimeSpan.FromSeconds(Math.Max(0, seconds)), cancellationToken);
    }

    private sealed record JobStep(string Status, string Message);
}
