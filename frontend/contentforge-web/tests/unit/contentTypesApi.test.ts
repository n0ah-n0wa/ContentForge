import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { listContentTypes } from '@/api/contentTypes';
import { setAccessTokenProvider, setRefreshHandler } from '@/api/client';

describe('contentTypes api', () => {
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
          pageSize: 10,
          totalItems: 0,
          totalPages: 0,
        }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        },
      ),
    );

    await listContentTypes({
      page: 2,
      pageSize: 25,
      sortBy: 'name',
      sortDirection: 'asc',
      isActive: true,
      search: 'article',
    });

    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/v1/content-types?');
    expect(url).toContain('page=2');
    expect(url).toContain('pageSize=25');
    expect(url).toContain('sortBy=name');
    expect(url).toContain('sortDirection=asc');
    expect(url).toContain('isActive=true');
    expect(url).toContain('search=article');
  });
});
