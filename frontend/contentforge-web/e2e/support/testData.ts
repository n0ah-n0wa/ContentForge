import type { Page } from '@playwright/test';
import { ApiClient } from './apiClient';
import { seededAdmin } from './env';
import type { CleanupRegistry } from './cleanup';
import { uniqueEmail, uniqueSlug, uniqueSuffix } from './ids';

const FieldType = {
  Text: 0,
  Media: 8,
} as const;

export interface SeededContentType {
  id: string;
  slug: string;
  name: string;
  hasMediaField: boolean;
}

export interface SeededUser {
  id: string;
  email: string;
  password: string;
  role: 'Editor' | 'Author' | 'Viewer' | 'Administrator';
}

export async function createAdminApi(): Promise<ApiClient> {
  const api = new ApiClient();
  await api.login(seededAdmin.email, seededAdmin.password);
  return api;
}

export async function seedContentType(
  api: ApiClient,
  cleanup: CleanupRegistry,
  options: { withMediaField?: boolean } = {},
): Promise<SeededContentType> {
  const suffix = uniqueSuffix('type');
  const created = await api.request<{ id: string; slug: string; name: string }>(
    'POST',
    '/api/v1/content-types',
    {
      name: `e2e${suffix.replace(/-/g, '')}`.slice(0, 40),
      displayName: `E2E Type ${suffix}`,
      slug: uniqueSlug('type'),
      description: 'Playwright E2E content type',
    },
  );

  cleanup.trackContentType(created.id);

  await api.request('POST', `/api/v1/content-types/${created.id}/fields`, {
    name: 'title',
    fieldType: FieldType.Text,
    displayName: 'Title',
    sortOrder: 0,
    configuration: {
      isRequired: true,
      minLength: null,
      maxLength: null,
      minValue: null,
      maxValue: null,
      pattern: null,
      allowMultiple: false,
      defaultValue: null,
      options: [],
      relationTarget: null,
      relationCardinality: null,
    },
  });

  if (options.withMediaField) {
    await api.request('POST', `/api/v1/content-types/${created.id}/fields`, {
      name: 'hero',
      fieldType: FieldType.Media,
      displayName: 'Hero',
      sortOrder: 1,
      configuration: {
        isRequired: false,
        minLength: null,
        maxLength: null,
        minValue: null,
        maxValue: null,
        pattern: null,
        allowMultiple: false,
        defaultValue: null,
        options: [],
        relationTarget: null,
        relationCardinality: null,
      },
    });
  }

  return {
    id: created.id,
    slug: created.slug,
    name: created.name,
    hasMediaField: !!options.withMediaField,
  };
}

export async function seedUser(
  api: ApiClient,
  cleanup: CleanupRegistry,
  role: SeededUser['role'],
): Promise<SeededUser> {
  const email = uniqueEmail(role.toLowerCase());
  const password =
    role === 'Editor'
      ? 'EditorPassword123!'
      : role === 'Author'
        ? 'AuthorPassword123!'
        : role === 'Viewer'
          ? 'ViewerPassword123!'
          : 'AdminPassword123!';

  const created = await api.request<{ id: string }>('POST', '/api/v1/users', {
    email,
    displayName: `E2E ${role}`,
    password,
    role,
  });

  cleanup.trackUser(created.id);
  return { id: created.id, email, password, role };
}

export async function seedDisabledUser(
  api: ApiClient,
  cleanup: CleanupRegistry,
): Promise<{ email: string; password: string }> {
  const user = await seedUser(api, cleanup, 'Viewer');
  await api.request('POST', `/api/v1/users/${user.id}/disable`);
  return { email: user.email, password: user.password };
}

export async function seedDraftEntry(
  api: ApiClient,
  cleanup: CleanupRegistry,
  contentTypeId: string,
  title = 'Seeded draft',
): Promise<{ id: string; slug: string }> {
  const slug = uniqueSlug('entry');
  const created = await api.request<{ id: string; slug: string }>('POST', '/api/v1/content', {
    contentTypeId,
    slug,
    data: { title },
  });
  cleanup.trackEntry(created.id);
  return created;
}

export async function seedDraftEntryAs(
  api: ApiClient,
  cleanup: CleanupRegistry,
  actor: { email: string; password: string },
  contentTypeId: string,
  title = 'Seeded draft',
): Promise<{ id: string; slug: string }> {
  const actorClient = new ApiClient();
  await actorClient.login(actor.email, actor.password);
  return seedDraftEntry(actorClient, cleanup, contentTypeId, title);
}

/** Authenticate via the login form (UI), not by injecting tokens. */
export async function loginAs(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/login');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.waitForURL(/\/dashboard/);
  await page.locator('.app-header__user').waitFor({ state: 'visible' });
}

export async function loginAsAdmin(page: Page): Promise<void> {
  await loginAs(page, seededAdmin.email, seededAdmin.password);
}
