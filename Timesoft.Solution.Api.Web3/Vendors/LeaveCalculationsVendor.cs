using System.Threading;
using System.Threading.Tasks;
using Timesoft.Solution.Api.Web3.Models;
using Timesoft.Solution.Api.Web3.Services;

namespace Timesoft.Solution.Api.Web3.Vendors
{
    public sealed class LeaveCalculationsVendor
    {
        private const string BackgroundSignalRMode = "BackgroundSignalR";
        private const string SynchronousHttpMode = "SynchronousHttp";

        private readonly XmlLeaveCalculationStore _store;
        private readonly DemoHubTokenService _hubTokenService;
        private readonly BackgroundLeaveCalculationRunner _backgroundRunner;

        public LeaveCalculationsVendor(
            XmlLeaveCalculationStore store,
            DemoHubTokenService hubTokenService,
            BackgroundLeaveCalculationRunner backgroundRunner)
        {
            _store = store;
            _hubTokenService = hubTokenService;
            _backgroundRunner = backgroundRunner;
        }

        public async Task<StartLeaveCalculationResult> StartAsync(
            LeaveCalculationStartRequest request,
            CancellationToken cancellationToken)
        {
            string validationMessage = ValidateStartRequest(request);

            if (validationMessage != null)
            {
                return new StartLeaveCalculationResult(
                    false,
                    null,
                    validationMessage);
            }

            // One calculation id tracks this request from API to UI.
            LeaveCalculationInfo calculation = _store.Create(request);
            string hubAccessToken = _hubTokenService.CreateToken(
                calculation.CompanyCode,
                calculation.LoginUserId,
                calculation.CalculationId);

            if (_backgroundRunner.SignalREnabled)
            {
                // SignalR mode returns fast and continues work in the background.
                _backgroundRunner.RunInBackground(calculation.CalculationId);

                return new StartLeaveCalculationResult(
                    true,
                    BuildStartResponse(calculation, hubAccessToken, BackgroundSignalRMode));
            }

            // Non-SignalR mode keeps the HTTP request open until work is complete.
            await _backgroundRunner.RunAsync(calculation.CalculationId, cancellationToken);

            LeaveCalculationInfo completedCalculation = _store.Get(calculation.CalculationId) ?? calculation;

            return new StartLeaveCalculationResult(
                false,
                BuildStartResponse(completedCalculation, hubAccessToken, SynchronousHttpMode));
        }

        public LeaveCalculationInfo GetById(string calculationId)
        {
            return _store.Get(calculationId);
        }

        private static string ValidateStartRequest(LeaveCalculationStartRequest request)
        {
            if (request == null)
            {
                return "Request body is required.";
            }

            if (string.IsNullOrWhiteSpace(request.CompanyCode))
            {
                return "companyCode is required.";
            }

            if (string.IsNullOrWhiteSpace(request.LoginUserId))
            {
                return "loginUserId is required.";
            }

            if (string.IsNullOrWhiteSpace(request.DepartmentCode))
            {
                return "departmentCode is required.";
            }

            return null;
        }

        private StartLeaveCalculationResponse BuildStartResponse(
            LeaveCalculationInfo calculation,
            string hubAccessToken,
            string executionMode)
        {
            return new StartLeaveCalculationResponse
            {
                CalculationId = calculation.CalculationId,
                CompanyCode = calculation.CompanyCode,
                LoginUserId = calculation.LoginUserId,
                HubAccessToken = hubAccessToken,
                SignalREnabled = _backgroundRunner.SignalREnabled,
                ExecutionMode = executionMode,
                Status = calculation.Status,
                Message = calculation.Message
            };
        }
    }
}
