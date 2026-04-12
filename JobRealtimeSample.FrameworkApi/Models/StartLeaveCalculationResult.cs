namespace JobRealtimeSample.FrameworkApi.Models
{
    public sealed class StartLeaveCalculationResult
    {
        public StartLeaveCalculationResult(bool accepted, StartLeaveCalculationResponse response)
        {
            Accepted = accepted;
            Response = response;
        }

        public bool Accepted { get; }

        public StartLeaveCalculationResponse Response { get; }
    }
}
