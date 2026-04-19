using System.Collections.Generic;

namespace Timesoft.Solution.Web3.Models
{
    public sealed class LeaveCalculationPageViewModel
    {
        public string ApiBaseUrl { get; set; }

        public string HubUrl { get; set; }

        public bool SignalREnabled { get; set; }

        public string RestoreStorageMode { get; set; }

        public string SignalRProvider { get; set; }

        public int CurrentYear { get; set; }

        public IReadOnlyList<SelectOption> Companies { get; set; }

        public IReadOnlyList<SelectOption> Departments { get; set; }

        public IReadOnlyList<SelectOption> Employees { get; set; }
    }
}
