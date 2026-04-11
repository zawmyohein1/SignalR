using JobRealtimeSample.RealtimeHub.Hubs;
using JobRealtimeSample.RealtimeHub.Models;
using JobRealtimeSample.RealtimeHub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace JobRealtimeSample.RealtimeHub.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(
    IHubContext<JobStatusHub> hubContext,
    BasicNotificationAuthService notificationAuth,
    ILogger<NotificationsController> logger) : ControllerBase
{
    [HttpPost("job-status")]
    public async Task<IActionResult> PostJobStatus(JobStatusNotification notification)
    {
        if (string.IsNullOrWhiteSpace(notification.JobId))
        {
            return BadRequest(new { message = "jobId is required." });
        }

        var groupName = JobStatusHub.JobGroupName(notification.JobId.Trim());

        await hubContext.Clients
            .Group(groupName)
            .SendAsync("JobStatusUpdated", notification);

        logger.LogInformation(
            "Pushed {Status} for job {JobId} to {GroupName}.",
            notification.Status,
            notification.JobId,
            groupName);

        return Ok(new { message = "Notification delivered." });
    }

    [HttpPost("leave-calculation-status")]
    public async Task<IActionResult> PostLeaveCalculationStatus(LeaveCalculationStatusNotification notification)
    {
        if (!notificationAuth.IsAuthorized(Request))
        {
            Response.Headers["WWW-Authenticate"] = "Basic realm=\"JobRealtimeSample.RealtimeHub\"";
            return Unauthorized(new { message = "API-to-Hub notification credentials are invalid." });
        }

        if (string.IsNullOrWhiteSpace(notification.CalculationId))
        {
            return BadRequest(new { message = "calculationId is required." });
        }

        if (string.IsNullOrWhiteSpace(notification.CompanyCode))
        {
            return BadRequest(new { message = "companyCode is required." });
        }

        if (string.IsNullOrWhiteSpace(notification.LoginUserId))
        {
            return BadRequest(new { message = "loginUserId is required." });
        }

        var groupName = JobStatusHub.CalculationGroupName(
            notification.CompanyCode.Trim(),
            notification.LoginUserId.Trim(),
            notification.CalculationId.Trim());

        await hubContext.Clients
            .Group(groupName)
            .SendAsync("LeaveCalculationStatusUpdated", notification);

        logger.LogInformation(
            "Pushed {Status} for calculation {CalculationId} to {GroupName}.",
            notification.Status,
            notification.CalculationId,
            groupName);

        return Ok(new { message = "Leave calculation notification delivered." });
    }
}
