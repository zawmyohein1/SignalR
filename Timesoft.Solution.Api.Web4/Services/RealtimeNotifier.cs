using System.Net.Http.Json;
using JobRealtimeSample.Api.Models;
using JobRealtimeSample.Api.Options;
using Microsoft.Extensions.Options;

namespace JobRealtimeSample.Api.Services;

public sealed class RealtimeNotifier(
    IHttpClientFactory httpClientFactory,
    IOptions<RealtimeHubOptions> options,
    ILogger<RealtimeNotifier> logger)
{
    private readonly RealtimeHubOptions _options = options.Value;

    public async Task<bool> NotifyLeaveCalculationAsync(
        LeaveCalculationStatusNotification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient(nameof(RealtimeNotifier));
            AddBasicAuth(httpClient);
            using var response = await httpClient.PostAsJsonAsync(
                _options.LeaveCalculationNotificationEndpoint,
                notification,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning(
                "Realtime hub returned {StatusCode} for leave calculation {CalculationId}.",
                response.StatusCode,
                notification.CalculationId);

            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(
                ex,
                "Could not notify realtime hub for leave calculation {CalculationId}.",
                notification.CalculationId);

            return false;
        }
    }

    private void AddBasicAuth(HttpClient httpClient)
    {
        var rawValue = $"{_options.NotificationUsername}:{_options.NotificationPassword}";
        var encodedValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(rawValue));
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encodedValue);
    }
}
