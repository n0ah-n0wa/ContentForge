import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { createContentPreviewToken, getContentPreview } from '@/api/content';
import { setAccessTokenProvider, setRefreshHandler } from '@/api/client';

describe('content preview api', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    setAccessTokenProvider(() => 'admin-token');
    setRefreshHandler(async () => false);
    vi.restoreAllMocks();
  });

  it('creates preview tokens with authenticated requests', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          token: 'preview-token-value',
          expiresAt: '2026-01-01T01:00:00.000Z',
          previewPath: '/api/v1/content/preview/preview-token-value',
        }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        },
      ),
    );

    const result = await createContentPreviewToken('33333333-3333-3333-3333-333333333333');

    expect(result.previewPath).toContain('preview-token-value');
    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect((init.headers as Headers).get('Authorization')).toBe('Bearer admin-token');
  });

  it('loads previews without attaching admin credentials', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          contentTypeSlug: 'articles',
          slug: 'draft-entry',
          status: 0,
          data: { title: 'Draft headline' },
          fields: [{ name: 'title', displayName: 'Title', fieldType: 0, sortOrder: 1 }],
          expiresAt: '2026-01-01T01:00:00.000Z',
        }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        },
      ),
    );

    await getContentPreview('preview-token-value');

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect((init.headers as Headers).get('Authorization')).toBeNull();
  });
});
