# Final Enhancement Plan - Full Task List

## Purpose

Enhance the existing `Timesoft.Solution.Demo` solution into a clearer Leave Calculation realtime demo.

The final demo should prove this scenario:

```text
Chrome  logs in as Company A
Firefox logs in as Company B
Edge    logs in as Company C

All use the same application concept and same realtime hub.
Each company starts Leave Calculation at the same time.
Each browser receives only its own company/user/calculation updates.
```

## Projects In Scope

All existing sample projects should be enhanced.

| Project | Type | Role |
| --- | --- | --- |
| `Timesoft.Solution.Web3` | ASP.NET MVC 5 / .NET Framework 4.8 | Primary legacy-style UI demo |
| `Timesoft.Solution.Api.Web3` | ASP.NET Web API 2 / .NET Framework 4.8 | Primary legacy-style API demo |
| `Timesoft.Solution.Web4` | ASP.NET Core MVC / .NET 8 | .NET 8 UI parity demo |
| `Timesoft.Solution.Api.Web4` | ASP.NET Core Web API / .NET 8 | .NET 8 API parity demo |
| `Timesoft.Solution.RealtimeHub` | ASP.NET Core SignalR / .NET 8 | Shared standalone realtime server |

Important:

- Keep the standalone SignalR Hub as ASP.NET Core SignalR.
- Do not use classic ASP.NET SignalR 2.x.
- Do not use `jquery.signalR`.
- Continue to use `@microsoft/signalr` JavaScript client.

## Recommended Port Plan

Primary .NET Framework demo:

| App | URL |
| --- | --- |
| Framework MVC UI | `http://localhost:5001` |
| Framework API | `http://localhost:5002` |
| Realtime Hub | `https://localhost:5003` |

.NET 8 parity demo:

| App | URL |
| --- | --- |
| .NET 8 MVC UI | `https://localhost:5101` |
| .NET 8 API | `https://localhost:5102` |
| Realtime Hub | `https://localhost:5003` |

Reason:

Both MVC projects and both API projects cannot use the same ports at the same time.
Separate ports allow both technology versions to be tested without conflict.

## Target Demo Concept

Replace the generic "heavy job" wording with:

```text
Leave Calculation / Leave Entitlement Process
```

The real calculation does not need to be implemented.
The API should simulate the process with realistic progress messages.

Example statuses:

```text
Accepted
Started
Loading selected employees
Calculating leave entitlement
Updating leave balances
Completed
Failed
```

## Sample Data

Use the saved sample data from:

```text
docs/use-cases/SampleData_LeaveCalculation.md
```

Data includes:

- companies
- employees
- departments
- leave types
- demo calculation inputs

Implementation note:

- Employee `804` and `8040` are different employees.
- Leave type `ANNU` appears twice in the source data. For dropdown display, de-duplicate it unless the demo needs to show raw source data.

## Architecture After Enhancement

```text
[MVC UI / .NET Framework or .NET 8]
        |
        | POST /api/leave-calculations/start
        v
[API / .NET Framework or .NET 8]
        |
        | save calculation state to XML
        | start background process
        | return calculationId immediately
        v
[Background Leave Calculation Runner]
        |
        | POST authenticated status notification
        v
[Standalone ASP.NET Core SignalR Hub]
        |
        | JobStatusUpdated / LeaveCalculationStatusUpdated
        v
[Only the matching browser group receives update]
```

## SignalR Group Rule

Use company-aware and calculation-aware groups.

Recommended group:

```text
company:{companyCode}:user:{loginUserId}:calculation:{calculationId}
```

Alternative simpler group:

```text
company:{companyCode}:job:{calculationId}
```

Recommended final choice:

```text
company:{companyCode}:user:{loginUserId}:calculation:{calculationId}
```

Reason:

- prevents Company A updates from going to Company B
- prevents one HR user's calculation from going to another HR user's browser
- supports many clients using the same application URL and API URL

## Important Browser Authentication Note

API-to-Hub notification can use Basic Authentication because it is server-to-server HTTP.

Browser-to-Hub SignalR authentication is different:

- browser WebSocket APIs do not reliably allow custom `Authorization` headers
- the ASP.NET Core SignalR JavaScript client supports `accessTokenFactory`
- the token may be sent as a query string for WebSockets/SSE in browser scenarios

For this sample, use a simple demo token from the login context for Browser-to-Hub.
Call it "basic demo hub token" in the UI/documentation, but implement it using the ASP.NET Core SignalR client token pattern.

## Full Task List

### 1. Configuration

- [ ] Add configurable API base URL and Hub URL for both MVC UIs.
- [ ] Add configurable execution timing for both APIs.
- [ ] Add configurable SignalR enabled/disabled flag per page.
- [ ] Add configurable API-to-Hub notification credentials.
- [ ] Add configurable browser-to-hub demo token settings.
- [ ] Avoid hardcoding URLs directly inside JavaScript where possible.

### 2. Login Page

- [ ] Add a simple login page to `Timesoft.Solution.Web3`.
- [ ] Add a matching simple login page to `Timesoft.Solution.Web4`.
- [ ] Include company selection.
- [ ] Include login id.
- [ ] Include password field for demo only.
- [ ] Include login period.
- [ ] Store demo login context in session/cookie/local page context.
- [ ] Use company codes from the sample data.
- [ ] Keep the UI visually close to the supplied TIMES SOFTWARE login screenshot.

### 3. Leave Calculation Page

- [ ] Replace generic "Start Job" screen with "Leave Calculation" screen.
- [ ] Add Department dropdown.
- [ ] Add Employee dropdown.
- [ ] Add Leave Type dropdown.
- [ ] Add Year selection.
- [ ] Add small attractive Process button.
- [ ] Add compact current status display.
- [ ] Add progress log.
- [ ] Show selected company/login context clearly.
- [ ] Keep the page small and demo-friendly.

### 4. Configurable SignalR Client

- [ ] Create reusable JavaScript SignalR client configuration.
- [ ] Allow each page to decide whether SignalR is enabled.
- [ ] If SignalR is disabled, page should still load normally.
- [ ] If SignalR is enabled, connect only when the page starts a calculation or explicitly needs realtime.
- [ ] Do not impact existing pages that do not use SignalR.
- [ ] Continue to use `@microsoft/signalr`.
- [ ] Do not use classic SignalR 2.x client.
- [ ] Do not use `jquery.signalR`.

### 5. Framework API Leave Calculation

- [ ] Add/convert endpoint: `POST /api/leave-calculations/start`.
- [ ] Accept company code, login user id, department, employee, leave type, and year.
- [ ] Create `calculationId`.
- [ ] Save calculation state to XML.
- [ ] Start background calculation runner.
- [ ] Immediately return `202 Accepted` with `calculationId`.
- [ ] Add endpoint: `GET /api/leave-calculations/{calculationId}`.
- [ ] Read current calculation state from XML.
- [ ] Send status notification to RealtimeHub for every status change.

### 6. .NET 8 API Leave Calculation

- [ ] Add matching endpoint: `POST /api/leave-calculations/start`.
- [ ] Add matching endpoint: `GET /api/leave-calculations/{calculationId}`.
- [ ] Use the same DTO field names as the Framework API.
- [ ] Save calculation state to XML.
- [ ] Use async/await and `IHttpClientFactory`.
- [ ] Use options classes for configuration.
- [ ] Send status notification to RealtimeHub for every status change.

### 7. XML Storage

- [ ] Add XML storage for Framework API.
- [ ] Add XML storage for .NET 8 API.
- [ ] Store calculation id.
- [ ] Store company code.
- [ ] Store login user id.
- [ ] Store department.
- [ ] Store employee.
- [ ] Store leave type.
- [ ] Store year.
- [ ] Store status.
- [ ] Store message.
- [ ] Store created timestamp.
- [ ] Store updated timestamp.
- [ ] Store history entries.
- [ ] Use locking to prevent XML file write conflicts in the demo.
- [ ] Keep XML simple and readable.

### 8. Background Execution

- [ ] Make execution duration configurable.
- [ ] Simulate selected employee calculation.
- [ ] Simulate department-wide calculation when employee is `All`.
- [ ] Send progress status at each step.
- [ ] Handle cancellation token where available.
- [ ] Catch exceptions and send `Failed` status.
- [ ] Keep HTTP request fast and non-blocking.

### 9. RealtimeHub Enhancements

- [ ] Update hub group naming to include company/user/calculation.
- [ ] Add server validation before joining a group.
- [ ] Add browser hub token validation.
- [ ] Keep `JoinJobGroup` temporarily or replace with `JoinCalculationGroup`.
- [ ] Add `LeaveCalculationStatusUpdated` event or keep `JobStatusUpdated` with leave calculation DTO.
- [ ] Protect notification endpoint with Basic Authentication.
- [ ] Validate API-to-Hub notification credentials.
- [ ] Send messages only to the matching group.

### 10. Security

- [ ] API-to-Hub: Basic Authentication for `/api/notifications/job-status` or new calculation notification endpoint.
- [ ] Browser-to-Hub: demo access token via `accessTokenFactory`.
- [ ] Do not place production secrets in source code.
- [ ] Keep demo credentials in local config only.
- [ ] Explain that this is sample security, not production identity.
- [ ] Restrict CORS origins to known MVC UI URLs.
- [ ] Keep credentials and token checks simple but explicit.

### 11. .NET Framework MVC Integration

- [ ] Update `Scripts/job-status.js` or create `Scripts/leave-calculation-status.js`.
- [ ] Use AJAX POST to Framework API.
- [ ] Receive `calculationId` immediately.
- [ ] Connect to Hub after API response.
- [ ] Join correct company/user/calculation group.
- [ ] Update status badge and progress log.
- [ ] Disable Process button while running.
- [ ] Re-enable button when Completed or Failed.

### 12. .NET 8 MVC Integration

- [ ] Mirror the Framework MVC demo behavior.
- [ ] Use `wwwroot/js/leave-calculation-status.js`.
- [ ] Use configuration from appsettings / data attributes.
- [ ] Use the same visual layout and wording as the Framework MVC page.
- [ ] Keep it as parity demo, not a separate architecture.

### 13. Documentation

- [ ] Update `README.md`.
- [ ] Explain both demo modes:
  - .NET Framework MVC + Framework API + .NET 8 Hub
  - .NET 8 MVC + .NET 8 API + .NET 8 Hub
- [ ] Explain why immediate API response prevents timeout.
- [ ] Explain XML storage.
- [ ] Explain SignalR group isolation.
- [ ] Explain browser-to-hub token note.
- [ ] Explain API-to-hub Basic Authentication.
- [ ] Include exact run steps.
- [ ] Include Chrome / Firefox / Edge multi-company test steps.

### 14. Test Plan

- [ ] Build full solution.
- [ ] Run Framework demo.
- [ ] Start calculation in Chrome as Company A.
- [ ] Start calculation in Firefox as Company B.
- [ ] Start calculation in Edge as Company C.
- [ ] Confirm each browser receives only its own updates.
- [ ] Run .NET 8 parity demo.
- [ ] Confirm same behavior.
- [ ] Test SignalR disabled page mode.
- [ ] Test RealtimeHub rejects invalid API notification credentials.
- [ ] Test RealtimeHub rejects invalid browser hub token.
- [ ] Test XML file contains final Completed status and history.

## Recommended Implementation Order

1. Add configuration and DTOs.
2. Add XML storage.
3. Add Framework API leave calculation flow.
4. Add RealtimeHub company/user/calculation groups and notification auth.
5. Add Framework MVC login and leave calculation page.
6. Test Framework demo with three browsers.
7. Add .NET 8 API parity.
8. Add .NET 8 MVC parity.
9. Update README and test plan.
10. Final build and manual test.

## Main Risk Areas

| Risk | Mitigation |
| --- | --- |
| Wrong browser receives another company's update | Use company/user/calculation group names and validate join requests |
| SignalR client breaks pages that do not use it | Make SignalR client page-configurable and opt-in |
| Browser Basic Auth header cannot be sent during WebSocket connection | Use `accessTokenFactory` token pattern for browser-to-hub demo auth |
| XML write conflict | Use file lock for demo storage |
| Port conflict between Framework and .NET 8 projects | Use separate ports for .NET 8 parity demo |
| Demo looks too generic | Rename job wording to Leave Calculation / Leave Entitlement Process |

## Definition Of Done

- Both Framework MVC/API and .NET 8 MVC/API support the Leave Calculation realtime demo.
- RealtimeHub sends updates only to the correct company/user/calculation group.
- State is saved in XML, not only memory.
- Execution time is configurable.
- SignalR client is configurable per page.
- API-to-Hub notification endpoint is protected.
- Browser-to-Hub connection uses demo token validation.
- README explains how to run and test.
- Chrome/Firefox/Edge multi-company test passes.
