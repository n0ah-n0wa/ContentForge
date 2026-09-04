import type { Page } from '@playwright/test';
import { expect } from '@playwright/test';

export class UsersPage {
  private readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async gotoCreate(): Promise<void> {
    await this.page.goto('/users/new');
  }

  async gotoList(): Promise<void> {
    await this.page.goto('/users');
    await expect(this.page.getByRole('heading', { name: 'Users', level: 2 })).toBeVisible();
  }

  async createUser(input: {
    email: string;
    displayName: string;
    password: string;
    role: 'Administrator' | 'Editor' | 'Author' | 'Viewer';
  }): Promise<string> {
    await this.page
      .locator('label.form-field', { hasText: 'Email' })
      .locator('input')
      .fill(input.email);
    await this.page
      .locator('label.form-field', { hasText: 'Display name' })
      .locator('input')
      .fill(input.displayName);
    await this.page
      .locator('label.form-field', { hasText: 'Password' })
      .locator('input')
      .fill(input.password);
    await this.page
      .locator('label.form-field', { hasText: 'Role' })
      .locator('select')
      .selectOption(input.role);
    await this.page.getByRole('button', { name: 'Create user' }).click();
    // `/users/new` already matches `/users/:id` — wait until we leave the create route.
    await this.page.waitForURL((url) => {
      const path = url.pathname.replace(/\/$/, '');
      return /^\/users\/[^/]+$/.test(path) && !path.endsWith('/new');
    });

    const match = this.page.url().match(/\/users\/([0-9a-f-]{36})(?:\/|$|\?)/i);
    if (!match?.[1]) {
      throw new Error(`Expected user edit URL with a GUID, got ${this.page.url()}`);
    }
    return match[1];
  }
}
