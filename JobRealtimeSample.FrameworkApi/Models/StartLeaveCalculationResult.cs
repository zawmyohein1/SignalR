namespace JobRealtimeSample.FrameworkApi.Models
{
    public sealed class StartLeaveCalculationResult
    {
        public StartLeaveCalculationResult(
            bool accepted,
            StartLeaveCalculationResponse response,
            string validationMessage = null)
        {
            Accepted = accepted;
            Response = response;
            ValidationMessage = validationMessage;
        }

        public bool Accepted { get; }

        public StartLeaveCalculationResponse Response { get; }

        public string ValidationMessage { get; }
    }
}
