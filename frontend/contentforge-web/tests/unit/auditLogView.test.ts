import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import AuditLogView from '@/views/audit/AuditLogView.vue';
import * as auditApi from '@/api/audit';
import { createTestJwt, adminPermissions } from '../helpers/auth';
import { useAuthStore } from '@/stores/authStore';

describe('AuditLogView', () => {
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

    vi.spyOn(auditApi, 'listAuditLogs').mockResolvedValue({
      items: [
        {
          id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
          timestamp: '2026-01-01T12:00:00.000Z',
          userId: '11111111-1111-1111-1111-111111111111',
          action: 9,
          entityType: 'ContentEntry',
          entityId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          metadata: '{"previousRole":"Viewer"}',
          ipAddress: '127.0.0.1',
          userAgent: 'unit-test-agent',
          correlationId: 'corr-123',
        },
      ],
      page: 1,
      pageSize: 25,
      totalItems: 1,
      totalPages: 1,
    });

    vi.spyOn(auditApi, 'getAuditLog');
  });

  it('loads audit events and shows event details', async () => {
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

    expect(auditApi.listAuditLogs).toHaveBeenCalledOnce();
    expect(wrapper.text()).toContain('Content published');
    expect(wrapper.text()).toContain('Apply');

    await wrapper.find('.audit-log-row__button').trigger('click');
    await flushPromises();

    expect(auditApi.getAuditLog).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Event details');
    expect(wrapper.text()).toContain('corr-123');
    expect(wrapper.text()).toContain('Viewer');
  });
});
