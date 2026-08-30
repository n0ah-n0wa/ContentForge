import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import UserListView from '@/views/users/UserListView.vue';
import { createTestJwt, viewerPermissions } from '../helpers/auth';
import { useAuthStore } from '@/stores/authStore';
import * as usersApi from '@/api/users';

describe('User administration access', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.spyOn(usersApi, 'listUsers');
  });

  it('shows access restriction for users without user.read', async () => {
    const authStore = useAuthStore();
    authStore.applyLoginResult({
      userId: '22222222-2222-2222-2222-222222222222',
      email: 'viewer@contentforge.test',
      displayName: 'Viewer',
      role: 3,
      accessToken: createTestJwt({
        sub: '22222222-2222-2222-2222-222222222222',
        permission: viewerPermissions,
        exp: Math.floor(Date.now() / 1000) + 3600,
      }),
      accessTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      refreshToken: 'refresh-token',
      refreshTokenExpiresAt: new Date(Date.now() + 86_400_000).toISOString(),
    });

    const wrapper = mount(UserListView, {
      global: {
        stubs: {
          AppButton: { template: '<button><slot /></button>' },
          AppSpinner: true,
          PaginationBar: true,
        },
      },
    });

    await flushPromises();

    expect(usersApi.listUsers).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Access restricted');
    expect(wrapper.text()).not.toContain('New user');
  });
});
