namespace JobRealtimeSample.Api.Options;

public sealed class GenericJobOptions
{
    public double InitialDelaySeconds { get; set; } = 1;

    public double StepDelaySeconds { get; set; } = 5;
}
