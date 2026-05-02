# Playwright Migration Test Plan

## Purpose

Use Playwright to verify that `Timesoft.Solution.Web4` behaves the same as `Timesoft.Solution.Web3`.

The goal is to protect the migration from `.NET Framework` to `.NET Core` by running the same UI tests against both apps.

## Target Applications

- `Timesoft.Solution.Web3`
  - Baseline application
  - Stable production UI
  - Source of truth for expected behavior
- `Timesoft.Solution.Web4`
  - Migration target
  - Must match Web3 behavior

## Core Testing Idea

Write one test suite and run it twice:

- once against Web3
- once against Web4

This lets us compare behavior with the same user actions and the same assertions.

## What We Will Verify

- Pages open correctly
- Inputs are present and can be filled
- Buttons work
- Navigation behaves the same
- Important UI text and states match

## Important Rule For Selectors

Prefer stable selectors.

Good options:

- `getByLabel`
- `getByRole`
- `data-testid`
- specific CSS selectors when structure is stable

Avoid relying only on runtime-changing `id` values.

Example:

```html
<input id="loginPassword" class="form-control" type="password" value="password" maxlength="30" />
```

If the `id` can change at runtime, we should prefer a selector like:

```ts
page.locator('input[type="password"]')
```

or a better semantic selector if the page has labels:

```ts
page.getByLabel('Password')
```

## Step-By-Step Plan

### Step 1: Confirm App URLs

- Confirm the local URL for Web3
- Confirm the local URL for Web4
- Use those URLs as the Playwright `baseURL` values

### Step 2: Create Two Playwright Projects

- One project for Web3
- One project for Web4
- Keep the same test files for both

### Step 3: Add One Smoke Test

Start with one simple test:

- open the login page
- find the password input
- fill it
- verify the value is accepted

### Step 4: Add Shared Helpers

Create helper functions for common actions:

- open login page
- find password field
- submit form
- check page state

This keeps Web3 and Web4 test logic identical.

### Step 5: Expand Coverage

After the smoke test works:

- test login flow
- test main navigation
- test important forms
- test critical UI actions

### Step 6: Compare Results

If Web3 passes and Web4 fails, that is a migration gap.

If both pass, that behavior is consistent.

## Current Known Information

- Web4 is already available at `http://localhost:5101/`
- Web3 URL still needs to be confirmed
- Playwright is already installed in the repository
- A starter Playwright config already exists at the repo root

## Recommended Next Actions

1. Confirm the Web3 local URL
2. Update `playwright.config.ts` for two targets
3. Replace the starter test with a real smoke test
4. Run the first comparison against Web3 and Web4

## Working Agreement

We will move in small steps:

- verify setup first
- add one test at a time
- keep selectors stable
- treat Web3 as the baseline
- treat Web4 failures as migration issues to investigate
