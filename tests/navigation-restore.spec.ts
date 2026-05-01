import { expect, test } from '@playwright/test';

test.describe('Leave Calculation navigation restore', () => {
  test('restores the active calculation after visiting View Leave', async ({ page }) => {
    const activeCalculation = {
      calculationId: 'CALC-RESTORE-001',
      companyCode: 'COMPANY_B',
      companyText: 'Company B - CLIENT2',
      loginUserId: 'HR_B',
      period: '2026/04',
      departmentCode: 'HR',
      employeeNo: '001',
      year: 2026,
      hubAccessToken: '',
      executionMode: 'Background + SignalR',
      signalREnabled: false,
      startedAt: new Date().toISOString()
    };

    await page.addInitScript((savedCalculation) => {
      window.sessionStorage.setItem(
        'timesoft.leaveCalculation.active',
        JSON.stringify(savedCalculation));
    }, activeCalculation);

    await page.route('**/LeaveCalculations/Details/**', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          calculationId: 'CALC-RESTORE-001',
          status: 'Started',
          message: 'Calculation is in progress.',
          history: [
            {
              calculationId: 'CALC-RESTORE-001',
              status: 'Started',
              message: 'Calculation is in progress.',
              timestamp: new Date().toISOString()
            }
          ]
        })
      });
    });

    await page.goto('/');

    await expect(page.locator('#calculationPanel')).toBeVisible();
    await expect(page.locator('#apiResponseDisplay')).toContainText('Restored from browser storage');
    await expect(page.locator('#calculationIdDisplay')).toHaveText('CALC-RESTORE-001');
    await expect(page.locator('#progressLog')).toContainText('Restored calculation CALC-RESTORE-001');

    await page.getByRole('link', { name: 'View Leave' }).click();
    await expect(page.locator('h1')).toContainText('Leave Calculation can keep running');

    await page.getByRole('link', { name: 'Back to Leave Calculation' }).click();

    await expect(page.locator('#calculationPanel')).toBeVisible();
    await expect(page.locator('#apiResponseDisplay')).toContainText('Restored from browser storage');
    await expect(page.locator('#currentStatusDisplay')).not.toHaveText('Idle');
  });
});
