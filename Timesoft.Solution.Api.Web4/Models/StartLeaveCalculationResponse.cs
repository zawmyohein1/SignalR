namespace JobRealtimeSample.Api.Models;

public sealed class StartLeaveCalculationResponse
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
