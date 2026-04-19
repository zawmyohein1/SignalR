using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Timesoft.Solution.Web3.Controllers
{
    public sealed class LeaveCalculationsController : Controller
    {
        static LeaveCalculationsController()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        [HttpPost]
        public async Task<ActionResult> Start()
        {
            // Browser posts to MVC, which forwards the request to the API.
            string requestBody;

            using (var reader = new StreamReader(Request.InputStream, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            try
            {
                using (var client = CreateHttpClient())
                using (var content = new StringContent(requestBody, Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage apiResponse = await client.PostAsync(
                        $"{ResolveApiBaseUrl()}/api/leave-calculations/start",
                        content);

                    return await CreatePassthroughResultAsync(apiResponse);
                }
            }
            catch (HttpRequestException ex)
            {
                return CreateUnavailableResult(ex);
            }
            catch (TaskCanceledException ex)
            {
                return CreateUnavailableResult(ex);
            }
        }

        [HttpGet]
        public async Task<ActionResult> Details(string id)
        {
            // Snapshot reads also proxy through MVC so the browser stays on one origin.
            if (string.IsNullOrWhiteSpace(id))
            {
                Response.StatusCode = 400;
                return Json(new { message = "calculationId is required." }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                using (var client = CreateHttpClient())
                {
                    HttpResponseMessage apiResponse = await client.GetAsync(
                        $"{ResolveApiBaseUrl()}/api/leave-calculations/{Uri.EscapeDataString(id)}");

                    return await CreatePassthroughResultAsync(apiResponse);
                }
            }
            catch (HttpRequestException ex)
            {
                return CreateUnavailableResult(ex);
            }
            catch (TaskCanceledException ex)
            {
                return CreateUnavailableResult(ex);
            }
        }

        private string ResolveApiBaseUrl()
        {
            string configuredApiBaseUrl = ReadAppSetting("LeaveCalculation-ApiBaseUrl");

            if (string.IsNullOrWhiteSpace(configuredApiBaseUrl)
                || string.Equals(configuredApiBaseUrl, "auto", StringComparison.OrdinalIgnoreCase))
            {
                // Default local API endpoint for the legacy MVC app.
                return "http://localhost:57636";
            }

            return configuredApiBaseUrl.TrimEnd('/');
        }

        private static HttpClient CreateHttpClient()
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
        }

        private ActionResult CreateUnavailableResult(Exception ex)
        {
            Response.StatusCode = 502;

            return Json(
                new
                {
                    message = "MVC UI could not call the Leave Calculation API. Make sure the API project is running.",
                    detail = ex.Message
                },
                JsonRequestBehavior.AllowGet);
        }

        private async Task<ActionResult> CreatePassthroughResultAsync(HttpResponseMessage apiResponse)
        {
            string responseBody = await apiResponse.Content.ReadAsStringAsync();
            string contentType = apiResponse.Content.Headers.ContentType?.ToString() ?? "application/json";

            Response.StatusCode = (int)apiResponse.StatusCode;

            return Content(responseBody, contentType);
        }

        private static string ReadAppSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}
