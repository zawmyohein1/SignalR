namespace Timesoft.Solution.RealtimeHub.Configuration;

internal enum SignalRProvider
{
    Local,
    Azure
}

internal sealed record SignalRSettings(bool Enabled, SignalRProvider Provider);

internal static class SignalRProviderConfigurationExtensions
{
    public static SignalRSettings GetSignalRSettings(this IConfiguration configuration)
    {
        var enabled = configuration.GetValue("SignalR:Enabled", true);
        var provider = configuration.GetSignalRProvider();

        return new SignalRSettings(enabled, provider);
    }

    private static SignalRProvider GetSignalRProvider(this IConfiguration configuration)
    {
        var rawValue = configuration["SignalR:Provider"];

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return SignalRProvider.Local;
        }

        if (Enum.TryParse<SignalRProvider>(rawValue, ignoreCase: true, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException(
            $"Invalid SignalR provider '{rawValue}'. Expected Local or Azure.");
    }
}
