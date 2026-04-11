using System;
using System.Collections.Generic;
using System.Linq;

namespace JobRealtimeSample.FrameworkApi.Models
{
    public sealed class LeaveCalculationInfo
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

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public List<LeaveCalculationStatusNotification> History { get; set; } =
            new List<LeaveCalculationStatusNotification>();

        public LeaveCalculationInfo Snapshot()
        {
            return new LeaveCalculationInfo
            {
                CalculationId = CalculationId,
                CompanyCode = CompanyCode,
                LoginUserId = LoginUserId,
                DepartmentCode = DepartmentCode,
                EmployeeNo = EmployeeNo,
                LeaveTypeCode = LeaveTypeCode,
                Year = Year,
                Status = Status,
                Message = Message,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                History = History.ToList()
            };
        }
    }
}

