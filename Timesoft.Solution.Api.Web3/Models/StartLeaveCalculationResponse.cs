namespace Timesoft.Solution.Api.Web3.Models
{
    public sealed class StartLeaveCalculationResponse
    {
        public string CalculationId { get; set; }

        public string CompanyCode { get; set; }

        public string LoginUserId { get; set; }

        public string HubAccessToken { get; set; }

        public bool SignalREnabled { get; set; }

        public string ExecutionMode { get; set; }

        public string Status { get; set; }

        public string Message { get; set; }
    }
}
