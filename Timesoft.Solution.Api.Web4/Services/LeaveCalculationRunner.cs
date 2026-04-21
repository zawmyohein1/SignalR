using Timesoft.Solution.Api.Web4.Models;
using Timesoft.Solution.Api.Web4.Options;
using Microsoft.Extensions.Options;

namespace Timesoft.Solution.Api.Web4.Services;

public sealed class LeaveCalculationRunner(
    LeaveCalculationStore store,
    IRealtimeNotificationPublisher notificationPublisher,
    IConfiguration configuration,
    IOptions<LeaveCalculationOptions> options,
    ILogger<LeaveCalculationRunner> logger)
{
    private readonly LeaveCalculationOptions _options = options.Value;

    private const string StartedStatus = "Started";
    private const string CalculatingStatus = "Calculating leave entitlement";
    private const string CompletedStatus = "Completed";
    private const string FailedStatus = "Failed";

    private static readonly CalculationEmployee[] CalculationEmployees =
    [
        new("001", "ANDY LOW"),
        new("002", "BEN LIM"),
        new("003", "COLIN KOH"),
        new("004", "DAVID GAN"),
        new("005", "EUGENE ONG"),
        new("006", "FRASER PANG"),
        new("101", "ANGELA GOH"),
        new("102", "BETTY CHIA"),
        new("103", "CECILIA NG"),
        new("104", "DAPHNE TAN"),
        new("105", "EMILY WONG"),
        new("106", "FIONA WONG"),
        new("801", "RACHEL WONG"),
        new("802", "SUSAN TAY"),
        new("803", "TERESA TAN"),
        new("804", "UNICE CHENG"),
        new("8040", "COPY UNICE CHENG"),
        new("805", "VIVIAN CHIA")
    ];

    private static readonly string[] LeaveCodes =
    [
        "ANNU",
        "SICK",
        "HOSP",
        "CHILDLVE",
        "COMP",
        "EXAM",
        "MATE",
        "PATE",
        "NPL",
        "RO"
    ];

    public bool SignalREnabled { get; } = configuration.GetValue("SignalR:Enabled", true);

    public void RunInBackground(string calculationId)
    {
        // The API returns immediately. The simulated leave entitlement process
        // continues outside the HTTP request and reports progress through SignalR.
        _ = Task.Run(() => RunAsync(calculationId, CancellationToken.None));
    }

    public Task RunAsync(string calculationId, CancellationToken cancellationToken)
    {
        return RunCalculationAsync(calculationId, cancellationToken);
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
                StartedStatus,
                $"Leave entitlement process started for {info.CompanyCode}.",
                cancellationToken);

            await DelayAsync(_options.StepDelaySeconds, cancellationToken);
            await RunEntitlementCalculationAsync(info, cancellationToken);

            await DelayAsync(_options.StepDelaySeconds, cancellationToken);
            await PublishStatusAsync(
                calculationId,
                CompletedStatus,
                "Leave entitlement process completed successfully.",
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await PublishStatusAsync(calculationId, FailedStatus, "Leave calculation was canceled.", CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Leave calculation {CalculationId} failed.", calculationId);
            await PublishStatusAsync(calculationId, FailedStatus, "Leave calculation failed. Check API logs for details.", CancellationToken.None);
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

        // XML history is always saved; hub notification is optional.
        if (!SignalREnabled)
        {
            return;
        }

        var wasSent = await notificationPublisher.NotifyLeaveCalculationAsync(notification, cancellationToken);

        if (!wasSent)
        {
            logger.LogWarning(
                "Leave calculation {CalculationId} status {Status} was saved but not delivered to the realtime hub.",
                calculationId,
                status);
        }
    }

    private async Task RunEntitlementCalculationAsync(
        LeaveCalculationInfo info,
        CancellationToken cancellationToken)
    {
        // Simulated work: each employee waits through the configured leave-code loop.
        foreach (var employee in ResolveEmployees(info))
        {
            foreach (var _ in LeaveCodes)
            {
                await DelayAsync(_options.LeaveCodeDelaySeconds, cancellationToken);
            }

            await PublishStatusAsync(
                info.CalculationId,
                CalculatingStatus,
                $"[{info.CompanyCode}]-[{employee.DisplayName}] done.",
                cancellationToken);
        }
    }

    private static IEnumerable<CalculationEmployee> ResolveEmployees(LeaveCalculationInfo info)
    {
        if (string.Equals(info.EmployeeNo, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationEmployees;
        }

        var employee = CalculationEmployees.FirstOrDefault(
            item => string.Equals(item.EmployeeNo, info.EmployeeNo, StringComparison.OrdinalIgnoreCase));

        return [employee ?? new CalculationEmployee(info.EmployeeNo, info.EmployeeNo)];
    }

    private static Task DelayAsync(double seconds, CancellationToken cancellationToken)
    {
        return Task.Delay(TimeSpan.FromSeconds(Math.Max(0, seconds)), cancellationToken);
    }

    private sealed record CalculationEmployee(string EmployeeNo, string EmployeeName)
    {
        public string DisplayName => $"{EmployeeNo}-{EmployeeName}";
    }
}
