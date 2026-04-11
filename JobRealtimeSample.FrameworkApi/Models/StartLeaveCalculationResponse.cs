namespace JobRealtimeSample.FrameworkApi.Models
{
    public sealed class StartLeaveCalculationResponse
    {
        public string CalculationId { get; set; }

        public string CompanyCode { get; set; }

        public string LoginUserId { get; set; }

        public string HubAccessToken { get; set; }

        public string Status { get; set; }

        public string Message { get; set; }
    }
}

