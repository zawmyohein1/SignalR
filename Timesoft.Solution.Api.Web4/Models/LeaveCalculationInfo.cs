namespace Timesoft.Solution.Api.Web4.Models;

public sealed class LeaveCalculationInfo : LeaveCalculationSummary
{
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
