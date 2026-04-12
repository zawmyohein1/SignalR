using System;
using System.Configuration;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Timesoft.Solution.Api.Web3.Models;
using Newtonsoft.Json;

namespace Timesoft.Solution.Api.Web3.Services
{
    public sealed class RealtimeNotifier : IDisposable
    {
        private readonly string _leaveCalculationNotificationEndpoint;
        private readonly HttpClient _httpClient;

        public RealtimeNotifier()
        {
            _leaveCalculationNotificationEndpoint =
                ConfigurationManager.AppSettings["RealtimeHubLeaveCalculationNotificationEndpoint"]
                ?? "https://localhost:5003/api/notifications/leave-calculation-status";

            var handler = new HttpClientHandler();

#if DEBUG
            // Local sample callback only. Trust the ASP.NET development cert in real environments.
            handler.ServerCertificateCustomValidationCallback = (message, certificate, chain, errors) => true;
#endif

            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Authorization = CreateBasicAuthHeader();
        }

        public async Task<bool> NotifyLeaveCalculationAsync(
            LeaveCalculationStatusNotification notification,
            CancellationToken cancellationToken)
        {
            try
            {
                string json = JsonConvert.SerializeObject(notification);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (HttpResponseMessage response = await _httpClient.PostAsync(_leaveCalculationNotificationEndpoint, content, cancellationToken))
                {
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "Could not notify realtime hub for leave calculation {0}. {1}",
                    notification.CalculationId,
                    ex.Message);

                return false;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private static AuthenticationHeaderValue CreateBasicAuthHeader()
        {
            string username = ConfigurationManager.AppSettings["RealtimeHubNotificationUsername"] ?? "sample-api";
            string password = ConfigurationManager.AppSettings["RealtimeHubNotificationPassword"] ?? "sample-secret";
            string rawValue = username + ":" + password;
            string encodedValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawValue));

            return new AuthenticationHeaderValue("Basic", encodedValue);
        }
    }
}
