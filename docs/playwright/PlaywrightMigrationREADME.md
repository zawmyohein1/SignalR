# Playwright Migration Testing

This repository uses Playwright to compare the UI behavior of:

- `Timesoft.Solution.Web3` as the stable baseline
- `Timesoft.Solution.Web4` as the migration target

The goal is to make sure Web4 behaves the same as Web3 for the main UI flows.

## Local App URLs

- Web3: `http://localhost:57635/`
- Web4: `https://localhost:5101/`

## What Was Set Up

- Playwright is configured to run the same tests against both apps
- The config lives in [playwright.config.ts](D:\Lab\SignalR\playwright.config.ts)
- Two Playwright projects are defined:
  - `web3-chromium`
  - `web4-chromium`
- Each project uses its own `baseURL`

## Test Files

- [tests/smoke.spec.ts](D:\Lab\SignalR\tests\smoke.spec.ts)
  - checks that the page loads
  - checks that the password field exists
  - checks that the password field can be filled

- [tests/login-flow.spec.ts](D:\Lab\SignalR\tests\login-flow.spec.ts)
  - checks the login flow
  - verifies the page switches from the login panel to the calculation panel

- [tests/helpers/leaveCalculation.ts](D:\Lab\SignalR\tests\helpers\leaveCalculation.ts)
  - shared helper functions used by both tests

## Commands

- Run all tests:
  - `npm test`

- Run tests in headed mode:
  - `npm run test:headed`

- Open the HTML report:
  - `npm run test:report`

## What We Verified

The suite initially passed on both apps:

- `4 passed`

That confirmed the same test logic works on both Web3 and Web4.

## Proof Cases

Two intentional failure demos were used to prove the tests are real.

### 1. Missing Login Button in Web4

- The `Login` button was temporarily hidden in Web4
- Web3 still passed
- Web4 failed
- This proved the tests catch UI problems

### 2. Broken Web4 Page-Load Controller

- The Web4 home controller was temporarily broken
- Web3 still passed
- Web4 failed before the page loaded normally
- This proved the tests catch page-load or controller problems

## How To Read Failures

- Missing button or field:
  - likely UI issue

- Page does not load or app root element is missing:
  - likely controller or page-load issue

- Button works, but the next screen does not appear:
  - likely API or business logic issue

- Test checks the wrong thing:
  - likely test issue

## Report Note

- `file:///D:/Lab/SignalR/playwright-report/index.html` can show an older static report
- `http://localhost:9323/` shows the live report for the latest test run

## Related Notes

- [PlaywrightLessons.md](D:\Lab\SignalR\docs\playwright\PlaywrightLessons.md) for short migration-testing rules
- [PlaywrightVsHumanQA.md](D:\Lab\SignalR\docs\playwright\PlaywrightVsHumanQA.md) for the automation vs manual-testing tradeoff

## Final State

- Web4 was restored after each proof case
- The suite was rerun
- Final result returned to `4 passed`

## How To Extend The Suite

Add a new spec when you want to test another page or feature:

- create a new `*.spec.ts` file in `tests/`
- reuse shared helpers when possible
- keep selectors stable
- run `npm test` to verify both Web3 and Web4
