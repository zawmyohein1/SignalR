namespace Timesoft.Solution.Web4.Models;

public sealed class LeaveCalculationPageViewModel
{
    public string ApiBaseUrl { get; set; } = string.Empty;

    public string HubUrl { get; set; } = string.Empty;

    public bool SignalREnabled { get; set; } = true;

    public string RestoreStorageMode { get; set; } = "session";

    public string SignalRProvider { get; set; } = "Local";

    public int CurrentYear { get; set; }

    public IReadOnlyList<SelectOption> Companies { get; set; } = [];

    public IReadOnlyList<SelectOption> Departments { get; set; } = [];

    public IReadOnlyList<SelectOption> Employees { get; set; } = [];
}
