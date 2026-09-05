import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { expect, type Page } from '@playwright/test';

const fixturesDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../fixtures');

export class MediaPage {
  private readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async goto(): Promise<void> {
    await this.page.goto('/media');
    await expect(this.page.getByRole('heading', { name: 'Media library', level: 2 })).toBeVisible();
  }

  async uploadFixture(fileName = 'sample.png', options: { title?: string } = {}): Promise<void> {
    const filePath = path.join(fixturesDir, fileName);
    await this.page.locator('input[type="file"]').setInputFiles(filePath);

    if (options.title) {
      await this.page
        .locator('label.form-field', { hasText: 'Title' })
        .locator('input')
        .fill(options.title);
    }

    // Exact match: aria-label "Choose file to upload" also matches /Upload/i.
    await this.page.getByRole('button', { name: 'Upload', exact: true }).click();
    const title = options.title ?? fileName;
    await expect(
      this.page.getByTestId('media-asset-card').filter({ hasText: title }),
    ).toBeVisible();
  }

  async selectAssetByName(name: string): Promise<void> {
    await this.page
      .getByTestId('media-asset-card')
      .filter({ hasText: name })
      .getByRole('button')
      .click();
    await expect(this.page.getByRole('button', { name: 'Delete' })).toBeEnabled();
  }

  async deleteSelected(): Promise<void> {
    await this.page.getByRole('button', { name: 'Delete' }).click();
    const dialog = this.page.getByRole('dialog', { name: 'Delete media' });
    await dialog.getByRole('button', { name: 'Delete media' }).click();
    await expect(dialog).toBeHidden();
  }

  assetCard(name: string) {
    return this.page.getByTestId('media-asset-card').filter({ hasText: name });
  }
}
