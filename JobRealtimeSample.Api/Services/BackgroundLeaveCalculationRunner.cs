using JobRealtimeSample.Api.Models;
using JobRealtimeSample.Api.Options;
using Microsoft.Extensions.Options;

namespace JobRealtimeSample.Api.Services;

public sealed class BackgroundLeaveCalculationRunner(
    XmlLeaveCalculationStore store,
    RealtimeNotifier realtimeNotifier,
    IOptions<LeaveCalculationOptions> options,
    ILogger<BackgroundLeaveCalculationRunner> logger)
{
    private readonly LeaveCalculationOptions _options = options.Value;

    public void RunInBackground(string calculationId)
    {
        // The API returns immediately. The simulated leave entitlement process
        // continues outside the HTTP request and reports progress through SignalR.
        _ = Task.Run(() => RunCalculationAsync(calculationId, CancellationToken.None));
    }

    private async Task RunCalculationAsync(string calculationId, CancellationToken cancellationToken)
    {
        try
        {
            var info = store.Get(calculationId);

            if (info is null)
            {
                logger.LogWarning("Leave calculation {CalculationId} was not found.", calculationId);
                return;
            }

            await DelayAsync(_options.InitialDelaySeconds, cancellationToken);
            await PublishStatusAsync(
                calculationId,
                "Started",
                $"Leave entitlement process started for {info.CompanyCode}.",
                cancellationToken);

            await DelayAsync(_options.StepDelaySeconds, cancellationToken);
            await PublishStatusAsync(
                calculationId,
                "Loading selected employees",
                BuildEmployeeLoadingMessage(info),
                cancellationToken);

            await DelayAsync(_options.StepDelaySeconds, cancellationToken);
            await PublishStatusAsync(
                calculationId,
                "Calculating leave entitlement",
                $"Calculating {info.LeaveTypeCode} entitlement for year {info.Year}.",
                cancellationToken);

            await DelayAsync(_options.StepDelaySeconds, cancellationToken);
            await PublishStatusAsync(
                calculationId,
                "Updating leave balances",
                "Updating leave balances and preparing result summary.",
                cancellationToken);

            await DelayAsync(_options.StepDelaySeconds, cancellationToken);
            await PublishStatusAsync(
                calculationId,
                "Completed",
                "Leave entitlement process completed successfully.",
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await PublishStatusAsync(calculationId, "Failed", "Leave calculation was canceled.", CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Leave calculation {CalculationId} failed.", calculationId);
            await PublishStatusAsync(calculationId, "Failed", "Leave calculation failed. Check API logs for details.", CancellationToken.None);
        }
    }

    private async Task PublishStatusAsync(
        string calculationId,
        string status,
        string message,
        CancellationToken cancellationToken)
    {
        LeaveCalculationStatusNotification? notification = store.UpdateStatus(calculationId, status, message);

        if (notification is null)
        {
            logger.LogWarning("Could not update missing leave calculation {CalculationId}.", calculationId);
            return;
        }

        var wasSent = await realtimeNotifier.NotifyLeaveCalculationAsync(notification, cancellationToken);

        if (!wasSent)
        {
            logger.LogWarning(
                "Leave calculation {CalculationId} status {Status} was saved but not delivered to the realtime hub.",
                calculationId,
                status);
        }
    }

    private static string BuildEmployeeLoadingMessage(LeaveCalculationInfo info)
    {
        return string.Equals(info.EmployeeNo, "ALL", StringComparison.OrdinalIgnoreCase)
            ? $"Loading all employees from {info.DepartmentCode}."
            : $"Loading employee {info.EmployeeNo} from {info.DepartmentCode}.";
    }

    private static Task DelayAsync(int seconds, CancellationToken cancellationToken)
    {
        return Task.Delay(TimeSpan.FromSeconds(Math.Max(0, seconds)), cancellationToken);
    }
}

