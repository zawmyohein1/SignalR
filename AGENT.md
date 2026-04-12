# AGENT.md

## Purpose

This file is guidance for a Codex agent working on this SignalR realtime leave calculation demo or applying the same feature to the existing project.

The goal is not to redesign the system. The project names and architecture are already decided.

---

## Fixed Project Names

Use these names exactly:

| Project | Purpose |
| --- | --- |
| `Timesoft.Solution.Web3` | ASP.NET MVC 5 UI |
| `Timesoft.Solution.Api.Web3` | ASP.NET Web API 2 |
| `Timesoft.Solution.Web4` | ASP.NET Core MVC on .NET 8 |
| `Timesoft.Solution.Api.Web4` | ASP.NET Core API on .NET 8 |
| `Timesoft.Solution.RealtimeHub` | Standalone ASP.NET Core SignalR hub |
| `Timesoft.Solution.Demo.sln` | Solution file |

Do not rename projects, namespaces, folders, or solution unless the user explicitly asks.

---

## Main Architecture

The architecture is fixed:

```text
Web3 Leave Calculation Page
        |
        | HTTP request
        v
Web3.Api
        |
        | status notification
        v
RealtimeHub
        |
        | SignalR push
        v
Correct Leave Calculation Page only
```

Web4 follows the same idea, but the main business demo focus is Web3.

---

## Main Business Scenario

The demo is based on Leave Calculation.

The HR user selects:

- company
- department
- employee
- year

Then clicks `Process`.

The system recalculates leave entitlement in the background and shows realtime progress.

This is a demo. Do not implement real leave entitlement rules unless the user asks.

---

## Important Rule

SignalR is only for progress delivery.

Web3.Api owns the business process.

If the browser does not join the SignalR group:

- the calculation still runs
- the browser does not receive realtime updates

Do not make business execution depend on SignalR connection success.

---

## SignalR Group Rule

Use this group format:

```text
company:{companyCode}:user:{loginUserId}:calculation:{calculationId}
```

This prevents wrong-browser updates.

Do not broadcast calculation status to all clients.

Use group-based delivery only.

---

## SignalR Enabled / Disabled Mode

The feature must support both modes.

### SignalR Enabled

```text
Web3 calls Web3.Api
Web3.Api creates Calculation Id
Web3.Api starts background process
Web3.Api returns 202 Accepted quickly
Web3 connects to RealtimeHub
Web3 joins calculation group
RealtimeHub pushes progress updates
```

### SignalR Disabled

```text
Web3 calls Web3.Api
Web3.Api runs process synchronously
Web3 waits for final response
No SignalR connection is required
```

Use the same business logic where possible. Only the execution mode should change.

---

## Existing System Safety

When applying this to an existing system:

- Do not break current HTTP behavior.
- Add SignalR by configuration.
- Allow selected pages to opt in or opt out.
- Keep existing business logic reusable.
- Keep controllers thin.
- Move orchestration logic into services or vendor classes.
- Avoid large unrelated refactors.
- Do not remove existing behavior unless the user confirms.

---

## Important Files

Start by reading these files:

```text
Timesoft.Solution.Web3\Scripts\leave-calculation-signalr-client.js
Timesoft.Solution.Web3\Views\Home\Index.cshtml
Timesoft.Solution.Web3\Controllers\LeaveCalculationsController.cs
Timesoft.Solution.Api.Web3\Controllers\LeaveCalculationsController.cs
Timesoft.Solution.Api.Web3\Vendors\LeaveCalculationsVendor.cs
Timesoft.Solution.Api.Web3\Services\BackgroundLeaveCalculationRunner.cs
Timesoft.Solution.RealtimeHub\Program.cs
Timesoft.Solution.RealtimeHub\Hubs\JobStatusHub.cs
Timesoft.Solution.RealtimeHub\Controllers\NotificationsController.cs
Timesoft.Solution.RealtimeHub\Services\DemoHubTokenService.cs
Timesoft.Solution.RealtimeHub\Services\BasicNotificationAuthService.cs
```

---

## JavaScript Rule

Keep SignalR client logic separate from page action logic.

SignalR wrapper:

```text
leave-calculation-signalr-client.js
```

Page-specific script can live in the page view or page script.

The page should call the SignalR wrapper through clear methods:

- configure
- start
- reset
- refreshSnapshot

---

## RealtimeHub Rule

RealtimeHub should stay simple.

It should:

- accept SignalR connections
- validate group access
- receive Web3.Api notifications
- push updates to the correct group

It should not:

- run leave calculation
- store business data
- decide business rules
- broadcast to all users

---

## Security Rule

Keep demo security simple, but document production needs.

Current demo security concepts:

- browser-to-hub token
- API-to-Hub basic authentication
- HTTPS
- group validation

Production should improve:

- real user authentication
- stronger service-to-service authentication
- secure secret storage
- authorization check before group join
- no sensitive data in SignalR messages

---

## Documentation Rule

When changing behavior, update the docs:

```text
docs\ProjectDetails.md
docs\TestPlan.md
docs\QuestionAndAnswer.md
docs\DeveloperNote.md
docs\SystemDesignDiagram.svg
docs\ProcessOneFullCycle.svg
```

Keep manager documents simple and concise.

Keep developer notes practical and source-code focused.

---

## Testing Rule

For SignalR verification, test at least:

- one browser, one calculation
- two browsers, different companies
- two browsers, same company, different calculations
- SignalR disabled mode
- RealtimeHub not running
- browser refresh during calculation

The key pass condition:

```text
Only the correct Leave Calculation Page receives its own progress updates.
```

---

## Git Rule

Do not commit runtime XML data unless the user asks.

Runtime files may change during testing:

```text
Timesoft.Solution.Api.Web3\App_Data\LeaveCalculationJobs.xml
Timesoft.Solution.Api.Web4\App_Data\LeaveCalculationJobs.xml
```

Treat these as test/runtime data, not source-code changes.

