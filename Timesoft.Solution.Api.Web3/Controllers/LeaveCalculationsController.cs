using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Timesoft.Solution.Api.Web3.Models;
using Timesoft.Solution.Api.Web3.Services;

namespace Timesoft.Solution.Api.Web3.Controllers
{
    [RoutePrefix("api/leave-calculations")]
    public sealed class LeaveCalculationsController : ApiController
    {
        private readonly LeaveCalculationService _service;

        public LeaveCalculationsController(LeaveCalculationService service)
        {
            _service = service;
        }

        [HttpPost]
        [Route("start")]
        public async Task<IHttpActionResult> Start(LeaveCalculationRequest request)
        {
            LeaveCalculationResult result = await _service.StartAsync(request, CancellationToken.None);

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
            LeaveCalculationInfo calculation = _service.GetById(calculationId);

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
