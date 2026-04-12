using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using JobRealtimeSample.FrameworkApi.Models;
using JobRealtimeSample.FrameworkApi.Services;
using JobRealtimeSample.FrameworkApi.Vendors;

namespace JobRealtimeSample.FrameworkApi.Controllers
{
    [RoutePrefix("api/leave-calculations")]
    public sealed class LeaveCalculationsController : ApiController
    {
        private static readonly XmlLeaveCalculationStore Store = new XmlLeaveCalculationStore();
        private static readonly RealtimeNotifier RealtimeNotifier = new RealtimeNotifier();
        private static readonly DemoHubTokenService HubTokenService = new DemoHubTokenService();
        private static readonly BackgroundLeaveCalculationRunner BackgroundRunner =
            new BackgroundLeaveCalculationRunner(Store, RealtimeNotifier);
        private static readonly LeaveCalculationsVendor Vendor =
            new LeaveCalculationsVendor(Store, HubTokenService, BackgroundRunner);

        [HttpPost]
        [Route("start")]
        public async Task<IHttpActionResult> Start(LeaveCalculationStartRequest request)
        {
            string validationMessage = LeaveCalculationsVendor.ValidateStartRequest(request);

            if (validationMessage != null)
            {
                return BadRequest(validationMessage);
            }

            StartLeaveCalculationResult result = await Vendor.StartAsync(request, CancellationToken.None);

            if (result.Accepted)
            {
                return Content(HttpStatusCode.Accepted, result.Response);
            }

            return Ok(result.Response);
        }

        [HttpGet]
        [Route("{calculationId}")]
        public IHttpActionResult GetById(string calculationId)
        {
            LeaveCalculationInfo calculation = Vendor.GetById(calculationId);

            if (calculation == null)
            {
                return Content(
                    HttpStatusCode.NotFound,
                    new { message = $"Leave calculation '{calculationId}' was not found." });
            }

            return Ok(calculation);
        }
    }
}
