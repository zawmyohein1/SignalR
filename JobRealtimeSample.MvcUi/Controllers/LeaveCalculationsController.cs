using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace JobRealtimeSample.MvcUi.Controllers;

[Route("LeaveCalculations")]
public sealed class LeaveCalculationsController(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : Controller
{
    [HttpPost("Start")]
    public async Task<IActionResult> Start()
    {
        // Browser calls MVC; MVC forwards to API server-side.
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var requestBody = await reader.ReadToEndAsync();

        try
        {
            var client = CreateHttpClient();
            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            using var apiResponse = await client.PostAsync(
                $"{ResolveApiBaseUrl()}/api/leave-calculations/start",
                content);

            return await CreateProxyResultAsync(apiResponse);
        }
        catch (HttpRequestException ex)
        {
            return CreateApiUnavailableResult(ex);
        }
        catch (TaskCanceledException ex)
        {
            return CreateApiUnavailableResult(ex);
        }
    }

    [HttpGet("Details/{calculationId}")]
    public async Task<IActionResult> Details(string calculationId)
    {
        // Snapshot reads also go through MVC, not directly from browser to API.
        if (string.IsNullOrWhiteSpace(calculationId))
        {
            return BadRequest(new { message = "calculationId is required." });
        }

        try
        {
            var client = CreateHttpClient();
            using var apiResponse = await client.GetAsync(
                $"{ResolveApiBaseUrl()}/api/leave-calculations/{Uri.EscapeDataString(calculationId)}");

            return await CreateProxyResultAsync(apiResponse);
        }
        catch (HttpRequestException ex)
        {
            return CreateApiUnavailableResult(ex);
        }
        catch (TaskCanceledException ex)
        {
            return CreateApiUnavailableResult(ex);
        }
    }

    private string ResolveApiBaseUrl()
    {
        var configuredApiBaseUrl = configuration["LeaveCalculationDemo:ApiBaseUrl"];

        return string.IsNullOrWhiteSpace(configuredApiBaseUrl)
            ? "https://localhost:5102"
            : configuredApiBaseUrl.TrimEnd('/');
    }

    private HttpClient CreateHttpClient()
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(10);

        return client;
    }

    private ObjectResult CreateApiUnavailableResult(Exception ex)
    {
        return StatusCode(
            StatusCodes.Status502BadGateway,
            new
            {
                message = "MVC UI could not call the Leave Calculation API. Make sure the API project is running.",
                detail = ex.Message
            });
    }

    private static async Task<ContentResult> CreateProxyResultAsync(HttpResponseMessage apiResponse)
    {
        var responseBody = await apiResponse.Content.ReadAsStringAsync();
        var contentType = apiResponse.Content.Headers.ContentType?.ToString() ?? "application/json";

        return new ContentResult
        {
            Content = responseBody,
            ContentType = contentType,
            StatusCode = (int)apiResponse.StatusCode
        };
    }
}
