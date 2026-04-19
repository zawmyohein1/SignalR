using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Timesoft.Solution.Api.Web3.Services;

namespace Timesoft.Solution.Api.Web3
{
    public sealed class CorsHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string allowedOrigin = GetAllowedOrigin(request);

            if (allowedOrigin == null)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            if (request.Method == HttpMethod.Options)
            {
                HttpResponseMessage preflightResponse = request.CreateResponse(HttpStatusCode.OK);
                AddCorsHeaders(preflightResponse, allowedOrigin);
                return preflightResponse;
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            AddCorsHeaders(response, allowedOrigin);
            return response;
        }

        private static string GetAllowedOrigin(HttpRequestMessage request)
        {
            if (!request.Headers.TryGetValues("Origin", out var origins))
            {
                return null;
            }

            string[] web3Origins = FilterPlaceholderOrigins(AppSettings.ReadList("Cors-AllowedOrigins-Web3"));
            string[] web4Origins = FilterPlaceholderOrigins(AppSettings.ReadList("Cors-AllowedOrigins-Web4"));
            string[] allowedOrigins = web3Origins.Concat(web4Origins).ToArray();

            if (allowedOrigins == null || allowedOrigins.Length == 0)
            {
                return null;
            }

            return origins.FirstOrDefault(origin => allowedOrigins.Any(allowedOrigin =>
                string.Equals(origin, allowedOrigin, StringComparison.OrdinalIgnoreCase)));
        }

        private static string[] FilterPlaceholderOrigins(string[] origins)
        {
            return (origins ?? Array.Empty<string>())
                .Where(origin => !string.Equals(origin, "xxxx", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private static void AddCorsHeaders(HttpResponseMessage response, string allowedOrigin)
        {
            response.Headers.Remove("Access-Control-Allow-Origin");
            response.Headers.Remove("Access-Control-Allow-Methods");
            response.Headers.Remove("Access-Control-Allow-Headers");

            response.Headers.Add("Access-Control-Allow-Origin", allowedOrigin);
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "content-type, authorization");
        }
    }
}
