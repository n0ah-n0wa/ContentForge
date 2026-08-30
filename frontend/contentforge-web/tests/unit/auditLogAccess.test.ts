import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import AuditLogView from '@/views/audit/AuditLogView.vue';
import * as auditApi from '@/api/audit';
import { createTestJwt, viewerPermissions } from '../helpers/auth';
import { useAuthStore } from '@/stores/authStore';

describe('Audit log access', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.spyOn(auditApi, 'listAuditLogs');
  });

  it('shows access restriction without audit.read permission', async () => {
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

    const wrapper = mount(AuditLogView, {
      global: {
        stubs: {
          AppButton: { template: '<button><slot /></button>' },
          AppSpinner: true,
          PaginationBar: true,
        },
      },
    });

    await flushPromises();

    expect(auditApi.listAuditLogs).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Access restricted');
  });
});
