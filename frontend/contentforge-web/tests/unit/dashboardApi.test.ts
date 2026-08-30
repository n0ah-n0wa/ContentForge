import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { setAccessTokenProvider, setRefreshHandler } from '@/api/client';

describe('dashboard api', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    setAccessTokenProvider(() => 'token');
    setRefreshHandler(async () => false);
    vi.restoreAllMocks();
  });

  it('requests dashboard endpoint', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          contentStatistics: {
            totalContent: 5,
            draftCount: 2,
            inReviewCount: 1,
            publishedCount: 1,
            archivedCount: 1,
          },
          recentContent: [],
          recentActivity: [],
        }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        },
      ),
    );

    const { getDashboard } = await import('@/api/dashboard');
    const result = await getDashboard();

    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/v1/dashboard');
    expect(result.contentStatistics?.totalContent).toBe(5);
  });
});
