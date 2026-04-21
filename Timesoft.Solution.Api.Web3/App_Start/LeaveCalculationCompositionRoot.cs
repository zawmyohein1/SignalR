using System;
using Timesoft.Solution.Api.Web3.Services;

namespace Timesoft.Solution.Api.Web3
{
    public static class LeaveCalculationCompositionRoot
    {
        private static readonly Lazy<LeaveCalculationStore> Store =
            new Lazy<LeaveCalculationStore>(() => new LeaveCalculationStore());

        private static readonly Lazy<IRealtimeNotificationPublisher> Publisher =
            new Lazy<IRealtimeNotificationPublisher>(CreatePublisher);

        private static readonly Lazy<HubTokenService> TokenService =
            new Lazy<HubTokenService>(() => new HubTokenService());

        private static readonly Lazy<LeaveCalculationRunner> BackgroundRunner =
            new Lazy<LeaveCalculationRunner>(
                () => new LeaveCalculationRunner(Store.Value, Publisher.Value));

        private static readonly Lazy<LeaveCalculationService> ServiceInstance =
            new Lazy<LeaveCalculationService>(
                () => new LeaveCalculationService(
                    Store.Value,
                    TokenService.Value,
                    BackgroundRunner.Value));

        public static LeaveCalculationService Service => ServiceInstance.Value;

        private static IRealtimeNotificationPublisher CreatePublisher()
        {
            bool signalREnabled;

            if (!bool.TryParse(AppSettings.Read("SignalR:Enabled"), out signalREnabled))
            {
                signalREnabled = true;
            }

            return signalREnabled
                ? (IRealtimeNotificationPublisher)new NotificationPublisher()
                : new DisabledRealtimeNotificationPublisher();
        }
    }
}
