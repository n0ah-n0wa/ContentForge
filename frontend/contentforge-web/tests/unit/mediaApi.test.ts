import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { listMedia } from '@/api/media';
import { setAccessTokenProvider, setRefreshHandler } from '@/api/client';

describe('media api', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    setAccessTokenProvider(() => 'token');
    setRefreshHandler(async () => false);
    vi.restoreAllMocks();
  });

  it('builds list query parameters', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          items: [],
          page: 1,
          pageSize: 24,
          totalItems: 0,
          totalPages: 0,
        }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        },
      ),
    );

    await listMedia({
      page: 2,
      pageSize: 24,
      search: 'logo',
      contentType: 'image/',
      sortBy: 'uploadedAt',
      sortDirection: 'desc',
    });

    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/v1/media?');
    expect(url).toContain('search=logo');
    expect(url).toContain('contentType=image%2F');
  });
});
