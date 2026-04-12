# Test Plan - SignalR Realtime Demo

## Purpose

Verify that Leave Calculation realtime updates are delivered to the correct Leave Calculation Page only.

Main target:

- No wrong company update.
- No wrong user update.
- No wrong calculation update.
- No overlap between multiple active pages.

## Test Environment

Run these projects together:

| Project | URL |
| --- | --- |
| `Timesoft.Solution.Web3` | `http://localhost:5001` |
| `Timesoft.Solution.Api.Web3` | `http://localhost:56541` |
| `Timesoft.Solution.RealtimeHub` | `https://localhost:5003` |

Optional parity test:

| Project | URL |
| --- | --- |
| `Timesoft.Solution.Web4` | `https://localhost:5001` |
| `Timesoft.Solution.Api.Web4` | `https://localhost:5002` |

## Test Data

Use different companies in different browsers:

| Browser | Company | Login User |
| --- | --- | --- |
| Chrome | `COMPANY_A` | `HR_A` |
| Firefox | `COMPANY_B` | `HR_B` |
| Edge | `COMPANY_C` | `HR_C` |

Use common process input:

| Field | Value |
| --- | --- |
| Department | `HR` |
| Employee | `ALL` |
| Year | `2026` |

## Test Case 1 - Single Page Realtime Flow

Steps:

1. Open Web3 Leave Calculation Page.
2. Login as `COMPANY_A / HR_A`.
3. Click `Process`.
4. Confirm Web3.Api returns a calculation id.
5. Confirm SignalR status is connected.
6. Confirm progress log updates until `Completed`.

Expected result:

- Leave Calculation Page receives realtime updates.
- Current status changes during the process.
- Progress log shows employee completion lines.
- Process button stops spinning after completion.

## Test Case 2 - Multi Company Isolation

Steps:

1. Open Chrome with `COMPANY_A / HR_A`.
2. Open Firefox with `COMPANY_B / HR_B`.
3. Open Edge with `COMPANY_C / HR_C`.
4. Click `Process` in all browsers close together.
5. Watch all progress logs.

Expected result:

- Each page has a different calculation id.
- Company A page receives only Company A updates.
- Company B page receives only Company B updates.
- Company C page receives only Company C updates.
- No progress line appears in the wrong page.

## Test Case 3 - Same Company, Different Calculation

Steps:

1. Open two browser windows.
2. Login both as `COMPANY_A / HR_A`.
3. Click `Process` in both pages.
4. Compare calculation ids.
5. Watch progress logs.

Expected result:

- Each page has a different calculation id.
- Each page receives only its own calculation updates.
- Updates do not cross between pages.

## Test Case 4 - SignalR Disabled Mode

Steps:

1. Set SignalR disabled in Web3 and Web3.Api configuration.
2. Restart Web3 and Web3.Api.
3. Open Leave Calculation Page.
4. Click `Process`.

Expected result:

- SignalR status shows disabled/off.
- Web3.Api runs the process inside the HTTP request.
- Leave Calculation Page waits for final response.
- Final status becomes `Completed`.
- No realtime push is used.

## Test Case 5 - RealtimeHub Not Running

Steps:

1. Start Web3 and Web3.Api.
2. Do not start `Timesoft.Solution.RealtimeHub`.
3. Open Leave Calculation Page.
4. Click `Process`.

Expected result:

- Web3.Api still accepts the process.
- Realtime notification cannot be delivered.
- Error should not crash Web3.Api.
- Status can still be checked from XML/history endpoint.

## Test Case 6 - Page Refresh During Process

Steps:

1. Start one Leave Calculation process.
2. Refresh the Leave Calculation Page before completion.
3. Start a new process.

Expected result:

- Refreshed page does not receive old calculation updates.
- New process receives only new calculation updates.
- Calculation id changes for the new cycle.

## Verification Checklist

For every SignalR test, confirm:

- `SignalR` shows connected.
- `Execution Mode` shows background + SignalR.
- Calculation id is visible.
- Group rule uses company, user, and calculation id.
- Only the matching Leave Calculation Page receives updates.
- Final status becomes `Completed`.

## Screenshot Evidence

| Screenshot | Purpose |
| --- | --- |
| `use-cases/images/test-plan/single-page-running.png` | Shows SignalR connected and one Leave Calculation Page receiving realtime progress. |
| `use-cases/images/test-plan/completed-state.png` | Shows one full SignalR cycle completed successfully. |
| `use-cases/images/test-plan/signalr-disabled-mode.png` | Shows normal HTTP completion when SignalR is disabled. |
| `use-cases/images/test-plan/multi-company-login-context.png` | Shows Company A and Company B using separate login contexts. |
| `use-cases/images/test-plan/multi-company-running.png` | Shows Company A and Company B running at the same time with separate calculation ids. |
| `use-cases/images/test-plan/multi-company-completed.png` | Shows both companies completed without crossing progress updates. |

## Pass Criteria

The SignalR implementation passes when:

- Multiple companies can process at the same time.
- Multiple pages can process at the same time.
- Each page receives only its own updates.
- No wrong calculation id appears in another page.
- No wrong company/user progress appears in another page.

## Known Limitations

- XML storage is demo-only.
- Background work is in-process.
- RealtimeHub scale-out is not configured.
- Demo authentication is not production security.
- Browser reconnect behavior is basic for demo purposes.
