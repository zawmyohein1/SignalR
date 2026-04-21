using Timesoft.Solution.Api.Web4.Models;

namespace Timesoft.Solution.Api.Web4.Services;

public interface IRealtimeNotificationPublisher
{
    Task<bool> NotifyLeaveCalculationAsync(
        LeaveCalculationStatusNotification notification,
        CancellationToken cancellationToken);
}
