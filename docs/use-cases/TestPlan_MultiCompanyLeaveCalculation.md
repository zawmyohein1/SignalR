# Test Plan - Multi-Company Leave Calculation Realtime Demo

## Purpose

Verify that the realtime leave calculation demo correctly separates updates by company, user, and calculation.

The same MVC URL, same API URL, and same SignalR Hub URL are used by all clients.

Different company login context must receive only its own updates.

Sample master data is stored in:

```text
docs/use-cases/SampleData_LeaveCalculation.md
```

## Main Acceptance Test

Run the application in three different browsers:

```text
Chrome  -> login as Company A
Firefox -> login as Company B
Edge    -> login as Company C
```

Then start leave calculation processing in all three browsers at nearly the same time.

## Expected Result

```text
Chrome receives only Company A calculation updates.
Firefox receives only Company B calculation updates.
Edge receives only Company C calculation updates.
```

No browser should receive another company's job/calculation updates.

## URLs

Framework MVC UI:

```text
http://localhost:5001
```

Framework API:

```text
http://localhost:5002
```

.NET 8 MVC UI:

```text
https://localhost:5101
```

.NET 8 API:

```text
https://localhost:5102
```

SignalR Hub:

```text
https://localhost:5003/hubs/jobstatus
```

## Recommended SignalR Group Format

Use:

```text
company:{companyCode}:user:{loginUserId}:calculation:{calculationId}
```

Examples:

```text
Chrome:
company:COMPANY_A:user:HR_A:calculation:CALC_A

Firefox:
company:COMPANY_B:user:HR_B:calculation:CALC_B

Edge:
company:COMPANY_C:user:HR_C:calculation:CALC_C
```

## Test Steps

### Step 1 - Start Applications

Start these projects:

```text
Timesoft.Solution.Web3
Timesoft.Solution.Api.Web3
Timesoft.Solution.RealtimeHub
```

Do not start these during this test:

```text
Timesoft.Solution.Web4
Timesoft.Solution.Api.Web4
```

## Step 2 - Login In Chrome

Open Chrome:

```text
http://localhost:5001
```

Login using:

```text
CompanyCode: COMPANY_A
LoginUserId: HR_A
```

Open Leave Calculation page.

## Step 3 - Login In Firefox

Open Firefox:

```text
http://localhost:5001
```

Login using:

```text
CompanyCode: COMPANY_B
LoginUserId: HR_B
```

Open Leave Calculation page.

## Step 4 - Login In Edge

Open Edge:

```text
http://localhost:5001
```

Login using:

```text
CompanyCode: COMPANY_C
LoginUserId: HR_C
```

Open Leave Calculation page.

## Step 5 - Start Process In All Browsers

In each browser, select leave calculation filters:

```text
Department
Employee
Year
Leave Type
```

Click:

```text
Process
```

Try to click Process in Chrome, Firefox, and Edge at nearly the same time.

## Step 6 - Verify UI Updates

Chrome should show:

```text
CompanyCode = COMPANY_A
User = HR_A
Only COMPANY_A calculation messages
```

Firefox should show:

```text
CompanyCode = COMPANY_B
User = HR_B
Only COMPANY_B calculation messages
```

Edge should show:

```text
CompanyCode = COMPANY_C
User = HR_C
Only COMPANY_C calculation messages
```

## Step 7 - Verify No Cross-Company Updates

Confirm:

```text
Chrome does not receive COMPANY_B or COMPANY_C updates.
Firefox does not receive COMPANY_A or COMPANY_C updates.
Edge does not receive COMPANY_A or COMPANY_B updates.
```

## Step 8 - Verify XML Storage

Open XML storage file and confirm each calculation entry includes:

```text
CompanyCode
LoginUserId
CalculationId
Department
Employee
Year
LeaveType
Status
Message
History
CreatedAt
UpdatedAt
```

Expected:

```text
COMPANY_A calculation exists separately.
COMPANY_B calculation exists separately.
COMPANY_C calculation exists separately.
```

## Step 9 - Verify Final Status

Each browser should eventually show:

```text
Completed
```

## Pass Criteria

The test passes if:

```text
All three browsers can start calculations at the same time.
Each browser receives only its own company/user/calculation updates.
All calculations complete.
XML storage contains separate records for each company/user/calculation.
No cross-company realtime message appears.
```

## Fail Criteria

The test fails if:

```text
Company A browser receives Company B or Company C update.
Company B browser receives Company A or Company C update.
Company C browser receives Company A or Company B update.
SignalR connection fails after login.
API starts job but XML is not updated.
API starts job but hub receives no notification.
```
