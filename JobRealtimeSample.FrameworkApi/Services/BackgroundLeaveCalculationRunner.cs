using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using JobRealtimeSample.FrameworkApi.Models;

namespace JobRealtimeSample.FrameworkApi.Services
{
    public sealed class BackgroundLeaveCalculationRunner
    {
        private readonly XmlLeaveCalculationStore _store;
        private readonly RealtimeNotifier _realtimeNotifier;
        private readonly int _initialDelaySeconds;
        private readonly int _stepDelaySeconds;

        public BackgroundLeaveCalculationRunner(XmlLeaveCalculationStore store, RealtimeNotifier realtimeNotifier)
        {
            _store = store;
            _realtimeNotifier = realtimeNotifier;
            _initialDelaySeconds = ReadSeconds("LeaveCalculationInitialDelaySeconds", 1);
            _stepDelaySeconds = ReadSeconds("LeaveCalculationStepDelaySeconds", 4);
        }

        public void RunInBackground(string calculationId)
        {
            // The HTTP request returns immediately with a calculationId.
            // The simulated leave entitlement process continues in the background.
            _ = Task.Run(() => RunCalculationAsync(calculationId, CancellationToken.None));
        }

        private async Task RunCalculationAsync(string calculationId, CancellationToken cancellationToken)
        {
            try
            {
                LeaveCalculationInfo info = _store.Get(calculationId);

                if (info == null)
                {
                    System.Diagnostics.Trace.TraceWarning("Leave calculation {0} was not found.", calculationId);
                    return;
                }

                await DelayAsync(_initialDelaySeconds, cancellationToken);
                await PublishStatusAsync(
                    calculationId,
                    "Started",
                    $"Leave entitlement process started for {info.CompanyCode}.",
                    cancellationToken);

                await DelayAsync(_stepDelaySeconds, cancellationToken);
                await PublishStatusAsync(
                    calculationId,
                    "Loading selected employees",
                    BuildEmployeeLoadingMessage(info),
                    cancellationToken);

                await DelayAsync(_stepDelaySeconds, cancellationToken);
                await PublishStatusAsync(
                    calculationId,
                    "Calculating leave entitlement",
                    $"Calculating {info.LeaveTypeCode} entitlement for year {info.Year}.",
                    cancellationToken);

                await DelayAsync(_stepDelaySeconds, cancellationToken);
                await PublishStatusAsync(
                    calculationId,
                    "Updating leave balances",
                    "Updating leave balances and preparing result summary.",
                    cancellationToken);

                await DelayAsync(_stepDelaySeconds, cancellationToken);
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
                System.Diagnostics.Trace.TraceError("Leave calculation {0} failed. {1}", calculationId, ex);
                await PublishStatusAsync(calculationId, "Failed", "Leave calculation failed. Check API logs for details.", CancellationToken.None);
            }
        }

        private async Task PublishStatusAsync(
            string calculationId,
            string status,
            string message,
            CancellationToken cancellationToken)
        {
            LeaveCalculationStatusNotification notification = _store.UpdateStatus(calculationId, status, message);

            if (notification == null)
            {
                System.Diagnostics.Trace.TraceWarning("Could not update missing leave calculation {0}.", calculationId);
                return;
            }

            bool wasSent = await _realtimeNotifier.NotifyLeaveCalculationAsync(notification, cancellationToken);

            if (!wasSent)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "Leave calculation {0} status {1} was saved but not delivered to the realtime hub.",
                    calculationId,
                    status);
            }
        }

        private static string BuildEmployeeLoadingMessage(LeaveCalculationInfo info)
        {
            if (string.Equals(info.EmployeeNo, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                return $"Loading all employees from {info.DepartmentCode}.";
            }

            return $"Loading employee {info.EmployeeNo} from {info.DepartmentCode}.";
        }

        private static Task DelayAsync(int seconds, CancellationToken cancellationToken)
        {
            return Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
        }

        private static int ReadSeconds(string key, int defaultValue)
        {
            int value;

            if (!int.TryParse(ConfigurationManager.AppSettings[key], out value) || value < 0)
            {
                return defaultValue;
            }

            return value;
        }
    }
}

