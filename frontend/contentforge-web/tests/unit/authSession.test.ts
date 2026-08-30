import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { configureApiAuthSession } from '@/api/authSession';
import { apiRequest } from '@/api/client';
import { useAuthStore } from '@/stores/authStore';
import { adminPermissions, createTestJwt } from '../helpers/auth';

describe('api auth session', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it('notifies when an expired session cannot be refreshed', async () => {
    const onUnauthorized = vi.fn();
    const authStore = useAuthStore();
    authStore.applyLoginResult({
      userId: '11111111-1111-1111-1111-111111111111',
      email: 'admin@contentforge.test',
      displayName: 'Admin',
      role: 0,
      accessToken: createTestJwt({
        sub: '11111111-1111-1111-1111-111111111111',
        permission: adminPermissions,
        exp: Math.floor(Date.now() / 1000) - 60,
      }),
      accessTokenExpiresAt: new Date(Date.now() - 60_000).toISOString(),
      refreshToken: null,
      refreshTokenExpiresAt: null,
    });

    configureApiAuthSession({
      ensureSession: () => authStore.ensureSession(),
      onUnauthorized,
    });

    vi.spyOn(globalThis, 'fetch');

    await expect(apiRequest('/api/v1/content-types')).rejects.toMatchObject({
      status: 401,
    });

    expect(onUnauthorized).toHaveBeenCalledWith('expired');
    expect(fetch).not.toHaveBeenCalled();
  });
});
