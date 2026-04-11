using System;

namespace JobRealtimeSample.FrameworkApi.Models
{
    public sealed class LeaveCalculationStatusNotification
    {
        public string CalculationId { get; set; }

        public string CompanyCode { get; set; }

        public string LoginUserId { get; set; }

        public string DepartmentCode { get; set; }

        public string EmployeeNo { get; set; }

        public string LeaveTypeCode { get; set; }

        public int Year { get; set; }

        public string Status { get; set; }

        public string Message { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}

