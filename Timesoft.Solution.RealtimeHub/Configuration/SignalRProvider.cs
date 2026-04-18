namespace Timesoft.Solution.RealtimeHub.Configuration;

internal enum SignalRProvider
{
    Disabled,
    Local,
    Azure
}

internal static class SignalRProviderConfigurationExtensions
{
    public static SignalRProvider GetSignalRProvider(this IConfiguration configuration)
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
            $"Invalid SignalR provider '{rawValue}'. Expected Disabled, Local, or Azure.");
    }
}
