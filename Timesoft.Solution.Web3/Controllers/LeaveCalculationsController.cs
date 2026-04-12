using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace JobRealtimeSample.FrameworkMvcUi.Controllers
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
            // Browser calls MVC; MVC forwards to API server-side.
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

                    return await CreateProxyResultAsync(apiResponse);
                }
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

        [HttpGet]
        public async Task<ActionResult> Details(string id)
        {
            // Snapshot reads also go through MVC, not directly from browser to API.
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

                    return await CreateProxyResultAsync(apiResponse);
                }
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
            string configuredApiBaseUrl = ConfigurationManager.AppSettings["LeaveCalculationApiBaseUrl"];

            if (string.IsNullOrWhiteSpace(configuredApiBaseUrl)
                || string.Equals(configuredApiBaseUrl, "auto", StringComparison.OrdinalIgnoreCase))
            {
                // Default demo API endpoint for Framework API.
                return "https://localhost:5002";
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

        private ActionResult CreateApiUnavailableResult(Exception ex)
        {
            Response.StatusCode = 502;

            return Json(
                new
                {
                    message = "MVC UI could not call the Leave Calculation API. Make sure the API project is running on https://localhost:5002.",
                    detail = ex.Message
                },
                JsonRequestBehavior.AllowGet);
        }

        private async Task<ActionResult> CreateProxyResultAsync(HttpResponseMessage apiResponse)
        {
            string responseBody = await apiResponse.Content.ReadAsStringAsync();
            string contentType = apiResponse.Content.Headers.ContentType?.ToString() ?? "application/json";

            Response.StatusCode = (int)apiResponse.StatusCode;

            return Content(responseBody, contentType);
        }
    }
}
