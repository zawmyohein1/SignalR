# Playwright Proof Case Summary

## What We Proved

We verified that the Playwright suite is really checking the UI.

When we intentionally removed the `Login` button from `Timesoft.Solution.Web4`, the tests failed on Web4 and still passed on Web3.

When we restored the button and restarted Web4, the suite returned to all green.

## Important Result

- Web3 stayed stable
- Web4 failed only when the UI was broken
- Playwright reported the failure correctly

## Why This Matters

This proves the migration test setup is trustworthy.

It is not just showing a fake pass. It is actually detecting UI regressions.

## Commands Used

- `npm test`
- `npm test -- --reporter=line`

## Report Behavior

- `file:///D:/Lab/SignalR/playwright-report/index.html` can show an older static report
- `http://localhost:9323/` shows the live HTML report for the latest run

## Learning Takeaway

For migration testing:

1. keep Web3 as the baseline
2. run the same tests against Web4
3. break one UI element to confirm the tests fail
4. restore the UI to confirm the suite returns to green

That is a good way to build confidence before adding more business flows.
