import type { Page } from '@playwright/test';

export class DashboardPage {
  private readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  heading() {
    return this.page
      .getByRole('heading', { name: /Dashboard|Overview|Home/i })
      .or(this.page.locator('.app-header__title'));
  }

  userLabel() {
    return this.page.locator('.app-header__user');
  }

  async signOut(): Promise<void> {
    await this.page.getByRole('button', { name: 'Sign out' }).click();
    await this.page.waitForURL(/\/login/);
  }
}
