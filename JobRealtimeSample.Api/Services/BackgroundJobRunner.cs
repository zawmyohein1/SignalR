using JobRealtimeSample.Api.Models;

namespace JobRealtimeSample.Api.Services;

public sealed class BackgroundJobRunner(
    JobService jobService,
    RealtimeNotifier realtimeNotifier,
    ILogger<BackgroundJobRunner> logger)
{
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
            logger.LogError(ex, "Background job {JobId} failed.", jobId);
            await PublishStatusAsync(jobId, "Failed", "Background task failed. Check API logs for details.", CancellationToken.None);
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
}
