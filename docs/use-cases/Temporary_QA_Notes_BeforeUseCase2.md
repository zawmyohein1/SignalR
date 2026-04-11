# Temporary Q&A Notes Before Use Case 2

These notes capture current questions, concerns, and answers for later conversion into a formal document.

## Q1. Can SignalR send JobId A to the wrong browser?

### Concern

If 10 clients click `Start Job` at the same time, can SignalR send the wrong job update?

Example wrong behavior:

```text
SignalR Hub sends JobId A to Client B
SignalR Hub sends JobId B to Client C
```

### Answer

With correct group usage, SignalR should not send Job A updates to Client B.

Current sample group:

```text
job:{jobId}
```

Example:

```text
Client A joins job:JobA
Client B joins job:JobB
Client C joins job:JobC
```

When API sends update for `JobA`, RealtimeHub sends only to:

```text
job:JobA
```

So only Client A should receive that update.

### Extra Safety

Add browser-side check:

```javascript
if (notification.jobId !== currentJobId) {
    return;
}
```

This protects the UI even if an unexpected message arrives.

## Q2. Existing application uses same app URL and same API URL for many clients. Is that a problem?

### Concern

The existing application is used by many companies/clients.

Same URLs:

```text
Application URL: same for all clients
API URL: same for all clients
SignalR Hub URL: same for all clients
```

Difference:

```text
Company Code
Database context
Login user
```

### Answer

Same app URL and same API URL is not a problem.

SignalR does not separate clients by URL.

SignalR separates clients by:

```text
ConnectionId
User
Group
```

For this application, group design is important.

Recommended group:

```text
company:{companyCode}:job:{jobId}
```

If job progress must be private per user:

```text
company:{companyCode}:user:{loginUserId}:job:{jobId}
```

## Q3. Why include Company Code in SignalR group?

### Concern

Different clients/companies use the same application but must not receive each other's updates.

### Answer

Company Code should be included in the SignalR group name.

Current demo:

```text
job:{jobId}
```

Enhanced real application:

```text
company:{companyCode}:job:{jobId}
```

More secure private-user version:

```text
company:{companyCode}:user:{loginUserId}:job:{jobId}
```

Example:

```text
Client A:
  CompanyCode = COMPANY_A
  JobId = JOB_A
  Group = company:COMPANY_A:job:JOB_A

Client B:
  CompanyCode = COMPANY_B
  JobId = JOB_B
  Group = company:COMPANY_B:job:JOB_B
```

If Framework API sends update for:

```text
CompanyCode = COMPANY_B
JobId = JOB_B
```

RealtimeHub sends only to:

```text
company:COMPANY_B:job:JOB_B
```

## Q4. What is reliability in SignalR connection?

### Short Answer

Reliability means:

```text
Will the correct client receive the correct message?
```

Reliability concerns:

- message goes to correct company/user/job group
- reconnect works after temporary network loss
- missed messages can be recovered by API snapshot
- hub does not send Company A data to Company B

## Q5. What is stability in SignalR connection?

### Short Answer

Stability means:

```text
Can the connection stay healthy under real usage?
```

Stability concerns:

- connection does not disconnect often
- reconnect works automatically
- hub can handle many users
- server does not crash under load
- network interruptions do not break the whole flow

## Q6. Can current SignalR handle the existing application scale?

### Context

Existing application scale:

```text
100+ clients/companies
200+ users per client/company
20,000+ total users
```

Users will not all access at the same time.

### Answer

SignalR cares about active connected browsers, not total registered users.

Important factors:

- active connected browsers at the same time
- active jobs at the same time
- messages per second
- server CPU
- server memory
- network bandwidth

Current SignalR technology can handle the use case if concurrent active usage is reasonable.

Current sample is demo-level and should be enhanced for production.

## Q7. How many active browsers can SignalR handle?

### Short Practical Guide

There is no fixed universal number.

Rough guide:

```text
100 - 500 active browsers:
  Usually easy for one normal server.

500 - 2,000 active browsers:
  Possible, but load test and monitor CPU/memory.

2,000 - 10,000 active browsers:
  Needs serious load testing, tuning, and probably scale-out.

10,000+ active browsers:
  Use scale-out, Redis backplane, or Azure SignalR Service.
```

### Key Question

For planning, estimate:

```text
At peak time, how many users keep the job progress page open at the same time?
```

If peak active users are under 500, the current style of SignalR hub may be fine for a first production version with monitoring.

If peak active users are 2,000+, plan scale-out.

## Production Recommendations Captured So Far

Recommended group format:

```text
company:{companyCode}:user:{loginUserId}:job:{jobId}
```

Minimum production enhancements:

- authenticate users
- validate CompanyCode from trusted login/session context
- validate user can access JobId
- protect SignalR `JoinJobGroup`
- protect RealtimeHub notification endpoint
- persist job state in database
- persist job history in database
- use background queue or controlled worker
- add notification retry
- keep `GET /api/jobs/{jobId}` as recovery snapshot
- add browser reconnect handling
- load test for expected active users
- consider Redis backplane or Azure SignalR Service for scale-out

## Future Document Task

Later, convert these notes into a formal document with sections:

1. Problem Statement
2. Existing Application Context
3. SignalR Group Strategy
4. Reliability
5. Stability
6. Performance Capacity
7. Security Requirements
8. Recommended Production Architecture
9. Implementation Tasks
10. Testing Plan
