import { expect, test } from '@playwright/test';
import { loginWithDefaultValues, openLeaveCalculationPage } from './helpers/leaveCalculation';

test.describe('Leave Calculation process flow', () => {
  test('starts a calculation and shows live status updates', async ({ page }) => {
    await openLeaveCalculationPage(page);
    await loginWithDefaultValues(page);

    const processButton = page.getByRole('button', { name: 'Process' });

    await expect(processButton).toBeVisible();
    await processButton.click();

    await expect(page.locator('#progressLog')).toContainText('Posting Leave Calculation request.');
    await expect(page.locator('#currentStatusDisplay')).toHaveText(
      /(Accepted|Started|Calculating leave entitlement|Completed|Failed)/);
    await expect(page.locator('#apiResponseDisplay')).not.toHaveText('Waiting for process');
    await expect(page.locator('#progressLog')).toContainText(
      /(Accepted|Started|Calculating|Completed|Failed)/);
  });
});
