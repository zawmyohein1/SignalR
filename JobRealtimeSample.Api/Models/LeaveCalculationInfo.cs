namespace JobRealtimeSample.Api.Models;

public sealed class LeaveCalculationInfo
{
    public string CalculationId { get; set; } = string.Empty;

    public string CompanyCode { get; set; } = string.Empty;

    public string LoginUserId { get; set; } = string.Empty;

    public string DepartmentCode { get; set; } = string.Empty;

    public string EmployeeNo { get; set; } = string.Empty;

    public int Year { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<LeaveCalculationStatusNotification> History { get; set; } = [];

    public LeaveCalculationInfo Snapshot()
    {
        return new LeaveCalculationInfo
        {
            CalculationId = CalculationId,
            CompanyCode = CompanyCode,
            LoginUserId = LoginUserId,
            DepartmentCode = DepartmentCode,
            EmployeeNo = EmployeeNo,
            Year = Year,
            Status = Status,
            Message = Message,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            History = [.. History]
        };
    }
}
