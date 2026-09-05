import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '../support/fixtures';
import { seededAdmin } from '../support/env';

test.describe('Accessibility', () => {
  test('login page has no critical WCAG AA violations', async ({ page, loginPage }) => {
    await loginPage.goto();

    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
      .analyze();

    expect(results.violations, JSON.stringify(results.violations, null, 2)).toEqual([]);
  });

  test('dashboard shell has no critical WCAG AA violations', async ({ page, loginPage }) => {
    await loginPage.goto();
    await loginPage.signIn(seededAdmin.email, seededAdmin.password);
    await expect(page).toHaveURL(/\/dashboard/);

    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
      .analyze();

    expect(results.violations, JSON.stringify(results.violations, null, 2)).toEqual([]);
  });
});
