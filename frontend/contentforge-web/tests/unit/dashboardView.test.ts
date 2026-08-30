import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import DashboardView from '@/views/DashboardView.vue';
import * as dashboardApi from '@/api/dashboard';
import { ContentStatus } from '@/types/contentEntries';
import { createTestJwt, adminPermissions } from '../helpers/auth';
import { useAuthStore } from '@/stores/authStore';

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: vi.fn() }),
  RouterLink: {
    name: 'RouterLink',
    props: ['to'],
    template: '<a><slot /></a>',
  },
}));

describe('DashboardView', () => {
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

    vi.spyOn(dashboardApi, 'getDashboard').mockResolvedValue({
      contentStatistics: {
        totalContent: 12,
        draftCount: 5,
        inReviewCount: 2,
        publishedCount: 4,
        archivedCount: 1,
      },
      recentContent: [
        {
          id: '33333333-3333-3333-3333-333333333333',
          contentTypeId: '22222222-2222-2222-2222-222222222222',
          contentTypeSlug: 'article',
          contentTypeDisplayName: 'Article',
          slug: 'hello-world',
          status: ContentStatus.Draft,
          updatedAt: '2026-01-02T00:00:00.000Z',
        },
      ],
      recentActivity: [
        {
          id: '44444444-4444-4444-4444-444444444444',
          timestamp: '2026-01-02T00:00:00.000Z',
          action: 5,
          entityType: 'ContentEntry',
          entityId: '33333333-3333-3333-3333-333333333333',
          userId: '11111111-1111-1111-1111-111111111111',
        },
      ],
    });
  });

  it('loads backend statistics and recent sections', async () => {
    const wrapper = mount(DashboardView, {
      global: {
        stubs: {
          AppButton: { template: '<button><slot /></button>' },
          AppSpinner: true,
        },
      },
    });

    await flushPromises();

    expect(dashboardApi.getDashboard).toHaveBeenCalledOnce();
    expect(wrapper.text()).toContain('12');
    expect(wrapper.text()).toContain('Pending review');
    expect(wrapper.text()).toContain('hello-world');
    expect(wrapper.text()).toContain('Content created');
  });

  it('shows error state when dashboard load fails', async () => {
    vi.mocked(dashboardApi.getDashboard).mockRejectedValueOnce(new Error('Network error'));

    const wrapper = mount(DashboardView, {
      global: {
        stubs: {
          AppButton: { template: '<button><slot /></button>' },
          AppSpinner: true,
        },
      },
    });

    await flushPromises();

    expect(wrapper.text()).toContain('Dashboard unavailable');
  });
});
