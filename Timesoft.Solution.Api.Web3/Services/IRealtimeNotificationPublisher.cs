using System.Threading;
using System.Threading.Tasks;
using Timesoft.Solution.Api.Web3.Models;

namespace Timesoft.Solution.Api.Web3.Services
{
    public interface IRealtimeNotificationPublisher
    {
        Task<bool> NotifyLeaveCalculationAsync(
            LeaveCalculationStatusNotification notification,
            CancellationToken cancellationToken);
    }
}
