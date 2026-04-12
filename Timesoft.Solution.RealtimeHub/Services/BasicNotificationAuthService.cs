using System.Net.Http.Headers;
using System.Text;

namespace Timesoft.Solution.RealtimeHub.Services;

public sealed class BasicNotificationAuthService(IConfiguration configuration)
{
    private readonly string _username = configuration["NotificationAuth:Username"] ?? "sample-api";
    private readonly string _password = configuration["NotificationAuth:Password"] ?? "sample-secret";

    public bool IsAuthorized(HttpRequest request)
    {
        // Basic auth protects API-to-Hub notification calls.
        if (!AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var header))
        {
            return false;
        }

        if (!string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string decoded;

        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter ?? string.Empty));
        }
        catch (FormatException)
        {
            return false;
        }

        var separatorIndex = decoded.IndexOf(':');

        if (separatorIndex < 0)
        {
            return false;
        }

        var username = decoded[..separatorIndex];
        var password = decoded[(separatorIndex + 1)..];

        return string.Equals(username, _username, StringComparison.Ordinal)
            && string.Equals(password, _password, StringComparison.Ordinal);
    }
}
