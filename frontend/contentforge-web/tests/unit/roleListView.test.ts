import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import RoleListView from '@/views/roles/RoleListView.vue';
import * as rolesApi from '@/api/roles';
import { createTestJwt, adminPermissions, viewerPermissions } from '../helpers/auth';
import { useAuthStore } from '@/stores/authStore';

describe('RoleListView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.spyOn(rolesApi, 'listRoles').mockResolvedValue([
      {
        name: 'Administrator',
        permissions: ['user.read', 'user.create', 'content.read'],
      },
      {
        name: 'Viewer',
        permissions: ['content.read'],
      },
    ]);
  });

  it('loads role permissions for administrators', async () => {
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

    const wrapper = mount(RoleListView, {
      global: {
        stubs: {
          AppSpinner: true,
        },
      },
    });

    await flushPromises();

    expect(rolesApi.listRoles).toHaveBeenCalledOnce();
    expect(wrapper.text()).toContain('Administrator');
    expect(wrapper.text()).toContain('user.read');
  });

  it('shows access restriction without user.read permission', async () => {
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

    const wrapper = mount(RoleListView, {
      global: {
        stubs: {
          AppSpinner: true,
        },
      },
    });

    await flushPromises();

    expect(rolesApi.listRoles).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Access restricted');
  });
});
