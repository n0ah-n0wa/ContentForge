import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { createRouter, createWebHistory } from 'vue-router';
import UserEditView from '@/views/users/UserEditView.vue';
import * as usersApi from '@/api/users';
import * as rolesApi from '@/api/roles';
import { createTestJwt, adminPermissions } from '../helpers/auth';
import { useAuthStore } from '@/stores/authStore';

describe('UserEditView', () => {
  const targetUserId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
  let router: ReturnType<typeof createRouter>;

  beforeEach(async () => {
    vi.clearAllMocks();
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

    vi.spyOn(rolesApi, 'listRoles').mockResolvedValue([
      {
        name: 'Editor',
        permissions: ['content.read', 'content.publish'],
      },
      {
        name: 'Viewer',
        permissions: ['content.read'],
      },
      {
        name: 'Author',
        permissions: ['content.read'],
      },
    ]);

    vi.spyOn(usersApi, 'getUser').mockResolvedValue({
      id: targetUserId,
      email: 'author@contentforge.test',
      displayName: 'Author User',
      isActive: true,
      role: 'Author',
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-01T00:00:00.000Z',
      lastLoginAt: null,
    });

    vi.spyOn(usersApi, 'updateUser').mockResolvedValue({
      id: targetUserId,
      email: 'author@contentforge.test',
      displayName: 'Author User',
      isActive: true,
      role: 'Editor',
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-02T00:00:00.000Z',
      lastLoginAt: null,
    });

    vi.spyOn(usersApi, 'disableUser').mockResolvedValue({
      id: targetUserId,
      email: 'author@contentforge.test',
      displayName: 'Author User',
      isActive: false,
      role: 'Author',
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-02T00:00:00.000Z',
      lastLoginAt: null,
    });

    router = createRouter({
      history: createWebHistory(),
      routes: [
        { path: '/users/:id', name: 'user-edit', component: UserEditView },
        { path: '/users', name: 'users', component: { template: '<div />' } },
      ],
    });

    await router.push({ name: 'user-edit', params: { id: targetUserId } });
    await router.isReady();
  });

  it('updates user role', async () => {
    const wrapper = mount(UserEditView, {
      global: {
        plugins: [router],
        stubs: {
          AppButton: { template: '<button type="submit"><slot /></button>' },
          AppSpinner: true,
          ConfirmActionPanel: true,
          RolePermissionsPanel: { template: '<div class="role-permissions-stub" />' },
        },
      },
    });

    await flushPromises();
    expect(wrapper.find('form').exists()).toBe(true);

    const roleSelect = wrapper.find('select');
    expect(roleSelect.exists()).toBe(true);
    await roleSelect.setValue('Editor');
    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(usersApi.updateUser).toHaveBeenCalledWith(targetUserId, {
      displayName: 'Author User',
      role: 'Editor',
    });
  }, 15_000);

  it('shows disabled status after disable action', async () => {
    vi.spyOn(usersApi, 'getUser').mockResolvedValue({
      id: targetUserId,
      email: 'author@contentforge.test',
      displayName: 'Author User',
      isActive: false,
      role: 'Author',
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-02T00:00:00.000Z',
      lastLoginAt: null,
    });

    const wrapper = mount(UserEditView, {
      global: {
        plugins: [router],
        stubs: {
          AppButton: { template: '<button><slot /></button>' },
          AppSpinner: true,
          ConfirmActionPanel: true,
          RolePermissionsPanel: { template: '<div />' },
        },
      },
    });

    await flushPromises();

    expect(wrapper.text()).toContain('Account disabled');
    expect(wrapper.text()).toContain('Disabled');
    expect(wrapper.text()).toContain('Activate user');
  }, 15_000);
});
