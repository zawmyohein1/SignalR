import { expect, test } from '@playwright/test';
import { openLeaveCalculationPage } from './helpers/leaveCalculation';

test.describe('Leave Calculation smoke', () => {
  test('loads the login page and accepts the password field', async ({ page }) => {
    await openLeaveCalculationPage(page);

    const passwordField = page.getByLabel('Password');
    await passwordField.fill('password');
    await expect(passwordField).toHaveValue('password');
  });
});
