import { expect, test } from '../support/fixtures';
import { loginAsAdmin, seedContentType, seedDraftEntry } from '../support/testData';

test.describe('Concurrency', () => {
  test('stale content update shows a concurrency conflict', async ({
    page,
    adminApi,
    cleanup,
    contentEntryPage,
  }) => {
    const contentType = await seedContentType(adminApi, cleanup);
    const entry = await seedDraftEntry(adminApi, cleanup, contentType.id, 'Concurrent draft');

    await loginAsAdmin(page);
    await contentEntryPage.gotoEdit(contentType.slug, entry.id);
    await expect(contentEntryPage.titleInput()).toHaveValue('Concurrent draft');

    // Another actor updates the same entry, advancing the concurrency token.
    const current = await adminApi.request<{
      concurrencyToken: number;
      slug: string;
    }>('GET', `/api/v1/content/${entry.id}`);

    await adminApi.request('PUT', `/api/v1/content/${entry.id}`, {
      slug: current.slug,
      data: { title: 'Updated by API actor' },
      changeSummary: 'Stale-token setup',
      concurrencyToken: current.concurrencyToken,
    });

    await contentEntryPage.setTitle('Stale UI edit');
    await contentEntryPage.saveDraft('Should conflict');

    const conflict = contentEntryPage.concurrencyConflictPanel();
    await expect(conflict).toBeVisible();
    await expect(conflict.getByRole('heading', { name: 'Concurrency conflict' })).toBeVisible();
    await expect(conflict).toContainText(/updated by someone else/i);
    await expect(conflict).toContainText(/you had version/i);

    await conflict.getByRole('button', { name: 'Reload latest' }).click();
    await expect(conflict).toHaveCount(0);
    await expect(contentEntryPage.titleInput()).toHaveValue('Updated by API actor');
    await contentEntryPage.expectSaved();
  });
});
