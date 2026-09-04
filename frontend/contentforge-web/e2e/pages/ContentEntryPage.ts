import type { Locator, Page } from '@playwright/test';
import { expect } from '@playwright/test';

export type EntryStatusLabel = 'Draft' | 'In review' | 'Published' | 'Archived' | 'Unpublished';

export class ContentEntryPage {
  private readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async gotoCreate(contentTypeSlug: string): Promise<void> {
    await this.page.goto(`/content/${contentTypeSlug}/new`);
  }

  async gotoEdit(contentTypeSlug: string, entryId: string): Promise<void> {
    await this.page.goto(`/content/${contentTypeSlug}/${entryId}`);
    await this.status().waitFor({ state: 'visible' });
  }

  async fillCreateForm(slug: string, title: string): Promise<void> {
    await this.page.locator('label.form-field', { hasText: 'Slug' }).locator('input').fill(slug);
    await this.titleInput().fill(title);
  }

  async createDraft(): Promise<void> {
    await this.page.getByRole('button', { name: 'Create draft' }).click();
    // `/content/:slug/new` already matches `/content/:slug/:id` — wait until create completes.
    await this.page.waitForURL((url) => {
      const path = url.pathname.replace(/\/$/, '');
      return /^\/content\/[^/]+\/[^/]+$/.test(path) && !path.endsWith('/new');
    });
    await this.expectStatus('Draft');
  }

  async saveDraft(changeSummary = 'E2E draft save'): Promise<void> {
    const summary = this.page
      .locator('label.form-field', { hasText: 'Change summary' })
      .locator('input');
    if ((await summary.count()) > 0) {
      await summary.fill(changeSummary);
    }
    await this.page.getByRole('button', { name: 'Save draft' }).click();
  }

  async saveDraftExpectingSuccess(changeSummary = 'E2E draft save'): Promise<void> {
    await this.saveDraft(changeSummary);
    await this.expectSaved();
  }

  saveDraftButton() {
    return this.page.getByRole('button', { name: 'Save draft' });
  }

  titleInput(): Locator {
    return this.page.locator('label.form-field', { hasText: 'Title' }).locator('input');
  }

  async setTitle(title: string): Promise<void> {
    await this.titleInput().fill(title);
  }

  status() {
    return this.page.getByTestId('entry-status');
  }

  /** @deprecated Prefer status() / expectStatus() */
  statusText() {
    return this.status();
  }

  validation() {
    return this.page.getByTestId('entry-validation');
  }

  currentVersion() {
    return this.page.getByTestId('entry-current-version');
  }

  concurrencyConflictPanel() {
    return this.page.getByTestId('concurrency-conflict');
  }

  async expectStatus(label: EntryStatusLabel): Promise<void> {
    await expect(this.status()).toHaveText(label);
  }

  async expectSaved(): Promise<void> {
    await expect(this.validation()).toHaveText('Saved');
  }

  async runLifecycleAction(
    actionLabel: string,
    options?: { changeSummary?: string; expectStatus?: EntryStatusLabel },
  ): Promise<void> {
    await this.page.getByRole('button', { name: actionLabel, exact: true }).click();
    const dialog = this.page.getByRole('dialog', { name: actionLabel });
    await expect(dialog).toBeVisible();

    const summary = dialog
      .locator('label.form-field', { hasText: 'Change summary' })
      .locator('input');
    if ((await summary.count()) > 0) {
      await summary.fill(options?.changeSummary ?? `E2E ${actionLabel}`);
    }

    const destructiveConfirm = dialog.getByRole('button', { name: 'Confirm destructive action' });
    if ((await destructiveConfirm.count()) > 0) {
      await destructiveConfirm.click();
    } else {
      await dialog.getByRole('button', { name: 'Confirm' }).click();
    }

    await expect(dialog).toBeHidden();
    if (options?.expectStatus) {
      await this.expectStatus(options.expectStatus);
    }
  }

  lifecycleButton(actionLabel: string) {
    return this.page.getByRole('button', { name: actionLabel, exact: true });
  }

  async chooseMediaAsset(fileName: string): Promise<void> {
    await expect(this.page.getByTestId('media-field-empty')).toBeVisible();
    // Media controls sit inside a <label>, so role name includes the field title.
    await this.page.getByRole('button', { name: /Choose media asset/ }).click();
    const dialog = this.page.getByRole('dialog', { name: 'Select media' });
    await expect(dialog).toBeVisible();

    await dialog.getByPlaceholder(/Search by file name/i).fill(fileName);
    await dialog.getByRole('button', { name: 'Apply' }).click();
    await dialog
      .getByTestId('media-asset-card')
      .filter({ hasText: fileName })
      .getByRole('button')
      .click();

    await dialog.getByRole('button', { name: /Use selected/ }).click();
    await expect(dialog).toBeHidden();
    await expect(this.page.getByTestId('media-field-selected-title')).toHaveText(fileName);
  }

  versionItems() {
    return this.page.locator('[data-testid^="version-item-"]');
  }

  async inspectVersion(versionNumber: number): Promise<void> {
    await this.page.getByTestId(`version-summary-${versionNumber}`).click();
    const detail = this.page.getByTestId('version-detail');
    await expect(detail.getByRole('heading', { name: `Version ${versionNumber}` })).toBeVisible();
  }

  async restoreVersion(versionNumber: number, summary: string): Promise<void> {
    const versionItem = this.page.getByTestId(`version-item-${versionNumber}`);
    await versionItem.getByRole('button', { name: 'Restore' }).click();
    const dialog = this.page.getByRole('dialog', { name: 'Restore version' });
    await dialog
      .locator('label.form-field', { hasText: 'Change summary' })
      .locator('input')
      .fill(summary);
    await dialog.getByRole('button', { name: 'Restore version' }).click();
    await expect(dialog).toBeHidden();
    await this.expectSaved();
  }

  entryIdFromUrl(): string {
    const match = this.page.url().match(/\/content\/[^/]+\/([0-9a-f-]{36})(?:\/|$|\?)/i);
    if (!match?.[1]) {
      throw new Error(`Expected an entry edit URL with a GUID, got ${this.page.url()}`);
    }
    return match[1];
  }
}
