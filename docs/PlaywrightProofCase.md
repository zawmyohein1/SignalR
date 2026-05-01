# Playwright Proof Case: Intentional Web4 Failure

## Purpose

This test case is for learning and proof.

It shows that the Playwright suite is really checking the UI, not just reporting success by mistake.

## Current Baseline

We already have a passing Playwright setup that checks both:

- `Timesoft.Solution.Web3`
- `Timesoft.Solution.Web4`

The current tests confirm:

- the login page loads
- the username and password fields are visible
- the password field accepts input
- the `Login` button can be clicked
- the page switches from the login panel to the calculation panel

## Proof Idea

We intentionally break one UI element in `Web4`, then run the tests again.

Example:

- keep `Web3` unchanged
- remove or hide the `Login` button in `Web4`
- run `npm test`

## Expected Result

If the tests are real, the result should change.

Expected outcome:

- `Web3` tests still pass
- `Web4` tests fail

Possible failure messages:

- the `Login` button is not visible
- Playwright cannot find a button named `Login`
- the login flow cannot continue because the click target is missing

## Why This Proves The Test Is Correct

If Playwright reports a failure after the UI is broken, that means:

- the locator is actually checking the page
- the test is not just a fake pass
- the report is trustworthy for migration work

## Learning Steps

1. Start from the known-good state
   - both Web3 and Web4 pass
2. Make one intentional UI change in Web4
   - remove the `Login` button or hide it
3. Run `npm test` again
4. Confirm Web3 still passes
5. Confirm Web4 now fails
6. Read the HTML report and the failure message

## What We Learn

- Playwright can detect a broken UI
- the Web3 vs Web4 comparison is useful
- the migration suite catches regressions early

## Important Note

This is only a temporary proof case.

After learning from it, restore Web4 back to the working state so the migration suite can continue normally.
