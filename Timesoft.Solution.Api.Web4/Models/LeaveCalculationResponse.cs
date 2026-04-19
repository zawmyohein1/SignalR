namespace Timesoft.Solution.Api.Web4.Models;

public sealed class LeaveCalculationResponse
{
    public string CalculationId { get; set; } = string.Empty;

    public string CompanyCode { get; set; } = string.Empty;

    public string LoginUserId { get; set; } = string.Empty;

    public string HubAccessToken { get; set; } = string.Empty;

    public bool SignalREnabled { get; set; }

    public string ExecutionMode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
