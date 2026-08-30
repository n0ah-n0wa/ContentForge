import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import ContentEntryListView from '@/views/content-entries/ContentEntryListView.vue';
import * as contentApi from '@/api/content';
import * as contentTypesApi from '@/api/contentTypes';
import { ContentStatus } from '@/types/contentEntries';
import { createTestJwt, adminPermissions } from '../helpers/auth';
import { useAuthStore } from '@/stores/authStore';

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { contentTypeSlug: 'article' } }),
  useRouter: () => ({ push: vi.fn() }),
}));

const contentType = {
  id: '22222222-2222-2222-2222-222222222222',
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
};

describe('ContentEntryListView', () => {
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

    vi.spyOn(contentTypesApi, 'listContentTypes').mockResolvedValue({
      items: [contentType],
      page: 1,
      pageSize: 100,
      totalItems: 1,
      totalPages: 1,
    });

    vi.spyOn(contentApi, 'listContentEntries').mockResolvedValue({
      items: [
        {
          id: '33333333-3333-3333-3333-333333333333',
          contentTypeId: contentType.id,
          slug: 'hello-world',
          status: ContentStatus.Draft,
          draftData: {},
          publishedData: null,
          currentVersion: 1,
          concurrencyToken: 1,
          createdBy: '11111111-1111-1111-1111-111111111111',
          updatedBy: '11111111-1111-1111-1111-111111111111',
          createdAt: '2026-01-01T00:00:00.000Z',
          updatedAt: '2026-01-02T00:00:00.000Z',
          publishedAt: null,
          publishedBy: null,
          isDeleted: false,
        },
      ],
      page: 1,
      pageSize: 25,
      totalItems: 1,
      totalPages: 1,
    });
  });

  it('loads paginated entries with filters applied', async () => {
    const wrapper = mount(ContentEntryListView, {
      global: {
        stubs: {
          AppButton: { template: '<button><slot /></button>' },
          AppSpinner: true,
        },
      },
    });

    await flushPromises();

    expect(contentApi.listContentEntries).toHaveBeenCalledWith(
      expect.objectContaining({
        contentTypeId: contentType.id,
        page: 1,
        pageSize: 25,
        sortBy: 'updatedAt',
        sortDirection: 'desc',
      }),
    );
    expect(wrapper.text()).toContain('hello-world');
    expect(wrapper.text()).toContain('Showing page 1 of 1');
  });
});
