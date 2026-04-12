# Project Details - SignalR Realtime Demo

## Purpose

This demo shows how a long-running Leave Calculation process can update the correct Leave Calculation Page in realtime without keeping one HTTP request open.

Key idea:

- Leave Calculation Page starts the process.
- Web3.Api returns a calculation id quickly.
- Web3.Api continues processing in the background.
- Web3.Api sends progress to the standalone SignalR hub.
- SignalR pushes updates only to the correct Leave Calculation Page group.

## Projects

| Project | Technology | Role |
| --- | --- | --- |
| `Timesoft.Solution.Web3` | ASP.NET MVC 5 / .NET Framework 4.8 | Legacy MVC UI demo |
| `Timesoft.Solution.Api.Web3` | ASP.NET Web API 2 / .NET Framework 4.8 | Legacy API demo |
| `Timesoft.Solution.Web4` | ASP.NET Core MVC / .NET 8 | .NET 8 MVC UI demo |
| `Timesoft.Solution.Api.Web4` | ASP.NET Core Web API / .NET 8 | .NET 8 API demo |
| `Timesoft.Solution.RealtimeHub` | ASP.NET Core SignalR / .NET 8 | Shared realtime server |

## SignalR Flow

```text
Leave Calculation Page
  |
  | Start Leave Calculation
  v
Web3
  |
  | POST /api/leave-calculations/start
  v
Web3.Api
  |
  | Return calculationId quickly
  | Run calculation in background
  | Send progress notification
  v
RealtimeHub
  |
  | Push JobStatusUpdated
  v
Correct Leave Calculation Page only
```

## Group Rule

Each Leave Calculation Page joins a private SignalR group:

```text
company:{companyCode}:user:{loginUserId}:calculation:{calculationId}
```

This prevents:

- Company A receiving Company B updates.
- User A receiving User B updates.
- Leave Calculation Page A receiving Leave Calculation Page B progress.

## Important Files

### Web Projects

Used by:

- `Timesoft.Solution.Web3`
- `Timesoft.Solution.Web4`

| File | Purpose |
| --- | --- |
| `Views/Home/Index.cshtml` | Login UI, Leave Calculation Page, Process button logic |
| `leave-calculation-signalr-client.js` | SignalR page connection, group join, update handling |
| `Controllers/LeaveCalculationsController.cs` | Web3 proxy that calls Web3.Api server-side |
| `Controllers/HomeController.cs` | Loads demo page data |
| `Web.config` / `appsettings.json` | Web3.Api URL, hub URL, SignalR enabled setting |

Main Web3 functions:

- Build request payload.
- Call Web3 controller.
- Connect to SignalR hub.
- Join calculation group.
- Show current status.
- Append progress log.
- Stop button spinner when completed or failed.

### Api Projects

Used by:

- `Timesoft.Solution.Api.Web3`
- `Timesoft.Solution.Api.Web4`

| File | Purpose |
| --- | --- |
| `Controllers/LeaveCalculationsController.cs` | Web3.Api start and status endpoints |
| `Vendors/LeaveCalculationsVendor.cs` | Main start-process flow |
| `Services/BackgroundLeaveCalculationRunner.cs` | Runs calculation in background or synchronous mode |
| `Services/RealtimeNotifier.cs` | Sends HTTP notification to RealtimeHub |
| `Services/XmlLeaveCalculationStore.cs` | Saves calculation state and history in XML |
| `Services/DemoHubTokenService.cs` | Creates demo page token for hub connection |

Main Web3.Api functions:

- Create calculation id.
- Save initial state.
- Return fast when SignalR is enabled.
- Run background calculation.
- Save each progress update.
- Notify RealtimeHub for each update.

### RealtimeHub Project

Used by:

- `Timesoft.Solution.RealtimeHub`

| File | Purpose |
| --- | --- |
| `Hubs/JobStatusHub.cs` | Leave Calculation Page connects and joins/leaves SignalR group |
| `Controllers/NotificationsController.cs` | Web3.Api posts status updates here |
| `Services/BasicNotificationAuthService.cs` | Protects Web3.Api-to-hub notification endpoint |
| `Services/DemoHubTokenService.cs` | Validates page hub token |
| `Models/LeaveCalculationStatusNotification.cs` | Realtime status message payload |
| `Program.cs` | Configures SignalR hub route and notification endpoint |

Main hub functions:

- Validate page token.
- Add Leave Calculation Page to correct group.
- Receive Web3.Api notification.
- Push `JobStatusUpdated` to matching group only.

## SignalR Enabled vs Disabled

| Mode | Behavior |
| --- | --- |
| SignalR enabled | Web3.Api returns fast, process runs in background, Leave Calculation Page receives realtime updates |
| SignalR disabled | Web3.Api runs process inside HTTP request, Leave Calculation Page waits for final response |

This makes the timeout problem easy to demonstrate.

## Pros

- Leave Calculation Page does not wait for the full calculation.
- Web3.Api response is fast when SignalR is enabled.
- Progress is visible in realtime.
- One standalone hub can support Web3 and Web4.
- Group rule protects updates from going to the wrong Leave Calculation Page.
- SignalR can be turned on or off for demo comparison.
- XML storage makes progress history easy to inspect.

## Cons / Limitations

- XML storage is for demo only.
- Background task is in-process, so it is not durable if Web3.Api restarts.
- No production authentication yet.
- No SignalR scale-out/backplane.
- No retry queue if Web3.Api-to-hub notification fails.
- SignalR disabled mode can still hit normal HTTP timeout risk.

## Production Improvement Ideas

- Replace XML with database.
- Move background work to queue or worker service.
- Add real authentication and authorization.
- Add SignalR scale-out or Azure SignalR.
- Add retry policy for hub notification.
- Add monitoring, audit log, and health checks.
