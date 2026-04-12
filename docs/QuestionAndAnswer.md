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
