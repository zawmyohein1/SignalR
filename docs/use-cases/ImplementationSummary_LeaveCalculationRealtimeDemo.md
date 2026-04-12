# Implementation Summary - Leave Calculation Realtime Demo

Date: 2026-04-11

## Completed

- Enhanced `Timesoft.Solution.RealtimeHub`.
- Enhanced `Timesoft.Solution.Api.Web3`.
- Enhanced `Timesoft.Solution.Web3`.
- Enhanced `Timesoft.Solution.Api.Web4`.
- Enhanced `Timesoft.Solution.Web4`.
- Updated `README.md`.
- Added XML storage files for Leave Calculation status/history.
- Added company/user/calculation SignalR group isolation.
- Added API-to-Hub Basic Authentication for Leave Calculation notifications.
- Added browser-to-Hub demo token validation using SignalR `accessTokenFactory`.
- Added configurable SignalR client behavior in both MVC UIs.
- Added demo login context and Leave Calculation UI.

## Main Flow

```text
User selects company and login id
        |
        v
User clicks Process on Leave Calculation page
        |
        v
MVC UI POSTs /api/leave-calculations/start
        |
        v
API creates calculationId and saves XML state
        |
        v
API starts background Leave Calculation process
        |
        v
API returns 202 Accepted + calculationId + hubAccessToken
        |
        v
Browser connects to standalone ASP.NET Core SignalR Hub
        |
        v
Browser calls JoinCalculationGroup(companyCode, loginUserId, calculationId)
        |
        v
API sends authenticated notifications to RealtimeHub
        |
        v
RealtimeHub pushes LeaveCalculationStatusUpdated to the matching group only
        |
        v
Browser updates progress log until Completed or Failed
```

## SignalR Group

```text
company:{companyCode}:user:{loginUserId}:calculation:{calculationId}
```

Example:

```text
company:COMPANY_A:user:HR_A:calculation:62f9090fc9a94cea9bcf42b4eb748a5e
```

## Verified

Build:

```text
MSBuild Timesoft.Solution.Demo.sln succeeded.
```

Framework runtime smoke test:

```text
Framework MVC UI returned HTTP 200.
Framework API returned 202 Accepted for Leave Calculation start.
Framework API XML state reached Completed.
RealtimeHub log showed notifications sent to company/user/calculation group.
Unauthenticated Leave Calculation notification endpoint returned HTTP 401.
```

.NET 8 parity smoke test:

```text
.NET 8 MVC UI returned HTTP 200.
.NET 8 API returned calculationId.
.NET 8 API snapshot returned company/status.
```

## Not Manually Completed In This Session

The full Chrome + Firefox + Edge multi-company browser test still needs to be run manually:

```text
Chrome  -> Company A / HR_A
Firefox -> Company B / HR_B
Edge    -> Company C / HR_C
```

Expected:

```text
Each browser receives only its own company/user/calculation updates.
```

