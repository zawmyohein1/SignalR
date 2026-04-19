using Timesoft.Solution.Api.Web4.Models;
using Timesoft.Solution.Api.Web4.Services;
using Microsoft.AspNetCore.Mvc;

namespace Timesoft.Solution.Api.Web4.Controllers;

[ApiController]
[Route("api/leave-calculations")]
public sealed class LeaveCalculationsController(LeaveCalculationService service) : ControllerBase
{
    [HttpPost("start")]
    public async Task<ActionResult<LeaveCalculationResponse>> Start(
        LeaveCalculationRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await service.StartAsync(request!, cancellationToken);

        if (result.ValidationMessage is not null)
        {
            return BadRequest(new { message = result.ValidationMessage });
        }

        return result.Accepted
            ? Accepted(result.Response)
            : Ok(result.Response);
    }

    [HttpGet("{calculationId}")]
    public ActionResult<LeaveCalculationInfo> GetById(string calculationId)
    {
        var calculation = service.GetById(calculationId);

        return calculation is null
            ? NotFound(new { message = $"Leave calculation '{calculationId}' was not found." })
            : Ok(calculation);
    }
}
