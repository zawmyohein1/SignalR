using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using JobRealtimeSample.FrameworkApi.Models;
using JobRealtimeSample.FrameworkApi.Vendors;

namespace JobRealtimeSample.FrameworkApi.Controllers
{
    [RoutePrefix("api/leave-calculations")]
    public sealed class LeaveCalculationsController : ApiController
    {
        private readonly LeaveCalculationsVendor _vendor;

        public LeaveCalculationsController(LeaveCalculationsVendor vendor)
        {
            _vendor = vendor;
        }

        [HttpPost]
        [Route("start")]
        public async Task<IHttpActionResult> Start(LeaveCalculationStartRequest request)
        {
            StartLeaveCalculationResult result = await _vendor.StartAsync(request, CancellationToken.None);

            if (result.ValidationMessage != null)
            {
                return BadRequest(result.ValidationMessage);
            }

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
            LeaveCalculationInfo calculation = _vendor.GetById(calculationId);

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
