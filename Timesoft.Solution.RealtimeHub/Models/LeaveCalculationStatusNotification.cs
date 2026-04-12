namespace JobRealtimeSample.RealtimeHub.Models;

public sealed record LeaveCalculationStatusNotification(
    string CalculationId,
    string CompanyCode,
    string LoginUserId,
    string DepartmentCode,
    string EmployeeNo,
    int Year,
    string Status,
    string Message,
    DateTimeOffset Timestamp);
