import { expect, test } from '../support/fixtures';
import { uniqueSlug } from '../support/ids';
import { expectPublicInvisible, expectPublicVisible } from '../support/publicApi';
import { loginAsAdmin, seedContentType } from '../support/testData';

test.describe('Content lifecycle', () => {
  test('create, edit, save draft, submit, publish, unpublish, and archive with public API checks', async ({
    page,
    adminApi,
    cleanup,
    contentEntryPage,
  }) => {
    const contentType = await seedContentType(adminApi, cleanup);
    await loginAsAdmin(page);

    const slug = uniqueSlug('article');
    await contentEntryPage.gotoCreate(contentType.slug);
    await contentEntryPage.fillCreateForm(slug, 'Original title');
    await contentEntryPage.createDraft();
    cleanup.trackEntry(contentEntryPage.entryIdFromUrl());

    await contentEntryPage.setTitle('Updated title');
    await contentEntryPage.saveDraftExpectingSuccess('Edited for E2E');
    await expect(contentEntryPage.titleInput()).toHaveValue('Updated title');

    await contentEntryPage.runLifecycleAction('Submit for review', { expectStatus: 'In review' });

    await contentEntryPage.runLifecycleAction('Publish', {
      changeSummary: 'Publishing for E2E',
      expectStatus: 'Published',
    });
    await expectPublicVisible(contentType.slug, slug, 'Updated title');

    await contentEntryPage.runLifecycleAction('Unpublish', {
      changeSummary: 'Unpublish for E2E',
      expectStatus: 'Draft',
    });
    await expectPublicInvisible(contentType.slug, slug);

    await contentEntryPage.runLifecycleAction('Submit for review', { expectStatus: 'In review' });
    await contentEntryPage.runLifecycleAction('Publish', {
      changeSummary: 'Publish again',
      expectStatus: 'Published',
    });
    await expectPublicVisible(contentType.slug, slug, 'Updated title');

    await contentEntryPage.runLifecycleAction('Archive', {
      changeSummary: 'Archive for E2E',
      expectStatus: 'Archived',
    });
    await expectPublicInvisible(contentType.slug, slug);
  });
});
