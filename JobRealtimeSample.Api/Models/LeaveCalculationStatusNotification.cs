namespace JobRealtimeSample.Api.Models;

public sealed class LeaveCalculationStatusNotification
{
    public string CalculationId { get; set; } = string.Empty;

    public string CompanyCode { get; set; } = string.Empty;

    public string LoginUserId { get; set; } = string.Empty;

    public string DepartmentCode { get; set; } = string.Empty;

    public string EmployeeNo { get; set; } = string.Empty;

    public string LeaveTypeCode { get; set; } = string.Empty;

    public int Year { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }
}

