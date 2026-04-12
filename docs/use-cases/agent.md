# Agent Rules - Leave Calculation Realtime Demo

Use this file as project-specific guidance before implementing the final enhancement.

## Main Goal

Build a demo that shows how a long-running Leave Calculation process can run in the background while progress is pushed to the correct browser through a standalone ASP.NET Core SignalR Hub.

Final demo target:

```text
Chrome  -> Company A
Firefox -> Company B
Edge    -> Company C

All companies run Leave Calculation at the same time.
Each browser receives only its own company/user/calculation updates.
```

## Projects To Keep Working

Enhance all existing projects:

```text
Timesoft.Solution.Web3
Timesoft.Solution.Api.Web3
Timesoft.Solution.Web4
Timesoft.Solution.Api.Web4
Timesoft.Solution.RealtimeHub
```

Do not remove or break the existing standalone hub architecture.

## Required Architecture

```text
MVC UI
  -> calls API to start Leave Calculation
  -> API returns calculationId immediately
  -> API runs process in background
  -> API notifies standalone SignalR Hub
  -> Hub pushes progress to only the matching browser group
```

## SignalR Rules

- Use ASP.NET Core SignalR only.
- Use `@microsoft/signalr` JavaScript client.
- Do not use classic ASP.NET SignalR 2.x.
- Do not use `jquery.signalR`.
- Do not require an old jQuery version.
- jQuery can be used only for normal DOM/AJAX work.

## Group Isolation Rule

Use company/user/calculation-aware SignalR groups.

Preferred group format:

```text
company:{companyCode}:user:{loginUserId}:calculation:{calculationId}
```

Purpose:

- Company A must not receive Company B updates.
- Company B must not receive Company C updates.
- One user's calculation should not leak to another user's browser.

## Data Rules

- No database.
- Do not save final job/calculation state only in memory.
- Save Leave Calculation job state in XML for the demo.
- Keep XML simple and readable.
- Use locking around XML writes to avoid demo file conflicts.
- Use sample data from:

```text
docs/use-cases/SampleData_LeaveCalculation.md
```

## Demo Process Rules

- Real leave entitlement calculation is not required.
- Simulate realistic Leave Calculation progress.
- Execution time must be configurable.
- Use statuses similar to:

```text
Accepted
Started
Loading selected employees
Calculating leave entitlement
Updating leave balances
Completed
Failed
```

## UI Rules

- Add a login page concept with company selection.
- Add a Leave Calculation page with department, employee, leave type, year, and Process button.
- Keep the page simple, small, and attractive.
- Keep the UI style close to the supplied TIMES SOFTWARE screenshots.
- Clearly show current company/login context.
- Clearly show current status and progress log.

## Configurable SignalR Rule

SignalR must be page-configurable.

Meaning:

- Existing pages should not be affected.
- Page A can use SignalR.
- Page B can choose not to use SignalR.
- If SignalR is disabled, the page should still load normally.

## Security Rules

For sample only:

- API-to-Hub notification must be protected.
- Use Basic Authentication for API-to-Hub HTTP notification.
- Browser-to-Hub should use the ASP.NET Core SignalR `accessTokenFactory` token pattern.
- Do not place production secrets in source code.
- Document clearly that this is demo security, not production authentication.

Browser note:

Custom Basic Auth headers are not reliable for browser WebSocket connections.
Use SignalR client token support for browser-to-hub demo authentication.

## Documentation Rules

Keep these documents updated:

```text
docs/use-cases/plan.md
docs/use-cases/TestPlan_MultiCompanyLeaveCalculation.md
docs/use-cases/SampleData_LeaveCalculation.md
README.md
```

## Definition Of Done

- Framework MVC/API demo works.
- .NET 8 MVC/API demo works.
- RealtimeHub pushes only to the correct group.
- XML storage contains calculation status/history.
- Execution timing is configurable.
- SignalR client is configurable per page.
- API-to-Hub notification is protected.
- Browser-to-Hub demo token is checked.
- Chrome, Firefox, and Edge multi-company test passes.

