import { expect, test } from '../support/fixtures';
import { seededAdmin } from '../support/env';
import { loginAs, seedDisabledUser, seedUser } from '../support/testData';

test.describe('Authentication', () => {
  test('login succeeds for seeded administrator', async ({ page, loginPage, dashboardPage }) => {
    await loginPage.goto();
    await loginPage.signIn(seededAdmin.email, seededAdmin.password);
    await expect(page).toHaveURL(/\/dashboard/);
    await expect(page.getByRole('heading', { name: 'Dashboard', level: 1 })).toBeVisible();
    await expect(dashboardPage.userLabel()).toContainText('Administrator');
  });

  test('invalid credentials show a sign-in error', async ({ page, loginPage }) => {
    await loginPage.goto();
    await loginPage.signIn(seededAdmin.email, 'DefinitelyWrongPassword1!');
    await expect(loginPage.errorAlert()).toBeVisible();
    await expect(loginPage.errorAlert()).toContainText('Invalid email or password.');
    await expect(page).toHaveURL(/\/login/);
  });

  test('logout returns to the login screen', async ({ page, loginPage, dashboardPage }) => {
    await loginPage.goto();
    await loginPage.signIn(seededAdmin.email, seededAdmin.password);
    await expect(page).toHaveURL(/\/dashboard/);
    await dashboardPage.signOut();
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();

    // Session must be cleared — protected routes redirect again.
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login/);
  });

  test('protected route redirects anonymous users to login', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible();
    expect(new URL(page.url()).searchParams.get('redirect')).toBe('/dashboard');
  });

  test('forbidden access shows access denied for unauthorized roles', async ({
    page,
    adminApi,
    cleanup,
  }) => {
    const viewer = await seedUser(adminApi, cleanup, 'Viewer');
    await loginAs(page, viewer.email, viewer.password);
    await page.goto('/users');
    await expect(page).toHaveURL(/\/access-denied/);
    await expect(page.getByRole('heading', { name: 'Access denied', level: 2 })).toBeVisible();
    await expect(page.getByText(/does not include the permissions required/i)).toBeVisible();
  });

  test('disabled account cannot sign in', async ({ page, loginPage, adminApi, cleanup }) => {
    const disabled = await seedDisabledUser(adminApi, cleanup);
    await loginPage.goto();
    await loginPage.signIn(disabled.email, disabled.password);
    await expect(loginPage.errorAlert()).toBeVisible();
    await expect(loginPage.errorAlert()).toContainText(/Invalid email or password|disabled/i);
    await expect(page).toHaveURL(/\/login/);
  });
});
