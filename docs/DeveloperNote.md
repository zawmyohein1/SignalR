# Developer Note

## Purpose

This note explains the important source-code parts for the SignalR realtime demo.

Focus areas:

- Web3 SignalR browser client
- RealtimeHub project
- How the page joins the correct group
- How Web3.Api pushes status to the correct page

This document does not explain every source file. It focuses only on the parts needed to understand and maintain the realtime flow.

---

## Important Projects

| Project | Role |
| --- | --- |
| `Timesoft.Solution.Web3` | ASP.NET MVC 5 UI. Shows the Leave Calculation Page. |
| `Timesoft.Solution.Api.Web3` | ASP.NET Web API 2. Starts and runs the leave calculation process. |
| `Timesoft.Solution.RealtimeHub` | ASP.NET Core SignalR server. Sends realtime updates to the page. |

---

## Main Realtime Flow

```text
Leave Calculation Page
        |
        | HTTP start request
        v
Web3.Api
        |
        | status notification
        v
RealtimeHub
        |
        | SignalR push
        v
Correct Leave Calculation Page group only
```

---

## Web3 SignalR Client

File:

```text
Timesoft.Solution.Web3\Scripts\leave-calculation-signalr-client.js
```

This file is the browser-side SignalR wrapper.

It keeps SignalR code separate from the page action script. The page does not need to know all SignalR connection details. It only calls this client to configure, start, reset, and refresh calculation status.

### Main Responsibilities

- Store current calculation context.
- Start SignalR connection when SignalR is enabled.
- Join the correct company/user/calculation group.
- Receive `LeaveCalculationStatusUpdated` events.
- Update the page through callback functions.
- Reconnect automatically if the SignalR connection drops.
- Fall back to snapshot polling if SignalR is disabled or unavailable.
- Ignore duplicate notifications.

### Runtime State

The `state` object stores the active page cycle:

```text
config
connection
pollingTimer
seenNotifications
currentCalculationId
currentHubAccessToken
currentCompanyCode
currentLoginUserId
```

This is important because one page can start one calculation cycle, then reset and start another cycle later.

### `configure(options)`

This function receives page-level configuration.

Important values:

- `calculationProxyUrl`
- `hubUrl`
- `signalREnabled`
- `callbacks`

The page passes callbacks so this client can update UI without directly depending on page HTML.

Example callback purposes:

- update SignalR badge
- update current status
- add progress log row
- stop Process button spinner when finished

### `start(response)`

This function starts the realtime tracking after Web3.Api returns the start response.

It saves:

- `calculationId`
- `hubAccessToken`
- `companyCode`
- `loginUserId`

Then it decides:

```text
If SignalR enabled:
    connect to RealtimeHub
    join calculation group
Else:
    use snapshot polling
```

Important point:

SignalR does not start the calculation. Web3.Api already started the calculation. SignalR only tracks progress.

### `connectToHub(response)`

This function creates the SignalR connection:

```javascript
new signalR.HubConnectionBuilder()
    .withUrl(state.config.hubUrl, {
        accessTokenFactory: function () {
            return state.currentHubAccessToken;
        }
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();
```

The token is sent to RealtimeHub so the hub can verify that this browser is allowed to join the requested calculation group.

### `registerHubHandlers()`

This function registers client-side SignalR events.

Important event:

```text
LeaveCalculationStatusUpdated
```

When this event arrives, the client calls `handleNotification(notification)`.

Reconnect handlers:

- `onreconnecting`
- `onreconnected`
- `onclose`

When reconnect succeeds, the client joins the calculation group again and refreshes the latest snapshot. This helps the page recover from short network interruptions.

### `joinCalculationGroup(response)`

This function calls the hub method:

```text
JoinCalculationGroup(companyCode, loginUserId, calculationId)
```

The group is not joined by calculation id only. It uses company, user, and calculation id together.

This prevents one browser from receiving another company or user's update.

### `handleNotification(notification)`

This function is called when a status update is received.

It does three important things:

1. Builds a duplicate-check key.
2. Skips the notification if it was already processed.
3. Calls UI callbacks to update status and progress log.

When status is `Completed` or `Failed`, it calls `notifyFinish()`.

### Snapshot Polling Fallback

If SignalR is disabled or the SignalR script is not loaded, the client uses polling:

```text
GET calculation snapshot every 2 seconds
```

This makes the page still usable even when realtime mode is off.

---

## RealtimeHub Project

Folder:

```text
Timesoft.Solution.RealtimeHub
```

This is the standalone ASP.NET Core SignalR server.

It has two main jobs:

1. Accept browser SignalR connections.
2. Accept status notifications from Web3.Api and push them to the correct group.

---

## RealtimeHub Startup

File:

```text
Timesoft.Solution.RealtimeHub\Program.cs
```

Important setup:

```text
AddCors
AddControllers
AddSignalR
AddSingleton BasicNotificationAuthService
AddSingleton DemoHubTokenService
```

Important routes:

```text
/hubs/jobstatus
/api/notifications/leave-calculation-status
/
/error
```

### CORS

RealtimeHub allows Web3 and Web4 origins from configuration:

```text
Cors:AllowedOrigins
```

This is required because the browser page and RealtimeHub run on different ports.

### SignalR Route

The hub is mapped here:

```text
/hubs/jobstatus
```

The browser SignalR client connects to this URL.

---

## JobStatusHub

File:

```text
Timesoft.Solution.RealtimeHub\Hubs\JobStatusHub.cs
```

This is the SignalR hub class.

### Group Name

The group name is created by:

```text
company:{companyCode}:user:{loginUserId}:calculation:{calculationId}
```

This is the most important rule for correct browser delivery.

### `JoinCalculationGroup(...)`

This method lets a browser join one calculation group.

Before joining, it calls:

```text
ValidateCalculationAccess(...)
```

If validation passes, the current SignalR connection is added to the group.

### `LeaveCalculationGroup(...)`

This method removes the browser connection from the group.

It uses the same validation rule before removing.

### Access Validation

`ValidateCalculationAccess(...)` checks:

- company code is not empty
- login user id is not empty
- calculation id is not empty
- hub access token is valid
- token belongs to the same company/user/calculation

If validation fails, the browser cannot join the group.

---

## Hub Access Token

File:

```text
Timesoft.Solution.RealtimeHub\Services\DemoHubTokenService.cs
```

The hub token protects browser-to-hub group joining.

The token contains:

- company code
- login user id
- calculation id
- expiry time

The token is signed with HMAC SHA-256.

When the browser calls `JoinCalculationGroup`, RealtimeHub validates that the token matches the requested group.

Important point:

The browser cannot freely join another group just by changing JavaScript values. The token must match the group.

---

## API-to-Hub Notification Endpoint

File:

```text
Timesoft.Solution.RealtimeHub\Controllers\NotificationsController.cs
```

Endpoint:

```text
POST /api/notifications/leave-calculation-status
```

Web3.Api calls this endpoint when calculation status changes.

The endpoint validates:

- API-to-Hub basic authentication
- calculation id
- company code
- login user id

Then it sends:

```text
LeaveCalculationStatusUpdated
```

to:

```text
company:{companyCode}:user:{loginUserId}:calculation:{calculationId}
```

Only browsers in that group receive the update.

---

## API-to-Hub Basic Authentication

File:

```text
Timesoft.Solution.RealtimeHub\Services\BasicNotificationAuthService.cs
```

This service protects the notification endpoint.

Web3.Api must send a Basic Authentication header. RealtimeHub checks the configured username and password.

Configuration:

```text
NotificationAuth:Username
NotificationAuth:Password
```

In this demo, the credentials are simple. In production, use stronger service-to-service authentication.

---

## Realtime Message Model

File:

```text
Timesoft.Solution.RealtimeHub\Models\LeaveCalculationStatusNotification.cs
```

This model represents one status update.

Important fields:

- `CalculationId`
- `CompanyCode`
- `LoginUserId`
- `DepartmentCode`
- `EmployeeNo`
- `Year`
- `Status`
- `Message`
- `Timestamp`

These fields allow the hub and page to know exactly which calculation the update belongs to.

---

## Why Wrong Browser Update Should Not Happen

Wrong browser update is prevented by three layers:

### 1. Group Name Is Specific

```text
company + user + calculation
```

This is more specific than calculation id alone.

### 2. Browser Must Have Valid Token

The browser cannot join a group unless its token matches the same company, user, and calculation id.

### 3. RealtimeHub Pushes Only To The Group

RealtimeHub uses:

```text
Clients.Group(groupName)
```

It does not broadcast to all connected browsers.

---

## If Browser Does Not Join Group

The calculation still runs.

Reason:

Web3.Api starts and owns the calculation process. SignalR group joining only controls realtime delivery to the browser.

Result:

| Case | Calculation Runs | Browser Gets Realtime Updates |
| --- | --- | --- |
| Browser joins group | Yes | Yes |
| Browser does not join group | Yes | No |

---

## Important Demo Limitation

This is a demo implementation.

For production, improve:

- real authentication
- secure token secret storage
- database job state
- retry for failed API-to-Hub notification
- central logging
- monitoring
- load testing
- multi-server SignalR scale-out

