# Playwright Lessons Learned

Compact rules to keep in mind when using Playwright for Web3 vs Web4 migration checks.

## Keep The Target Exact

- Use the real live URL and scheme for each app.
- If the port or protocol is wrong, Playwright can fail even when a browser tab still looks fine.

## Trust Fresh Requests, Not Open Tabs

- Playwright always starts with a fresh navigation.
- A page already open in the browser may hide server instability.
- When Playwright fails on `page.goto('/')`, check the endpoint first.

## Treat Config As Part Of The Test

- Web3 and Web4 only behave correctly when their app, API, hub, and CORS settings match the running services.
- A passing test is only valid for the current runtime configuration.

## Make The Assertions Strong Enough

- A green run only proves what the test asserted.
- For process flows, check the final success state, not only that the UI changed.
- If failure is possible, assert that `Failed` does not count as success.

## Compare The Same Flow On Both Apps

- Reuse the same test steps for Web3 and Web4.
- Compare load, login, process, and restore behavior.
- When one side fails, classify the cause as UI, page-load/controller, API/business logic, or config.

## Keep Proof Cases

- Intentional break/fix demos are useful.
- They prove the test suite is real and catches regressions.

