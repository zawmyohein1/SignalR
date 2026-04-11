namespace JobRealtimeSample.Api.Models;

public sealed class LeaveCalculationStartRequest
{
    public string? CompanyCode { get; set; }

    public string? LoginUserId { get; set; }

    public string? DepartmentCode { get; set; }

    public string? EmployeeNo { get; set; }

    public string? LeaveTypeCode { get; set; }

    public int Year { get; set; }
}

