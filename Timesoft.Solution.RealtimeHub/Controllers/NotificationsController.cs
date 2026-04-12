using Timesoft.Solution.RealtimeHub.Hubs;
using Timesoft.Solution.RealtimeHub.Models;
using Timesoft.Solution.RealtimeHub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Timesoft.Solution.RealtimeHub.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(
    IHubContext<JobStatusHub> hubContext,
    BasicNotificationAuthService notificationAuth,
    ILogger<NotificationsController> logger) : ControllerBase
{
    [HttpPost("leave-calculation-status")]
    public async Task<IActionResult> PostLeaveCalculationStatus(LeaveCalculationStatusNotification notification)
    {
        // Only the API project may post server-to-hub notifications.
        if (!notificationAuth.IsAuthorized(Request))
        {
            Response.Headers["WWW-Authenticate"] = "Basic realm=\"Timesoft.Solution.RealtimeHub\"";
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

        // Push only to browsers that joined this exact calculation group.
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
