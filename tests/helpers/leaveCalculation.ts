import { expect, type Page } from '@playwright/test';

export async function openLeaveCalculationPage(page: Page) {
  await page.goto('/');

  await expect(page.locator('#leaveCalculationApp')).toBeVisible();
  await expect(page.locator('#loginPanel')).toBeVisible();
  await expect(page.locator('#calculationPanel')).toHaveClass(/d-none/);
  await expect(page.getByLabel('Login Id')).toBeVisible();
  await expect(page.getByLabel('Password')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Login' })).toBeVisible();
}

export async function loginWithDefaultValues(page: Page, companyCode = 'COMPANY_A') {
  const company = page.locator('#companyCode');
  const loginId = page.getByLabel('Login Id');
  const password = page.getByLabel('Password');
  const loginButton = page.getByRole('button', { name: 'Login' });

  if (companyCode !== 'COMPANY_A') {
    await company.selectOption(companyCode);
  }

  const expectedLoginId = companyCode === 'COMPANY_B'
    ? 'HR_B'
    : companyCode === 'COMPANY_C'
      ? 'HR_C'
      : 'HR_A';

  await expect(loginId).toHaveValue(expectedLoginId);
  await expect(password).toHaveValue('password');

  await password.fill('password');
  await loginButton.click();

  await expect(page.locator('#loginPanel')).toHaveClass(/d-none/);
  await expect(page.locator('#calculationPanel')).toBeVisible();
  await expect(page.locator('#contextUser')).toHaveText(expectedLoginId);
  await expect(page.locator('#contextPeriod')).toContainText('Period');
}
