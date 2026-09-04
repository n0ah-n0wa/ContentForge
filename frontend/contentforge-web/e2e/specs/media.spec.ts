import { expect, test } from '../support/fixtures';
import { loginAsAdmin, seedContentType } from '../support/testData';
import { uniqueSlug } from '../support/ids';

test.describe('Media', () => {
  test('upload, select on a content entry, and delete', async ({
    page,
    adminApi,
    cleanup,
    mediaPage,
    contentEntryPage,
  }) => {
    const contentType = await seedContentType(adminApi, cleanup, { withMediaField: true });
    const title = `E2E media ${uniqueSlug('asset')}`;

    await loginAsAdmin(page);
    await mediaPage.goto();
    await mediaPage.uploadFixture('sample.png', { title });
    await expect(mediaPage.assetCard(title)).toBeVisible();

    const assets = await adminApi.request<{ items: Array<{ id: string; title: string | null }> }>(
      'GET',
      `/api/v1/media?search=${encodeURIComponent(title)}&page=1&pageSize=10`,
    );
    const uploaded = assets.items.find((item) => item.title === title);
    expect(uploaded, `media asset titled ${title} should exist after upload`).toBeDefined();
    cleanup.trackMedia(uploaded!.id);

    const slug = uniqueSlug('media-entry');
    await contentEntryPage.gotoCreate(contentType.slug);
    await contentEntryPage.fillCreateForm(slug, 'Entry with media');
    await contentEntryPage.chooseMediaAsset(title);
    await contentEntryPage.createDraft();
    cleanup.trackEntry(contentEntryPage.entryIdFromUrl());

    await mediaPage.goto();
    await mediaPage.selectAssetByName(title);
    await mediaPage.deleteSelected();
    await expect(mediaPage.assetCard(title)).toHaveCount(0);

    // Soft-deleted assets remain fetchable by id for admins, but leave the library list.
    const listedAfter = await adminApi.request<{
      items: Array<{ id: string; title: string | null }>;
    }>('GET', `/api/v1/media?search=${encodeURIComponent(title)}&page=1&pageSize=10`);
    expect(listedAfter.items.find((item) => item.id === uploaded!.id)).toBeUndefined();

    const byId = await adminApi.request<{ id: string; isDeleted: boolean }>(
      'GET',
      `/api/v1/media/${uploaded!.id}`,
    );
    expect(byId.isDeleted).toBe(true);
  });
});
