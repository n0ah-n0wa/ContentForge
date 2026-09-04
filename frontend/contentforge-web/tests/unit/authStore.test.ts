import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import * as authApi from '@/api/auth';
import { ApiError } from '@/api/errors';
import { describeAuthError, useAuthStore } from '@/stores/authStore';
import { adminPermissions, createTestJwt } from '../helpers/auth';

vi.mock('@/api/auth', () => ({
  login: vi.fn(),
  refresh: vi.fn(),
  getCurrentUser: vi.fn(),
  logout: vi.fn(),
}));

const loginResult = {
  userId: '11111111-1111-1111-1111-111111111111',
  email: 'admin@contentforge.test',
  displayName: 'Admin',
  role: 0 as const,
  accessToken: createTestJwt({
    sub: '11111111-1111-1111-1111-111111111111',
    permission: adminPermissions,
    exp: Math.floor(Date.now() / 1000) + 3600,
  }),
  accessTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
  refreshToken: 'refresh-token',
  refreshTokenExpiresAt: new Date(Date.now() + 86_400_000).toISOString(),
};

describe('authStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    sessionStorage.clear();
    vi.clearAllMocks();
  });

  it('stores session data after successful login', async () => {
    vi.mocked(authApi.login).mockResolvedValue(loginResult);

    const store = useAuthStore();
    await store.login('admin@contentforge.test', 'password');

    expect(store.isAuthenticated).toBe(true);
    expect(store.user?.email).toBe('admin@contentforge.test');
    expect(store.hasPermission('user.read')).toBe(true);
    expect(sessionStorage.getItem('contentforge.accessToken')).toBe(loginResult.accessToken);
    expect(sessionStorage.getItem('contentforge.refreshToken')).toBe('refresh-token');
  });

  it('does not persist credentials after login', async () => {
    vi.mocked(authApi.login).mockResolvedValue(loginResult);

    const store = useAuthStore();
    await store.login('admin@contentforge.test', 'password');

    expect(sessionStorage.getItem('contentforge.password')).toBeNull();
    expect(sessionStorage.getItem('contentforge.email')).toBeNull();
  });

  it('surfaces failed login without storing tokens', async () => {
    vi.mocked(authApi.login).mockRejectedValue(
      new ApiError(401, {
        status: 401,
        title: 'Unauthorized',
        detail: 'Authentication failed.',
      }),
    );

    const store = useAuthStore();
    await expect(store.login('admin@contentforge.test', 'wrong-password')).rejects.toThrow();

    expect(store.isAuthenticated).toBe(false);
    expect(sessionStorage.getItem('contentforge.accessToken')).toBeNull();
  });

  it('clears session on logout', async () => {
    vi.mocked(authApi.logout).mockResolvedValue(undefined);

    const store = useAuthStore();
    store.applyLoginResult(loginResult);

    await store.logout();

    expect(store.isAuthenticated).toBe(false);
    expect(sessionStorage.getItem('contentforge.accessToken')).toBeNull();
  });

  it('clears invalid sessions during initialization', async () => {
    sessionStorage.setItem('contentforge.accessToken', 'stale-token');
    sessionStorage.setItem(
      'contentforge.accessTokenExpiresAt',
      new Date(Date.now() + 3600000).toISOString(),
    );
    vi.mocked(authApi.getCurrentUser).mockRejectedValue(new Error('401'));

    const store = useAuthStore();
    await store.initialize();

    expect(store.isAuthenticated).toBe(false);
    expect(store.sessionStatus).toBe('expired');
  });

  it('refreshes expired sessions when a refresh token is available', async () => {
    vi.mocked(authApi.refresh).mockResolvedValue(loginResult);

    const store = useAuthStore();
    store.applyLoginResult({
      ...loginResult,
      accessTokenExpiresAt: new Date(Date.now() - 60_000).toISOString(),
    });

    const refreshed = await store.ensureSession();

    expect(refreshed).toBe(true);
    expect(store.isAuthenticated).toBe(true);
    expect(authApi.refresh).toHaveBeenCalledOnce();
  });

  it('treats a non-expired access token as a valid session before initialize completes', async () => {
    sessionStorage.setItem('contentforge.accessToken', loginResult.accessToken);
    sessionStorage.setItem('contentforge.refreshToken', loginResult.refreshToken);
    sessionStorage.setItem('contentforge.accessTokenExpiresAt', loginResult.accessTokenExpiresAt);

    const store = useAuthStore();

    expect(store.sessionStatus).toBe('anonymous');
    expect(store.user).toBeNull();
    await expect(store.ensureSession()).resolves.toBe(true);
    expect(authApi.refresh).not.toHaveBeenCalled();
  });

  it('denies routes when required permissions are missing from the token', () => {
    const store = useAuthStore();
    store.applyLoginResult({
      ...loginResult,
      accessToken: createTestJwt({
        sub: loginResult.userId,
        permission: ['content.read'],
        exp: Math.floor(Date.now() / 1000) + 3600,
      }),
    });

    expect(store.canAccess(['user.read'])).toBe(false);
    expect(store.canAccess(['content.read'])).toBe(true);
  });
});

describe('describeAuthError', () => {
  it('masks unauthorized login failures', () => {
    const error = new ApiError(401, {
      status: 401,
      title: 'Unauthorized',
      detail: 'Authentication failed.',
    });

    expect(describeAuthError(error)).toBe('Invalid email or password.');
  });
});
