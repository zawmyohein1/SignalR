using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JobRealtimeSample.FrameworkApi
{
    public sealed class SimpleCorsHandler : DelegatingHandler
    {
        private static readonly string[] AllowedOrigins =
        {
            "https://localhost:5001",
            "http://localhost:5001",
            "https://localhost:5101",
            "http://localhost:5101"
        };

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

            return origins.FirstOrDefault(origin => AllowedOrigins.Any(allowedOrigin =>
                string.Equals(origin, allowedOrigin, StringComparison.OrdinalIgnoreCase)));
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
