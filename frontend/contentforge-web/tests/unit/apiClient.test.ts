import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { apiRequest, setAccessTokenProvider, setRefreshHandler } from '@/api/client';

describe('apiRequest', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    setAccessTokenProvider(() => 'test-token');
    setRefreshHandler(async () => false);
    vi.restoreAllMocks();
  });

  it('attaches authorization and parses json responses', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ ok: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    const result = await apiRequest<{ ok: boolean }>('/api/v1/auth/me');

    expect(result.ok).toBe(true);
    expect(fetchMock).toHaveBeenCalledOnce();

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = new Headers(init.headers);
    expect(headers.get('Authorization')).toBe('Bearer test-token');
  });

  it('throws ApiError for failed responses', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          title: 'Unauthorized',
          status: 401,
          detail: 'Authentication is required.',
        }),
        {
          status: 401,
          headers: { 'Content-Type': 'application/problem+json' },
        },
      ),
    );

    await expect(apiRequest('/api/v1/content-types')).rejects.toMatchObject({
      status: 401,
    });
  });
});
