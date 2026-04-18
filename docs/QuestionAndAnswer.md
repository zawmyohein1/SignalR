# Question & Answer

## Project Architecture Level

### 1. Why do we use SignalR in this demo?

SignalR is used to send realtime progress updates from the server to the Leave Calculation Page.

Without SignalR, the page must wait for the HTTP request to finish. For a long-running process, this can cause timeout, poor user experience, or confusion because the user cannot see progress.

---

### 2. What problem does this architecture solve?

It solves the long HTTP request problem.

The Leave Calculation Page sends a short request to Web3.Api. Web3.Api returns the Calculation Id quickly and continues the calculation in the background. RealtimeHub then pushes progress updates back to the correct page.

---

### 3. What is the role of Web3?

Web3 is the user-facing application.

It shows the Leave Calculation Page, sends the Process request, connects to RealtimeHub, joins the correct SignalR group, and displays progress updates to the user.

---

### 4. What is the role of Web3.Api?

Web3.Api owns the business process.

It creates the Calculation Id, stores the calculation status, starts the background calculation, and sends progress notifications to RealtimeHub.

---

### 5. What is the role of RealtimeHub?

RealtimeHub is the standalone realtime server.

It does not run the leave calculation. It only receives status notifications from Web3.Api and pushes those updates to the correct SignalR group.

---

### 6. Why is RealtimeHub separate from Web3.Api?

RealtimeHub is separated so realtime communication is isolated from business processing.

This makes the design cleaner:

- Web3.Api handles calculation logic.
- RealtimeHub handles realtime delivery.
- Web3 handles user interaction and display.

This separation also makes it easier to reuse the same RealtimeHub for other pages or future processes.

---

### 7. How does the system prevent wrong browser updates?

The system uses a SignalR group key.

Current group rule:

```text
company:{companyCode}:user:{loginUserId}:calculation:{calculationId}
```

This means each page joins only its own calculation group. RealtimeHub sends updates only to that matching group, so another company, user, or calculation page should not receive the wrong update.

---

### 8. If the browser does not join the SignalR group, will the calculation still run?

Yes.

The calculation is started by Web3.Api. SignalR group joining is only for realtime progress delivery.

If the browser does not join the group:

- Web3.Api still runs the calculation.
- RealtimeHub may still publish updates.
- The page will not receive those updates.

---

### 9. What happens when SignalR is disabled?

When SignalR is disabled, the system runs in normal HTTP mode.

Web3 sends the request and waits until Web3.Api finishes the calculation. No realtime callback is used. The page receives the final response only after the full process completes.

This mode is useful for demo comparison:

- SignalR enabled: fast response + realtime progress.
- SignalR disabled: normal HTTP request waits until completion.

---

### 10. What are the main architecture limitations in this demo?

This is a demo architecture, not a production-ready design.

Main limitations:

- XML storage is used instead of a database.
- Background task execution is simple and in-process.
- Authentication is basic demo-level only.
- No retry queue is used for failed notifications.
- If the application restarts, in-memory or running process state may be affected depending on the project.

For production, a database, queue, stronger authentication, retry handling, logging, and monitoring should be added.

---

### 11. How many active browsers can connect to SignalR?

SignalR can support many active browser connections, but the exact number depends on infrastructure.

Important factors:

- Server CPU and memory
- Network capacity
- Message frequency
- Number of connected browsers
- Whether one server or multiple servers are used
- Whether SignalR backplane or managed SignalR service is used

For this demo, a normal development machine is enough. For production, load testing is required before deciding the supported browser count.

---

### 12. Is SignalR stable and reliable?

SignalR is stable for realtime browser updates when it is configured and hosted correctly.

It supports automatic reconnect on the client side, and it can fall back to other transports when WebSocket is not available.

However, SignalR should be treated as a realtime notification channel, not the main business storage. The calculation state should still be saved by Web3.Api so the page can recover status if the browser refreshes or reconnects.

---

### 13. What security is required for this architecture?

Production implementation should secure both sides:

- Leave Calculation Page to RealtimeHub
- Web3.Api to RealtimeHub

Recommended controls:

- Authenticate the browser user before allowing SignalR connection.
- Validate company code and login user before joining a group.
- Do not trust group names sent from the browser without validation.
- Protect Web3.Api to RealtimeHub notification endpoint with token or service authentication.
- Use HTTPS only.
- Avoid sending sensitive data in SignalR messages.

In this demo, security is intentionally simple to keep the concept easy to understand.

---

### 14. What infrastructure is required?

Minimum infrastructure:

- Web3 application server
- Web3.Api application server
- RealtimeHub application server
- HTTPS endpoint for browser-to-hub connection
- Network access from Web3.Api to RealtimeHub

For production scale, additional infrastructure may be required:

- Load balancer with WebSocket support
- Sticky sessions or SignalR backplane
- Redis backplane or Azure SignalR Service if multiple hub servers are used
- Central logging and monitoring
- Database for durable job state

---

### 15. If implemented, how can this impact the existing system?

The impact can be controlled if SignalR is added page by page.

Low-risk approach:

- Keep existing HTTP process unchanged first.
- Add SignalR only to selected pages by configuration.
- Keep the same business calculation logic.
- Add realtime notification around the existing process.
- Allow SignalR enabled and disabled modes for comparison.

Expected impact:

- Better user experience for long-running processes.
- Less browser timeout risk.
- More visibility while processing.
- Some additional infrastructure and monitoring responsibility.

The existing system should not be heavily affected if the integration is isolated to selected pages and controlled by configuration.

---

### 16. What happens if the user leaves the Leave Calculation Page while the process is still running?

The calculation continues running in Api.

The browser page does not own the process. After the start request is accepted, Api owns the calculation and saves status/history in XML. The page can be closed, refreshed, or moved to `View Leave` without stopping the background calculation.

When the user returns to the Leave Calculation Page, the page reads the saved active calculation id from browser storage and asks Api for the latest snapshot.

---

### 17. How does the page know whether the process is still running or already completed?

The page checks the latest Api snapshot by calculation id.

Restore flow:

1. Browser storage gives the page the last active calculation id.
2. Web calls the MVC `Details` endpoint.
3. MVC calls Api `GET /api/leave-calculations/{calculationId}`.
4. Api loads the current status and history from XML.
5. The page decides what to show from that returned status.

If the snapshot says the process is still running, the page disables the Process button and resumes SignalR/polling. If the snapshot says `Completed` or `Failed`, the page shows the final state and enables the Process button again.

---

### 18. Is browser storage the source of truth for calculation status?

No.

Browser storage is only a resume pointer. It remembers which calculation id the browser should reload after navigation.

The source of truth is Api storage:

```text
Api XML storage -> calculation status + progress history
Browser storage -> last active calculation id and resume context only
```

This is important because a running process can continue while the page is not open. The latest status must come from Api, not from the old browser state.

---

### 19. What is the difference between session restore and local restore?

The restore mode is controlled by configuration.

Web3:

```text
LeaveCalculationDemo-RestoreStorage
```

Web4:

```text
LeaveCalculationDemo:RestoreStorage
```

Supported values:

| Value | Meaning |
| --- | --- |
| `session` | Use browser `sessionStorage`. Restore works in the same browser tab/session. |
| `local` | Use browser `localStorage`. Restore can work after closing and reopening the browser. |
| `off` | Do not restore the active calculation from browser storage. |

---

### 20. What happens if SignalR cannot reconnect after returning to the page?

The page falls back to snapshot polling.

SignalR is the realtime push channel, but it is not the only way to know status. The page can call the snapshot endpoint repeatedly and update the UI from Api state.

Expected behavior:

- Api process continues running.
- Page shows polling/fallback state.
- Progress is refreshed from the snapshot endpoint.
- Final status still becomes `Completed` when Api finishes.

---

### 21. Why was a `View Leave` page added?

`View Leave` is a simple navigation test page.

It exists so the demo can prove this behavior:

1. Start Leave Calculation.
2. Leave the calculation page.
3. Return to the calculation page.
4. See the current running status or completed result.

`View Leave` does not run calculation logic and does not own calculation state. The important feature is the restore flow on the Leave Calculation Page.

---

### 22. How does Azure SignalR fit into this demo?

Azure SignalR Service replaces the local realtime transport layer only.

The Leave Calculation Page still connects to the same hub route, and Web3.Api still sends the same calculation notifications. The difference is that `Timesoft.Solution.RealtimeHub` uses Azure SignalR behind the scenes when `SignalR:Provider` is set to `Azure`.

Recommended configuration:

- `SignalR:Provider = Azure`
- `Azure:SignalR:ConnectionString = ...`

This keeps the business flow unchanged while giving the demo a managed realtime service for browser delivery.
