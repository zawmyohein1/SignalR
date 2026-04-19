using Timesoft.Solution.Api.Web4.Models;
using Timesoft.Solution.Api.Web4.Services;

namespace Timesoft.Solution.Api.Web4.Services;

public sealed class LeaveCalculationService(
    LeaveCalculationStore store,
    HubTokenService hubTokenService,
    LeaveCalculationRunner backgroundRunner)
{
    private const string BackgroundSignalRMode = "BackgroundSignalR";
    private const string SynchronousHttpMode = "SynchronousHttp";

    public async Task<LeaveCalculationResult> StartAsync(
        LeaveCalculationRequest? request,
        CancellationToken cancellationToken)
    {
        var validationMessage = ValidateStartRequest(request);

        if (validationMessage is not null)
        {
            return new LeaveCalculationResult(
                Accepted: false,
                Response: null,
                ValidationMessage: validationMessage);
        }

        // One calculation id tracks this request from API to UI.
        var calculation = store.Create(request!);
        var hubAccessToken = hubTokenService.CreateToken(
            calculation.CompanyCode,
            calculation.LoginUserId,
            calculation.CalculationId);

        if (backgroundRunner.SignalREnabled)
        {
            // SignalR mode returns fast and continues work in the background.
            backgroundRunner.RunInBackground(calculation.CalculationId);

            return new LeaveCalculationResult(
                Accepted: true,
            Response: BuildStartResponse(calculation, hubAccessToken, BackgroundSignalRMode));
        }

        // Non-SignalR mode keeps the HTTP request open until work is complete.
        await backgroundRunner.RunAsync(calculation.CalculationId, cancellationToken);

        var completedCalculation = store.Get(calculation.CalculationId) ?? calculation;

        return new LeaveCalculationResult(
            Accepted: false,
            Response: BuildStartResponse(completedCalculation, hubAccessToken, SynchronousHttpMode));
    }

    public LeaveCalculationInfo? GetById(string calculationId)
    {
        return store.Get(calculationId);
    }

    private static string? ValidateStartRequest(LeaveCalculationRequest? request)
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

        return null;
    }

    private LeaveCalculationResponse BuildStartResponse(
        LeaveCalculationInfo calculation,
        string hubAccessToken,
        string executionMode)
    {
        return new LeaveCalculationResponse
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
