import { expect, test } from '../support/fixtures';
import { uniqueSlug } from '../support/ids';
import { loginAsAdmin, seedContentType } from '../support/testData';

test.describe('Versioning', () => {
  test('create multiple versions, inspect history, and restore a previous version', async ({
    page,
    adminApi,
    cleanup,
    contentEntryPage,
  }) => {
    const contentType = await seedContentType(adminApi, cleanup);
    await loginAsAdmin(page);

    const slug = uniqueSlug('versioned');
    await contentEntryPage.gotoCreate(contentType.slug);
    await contentEntryPage.fillCreateForm(slug, 'Version one title');
    await contentEntryPage.createDraft();
    cleanup.trackEntry(contentEntryPage.entryIdFromUrl());
    await expect(contentEntryPage.currentVersion()).toHaveText('1');

    await contentEntryPage.setTitle('Version two title');
    await contentEntryPage.saveDraftExpectingSuccess('Second draft version');
    await expect(contentEntryPage.titleInput()).toHaveValue('Version two title');
    await expect(contentEntryPage.currentVersion()).toHaveText('2');

    await contentEntryPage.setTitle('Version three title');
    await contentEntryPage.saveDraftExpectingSuccess('Third draft version');
    await expect(contentEntryPage.titleInput()).toHaveValue('Version three title');
    await expect(contentEntryPage.currentVersion()).toHaveText('3');

    await expect(contentEntryPage.versionItems()).toHaveCount(3);
    await contentEntryPage.inspectVersion(1);
    await expect(page.getByTestId('version-json')).toContainText('Version one title');
    await expect(page.getByTestId('version-comparison')).toContainText(
      /difference\(s\) compared to current version 3/,
    );

    await contentEntryPage.restoreVersion(1, 'Restore first snapshot');
    await contentEntryPage.expectStatus('Draft');
    await expect(contentEntryPage.titleInput()).toHaveValue('Version one title');
    await expect(contentEntryPage.currentVersion()).toHaveText('4');
    await expect(contentEntryPage.versionItems()).toHaveCount(4);
  });
});
