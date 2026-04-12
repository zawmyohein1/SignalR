namespace JobRealtimeSample.MvcUi.Models;

public sealed class LeaveCalculationPageViewModel
{
    public string ApiBaseUrl { get; set; } = string.Empty;

    public string HubUrl { get; set; } = string.Empty;

    public bool SignalREnabled { get; set; } = true;

    public int CurrentYear { get; set; }

    public IReadOnlyList<DemoOption> Companies { get; set; } = [];

    public IReadOnlyList<DemoOption> Departments { get; set; } = [];

    public IReadOnlyList<DemoOption> Employees { get; set; } = [];
}
