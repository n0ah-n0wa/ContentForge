import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { listContentEntries, updateContentEntry } from '@/api/content';
import { setAccessTokenProvider, setRefreshHandler } from '@/api/client';

describe('contentEntries api', () => {
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

    await listContentEntries({
      page: 2,
      pageSize: 25,
      contentTypeId: '22222222-2222-2222-2222-222222222222',
      status: 'Draft',
      search: 'hello',
    });

    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/v1/content?');
    expect(url).toContain('contentTypeId=22222222-2222-2222-2222-222222222222');
    expect(url).toContain('status=Draft');
    expect(url).toContain('search=hello');
  });

  it('sends update payload with concurrency token', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          id: '33333333-3333-3333-3333-333333333333',
          contentTypeId: '22222222-2222-2222-2222-222222222222',
          slug: 'entry',
          status: 0,
          draftData: { title: 'Updated' },
          publishedData: null,
          currentVersion: 1,
          concurrencyToken: 2,
          createdBy: '11111111-1111-1111-1111-111111111111',
          updatedBy: '11111111-1111-1111-1111-111111111111',
          createdAt: '2026-01-01T00:00:00.000Z',
          updatedAt: '2026-01-02T00:00:00.000Z',
          publishedAt: null,
          publishedBy: null,
          isDeleted: false,
        }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        },
      ),
    );

    await updateContentEntry('33333333-3333-3333-3333-333333333333', {
      slug: 'entry',
      data: { title: 'Updated' },
      changeSummary: 'Updated title',
      concurrencyToken: 1,
    });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(init.method).toBe('PUT');
    expect(JSON.parse(String(init.body))).toEqual({
      slug: 'entry',
      data: { title: 'Updated' },
      changeSummary: 'Updated title',
      concurrencyToken: 1,
    });
  });
});
