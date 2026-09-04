import type { Page } from '@playwright/test';
import { expect } from '@playwright/test';

export class LoginPage {
  private readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async goto(): Promise<void> {
    await this.page.goto('/login');
    await expect(this.page.getByRole('button', { name: 'Sign in' })).toBeVisible();
  }

  async signIn(email: string, password: string): Promise<void> {
    await this.page.getByLabel('Email').fill(email);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
  }

  errorAlert() {
    return this.page.getByRole('alert').filter({ hasText: 'Sign in failed' });
  }
}
