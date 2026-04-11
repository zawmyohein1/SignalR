using System.Security.Cryptography;
using System.Text;
using JobRealtimeSample.RealtimeHub.Models;

namespace JobRealtimeSample.RealtimeHub.Services;

public sealed class DemoHubTokenService(IConfiguration configuration)
{
    private readonly byte[] _secret = Encoding.UTF8.GetBytes(
        configuration["HubToken:Secret"] ?? "dev-only-demo-hub-token-secret");

    public bool TryValidate(
        string? token,
        string companyCode,
        string loginUserId,
        string calculationId,
        out HubAccessTokenClaims? claims)
    {
        claims = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.');

        if (parts.Length != 2)
        {
            return false;
        }

        var payload = parts[0];
        var signature = parts[1];

        if (!IsSignatureValid(payload, signature))
        {
            return false;
        }

        string decodedPayload;

        try
        {
            decodedPayload = Encoding.UTF8.GetString(Base64UrlDecode(payload));
        }
        catch (FormatException)
        {
            return false;
        }

        var values = decodedPayload.Split('|');

        if (values.Length != 4)
        {
            return false;
        }

        if (!long.TryParse(values[3], out var expiresUnixSeconds))
        {
            return false;
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresUnixSeconds);

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        claims = new HubAccessTokenClaims(values[0], values[1], values[2], expiresAt);

        return string.Equals(claims.CompanyCode, companyCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(claims.LoginUserId, loginUserId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(claims.CalculationId, calculationId, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsSignatureValid(string payload, string signature)
    {
        var expectedSignature = Sign(payload);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
        var actualBytes = Encoding.UTF8.GetBytes(signature);

        return expectedBytes.Length == actualBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private string Sign(string payload)
    {
        using var hmac = new HMACSHA256(_secret);
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');

        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return Convert.FromBase64String(padded);
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

