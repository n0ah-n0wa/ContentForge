import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { flushPromises, mount } from '@vue/test-utils';
import ContentTypeListView from '@/views/content-types/ContentTypeListView.vue';
import * as contentTypesApi from '@/api/contentTypes';
import { useAuthStore } from '@/stores/authStore';
import { adminPermissions, createTestJwt } from '../helpers/auth';

vi.mock('@/api/contentTypes', () => ({
  listContentTypes: vi.fn(),
}));

describe('ContentTypeListView', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('loads and renders content types', async () => {
    vi.mocked(contentTypesApi.listContentTypes).mockResolvedValue({
      items: [
        {
          id: '11111111-1111-1111-1111-111111111111',
          name: 'Article',
          displayName: 'Article',
          description: null,
          slug: 'article',
          isActive: true,
          version: 1,
          createdBy: '11111111-1111-1111-1111-111111111111',
          updatedBy: '11111111-1111-1111-1111-111111111111',
          createdAt: '2026-01-01T00:00:00.000Z',
          updatedAt: '2026-01-01T00:00:00.000Z',
          fields: [],
        },
      ],
      page: 1,
      pageSize: 100,
      totalItems: 1,
      totalPages: 1,
    });

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

    const wrapper = mount(ContentTypeListView, {
      global: {
        stubs: {
          AppButton: true,
        },
      },
    });

    await flushPromises();

    expect(contentTypesApi.listContentTypes).toHaveBeenCalledOnce();
    expect(wrapper.text()).toContain('Article');
    expect(wrapper.find('table').exists()).toBe(true);
  });
});
