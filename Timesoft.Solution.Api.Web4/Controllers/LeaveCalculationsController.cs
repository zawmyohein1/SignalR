using JobRealtimeSample.Api.Models;
using JobRealtimeSample.Api.Vendors;
using Microsoft.AspNetCore.Mvc;

namespace JobRealtimeSample.Api.Controllers;

[ApiController]
[Route("api/leave-calculations")]
public sealed class LeaveCalculationsController(LeaveCalculationsVendor vendor) : ControllerBase
{
    [HttpPost("start")]
    public async Task<ActionResult<StartLeaveCalculationResponse>> Start(
        LeaveCalculationStartRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await vendor.StartAsync(request!, cancellationToken);

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
        var calculation = vendor.GetById(calculationId);

        return calculation is null
            ? NotFound(new { message = $"Leave calculation '{calculationId}' was not found." })
            : Ok(calculation);
    }
}
