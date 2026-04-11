namespace JobRealtimeSample.Api.Options;

public sealed class LeaveCalculationOptions
{
    public string XmlPath { get; set; } = "App_Data/LeaveCalculationJobs.xml";

    public int InitialDelaySeconds { get; set; } = 1;

    public int StepDelaySeconds { get; set; } = 4;
}

