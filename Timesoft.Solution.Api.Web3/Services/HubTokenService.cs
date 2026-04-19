using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Timesoft.Solution.Api.Web3.Services
{
    public sealed class HubTokenService
    {
        private readonly byte[] _secret;
        private readonly int _tokenLifetimeMinutes;

        public HubTokenService()
        {
            _secret = Encoding.UTF8.GetBytes(
                AppSettings.Read("HubToken-Secret") ?? "dev-only-hub-token-secret");

            if (!int.TryParse(AppSettings.Read("HubToken-LifetimeMinutes"), out _tokenLifetimeMinutes))
            {
                _tokenLifetimeMinutes = 30;
            }
        }

        public string CreateToken(string companyCode, string loginUserId, string calculationId)
        {
            // Token binds one browser to one company/user/calculation group.
            long expiresUnixSeconds = DateTimeOffset.UtcNow
                .AddMinutes(_tokenLifetimeMinutes)
                .ToUnixTimeSeconds();

            string rawPayload = string.Join("|", companyCode, loginUserId, calculationId, expiresUnixSeconds);
            string payload = Base64UrlEncode(Encoding.UTF8.GetBytes(rawPayload));
            string signature = Sign(payload);

            return payload + "." + signature;
        }

        private string Sign(string payload)
        {
            using (var hmac = new HMACSHA256(_secret))
            {
                return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            }
        }

        private static string Base64UrlEncode(byte[] value)
        {
            return Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
