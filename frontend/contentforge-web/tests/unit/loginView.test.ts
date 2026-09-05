import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { flushPromises, mount } from '@vue/test-utils';
import { createRouter, createWebHistory } from 'vue-router';
import LoginView from '@/views/auth/LoginView.vue';
import * as authApi from '@/api/auth';
import { useAuthStore } from '@/stores/authStore';
import { adminPermissions, createTestJwt } from '../helpers/auth';

vi.mock('@/api/auth', () => ({
  login: vi.fn(),
  refresh: vi.fn(),
  getCurrentUser: vi.fn(),
  logout: vi.fn(),
}));

describe('LoginView', () => {
  const router = createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/login', component: LoginView, name: 'login' },
      { path: '/dashboard', component: { template: '<div>Dashboard</div>' }, name: 'dashboard' },
    ],
  });

  beforeEach(async () => {
    setActivePinia(createPinia());
    sessionStorage.clear();
    vi.clearAllMocks();
    await router.push('/login');
    await router.isReady();
  });

  it('submits credentials and navigates after successful login', async () => {
    vi.mocked(authApi.login).mockResolvedValue({
      userId: '11111111-1111-1111-1111-111111111111',
      email: 'admin@contentforge.test',
      displayName: 'Admin',
      role: 0,
      accessToken: createTestJwt({
        sub: '11111111-1111-1111-1111-111111111111',
        permission: adminPermissions,
        exp: Math.floor(Date.now() / 1000) + 3600,
      }),
      accessTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      refreshToken: 'refresh-token',
      refreshTokenExpiresAt: new Date(Date.now() + 86_400_000).toISOString(),
    });

    const wrapper = mount(LoginView, {
      global: {
        plugins: [router],
      },
    });

    await wrapper.find('input[name="email"]').setValue('admin@contentforge.test');
    await wrapper.find('input[name="password"]').setValue('password');
    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(useAuthStore().isAuthenticated).toBe(true);
    expect(router.currentRoute.value.name).toBe('dashboard');
  });

  it('shows an error message for failed login', async () => {
    vi.mocked(authApi.login).mockRejectedValue(new Error('Authentication failed.'));

    const wrapper = mount(LoginView, {
      global: {
        plugins: [router],
      },
    });

    await wrapper.find('input[name="email"]').setValue('admin@contentforge.test');
    await wrapper.find('input[name="password"]').setValue('wrong-password');
    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(wrapper.text()).toContain('Sign in failed');
    expect(useAuthStore().isAuthenticated).toBe(false);
  });

  it('shows a session-expired notice when redirected after expiry', async () => {
    await router.push({ name: 'login', query: { reason: 'expired' } });

    const wrapper = mount(LoginView, {
      global: {
        plugins: [router],
      },
    });

    expect(wrapper.text()).toContain('Your session expired');
  });

  it('rejects sign-in for disabled users with a generic auth error', async () => {
    vi.mocked(authApi.login).mockRejectedValue(
      Object.assign(new Error('Invalid email or password.'), { status: 401 }),
    );

    const wrapper = mount(LoginView, {
      global: {
        plugins: [router],
      },
    });

    await wrapper.find('input[name="email"]').setValue('disabled@contentforge.test');
    await wrapper.find('input[name="password"]').setValue('SecurePassword123!');
    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(wrapper.text()).toContain('Sign in failed');
    expect(useAuthStore().isAuthenticated).toBe(false);
  });

  it('ignores open-redirect query values after successful login', async () => {
    vi.mocked(authApi.login).mockResolvedValue({
      userId: '11111111-1111-1111-1111-111111111111',
      email: 'admin@contentforge.test',
      displayName: 'Admin',
      role: 0,
      accessToken: createTestJwt({
        sub: '11111111-1111-1111-1111-111111111111',
        permission: adminPermissions,
        exp: Math.floor(Date.now() / 1000) + 3600,
      }),
      accessTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      refreshToken: 'refresh-token',
      refreshTokenExpiresAt: new Date(Date.now() + 86_400_000).toISOString(),
    });

    await router.push({ name: 'login', query: { redirect: 'https://evil.example/phish' } });

    const wrapper = mount(LoginView, {
      global: {
        plugins: [router],
      },
    });

    await wrapper.find('input[name="email"]').setValue('admin@contentforge.test');
    await wrapper.find('input[name="password"]').setValue('password');
    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(router.currentRoute.value.name).toBe('dashboard');
  });
});
