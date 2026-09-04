import { test as base, expect } from '@playwright/test';
import { ApiClient } from './apiClient';
import { CleanupRegistry } from './cleanup';
import { createAdminApi } from './testData';
import { LoginPage } from '../pages/LoginPage';
import { DashboardPage } from '../pages/DashboardPage';
import { ContentEntryPage } from '../pages/ContentEntryPage';
import { MediaPage } from '../pages/MediaPage';
import { UsersPage } from '../pages/UsersPage';

type E2EFixtures = {
  adminApi: ApiClient;
  cleanup: CleanupRegistry;
  loginPage: LoginPage;
  dashboardPage: DashboardPage;
  contentEntryPage: ContentEntryPage;
  mediaPage: MediaPage;
  usersPage: UsersPage;
};

export const test = base.extend<E2EFixtures>({
  // Playwright requires object-destructuring fixture args; cleanup has no dependencies.
  // eslint-disable-next-line no-empty-pattern -- required by @playwright/test fixture signature
  cleanup: async ({}, use) => {
    const cleanup = new CleanupRegistry();
    await use(cleanup);
  },

  adminApi: async ({ cleanup }, use) => {
    const adminApi = await createAdminApi();
    await use(adminApi);
    await cleanup.run(adminApi);
  },

  loginPage: async ({ page }, use) => {
    await use(new LoginPage(page));
  },

  dashboardPage: async ({ page }, use) => {
    await use(new DashboardPage(page));
  },

  contentEntryPage: async ({ page }, use) => {
    await use(new ContentEntryPage(page));
  },

  mediaPage: async ({ page }, use) => {
    await use(new MediaPage(page));
  },

  usersPage: async ({ page }, use) => {
    await use(new UsersPage(page));
  },
});

export { expect };
