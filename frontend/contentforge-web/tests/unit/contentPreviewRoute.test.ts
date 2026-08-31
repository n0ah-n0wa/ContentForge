import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import router from '@/router';
import { useAuthStore } from '@/stores/authStore';

describe('content preview route', () => {
  beforeEach(async () => {
    setActivePinia(createPinia());
    sessionStorage.clear();
    await router.push('/__test-reset__');
    await router.isReady();
  });

  it('allows unauthenticated access to preview pages', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          contentTypeSlug: 'articles',
          slug: 'draft-entry',
          status: 0,
          data: { title: 'Draft headline' },
          fields: [{ name: 'title', displayName: 'Title', fieldType: 0, sortOrder: 1 }],
          expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
        }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        },
      ),
    );

    await router.push('/preview/preview-token-value');

    expect(router.currentRoute.value.name).toBe('content-preview');
    expect(useAuthStore().isAuthenticated).toBe(false);
  });
});
