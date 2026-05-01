import { test } from '@playwright/test';
import { loginWithDefaultValues, openLeaveCalculationPage } from './helpers/leaveCalculation';

test.describe('Leave Calculation login flow', () => {
  test('switches from login panel to calculation panel after login', async ({ page }) => {
    await openLeaveCalculationPage(page);
    await loginWithDefaultValues(page);
  });
});
