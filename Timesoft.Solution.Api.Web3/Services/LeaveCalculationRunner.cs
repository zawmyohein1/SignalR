using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Timesoft.Solution.Api.Web3.Models;

namespace Timesoft.Solution.Api.Web3.Services
{
    public sealed class LeaveCalculationRunner
    {
        private readonly LeaveCalculationStore _store;
        private readonly NotificationPublisher _notificationPublisher;
        private readonly int _initialDelaySeconds;
        private readonly int _stepDelaySeconds;
        private readonly double _leaveCodeDelaySeconds;

        private const string StartedStatus = "Started";
        private const string CalculatingStatus = "Calculating leave entitlement";
        private const string CompletedStatus = "Completed";
        private const string FailedStatus = "Failed";

        private static readonly CalculationEmployee[] CalculationEmployees = new[]
        {
            new CalculationEmployee("001", "ANDY LOW"),
            new CalculationEmployee("002", "BEN LIM"),
            new CalculationEmployee("003", "COLIN KOH"),
            new CalculationEmployee("004", "DAVID GAN"),
            new CalculationEmployee("005", "EUGENE ONG"),
            new CalculationEmployee("006", "FRASER PANG"),
            new CalculationEmployee("101", "ANGELA GOH"),
            new CalculationEmployee("102", "BETTY CHIA"),
            new CalculationEmployee("103", "CECILIA NG"),
            new CalculationEmployee("104", "DAPHNE TAN"),
            new CalculationEmployee("105", "EMILY WONG"),
            new CalculationEmployee("106", "FIONA WONG"),
            new CalculationEmployee("801", "RACHEL WONG"),
            new CalculationEmployee("802", "SUSAN TAY"),
            new CalculationEmployee("803", "TERESA TAN"),
            new CalculationEmployee("804", "UNICE CHENG"),
            new CalculationEmployee("8040", "COPY UNICE CHENG"),
            new CalculationEmployee("805", "VIVIAN CHIA")
        };

        private static readonly string[] LeaveCodes = new[]
        {
            "ANNU",
            "SICK",
            "HOSP",
            "CHILDLVE",
            "COMP",
            "EXAM",
            "MATE",
            "PATE",
            "NPL",
            "RO",
            "SEMINAR",
            "TRAINING"
        };

        public LeaveCalculationRunner(LeaveCalculationStore store, NotificationPublisher notificationPublisher)
        {
            _store = store;
            _notificationPublisher = notificationPublisher;
            _initialDelaySeconds = ReadSeconds(
                "LeaveCalculation-InitialDelaySeconds",
                1);
            _stepDelaySeconds = ReadSeconds(
                "LeaveCalculation-StepDelaySeconds",
                4);
            _leaveCodeDelaySeconds = ReadDoubleSeconds(
                "LeaveCalculation-LeaveCodeDelaySeconds",
                3);
            SignalREnabled = ReadBoolean("SignalREnabled", true);
        }

        public bool SignalREnabled { get; }

        public void RunInBackground(string calculationId)
        {
            // The HTTP request returns immediately with a calculationId.
            // The simulated leave entitlement process continues in the background.
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
                LeaveCalculationInfo info = _store.Get(calculationId);

                if (info == null)
                {
                    System.Diagnostics.Trace.TraceWarning("Leave calculation {0} was not found.", calculationId);
                    return;
                }

                await DelayAsync(_initialDelaySeconds, cancellationToken);
                await PublishStatusAsync(
                    calculationId,
                    StartedStatus,
                    $"Leave entitlement process started for {info.CompanyCode}.",
                    cancellationToken);

                await DelayAsync(_stepDelaySeconds, cancellationToken);
                await RunEntitlementCalculationAsync(info, cancellationToken);

                await DelayAsync(_stepDelaySeconds, cancellationToken);
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
                System.Diagnostics.Trace.TraceError("Leave calculation {0} failed. {1}", calculationId, ex);
                await PublishStatusAsync(calculationId, FailedStatus, "Leave calculation failed. Check API logs for details.", CancellationToken.None);
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

            // XML history is always saved; hub notification is optional.
            if (!SignalREnabled)
            {
                return;
            }

            bool wasSent = await _notificationPublisher.NotifyLeaveCalculationAsync(notification, cancellationToken);

            if (!wasSent)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "Leave calculation {0} status {1} was saved but not delivered to the realtime hub.",
                    calculationId,
                    status);
            }
        }

        private async Task RunEntitlementCalculationAsync(
            LeaveCalculationInfo info,
            CancellationToken cancellationToken)
        {
            // Simulated work: each employee waits through the configured leave-code loop.
            foreach (CalculationEmployee employee in ResolveEmployees(info))
            {
                foreach (string _ in LeaveCodes)
                {
                    await DelayAsync(_leaveCodeDelaySeconds, cancellationToken);
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

            CalculationEmployee employee = CalculationEmployees.FirstOrDefault(
                item => string.Equals(item.EmployeeNo, info.EmployeeNo, StringComparison.OrdinalIgnoreCase));

            return new[]
            {
                employee ?? new CalculationEmployee(info.EmployeeNo, info.EmployeeNo)
            };
        }

        private static Task DelayAsync(double seconds, CancellationToken cancellationToken)
        {
            return Task.Delay(TimeSpan.FromSeconds(Math.Max(0, seconds)), cancellationToken);
        }

        private static int ReadSeconds(string key, int defaultValue)
        {
            int value;

            if (!int.TryParse(AppSettings.Read(key), out value) || value < 0)
            {
                return defaultValue;
            }

            return value;
        }

        private static double ReadDoubleSeconds(string key, double defaultValue)
        {
            double value;

            if (!double.TryParse(
                    AppSettings.Read(key),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value)
                || value < 0)
            {
                return defaultValue;
            }

            return value;
        }

        private static bool ReadBoolean(string key, bool defaultValue)
        {
            bool value;

            return bool.TryParse(AppSettings.Read(key), out value)
                ? value
                : defaultValue;
        }

        private sealed class CalculationEmployee
        {
            public CalculationEmployee(string employeeNo, string employeeName)
            {
                EmployeeNo = employeeNo;
                EmployeeName = employeeName;
            }

            public string EmployeeNo { get; }

            public string EmployeeName { get; }

            public string DisplayName => $"{EmployeeNo}-{EmployeeName}";
        }
    }
}
