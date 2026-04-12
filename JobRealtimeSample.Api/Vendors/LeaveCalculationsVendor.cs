using JobRealtimeSample.Api.Models;
using JobRealtimeSample.Api.Services;

namespace JobRealtimeSample.Api.Vendors;

public sealed class LeaveCalculationsVendor(
    XmlLeaveCalculationStore store,
    DemoHubTokenService hubTokenService,
    BackgroundLeaveCalculationRunner backgroundRunner)
{
    private const string BackgroundSignalRMode = "BackgroundSignalR";
    private const string SynchronousHttpMode = "SynchronousHttp";

    public static string? ValidateStartRequest(LeaveCalculationStartRequest? request)
    {
        if (request is null)
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

        return string.IsNullOrWhiteSpace(request.LeaveTypeCode)
            ? "leaveTypeCode is required."
            : null;
    }

    public async Task<StartLeaveCalculationResult> StartAsync(
        LeaveCalculationStartRequest request,
        CancellationToken cancellationToken)
    {
        // One calculation id tracks this request from API to UI.
        var calculation = store.Create(request);
        var hubAccessToken = hubTokenService.CreateToken(
            calculation.CompanyCode,
            calculation.LoginUserId,
            calculation.CalculationId);

        if (backgroundRunner.SignalREnabled)
        {
            // SignalR mode returns fast and continues work in the background.
            backgroundRunner.RunInBackground(calculation.CalculationId);

            return new StartLeaveCalculationResult(
                Accepted: true,
            Response: BuildStartResponse(calculation, hubAccessToken, BackgroundSignalRMode));
        }

        // Non-SignalR mode keeps the HTTP request open until work is complete.
        await backgroundRunner.RunAsync(calculation.CalculationId, cancellationToken);

        var completedCalculation = store.Get(calculation.CalculationId) ?? calculation;

        return new StartLeaveCalculationResult(
            Accepted: false,
            Response: BuildStartResponse(completedCalculation, hubAccessToken, SynchronousHttpMode));
    }

    public LeaveCalculationInfo? GetById(string calculationId)
    {
        return store.Get(calculationId);
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
            SignalREnabled = backgroundRunner.SignalREnabled,
            ExecutionMode = executionMode,
            Status = calculation.Status,
            Message = calculation.Message
        };
    }
}
