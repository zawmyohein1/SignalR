namespace Timesoft.Solution.Api.Web3.Models
{
    public sealed class LeaveCalculationResult
    {
        public LeaveCalculationResult(
            bool accepted,
            LeaveCalculationResponse response,
            string validationMessage = null)
        {
            Accepted = accepted;
            Response = response;
            ValidationMessage = validationMessage;
        }

        public bool Accepted { get; }

        public LeaveCalculationResponse Response { get; }

        public string ValidationMessage { get; }
    }
}
