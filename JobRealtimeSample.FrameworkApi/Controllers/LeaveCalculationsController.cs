using System.Net;
using System.Web.Http;
using JobRealtimeSample.FrameworkApi.Models;
using JobRealtimeSample.FrameworkApi.Services;

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

        [HttpPost]
        [Route("start")]
        public IHttpActionResult Start(LeaveCalculationStartRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.CompanyCode))
            {
                return BadRequest("companyCode is required.");
            }

            if (string.IsNullOrWhiteSpace(request.LoginUserId))
            {
                return BadRequest("loginUserId is required.");
            }

            if (string.IsNullOrWhiteSpace(request.DepartmentCode))
            {
                return BadRequest("departmentCode is required.");
            }

            if (string.IsNullOrWhiteSpace(request.LeaveTypeCode))
            {
                return BadRequest("leaveTypeCode is required.");
            }

            LeaveCalculationInfo calculation = Store.Create(request);
            string hubAccessToken = HubTokenService.CreateToken(
                calculation.CompanyCode,
                calculation.LoginUserId,
                calculation.CalculationId);

            BackgroundRunner.RunInBackground(calculation.CalculationId);

            return Content(HttpStatusCode.Accepted, new StartLeaveCalculationResponse
            {
                CalculationId = calculation.CalculationId,
                CompanyCode = calculation.CompanyCode,
                LoginUserId = calculation.LoginUserId,
                HubAccessToken = hubAccessToken,
                Status = calculation.Status,
                Message = calculation.Message
            });
        }

        [HttpGet]
        [Route("{calculationId}")]
        public IHttpActionResult GetById(string calculationId)
        {
            LeaveCalculationInfo calculation = Store.Get(calculationId);

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

