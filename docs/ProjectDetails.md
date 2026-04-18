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

## Navigation Restore Flow

The Leave Calculation Page can restore the last active calculation after the user navigates away and comes back.

Important rule:

```text
Browser storage keeps only the resume pointer.
Web3.Api / Web4.Api XML storage remains the source of truth.
```

When the user clicks `Process` in SignalR enabled mode:

1. Web sends a start request through the MVC proxy.
2. Api creates the calculation id and saves the initial XML state.
3. Api returns the calculation id quickly.
4. Web saves the active calculation pointer in browser storage.
5. Api continues the calculation in the background.
6. SignalR and snapshot polling keep the page updated.

If the user goes to `View Leave` and then returns to `Leave Calculation`:

1. Web reads the saved active calculation pointer.
2. Web calls the MVC `Details` endpoint for the calculation id.
3. MVC forwards the request to Api.
4. Api loads the latest calculation status and history from XML.
5. Web rebuilds the progress log from the returned history.
6. If the calculation is still running, Web reconnects to SignalR and continues polling snapshots.
7. If the calculation is completed or failed, Web shows the final status and keeps the Process button available for the next run.

This means the browser can leave the page while the background process continues. Returning to the page shows the current running status or the completed result based on the latest API snapshot.

`View Leave` is only a navigation test page for this demo. It does not run or own the calculation process.

## Restore Storage Configuration

The restore behavior is controlled by configuration:

| Project | Key | Values |
| --- | --- | --- |
| `Timesoft.Solution.Web3` | `LeaveCalculationDemo-RestoreStorage` | `session`, `local`, `off` |
| `Timesoft.Solution.Web4` | `LeaveCalculationDemo:RestoreStorage` | `session`, `local`, `off` |

Storage modes:

| Value | Behavior |
| --- | --- |
| `session` | Restore inside the same browser tab/session. This is the recommended demo default. |
| `local` | Restore even after closing and reopening the browser. |
| `off` | Do not restore active calculation state from browser storage. |

The active calculation storage key is:

```text
timesoft.leaveCalculation.active
```

The page also stores login form context as a small convenience, but that is not the main resume mechanism. The active calculation id plus the API snapshot is what restores running or completed calculation status.

## Important Files

### Web Projects

Used by:

- `Timesoft.Solution.Web3`
- `Timesoft.Solution.Web4`

| File | Purpose |
| --- | --- |
| `Views/Home/Index.cshtml` | Login UI, Leave Calculation Page, Process button logic |
| `Views/Home/ViewLeave.cshtml` | Demo navigation page used to leave and return to the calculation page |
| `leave-calculation-signalr-client.js` | SignalR page connection, group join, update handling |
| `Controllers/LeaveCalculationsController.cs` | MVC proxy that calls the matching Api project server-side |
| `Controllers/HomeController.cs` | Loads demo page data |
| `Web.config` / `appsettings.json` | Api URL, hub URL, SignalR enabled setting, restore storage mode |

Main Web functions:

- Build request payload.
- Call Web3 controller.
- Connect to SignalR hub.
- Join calculation group.
- Save the active calculation pointer for navigation restore.
- Read the latest calculation snapshot when the page loads again.
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

Main Api functions:

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
| SignalR enabled | Api returns fast, process runs in background, Leave Calculation Page receives realtime updates and can restore a running process after navigation |
| SignalR disabled | Api runs process inside the HTTP request, Leave Calculation Page waits for the final response |

This makes the timeout problem easy to demonstrate.

When SignalR is enabled but the browser cannot connect to RealtimeHub, the page uses snapshot polling against the calculation `Details` endpoint. Polling is a fallback for display only; the calculation still runs in Api.

## Current Working Ports

The demo is currently wired to these local ports:

| Project | URL |
| --- | --- |
| `Timesoft.Solution.Web3` | `http://localhost:57635` |
| `Timesoft.Solution.Api.Web3` | `http://localhost:57636` |
| `Timesoft.Solution.RealtimeHub` | `https://localhost:5003` |
| `Timesoft.Solution.Web4` | `https://localhost:5101` |
| `Timesoft.Solution.Api.Web4` | `https://localhost:5102` |

Web3 and Web4 both use the same RealtimeHub route:

```text
/hubs/jobstatus
```

Web3 and Web3.Api are configured for Azure SignalR testing, while Web4 remains the newer .NET 8 path that also uses the same hub.

## Azure SignalR Service

The realtime hub can run with Azure SignalR Service by configuration.

Required settings:

| Setting | Purpose |
| --- | --- |
| `SignalR:Provider` | Selects `Local` or `Azure` provider mode |
| `Azure:SignalR:ConnectionString` | Azure SignalR access string used by `Timesoft.Solution.RealtimeHub` |

Behavior:

- Keep the hub route unchanged: `/hubs/jobstatus`
- Keep the calculation group rule unchanged
- Use Azure SignalR only for the realtime transport and scale-out layer
- Keep local SignalR available for development or rollback

This lets the same Leave Calculation Page and API flow work without changing the business process.

## Final Working Setup

The repo currently runs with these local endpoints:

| Project | URL |
| --- | --- |
| `Timesoft.Solution.Web3` | `http://localhost:57635` |
| `Timesoft.Solution.Api.Web3` | `http://localhost:57636` |
| `Timesoft.Solution.RealtimeHub` | `https://localhost:5003` |
| `Timesoft.Solution.Web4` | `https://localhost:5101` |
| `Timesoft.Solution.Api.Web4` | `https://localhost:5102` |

Final configuration for Azure SignalR testing:

| Project | Key settings |
| --- | --- |
| `Timesoft.Solution.Web3` | `LeaveCalculationApiBaseUrl=http://localhost:57636`, `RealtimeHubUrl=https://localhost:5003/hubs/jobstatus`, `SignalREnabled=true`, `SignalRProvider=Azure` |
| `Timesoft.Solution.Web4` | `ApiBaseUrl=https://localhost:5102`, `HubUrl=https://localhost:5003/hubs/jobstatus`, `SignalREnabled=true`, `SignalRProvider=Azure` |
| `Timesoft.Solution.RealtimeHub` | `SignalR:Provider=Azure`, `Azure:SignalR:ConnectionString=<Azure connection string>` |

RealtimeHub CORS must include the browser origins used by the demo, especially:

- `http://localhost:57635`
- `https://localhost:57635`
- `http://localhost:57636`
- `https://localhost:57636`

The hub route remains unchanged:

```text
/hubs/jobstatus
```

## Pros

- Leave Calculation Page does not wait for the full calculation.
- Api response is fast when SignalR is enabled.
- Progress is visible in realtime.
- User can navigate away and return to the current running or completed calculation status.
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
- Browser storage stores only the last active calculation pointer for that browser/session.
- SignalR disabled mode can still hit normal HTTP timeout risk.

## Production Improvement Ideas

- Replace XML with database.
- Move background work to queue or worker service.
- Add real authentication and authorization.
- Add SignalR scale-out or Azure SignalR.
- Add retry policy for hub notification.
- Add monitoring, audit log, and health checks.
