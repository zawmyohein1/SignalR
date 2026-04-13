# PLAN.md

## Feature Plan: Restore Leave Calculation Status After Navigation

### Purpose

Add configurable browser-side restore behavior for the Leave Calculation page.

When a user starts a calculation, navigates to another page, and returns to the Leave Calculation page, the page should reload the latest calculation state from the API. If the process is still running, the page should show the running status and reconnect to SignalR. If the process is complete, the page should show the completed status and progress history.

### Configuration

Add a storage mode setting for both Web3 and Web4.

Supported values:

- `off`: Do not remember or restore the active calculation.
- `session`: Use `sessionStorage`. Restore only within the same browser tab/session.
- `local`: Use `localStorage`. Restore after browser close/reopen when browser storage still exists.

Web4 setting:

```json
"LeaveCalculationDemo": {
  "RestoreStorage": "session"
}
```

Web3 setting:

```xml
<add key="LeaveCalculationDemo-RestoreStorage" value="session" />
```

### Pages

Add a second simple navigation page in both MVC UI projects.

Required behavior:

- Leave Calculation page includes a link to the second page.
- Second page includes a link back to Leave Calculation.
- This gives a clear manual test for leaving and returning while a calculation is running.

Suggested route names:

- Web4: `Home/ViewLeave`
- Web3: `Home/ViewLeave`

### Browser State

When a calculation starts successfully, store the active calculation context according to the configured storage mode.

Store:

- `calculationId`
- `companyCode`
- `companyText`
- `loginUserId`
- `period`
- `hubAccessToken`
- `executionMode`
- `signalREnabled`
- `startedAt`

Do not store anything when storage mode is `off`.

### Restore Flow

On Leave Calculation page load:

1. Read configured storage mode.
2. Try to load the saved active calculation.
3. If no saved calculation exists, show the normal login screen.
4. If saved calculation exists, rebuild the login context and show the calculation panel.
5. Call the existing details endpoint for the saved `calculationId`.
6. Rebuild the status panel and progress log from the API snapshot/history.
7. If status is `Completed` or `Failed`, keep the final status visible and enable the Process button.
8. If status is still running, disable the Process button, reconnect to SignalR, join the calculation group, and refresh the snapshot again after connecting.

### SignalR Client Updates

Update both SignalR client scripts so they can resume an existing calculation, not only start a new one.

Add a method similar to:

```text
resume(savedCalculation)
```

Responsibilities:

- Set current calculation id, token, company, and user.
- Connect to SignalR when enabled and available.
- Join the correct company/user/calculation group.
- Fall back to polling if SignalR is disabled or the client script is unavailable.
- Refresh the latest snapshot after reconnecting.

### UI Updates

Update both Leave Calculation pages to:

- Read restore storage mode from server-rendered data attributes.
- Save active calculation state after `Start` succeeds.
- Restore active calculation state on page load.
- Show a clear log row when the page restores from browser storage.
- Keep completed calculations visible when returning to the page.
- Allow a new Process click after `Completed` or `Failed`.

### Files To Update

Web4:

- `Timesoft.Solution.Web4/appsettings.json`
- `Timesoft.Solution.Web4/Models/LeaveCalculationPageViewModel.cs`
- `Timesoft.Solution.Web4/Controllers/HomeController.cs`
- `Timesoft.Solution.Web4/Views/Home/Index.cshtml`
- `Timesoft.Solution.Web4/Views/Home/ViewLeave.cshtml`
- `Timesoft.Solution.Web4/wwwroot/js/leave-calculation-signalr-client.js`

Web3:

- `Timesoft.Solution.Web3/Web.config`
- `Timesoft.Solution.Web3/Models/LeaveCalculationPageViewModel.cs`
- `Timesoft.Solution.Web3/Controllers/HomeController.cs`
- `Timesoft.Solution.Web3/Views/Home/Index.cshtml`
- `Timesoft.Solution.Web3/Views/Home/ViewLeave.cshtml`
- `Timesoft.Solution.Web3/Scripts/leave-calculation-signalr-client.js`

### Manual Test Cases

1. Set restore mode to `session`, start a calculation, navigate to the second page, return before completion, and confirm the page shows the running status and receives new updates.
2. Set restore mode to `session`, start a calculation, navigate away until completion, return, and confirm the page shows `Completed` and the full progress log.
3. Set restore mode to `local`, start a calculation, close/reopen the browser if possible, and confirm the page restores the latest status.
4. Set restore mode to `off`, start a calculation, navigate away and return, and confirm the page starts fresh.
5. Disable SignalR and confirm the restore flow still loads the latest snapshot through the details endpoint.
6. Use separate browser tabs/users and confirm one saved calculation does not overwrite another tab when `session` mode is used.

### Build Verification

Run:

```powershell
dotnet build D:\Zaw\SignalR\Timesoft.Solution.Web4\Timesoft.Solution.Web4.csproj --no-restore
```

Run:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' D:\Zaw\SignalR\Timesoft.Solution.Web3\Timesoft.Solution.Web3.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /nologo /v:m
```

### Notes

The API XML store remains the source of truth. Browser storage is only a bookmark to the active calculation and the data needed to rejoin the SignalR group.

For production, prefer issuing a fresh hub token during restore instead of keeping a long-lived token in browser storage.

## Purpose

This plan explains how to implement the same SignalR realtime leave calculation feature in the existing project.

Project names and architecture are fixed. Do not redesign them.

---

## Goal

Add realtime progress updates for long-running Leave Calculation.

The user should not wait on one long HTTP request when SignalR is enabled.

The page should show:

- Calculation Id
- SignalR connection state
- current status
- API response time
- progress log
- completed or failed result

---

## Target Architecture

```text
Timesoft.Solution.Web3
        |
        | HTTP start request
        v
Timesoft.Solution.Api.Web3
        |
        | status notification
        v
Timesoft.Solution.RealtimeHub
        |
        | SignalR group push
        v
Correct Leave Calculation Page only
```

Web4 follows the same architecture after Web3 behavior is stable.

---

## Phase 1: Confirm Current Behavior

1. Run Web3.
2. Run Web3.Api.
3. Confirm Leave Calculation works without SignalR.
4. Identify current controller action for Process button.
5. Identify current business calculation method.
6. Confirm current timeout or long-request behavior.

Do not edit code before this baseline is understood.

---

## Phase 2: Add Configuration

Add configuration to control SignalR mode.

Required behavior:

```text
SignalR enabled:
    return 202 Accepted quickly
    run calculation in background
    send realtime progress

SignalR disabled:
    run calculation inside normal HTTP request
    return final Completed response
```

Keep configuration simple. Avoid too many on/off flags.

Recommended setting:

```text
SignalREnabled
```

---

## Phase 3: Add Calculation State

Create a calculation state model.

Minimum fields:

- calculation id
- company code
- login user id
- department code
- employee no
- year
- status
- message
- timestamp
- history

For demo:

```text
XML storage is acceptable.
```

For production:

```text
Use database storage.
```

---

## Phase 4: Refactor Web3.Api

Keep Web3.Api controller thin.

Move orchestration into a service or vendor class.

Controller should mainly:

- receive request
- call vendor/service
- return HTTP result

Vendor/service should:

- validate input
- create calculation id
- save accepted state
- decide SignalR enabled/disabled path
- start background execution when enabled
- run synchronously when disabled
- return start or completed result

---

## Phase 5: Reuse Same Business Logic

Use one calculation function for both modes.

```text
SignalR enabled:
    background task calls calculation function

SignalR disabled:
    HTTP request calls calculation function
```

Do not duplicate business logic.

Only the execution wrapper should be different.

---

## Phase 6: Add RealtimeHub

Use standalone project:

```text
Timesoft.Solution.RealtimeHub
```

Required features:

- SignalR hub route: `/hubs/jobstatus`
- HTTP notification endpoint
- CORS for Web3 and Web4
- browser hub token validation
- API-to-Hub authentication
- group-based message delivery

Do not add leave calculation logic to RealtimeHub.

---

## Phase 7: Add Group Isolation

Use this group rule:

```text
company:{companyCode}:user:{loginUserId}:calculation:{calculationId}
```

Group join must be validated.

RealtimeHub must push only to:

```text
Clients.Group(groupName)
```

Never use broadcast for calculation progress.

---

## Phase 8: Add Web3 Client Script

Create or update:

```text
Timesoft.Solution.Web3\Scripts\leave-calculation-signalr-client.js
```

Responsibilities:

- configure SignalR settings
- connect to RealtimeHub
- send hub access token
- join calculation group
- receive status updates
- handle reconnect
- refresh snapshot after reconnect
- fallback to polling when SignalR disabled

Keep page-specific UI code separate.

---

## Phase 9: Update Leave Calculation Page

The page should show:

- login context
- department
- employee
- year
- Process button
- SignalR state
- execution mode
- calculation id
- current status
- API response time
- progress log

During processing:

- Process button shows spinner
- current status updates
- progress log auto-scrolls

After completion:

- spinner stops
- status becomes Completed
- final log row is shown

---

## Phase 10: Add API-to-Hub Notification

Web3.Api should notify RealtimeHub for each status update.

Notification should include:

- calculation id
- company code
- login user id
- department code
- employee no
- year
- status
- message
- timestamp

If notification fails, log the error.

For production, add retry or queue support.

---

## Phase 11: Add Security

Demo security:

- browser hub access token
- API-to-Hub basic authentication
- HTTPS

Production security:

- real user authentication
- authorization before group join
- secure secret storage
- stronger service-to-service authentication
- no sensitive data in SignalR message

---

## Phase 12: Test SignalR Correctness

Required tests:

1. Single browser, one calculation.
2. Chrome company A and Firefox company B at same time.
3. Same company, two users, two calculations.
4. SignalR disabled mode.
5. RealtimeHub stopped.
6. Browser refresh during calculation.

Pass condition:

```text
Each Leave Calculation Page receives only its own calculation updates.
```

---

## Phase 13: Update Documentation

Update:

```text
docs\ProjectDetails.md
docs\TestPlan.md
docs\QuestionAndAnswer.md
docs\DeveloperNote.md
docs\SystemDesignDiagram.svg
docs\ProcessOneFullCycle.svg
```

Keep documents presentation-friendly.

---

## Rollback Plan

If SignalR causes issues:

1. Set `SignalREnabled` to false.
2. Use normal HTTP request mode.
3. Keep existing calculation behavior available.
4. Disable RealtimeHub dependency for the affected page.

This allows the existing system to continue working while SignalR is investigated.

---

## Production Readiness Checklist

Before production:

- Replace XML storage with database.
- Add durable background processing or queue.
- Add retry for failed hub notifications.
- Add real authentication and authorization.
- Secure secrets.
- Add logging and monitoring.
- Load test active SignalR connections.
- Confirm WebSocket support in hosting environment.
- Decide scale-out strategy.
- Document support and troubleshooting steps.

