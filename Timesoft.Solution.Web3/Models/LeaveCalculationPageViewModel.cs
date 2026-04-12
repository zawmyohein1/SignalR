using System.Collections.Generic;

namespace Timesoft.Solution.Web3.Models
{
    public sealed class LeaveCalculationPageViewModel
    {
        public string ApiBaseUrl { get; set; }

        public string HubUrl { get; set; }

        public bool SignalREnabled { get; set; }

        public int CurrentYear { get; set; }

        public IReadOnlyList<DemoOption> Companies { get; set; }

        public IReadOnlyList<DemoOption> Departments { get; set; }

        public IReadOnlyList<DemoOption> Employees { get; set; }
    }
}
