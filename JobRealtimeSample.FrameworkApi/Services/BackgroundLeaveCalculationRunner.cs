using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
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
        private readonly double _leaveCodeDelaySeconds;

        private const string StartedStatus = "Started";
        private const string CalculatingStatus = "Calculating leave entitlement";
        private const string CompletedStatus = "Completed";
        private const string FailedStatus = "Failed";

        private static readonly DemoEmployee[] DemoEmployees = new[]
        {
            new DemoEmployee("001", "ANDY LOW"),
            new DemoEmployee("002", "BEN LIM"),
            new DemoEmployee("003", "COLIN KOH"),
            new DemoEmployee("004", "DAVID GAN"),
            new DemoEmployee("005", "EUGENE ONG"),
            new DemoEmployee("006", "FRASER PANG"),
            new DemoEmployee("101", "ANGELA GOH"),
            new DemoEmployee("102", "BETTY CHIA"),
            new DemoEmployee("103", "CECILIA NG"),
            new DemoEmployee("104", "DAPHNE TAN"),
            new DemoEmployee("105", "EMILY WONG"),
            new DemoEmployee("106", "FIONA WONG"),
            new DemoEmployee("801", "RACHEL WONG"),
            new DemoEmployee("802", "SUSAN TAY"),
            new DemoEmployee("803", "TERESA TAN"),
            new DemoEmployee("804", "UNICE CHENG"),
            new DemoEmployee("8040", "COPY UNICE CHENG"),
            new DemoEmployee("805", "VIVIAN CHIA")
        };

        private static readonly string[] DemoLeaveCodes = new[]
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

        public BackgroundLeaveCalculationRunner(XmlLeaveCalculationStore store, RealtimeNotifier realtimeNotifier)
        {
            _store = store;
            _realtimeNotifier = realtimeNotifier;
            _initialDelaySeconds = ReadSeconds("LeaveCalculationInitialDelaySeconds", 1);
            _stepDelaySeconds = ReadSeconds("LeaveCalculationStepDelaySeconds", 4);
            _leaveCodeDelaySeconds = ReadDoubleSeconds("LeaveCalculationLeaveCodeDelaySeconds", 3);
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

            bool wasSent = await _realtimeNotifier.NotifyLeaveCalculationAsync(notification, cancellationToken);

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
            foreach (DemoEmployee employee in ResolveEmployees(info))
            {
                foreach (string _ in DemoLeaveCodes)
                {
                    await DelayAsync(_leaveCodeDelaySeconds, cancellationToken);
                }

                await PublishStatusAsync(
                    info.CalculationId,
                    CalculatingStatus,
                    $"[{employee.DisplayName}] done.",
                    cancellationToken);
            }
        }

        private static IEnumerable<DemoEmployee> ResolveEmployees(LeaveCalculationInfo info)
        {
            if (string.Equals(info.EmployeeNo, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                return DemoEmployees;
            }

            DemoEmployee employee = DemoEmployees.FirstOrDefault(
                item => string.Equals(item.EmployeeNo, info.EmployeeNo, StringComparison.OrdinalIgnoreCase));

            return new[]
            {
                employee ?? new DemoEmployee(info.EmployeeNo, info.EmployeeNo)
            };
        }

        private static Task DelayAsync(double seconds, CancellationToken cancellationToken)
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

        private static double ReadDoubleSeconds(string key, double defaultValue)
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

        private sealed class DemoEmployee
        {
            public DemoEmployee(string employeeNo, string employeeName)
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
