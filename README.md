# Timesoft.Solution.Demo

Timesoft.Solution.Demo demonstrates how an MVC page can start a long-running Leave Calculation process without waiting on one timeout-prone HTTP request.

The API returns a calculation id immediately, runs the work in the background, saves progress to XML, and sends status updates to a standalone ASP.NET Core SignalR hub.

## Current Demo

The main scenario is:

```text
HR user logs in
HR opens Leave Calculation
HR selects Department, Employee, Leave Type, and Year
HR clicks Process
API returns calculationId immediately
API simulates Leave Entitlement processing in the background
RealtimeHub pushes progress only to the matching browser group
```

## Architecture

```text
[ASP.NET MVC UI]
    |
    | POST /api/leave-calculations/start
    v
[ASP.NET REST API] -- leave calculation in background --> send notification
                                                       |
                                                       v
                                           [Standalone SignalR Hub]
                                                       |
                                                       v
                                             push update to MVC UI
```

## Projects

| Project | Technology | Role |
| --- | --- | --- |
| `Timesoft.Solution.Web3` | ASP.NET MVC 5 / .NET Framework 4.8 | Primary legacy-style UI demo |
| `Timesoft.Solution.Api.Web3` | ASP.NET Web API 2 / .NET Framework 4.8 | Primary legacy-style API demo |
| `Timesoft.Solution.Web4` | ASP.NET Core MVC / .NET 8 | .NET 8 UI parity demo |
| `Timesoft.Solution.Api.Web4` | ASP.NET Core Web API / .NET 8 | .NET 8 API parity demo |
| `Timesoft.Solution.RealtimeHub` | ASP.NET Core SignalR / .NET 8 | Shared standalone realtime server |

## Ports

Framework demo:

| App | URL |
| --- | --- |
| Framework MVC UI | `http://localhost:5001` |
| Framework API | `http://localhost:5002` |
| RealtimeHub | `https://localhost:5003` |

.NET 8 parity demo:

| App | URL |
| --- | --- |
| .NET 8 MVC UI | `https://localhost:5101` |
| .NET 8 API | `https://localhost:5102` |
| RealtimeHub | `https://localhost:5003` |

## How Timeout Is Solved

A synchronous HTTP approach would make the browser wait while the API performs the full Leave Calculation. If the process takes too long, browser, proxy, load balancer, or hosting timeouts can break the request.

This sample avoids that:

1. UI sends `POST /api/leave-calculations/start`.
2. API creates a `calculationId`.
3. API saves the calculation state to XML.
4. API starts background execution.
5. API immediately returns `202 Accepted` with `calculationId` and a demo hub token.
6. Browser connects to `https://localhost:5003/hubs/jobstatus`.
7. Browser joins its company/user/calculation group.
8. API sends authenticated status notifications to RealtimeHub.
9. RealtimeHub pushes updates only to the matching group.
10. UI updates the progress log until `Completed` or `Failed`.

## SignalR Group Isolation

The final Leave Calculation flow uses company/user/calculation-aware groups:

```text
company:{companyCode}:user:{loginUserId}:calculation:{calculationId}
```

Example:

```text
company:COMPANY_A:user:HR_A:calculation:4db821d78ad94fb4a86f401de7ecf9af
```

This prevents Company A, Company B, and Company C browsers from receiving each other's updates when they run at the same time.

## Security In This Sample

This is demo security, not production identity.

- API-to-Hub notification uses Basic Authentication.
- Browser-to-Hub SignalR connection uses the ASP.NET Core SignalR `accessTokenFactory` token pattern.
- The browser receives a short-lived demo hub token from the API after starting a calculation.
- The hub validates the token before allowing the browser to join a company/user/calculation group.
- Secrets are stored in sample configuration only for local demo.

Important browser note:

Browser WebSocket connections cannot reliably send custom Basic Auth headers, so the browser hub connection uses the SignalR token pattern instead of HTTP Basic Auth.

## Storage

The Leave Calculation flow saves status to XML:

```text
Timesoft.Solution.Api.Web3/App_Data/LeaveCalculationJobs.xml
Timesoft.Solution.Api.Web4/App_Data/LeaveCalculationJobs.xml
```

The generic old job sample may still have in-memory code, but the Leave Calculation demo uses XML state.

## Run Framework Demo

1. Open `Timesoft.Solution.Demo.sln` in Visual Studio.
2. Set multiple startup projects.
3. Start these projects together:
   - `Timesoft.Solution.Web3`
   - `Timesoft.Solution.Api.Web3`
   - `Timesoft.Solution.RealtimeHub`
4. Open `http://localhost:5001`.
5. Select a company and login.
6. Select Department, Employee, Leave Type, and Year.
7. Click `Process`.
8. Observe realtime updates until `Completed`.

## Run .NET 8 Parity Demo

1. Open `Timesoft.Solution.Demo.sln` in Visual Studio.
2. Set multiple startup projects.
3. Start these projects together:
   - `Timesoft.Solution.Web4`
   - `Timesoft.Solution.Api.Web4`
   - `Timesoft.Solution.RealtimeHub`
4. Open `https://localhost:5101`.
5. Select a company and login.
6. Click `Process`.
7. Observe realtime updates until `Completed`.

If your local HTTPS development certificate is not trusted yet, run:

```powershell
dotnet dev-certs https --trust
```

## Multi-Company Test

Use three browsers:

```text
Chrome  -> Company A / HR_A
Firefox -> Company B / HR_B
Edge    -> Company C / HR_C
```

Start Leave Calculation in all three browsers at nearly the same time.

Expected result:

```text
Chrome receives only Company A updates.
Firefox receives only Company B updates.
Edge receives only Company C updates.
```

Detailed test plan:

```text
docs/use-cases/TestPlan_MultiCompanyLeaveCalculation.md
```

## Configurable SignalR

The MVC pages read a page-level SignalR enabled flag.

If SignalR is enabled:

```text
Browser connects to RealtimeHub and receives push updates.
```

If SignalR is disabled:

```text
Page still loads and can use API snapshot polling instead.
```

This keeps existing pages unaffected unless they explicitly opt into SignalR.

## Build

Build the full solution with Visual Studio MSBuild:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" Timesoft.Solution.Demo.sln /restore /p:Configuration=Debug /p:Platform="Any CPU"
```

## Important Rules

- Do not use classic ASP.NET SignalR 2.x.
- Do not use `jquery.signalR`.
- Do not require old jQuery.
- Use `@microsoft/signalr`.
- Keep RealtimeHub as a standalone ASP.NET Core SignalR server.
- Keep the sample database-free.
- Use XML for Leave Calculation status/history in this demo.
