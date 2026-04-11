# Sample Data - Leave Calculation Demo

Purpose:
This file keeps simple demo data for the Leave Calculation SignalR sample.
It can be used later when implementing the login page, Leave Calculation page,
XML job storage, and multi-company testing.

## Companies

For multi-company testing, use these sample company codes:

| Company Code | Demo Browser | Description |
| --- | --- | --- |
| COMPANY_A | Chrome | Company A demo login |
| COMPANY_B | Firefox | Company B demo login |
| COMPANY_C | Edge | Company C demo login |

Important isolation rule:

SignalR updates must be separated by company and job/calculation id.

Example group name:

```text
company:{companyCode}:job:{jobId}
```

Example:

```text
company:COMPANY_A:job:8f4f2b2d-7a11-47b1-a61f-2b0f82b8b4f4
```

## Employees

| EMP_NO | EMP_NAME |
| --- | --- |
| 001 | ANDY LOW |
| 002 | BEN LIM |
| 003 | COLIN KOH |
| 004 | DAVID GAN |
| 005 | EUGENE ONG |
| 006 | FRASER PANG |
| 101 | ANGELA GOH |
| 102 | BETTY CHIA |
| 103 | CECILIA NG |
| 104 | DAPHNE TAN |
| 105 | EMILY WONG |
| 106 | FIONA WONG |
| 801 | RACHEL WONG |
| 802 | SUSAN TAY |
| 803 | TERESA TAN |
| 804 | UNICE CHENG |
| 8040 | COPY UNICE CHENG |
| 805 | VIVIAN CHIA |

## Departments

| CODE | DESC |
| --- | --- |
| FIN | FINANCE DEPARTMENT |
| IT | IT DEPARTMENT |
| HR | HUMAN RESOURCE DEPARTMENT |
| SALES | SALES DEPARTMENT |
| PUR | PURCHASING DEPARTMENT |
| SG | SINGAPORE DIVISION |

## Leave Types

| CODE | DESC |
| --- | --- |
| ABSENT | ABSENT |
| ACHILD | Adoption Leave |
| AGM | AGM MEETING |
| ANNU | ANNUAL LEAVE |
| ANNU | ANNUAL LEAVE |
| CHILDLVE | CHILD CARE LEAVE |
| COMP | COMPASSIONATE LEAVE |
| CSICK | CHILD SICK LEAVE |
| ECHILD | Enhanced Child Care Leave |
| EMATE | EXTENDED MATERNITY LEAVE |
| EXAM | EXAM LEAVE |
| FAMILY | FAMILY LEAVE |
| HOSP | HOSPITALISATION |
| INFANT | Infant Care Leave |
| LIEU | Mobile Work-WFH( 5 days) |
| LIEUPHSATSUN | WORKING ON PH/SAT/SUN |
| MATE | MATERNITY LEAVE |
| NONE | NONE |
| NPL | NO PAY LEAVE |
| NPLHOUR | NO PAY HOUR |
| NSP | NATIONAL SERVICE |
| OVERSEAS | OVERSEAS TRIP |
| PATE | PATERNITY LEAVE |
| RO | REPLACEMENT OFF |
| SCHILDLV | SPECIAL CHILD CARE LEAVE |
| SEMINAR | SEMINAR |
| SICK | SICK LEAVE |
| SPATE | Shared Parental Leave |
| TRAINING | TRAINING LEAVE |

## Demo Calculation Inputs

Use these combinations for manual testing:

| Test | Company | Department | Employee | Leave Type | Expected Behavior |
| --- | --- | --- | --- | --- | --- |
| 1 | COMPANY_A | HR | 001 - ANDY LOW | ANNU | Only Company A browser receives updates |
| 2 | COMPANY_B | IT | 102 - BETTY CHIA | SICK | Only Company B browser receives updates |
| 3 | COMPANY_C | FIN | 805 - VIVIAN CHIA | HOSP | Only Company C browser receives updates |
| 4 | COMPANY_A | SALES | All | ANNU | Company A receives department calculation progress |
| 5 | COMPANY_B | SG | 8040 - COPY UNICE CHENG | NPL | Company B receives employee calculation progress |

## Notes For Implementation

- Employee `804` and `8040` are different employee numbers.
- Leave type `ANNU` appears twice in the supplied data. For the demo, either keep both rows to match source data or de-duplicate it when displaying dropdown values.
- This sample does not require real leave entitlement calculation logic.
- The process can simulate these statuses:
  - Accepted
  - Started
  - Loading selected employees
  - Calculating leave entitlement
  - Updating leave balances
  - Completed
  - Failed
- XML storage can save job/calculation status separately from this master sample data.

