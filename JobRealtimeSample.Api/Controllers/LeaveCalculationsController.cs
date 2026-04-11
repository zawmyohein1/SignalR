using JobRealtimeSample.Api.Models;
using JobRealtimeSample.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobRealtimeSample.Api.Controllers;

[ApiController]
[Route("api/leave-calculations")]
public sealed class LeaveCalculationsController(
    XmlLeaveCalculationStore store,
    DemoHubTokenService hubTokenService,
    BackgroundLeaveCalculationRunner backgroundRunner) : ControllerBase
{
    [HttpPost("start")]
    public ActionResult<StartLeaveCalculationResponse> Start(LeaveCalculationStartRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyCode))
        {
            return BadRequest(new { message = "companyCode is required." });
        }

        if (string.IsNullOrWhiteSpace(request.LoginUserId))
        {
            return BadRequest(new { message = "loginUserId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.DepartmentCode))
        {
            return BadRequest(new { message = "departmentCode is required." });
        }

        if (string.IsNullOrWhiteSpace(request.LeaveTypeCode))
        {
            return BadRequest(new { message = "leaveTypeCode is required." });
        }

        var calculation = store.Create(request);
        var hubAccessToken = hubTokenService.CreateToken(
            calculation.CompanyCode,
            calculation.LoginUserId,
            calculation.CalculationId);

        backgroundRunner.RunInBackground(calculation.CalculationId);

        return Accepted(new StartLeaveCalculationResponse
        {
            CalculationId = calculation.CalculationId,
            CompanyCode = calculation.CompanyCode,
            LoginUserId = calculation.LoginUserId,
            HubAccessToken = hubAccessToken,
            Status = calculation.Status,
            Message = calculation.Message
        });
    }

    [HttpGet("{calculationId}")]
    public ActionResult<LeaveCalculationInfo> GetById(string calculationId)
    {
        var calculation = store.Get(calculationId);

        return calculation is null
            ? NotFound(new { message = $"Leave calculation '{calculationId}' was not found." })
            : Ok(calculation);
    }
}

