namespace Timesoft.Solution.Api.Web3.Models
{
    public sealed class LeaveCalculationRequest
    {
        public string CompanyCode { get; set; }

        public string LoginUserId { get; set; }

        public string DepartmentCode { get; set; }

        public string EmployeeNo { get; set; }

        public int Year { get; set; }
    }
}
