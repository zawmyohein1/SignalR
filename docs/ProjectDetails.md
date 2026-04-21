# SignalR Realtime Demo

## Overview

This solution demonstrates a leave calculation workflow where the browser starts a long-running process, the API returns quickly, and progress is pushed back to the correct page in realtime.

The current design supports both the legacy .NET Framework path (`Web3` / `Api.Web3`) and the .NET 8 path (`Web4` / `Api.Web4`) while sharing one standalone realtime server (`RealtimeHub`).

## Solution Structure

| Project | Technology | Responsibility |
| --- | --- | --- |
| `Timesoft.Solution.Web3` | ASP.NET MVC 5 / .NET Framework 4.8 | Legacy browser UI |
| `Timesoft.Solution.Api.Web3` | ASP.NET Web API 2 / .NET Framework 4.8 | Legacy API and background calculation runner |
| `Timesoft.Solution.Web4` | ASP.NET Core MVC / .NET 8 | .NET 8 browser UI |
| `Timesoft.Solution.Api.Web4` | ASP.NET Core Web API / .NET 8 | .NET 8 API and background calculation runner |
| `Timesoft.Solution.RealtimeHub` | ASP.NET Core SignalR / .NET 8 | Shared realtime delivery server |

## End-to-End Flow

```text
Browser UI
  -> MVC proxy controller
  -> API start endpoint
  -> calculation saved to XML store
  -> background runner continues work
  -> API publishes progress to Azure Service Bus
  -> RealtimeHub consumes queue messages
  -> RealtimeHub pushes SignalR updates to the correct browser group
```

When realtime is unavailable, the browser falls back to polling the calculation `Details` endpoint. The background process still runs in the API either way.

## Why Azure Services

### Why Azure Service Bus

- replaces the direct API-to-hub HTTP notification path with a queue-based handoff
- fits better when employee count and progress update volume increase
- lets the API publish progress without depending on the hub being available at that moment
- gives one shared queue for both `Api.Web3` and `Api.Web4`

### Why Azure SignalR

- keeps the browser SignalR contract unchanged while moving realtime transport to a managed service
- reduces the need for the app server to own all realtime connection scale concerns directly
- lets both Web3 and Web4 use the same hub route and delivery model
- keeps local fallback possible because provider mode can still switch between `Local` and `Azure`

## Realtime Delivery Model

### SignalR Group Rule

Each browser joins a private group using:

```text
company:{companyCode}:user:{loginUserId}:calculation:{calculationId}
```

This prevents updates from crossing between companies, users, or calculations.

### Hub Token

The API creates a hub access token that includes:

- company code
- login user id
- calculation id
- expiry time

`RealtimeHub` validates that token before allowing the browser to join the group.

Important rule:

```text
The API and RealtimeHub must use the same hub token secret.
```

## Configuration Model

### Shared SignalR Settings

The current source of truth is:

```text
SignalR:Enabled
SignalR:Provider
```

Meaning:

- `SignalR:Enabled` controls whether realtime mode is active at all
- `SignalR:Provider` chooses the transport provider when realtime is enabled
- supported provider values are `Local` and `Azure`

### Service Bus Settings

The API projects publish progress to one queue and `RealtimeHub` consumes from the same queue.

Current queue setting:

```text
leave-calculation-status
```

The repo now supports a transport setting for local reliability:

- `ServiceBus:TransportType` in .NET 8 apps
- `ServiceBus-TransportType` in `Api.Web3`

Recommended local value:

```text
AmqpWebSockets
```

### Azure SignalR Settings

`RealtimeHub` reads:

```text
SignalR:Provider
Azure:SignalR:ConnectionString
```

The browser still connects to the same hub route:

```text
/hubs/jobstatus
```

### Local Override Files

Local machine settings are stored in:

- `Timesoft.Solution.Api.Web3/Web.local.config`
- `Timesoft.Solution.Web3/Web.local.config`
- `Timesoft.Solution.Api.Web4/appsettings.local.json`
- `Timesoft.Solution.Web4/appsettings.local.json`
- `Timesoft.Solution.RealtimeHub/appsettings.local.json`

These files are intentionally ignored by Git but included in the projects so they appear in Solution Explorer.

## Local Development Setup

### Current Local Ports

| Project | URL |
| --- | --- |
| `Timesoft.Solution.Web3` | `http://localhost:57635` |
| `Timesoft.Solution.Api.Web3` | `http://localhost:57636` |
| `Timesoft.Solution.RealtimeHub` | `https://localhost:5003` |
| `Timesoft.Solution.Web4` | `https://localhost:5101` |
| `Timesoft.Solution.Api.Web4` | `https://localhost:5102` |

### Required Matching Settings

For Azure realtime testing, keep these aligned:

- both API projects publish to the same Service Bus queue
- `RealtimeHub` consumes the same queue
- API and `RealtimeHub` share the same hub token secret
- browser `HubUrl` points to `https://localhost:5003/hubs/jobstatus`
- `RealtimeHub` CORS allows the exact browser origins that are running

Example:

- Web3 origin: `http://localhost:57635`
- Web4 origin: `https://localhost:5101`

If the origin does not match exactly, the browser will fail SignalR connect and fall back to polling.

## Runtime Behavior

### SignalR Enabled

When `SignalR:Enabled=true`:

- API returns quickly
- calculation continues in the background
- progress is saved to XML
- progress is also published to Service Bus
- browser attempts SignalR connection and group join

### SignalR Disabled

When `SignalR:Enabled=false`:

- API keeps the request open and runs the calculation synchronously
- realtime publish path is skipped
- browser does not attempt realtime tracking

### Polling Fallback

If the browser cannot connect to SignalR, it falls back to polling the `Details` endpoint every two seconds.

UI meaning:

- `Connected` = realtime connection is active
- `Polling` = realtime failed and the page is using snapshot polling
- `Disabled` = realtime is intentionally off by configuration

`Polling` is a valid fallback, but it is not the preferred healthy state.

## Navigation Restore

The browser stores only a resume pointer, not the full calculation state.

Storage key:

```text
timesoft.leaveCalculation.active
```

Restore behavior:

1. page reads the saved calculation pointer
2. page calls the `Details` endpoint
3. API returns the latest XML-backed snapshot and history
4. page rebuilds the progress log
5. if the calculation is still running, the page resumes SignalR or polling

Important rule:

```text
Browser storage is not the source of truth. XML storage in the API is.
```

## Important Source Files

### Web Projects

| File | Purpose |
| --- | --- |
| `Views/Home/Index.cshtml` | Main leave calculation page |
| `Views/Home/ViewLeave.cshtml` | Navigation test page |
| `leave-calculation-signalr-client.js` | Browser SignalR wrapper and polling fallback |
| `Controllers/HomeController.cs` | Loads page model and config |
| `Controllers/LeaveCalculationsController.cs` | MVC proxy to the API |

### API Projects

| File | Purpose |
| --- | --- |
| `Controllers/LeaveCalculationsController.cs` | Start and details endpoints |
| `Services/LeaveCalculationService.cs` | Main start flow |
| `Services/LeaveCalculationRunner.cs` | Background or synchronous execution |
| `Services/LeaveCalculationStore.cs` | XML persistence and history |
| `Services/NotificationPublisher.cs` | Service Bus publish path |
| `Services/HubTokenService.cs` | Hub token creation |

### RealtimeHub

| File | Purpose |
| --- | --- |
| `Program.cs` | Startup, CORS, provider selection, hub route |
| `Configuration/SignalRProvider.cs` | Reads SignalR enabled/provider settings |
| `Extensions/ServiceCollectionExtensions.cs` | SignalR and Service Bus registration |
| `Hubs/NotificationHub.cs` | Browser group join endpoint |
| `Services/NotificationConsumer.cs` | Service Bus queue consumer |
| `Services/NotificationPublisher.cs` | Group-targeted SignalR delivery |
| `Services/HubTokenService.cs` | Hub token validation |

## Troubleshooting

### Web4 shows `Polling` instead of `Connected`

Most common cause: `RealtimeHub` CORS does not include the exact Web4 origin.

Example mismatch:

- Web4 runs on `https://localhost:5101`
- `RealtimeHub` allows `https://localhost:5001`

Result:

- SignalR connect fails
- browser falls back to polling

### Service Bus receive/send connection resets

Local networks or VPNs may block AMQP TCP. Use:

```text
AmqpWebSockets
```

for Service Bus transport settings.

### Web3 API fails to load Azure Service Bus dependencies

`Api.Web3` is a .NET Framework app and needs binding redirects in `Web.config` for transitive Azure dependencies such as:

- `System.Diagnostics.DiagnosticSource`
- `Microsoft.Bcl.AsyncInterfaces`
- `System.Memory`
- `System.Buffers`
- `System.Runtime.CompilerServices.Unsafe`
- `System.Threading.Tasks.Extensions`

If these redirects are missing, the app may fail only when the user clicks `Process`.

### Hub token mismatch

If the API and `RealtimeHub` use different secrets:

- API still starts the calculation
- browser receives a hub token
- RealtimeHub rejects group join
- browser falls back to polling

### Local config appears in the project but does not affect runtime

Current runtime filenames are:

- `appsettings.local.json` for .NET 8 apps
- `Web.local.config` for .NET Framework apps

Make sure the actual runtime loader and file name match exactly.

## Current Constraints

- XML storage is demo-only
- background work is in-process, not durable across API restarts
- Web3 requires extra .NET Framework compatibility care
- no production authentication or authorization
- one shared queue and one shared hub keep the demo simple but not production-grade

## Future Improvements

- replace XML storage with a database
- move background work into a durable worker or queue-driven processor
- add production authentication and authorization
- add richer monitoring and health checks
- unify more of the Web3 and Web4 behavior behind shared abstractions
- reduce legacy .NET Framework-specific maintenance overhead over time
