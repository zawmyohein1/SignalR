namespace JobRealtimeSample.Api.Options;

public sealed class HubTokenOptions
{
    public string Secret { get; set; } = "dev-only-demo-hub-token-secret";

    public int LifetimeMinutes { get; set; } = 30;
}

