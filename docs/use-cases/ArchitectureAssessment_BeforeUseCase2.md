# Architecture Assessment Before Use Case 2

## Existing Application Context

The existing application can be used by many clients/companies at the same time.

- Minimum clients/companies: 100+
- Users per client/company: 200+
- Same application URL
- Same API URL
- Same SignalR Hub URL
- Company/database context is separated by Company Code
- Users will not all access at the exact same time

## Current Target Architecture

```text
.NET Framework MVC UI
        |
        | AJAX POST
        v
.NET Framework Web API
        |
        | background task
        |
        | HTTP POST notification
        v
ASP.NET Core SignalR Hub
        |
        | JobStatusUpdated
        v
Browser
```

Current demo group format:

```text
job:{jobId}
```

Recommended production group format:

```text
company:{companyCode}:job:{jobId}
```

Recommended private-user group format:

```text
company:{companyCode}:user:{loginUserId}:job:{jobId}
```

## Reliability Assessment

The current sample is reliable enough for demonstration, but not enough for production.

Current sample uses:

- in-memory job store
- in-process background task
- direct HTTP notification to SignalR hub
- no retry queue
- no persistent job history

Risks:

- If API restarts, job state is lost.
- If API restarts, background task stops.
- If RealtimeHub is unavailable, notification can be missed.
- If browser refreshes, SignalR connection is lost and UI must recover from API snapshot.

Recommended production improvements:

- Store job state in database.
- Store job progress/history in database.
- Use durable background job runner or queue.
- Add retry for API-to-hub notification delivery.
- Keep `GET /api/jobs/{jobId}` as a snapshot recovery endpoint.
- Add SignalR reconnect handling in browser.
- Add CompanyCode and UserId to every job record.

## Performance Assessment

Scale estimate:

```text
100 companies x 200 users = 20,000 registered users
```

Performance depends more on active concurrent users/jobs than total registered users.

Current sample is good for demo and low traffic because:

- API returns immediately.
- Browser does not wait for the heavy task.
- SignalR sends to group, not all clients.
- Each update is small JSON.

Potential production risks:

- Too many simultaneous `Task.Run` jobs can overload API server CPU/memory.
- Too many SignalR connections can overload one hub server.
- No throttling.
- No queue.
- No scale-out/backplane.

Recommended production improvements:

- Limit concurrent background jobs.
- Use a background queue instead of unlimited `Task.Run`.
- Persist progress in database.
- Send only meaningful status updates.
- Use company/job/user SignalR groups.
- If multiple hub servers are used, add Redis backplane or Azure SignalR Service.
- Add rate limiting for start job endpoint.
- Add cancellation/timeout for background jobs.

## Security Assessment

The current sample is intentionally simple and is not production-secure.

Current sample has:

- no authentication
- no authorization
- no trusted company validation
- no user isolation
- no API-to-hub secret
- no protection on the notification endpoint

Production risks:

- A user may join another company/job SignalR group.
- A user may guess a JobId.
- A user may call API directly.
- Browser may send fake CompanyCode.
- Anyone who can reach the hub notification endpoint could send fake job updates.

Recommended production improvements:

- Authenticate users.
- Store CompanyCode in trusted server-side session or token.
- Validate that user belongs to CompanyCode.
- Validate that user can access JobId.
- Do not trust CompanyCode only from JavaScript.
- Protect RealtimeHub notification endpoint.
- Protect SignalR `JoinJobGroup`.
- Use HTTPS for all projects.
- Add audit logs.

## Group Naming Decision

Use this if multiple users in the same company can watch the same job:

```text
company:{companyCode}:job:{jobId}
```

Use this if only the user who started the job should see the job progress:

```text
company:{companyCode}:user:{loginUserId}:job:{jobId}
```

Avoid using only:

```text
company:{companyCode}
```

for job progress unless every user in the company should see every job update.

## Recommended Default For Existing Application

For the existing application, the safest default is:

```text
company:{companyCode}:user:{loginUserId}:job:{jobId}
```

This separates updates by:

- company
- login user
- job

## Production Flow Recommendation

```text
MVC login
    |
    v
CompanyCode stored in trusted session/user context
    |
    v
User clicks Start Job
    |
    v
Framework API validates user and company
    |
    v
Framework API creates job with CompanyCode + UserId + JobId
    |
    v
Framework API stores job in database
    |
    v
Framework API starts durable background work
    |
    v
Background worker updates database progress
    |
    v
Background worker sends notification to RealtimeHub
    |
    v
RealtimeHub sends to company/user/job group
    |
    v
Browser receives only its own job updates
```

## Summary

Current sample:

```text
Reliability: Demo only
Performance: Good for low traffic
Security: Not production-ready
```

Production direction:

```text
Reliability: DB job state + queue + retry + snapshot recovery
Performance: concurrency control + grouped SignalR + scale-out plan
Security: authenticated company/user context + protected hub notification endpoint
```

Before Use Case 2, enhance the design notes and future implementation tasks around:

- company-aware SignalR groups
- authenticated company/user context
- persistent job store
- background queue
- notification retry
- protected hub notification endpoint
- SignalR scale-out plan
