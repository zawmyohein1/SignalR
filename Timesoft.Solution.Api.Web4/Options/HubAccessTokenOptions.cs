namespace Timesoft.Solution.Api.Web4.Options;

public sealed class HubAccessTokenOptions
{
    public string Secret { get; set; } = "dev-only-hub-token-secret";

    public int LifetimeMinutes { get; set; } = 30;
}
