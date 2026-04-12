using System;
using JobRealtimeSample.FrameworkApi.Services;
using JobRealtimeSample.FrameworkApi.Vendors;

namespace JobRealtimeSample.FrameworkApi
{
    public static class LeaveCalculationCompositionRoot
    {
        private static readonly Lazy<XmlLeaveCalculationStore> Store =
            new Lazy<XmlLeaveCalculationStore>(() => new XmlLeaveCalculationStore());

        private static readonly Lazy<RealtimeNotifier> RealtimeNotifier =
            new Lazy<RealtimeNotifier>(() => new RealtimeNotifier());

        private static readonly Lazy<DemoHubTokenService> HubTokenService =
            new Lazy<DemoHubTokenService>(() => new DemoHubTokenService());

        private static readonly Lazy<BackgroundLeaveCalculationRunner> BackgroundRunner =
            new Lazy<BackgroundLeaveCalculationRunner>(
                () => new BackgroundLeaveCalculationRunner(Store.Value, RealtimeNotifier.Value));

        private static readonly Lazy<LeaveCalculationsVendor> VendorInstance =
            new Lazy<LeaveCalculationsVendor>(
                () => new LeaveCalculationsVendor(
                    Store.Value,
                    HubTokenService.Value,
                    BackgroundRunner.Value));

        public static LeaveCalculationsVendor Vendor => VendorInstance.Value;
    }
}
