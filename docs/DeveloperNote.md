# Developer Note

## Purpose

This note explains the important source-code parts for the SignalR realtime demo.

Focus areas:

- Web3 SignalR browser client
- Navigation restore after leaving and returning to the calculation page
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

The same client behavior exists in the Web4 project:

```text
Timesoft.Solution.Web4\wwwroot\js\leave-calculation-signalr-client.js
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
- Resume an existing active calculation after page navigation.
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

If the SignalR connection attempt fails, the client does not fail the calculation start. It switches the display layer to snapshot polling because Api already owns the running process.

### `resume(savedCalculation)`

This function resumes tracking for a calculation that was saved before the user navigated away.

It restores:

- `calculationId`
- `hubAccessToken`
- `companyCode`
- `loginUserId`

Then it uses the same tracking decision as `start(response)`:

```text
If SignalR enabled:
    reconnect to RealtimeHub
    join calculation group again
Else:
    use snapshot polling
```

The calculation is not started again. The page only resumes watching the existing calculation id.

### `refreshSnapshot(calculationId)`

This function reads the latest calculation snapshot through the MVC `Details` endpoint.

For Web3 MVC:

```text
GET /LeaveCalculations/Details/{id}
```

For Web4 MVC:

```text
GET /LeaveCalculations/Details/{calculationId}
```

The MVC controller forwards the request to Api:

```text
GET /api/leave-calculations/{calculationId}
```

The returned snapshot includes the current status and history. The page uses it to rebuild the progress log after navigation and to catch missed SignalR messages after reconnect.

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

## Navigation Restore Implementation

The navigation restore feature is implemented in the Leave Calculation page script.

Files:

```text
Timesoft.Solution.Web3\Views\Home\Index.cshtml
Timesoft.Solution.Web4\Views\Home\Index.cshtml
```

### Active Calculation Storage

When a start response contains a calculation id, the page saves an active calculation pointer in configured browser storage.

Storage key:

```text
timesoft.leaveCalculation.active
```

The stored value contains the information needed to resume watching the same process:

- calculation id
- company code
- login user id
- period
- department
- employee
- year
- hub access token
- execution mode
- SignalR enabled flag

Important:

```text
Browser storage is not the source of truth.
```

Browser storage only tells the page which calculation id to reload. The actual current status and progress history are loaded from Api XML storage through the `Details` endpoint.

### Page Load Restore Flow

When `Index.cshtml` loads:

1. Read `timesoft.leaveCalculation.active` from the configured storage.
2. If no active calculation exists, optionally restore only the login form context.
3. If an active calculation exists, restore the form values and show the calculation panel.
4. Call `refreshSnapshot(calculationId)` to read the latest Api state.
5. Rebuild the progress log from snapshot history.
6. If status is still running, call `resume(savedCalculation)`.
7. If status is `Completed` or `Failed`, show the final status and enable the Process button.

This is the main reason the page can show a running status after the user goes to `View Leave` and returns.

### Running vs Completed Decision

The decision is based on the latest API snapshot status, not on browser storage.

| Snapshot status | Page behavior after return |
| --- | --- |
| `Accepted`, `Started`, `Calculating leave entitlement`, or any non-terminal status | Disable Process button, reconnect to SignalR when possible, continue snapshot polling |
| `Completed` | Show completed state, replay history, enable Process button |
| `Failed` | Show failed state, replay history, enable Process button |

### Restore Storage Configuration

Web3 reads these keys from `Web.config`:

```text
LeaveCalculationDemo-RestoreStorage
LeaveCalculationDemo-SignalREnabled
LeaveCalculationDemo-ApiBaseUrl
LeaveCalculationDemo-HubUrl
```

Web4 reads these keys from `appsettings.json`:

```text
LeaveCalculationDemo:RestoreStorage
LeaveCalculationDemo:SignalREnabled
LeaveCalculationDemo:ApiBaseUrl
LeaveCalculationDemo:HubUrl
```

Supported restore values:

| Value | Meaning |
| --- | --- |
| `session` | Use `sessionStorage`; restore in the same tab/session. |
| `local` | Use `localStorage`; restore after browser close/reopen. |
| `off` | Do not restore active calculation state. |

### Navigation Test Page

The navigation page is:

```text
Timesoft.Solution.Web3\Views\Home\ViewLeave.cshtml
Timesoft.Solution.Web4\Views\Home\ViewLeave.cshtml
```

It is only used to test leaving and returning to the Leave Calculation page. It does not own calculation state and does not call Api.

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

