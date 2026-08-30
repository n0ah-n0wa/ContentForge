import { beforeEach, describe, expect, it } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import router from '@/router';
import { useAuthStore } from '@/stores/authStore';
import { adminPermissions, createTestJwt, viewerPermissions } from '../helpers/auth';

function seedAuthenticatedSession(permissions: string[]): void {
  const authStore = useAuthStore();
  authStore.applyLoginResult({
    userId: '11111111-1111-1111-1111-111111111111',
    email: 'admin@contentforge.test',
    displayName: 'Admin',
    role: 0,
    accessToken: createTestJwt({
      sub: '11111111-1111-1111-1111-111111111111',
      permission: permissions,
      exp: Math.floor(Date.now() / 1000) + 3600,
    }),
    accessTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
    refreshToken: 'refresh-token',
    refreshTokenExpiresAt: new Date(Date.now() + 86_400_000).toISOString(),
  });
  authStore.$patch({ isInitialized: true });
}

describe('router guards', () => {
  beforeEach(async () => {
    setActivePinia(createPinia());
    sessionStorage.clear();
    await router.push('/__test-reset__');
    await router.isReady();
  });

  it('redirects unauthenticated users to login for protected routes', async () => {
    await router.push('/dashboard');

    expect(router.currentRoute.value.name).toBe('login');
    expect(router.currentRoute.value.query.redirect).toBe('/dashboard');
  });

  it('redirects authenticated users away from login', async () => {
    seedAuthenticatedSession(adminPermissions);

    await router.push('/login');

    expect(router.currentRoute.value.name).toBe('dashboard');
  });

  it('redirects expired sessions to login with a reason', async () => {
    const authStore = useAuthStore();
    authStore.$patch({ sessionStatus: 'expired', isInitialized: true });

    await router.push('/dashboard');

    expect(router.currentRoute.value.name).toBe('login');
    expect(router.currentRoute.value.query.reason).toBe('expired');
  });

  it('blocks forbidden routes when permissions are missing', async () => {
    seedAuthenticatedSession(viewerPermissions);

    await router.push('/users');

    expect(router.currentRoute.value.name).toBe('access-denied');
  });

  it('allows protected routes when permissions are present', async () => {
    seedAuthenticatedSession(adminPermissions);

    await router.push('/users');

    expect(router.currentRoute.value.name).toBe('users');
  });
});
