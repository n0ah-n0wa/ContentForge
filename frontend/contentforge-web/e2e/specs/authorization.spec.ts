import { expect, test } from '../support/fixtures';
import { ApiClient } from '../support/apiClient';
import { uniqueEmail } from '../support/ids';
import {
  loginAs,
  loginAsAdmin,
  seedContentType,
  seedDraftEntryAs,
  seedUser,
} from '../support/testData';

test.describe('Authorization', () => {
  test('author cannot publish', async ({ page, adminApi, cleanup, contentEntryPage }) => {
    const contentType = await seedContentType(adminApi, cleanup);
    const author = await seedUser(adminApi, cleanup, 'Author');
    const entry = await seedDraftEntryAs(adminApi, cleanup, author, contentType.id, 'Author draft');

    await loginAs(page, author.email, author.password);
    await contentEntryPage.gotoEdit(contentType.slug, entry.id);
    await contentEntryPage.runLifecycleAction('Submit for review', { expectStatus: 'In review' });
    await expect(contentEntryPage.lifecycleButton('Publish')).toHaveCount(0);
    await expect(contentEntryPage.lifecycleButton('Submit for review')).toHaveCount(0);
  });

  test('viewer cannot edit', async ({ page, adminApi, cleanup, contentEntryPage }) => {
    const contentType = await seedContentType(adminApi, cleanup);
    const viewer = await seedUser(adminApi, cleanup, 'Viewer');
    const author = await seedUser(adminApi, cleanup, 'Author');
    const entry = await seedDraftEntryAs(
      adminApi,
      cleanup,
      author,
      contentType.id,
      'Viewer locked',
    );

    await loginAs(page, viewer.email, viewer.password);
    await contentEntryPage.gotoEdit(contentType.slug, entry.id);
    await expect(contentEntryPage.saveDraftButton()).toBeDisabled();
    await expect(contentEntryPage.titleInput()).toBeDisabled();
    await expect(contentEntryPage.lifecycleButton('Submit for review')).toHaveCount(0);
  });

  test('editor can publish', async ({ page, adminApi, cleanup, contentEntryPage }) => {
    const contentType = await seedContentType(adminApi, cleanup);
    const author = await seedUser(adminApi, cleanup, 'Author');
    const editor = await seedUser(adminApi, cleanup, 'Editor');
    const entry = await seedDraftEntryAs(
      adminApi,
      cleanup,
      author,
      contentType.id,
      'Ready to publish',
    );

    const authorClient = new ApiClient();
    await authorClient.login(author.email, author.password);
    const draft = await authorClient.request<{ concurrencyToken: number }>(
      'GET',
      `/api/v1/content/${entry.id}`,
    );
    await authorClient.request('POST', `/api/v1/content/${entry.id}/submit-for-review`, {
      concurrencyToken: draft.concurrencyToken,
    });

    await loginAs(page, editor.email, editor.password);
    await contentEntryPage.gotoEdit(contentType.slug, entry.id);
    await contentEntryPage.expectStatus('In review');
    await contentEntryPage.runLifecycleAction('Publish', {
      changeSummary: 'Editor publish',
      expectStatus: 'Published',
    });
  });

  test('administrator can manage users', async ({ page, adminApi, cleanup, usersPage }) => {
    await loginAsAdmin(page);
    const email = uniqueEmail('managed');
    await usersPage.gotoCreate();
    await expect(page.getByRole('heading', { name: 'Create user', level: 2 })).toBeVisible();
    const createdId = await usersPage.createUser({
      email,
      displayName: 'Managed Viewer',
      password: 'ViewerPassword123!',
      role: 'Viewer',
    });
    cleanup.trackUser(createdId);

    await usersPage.gotoList();
    await page.getByPlaceholder('Search by email or name').fill(email);
    await page.getByRole('button', { name: 'Apply' }).click();
    await expect(page.getByText(email, { exact: true })).toBeVisible();

    const listed = await adminApi.request<{ items: Array<{ email: string }> }>(
      'GET',
      `/api/v1/users?search=${encodeURIComponent(email)}&page=1&pageSize=10`,
    );
    expect(listed.items.map((item) => item.email)).toContain(email);
  });
});
