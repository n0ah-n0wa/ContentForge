import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import UserListView from '@/views/users/UserListView.vue';
import * as usersApi from '@/api/users';
import { createTestJwt, adminPermissions } from '../helpers/auth';
import { useAuthStore } from '@/stores/authStore';

describe('UserListView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    const authStore = useAuthStore();
    authStore.applyLoginResult({
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

    vi.spyOn(usersApi, 'listUsers').mockResolvedValue({
      items: [
        {
          id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          email: 'editor@contentforge.test',
          displayName: 'Editor User',
          isActive: true,
          role: 'Editor',
          createdAt: '2026-01-01T00:00:00.000Z',
          updatedAt: '2026-01-01T00:00:00.000Z',
          lastLoginAt: '2026-01-02T00:00:00.000Z',
        },
      ],
      page: 1,
      pageSize: 25,
      totalItems: 1,
      totalPages: 1,
    });
  });

  it('loads users for administrators', async () => {
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

    expect(usersApi.listUsers).toHaveBeenCalledOnce();
    expect(wrapper.text()).toContain('editor@contentforge.test');
    expect(wrapper.text()).toContain('New user');
  });
});
