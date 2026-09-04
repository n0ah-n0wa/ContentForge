import { expect } from '@playwright/test';
import { ApiClient } from './apiClient';
import { e2eEnv } from './env';

export interface PublicContentPayload {
  contentTypeSlug: string;
  slug: string;
  data: Record<string, unknown>;
  publishedAt: string;
}

/** Anonymous public API client (no auth header). */
export function createPublicApi(): ApiClient {
  return new ApiClient(e2eEnv.apiBaseURL, null);
}

export async function getPublicContent(
  contentTypeSlug: string,
  entrySlug: string,
): Promise<{ status: number; body: PublicContentPayload | null }> {
  const client = createPublicApi();
  const response = await client.requestRaw<PublicContentPayload>(
    'GET',
    `/api/v1/public/${encodeURIComponent(contentTypeSlug)}/${encodeURIComponent(entrySlug)}`,
    undefined,
    { auth: false },
  );
  return { status: response.status, body: response.body };
}

/** Polls until published content is visible (covers cache invalidation lag). */
export async function expectPublicVisible(
  contentTypeSlug: string,
  entrySlug: string,
  expectedTitle?: string,
): Promise<PublicContentPayload> {
  let lastBody: PublicContentPayload | null = null;

  await expect
    .poll(
      async () => {
        const result = await getPublicContent(contentTypeSlug, entrySlug);
        lastBody = result.body;
        if (result.status !== 200 || !result.body) {
          return `status=${result.status}`;
        }
        if (expectedTitle !== undefined && result.body.data.title !== expectedTitle) {
          return `title=${JSON.stringify(result.body.data.title)}`;
        }
        return 'ok';
      },
      { timeout: 15_000, intervals: [200, 400, 800] },
    )
    .toBe('ok');

  if (!lastBody) {
    throw new Error(`Public content ${contentTypeSlug}/${entrySlug} missing after poll`);
  }
  return lastBody;
}

/** Polls until published content is gone (covers cache invalidation lag). */
export async function expectPublicInvisible(
  contentTypeSlug: string,
  entrySlug: string,
): Promise<void> {
  await expect
    .poll(
      async () => {
        const result = await getPublicContent(contentTypeSlug, entrySlug);
        return result.status;
      },
      { timeout: 15_000, intervals: [200, 400, 800] },
    )
    .toBe(404);
}
