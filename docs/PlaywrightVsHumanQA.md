# Playwright vs Human QA

## Playwright Pros

- Fast and repeatable
- Good for regression testing
- Easy to run on every build or commit
- Useful for comparing Web3 and Web4
- Gives clear failure points
- Saves time once the suite is built

## Playwright Cons

- Only checks what the tests cover
- Can be brittle if selectors are unstable
- Takes time to set up and maintain
- Does not judge visual quality or UX well
- Can pass even if the app still feels awkward

## Human QA Pros

- Better for exploratory testing
- Better at noticing UX and visual issues
- Can adapt quickly when something looks odd
- Helpful for finding issues outside the test suite

## Human QA Cons

- Slower than automated tests
- Harder to repeat exactly
- Expensive to run often
- Easy to miss edge cases over time

## Best Practice

Use both together:

- Playwright for repeated regression checks
- Human QA for exploratory testing and UX review

## Simple Rule

- Playwright asks: "Does it still work?"
- Human QA asks: "Does it feel right?"

