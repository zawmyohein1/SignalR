using System.Security.Cryptography;
using System.Text;
using Timesoft.Solution.Api.Web4.Options;
using Microsoft.Extensions.Options;

namespace Timesoft.Solution.Api.Web4.Services;

public sealed class HubTokenService(IOptions<HubAccessTokenOptions> options)
{
    private readonly HubAccessTokenOptions _options = options.Value;

    public string CreateToken(string companyCode, string loginUserId, string calculationId)
    {
        // Token binds one browser to one company/user/calculation group.
        var expiresUnixSeconds = DateTimeOffset.UtcNow
            .AddMinutes(_options.LifetimeMinutes)
            .ToUnixTimeSeconds();

        var rawPayload = string.Join("|", companyCode, loginUserId, calculationId, expiresUnixSeconds);
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(rawPayload));
        var signature = Sign(payload);

        return $"{payload}.{signature}";
    }

    private string Sign(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.Secret));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
