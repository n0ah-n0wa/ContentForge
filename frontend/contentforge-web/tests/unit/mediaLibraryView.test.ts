import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import MediaLibraryView from '@/views/media/MediaLibraryView.vue';
import MediaUploadPanel from '@/components/media/MediaUploadPanel.vue';
import * as mediaApi from '@/api/media';
import { createTestJwt, adminPermissions } from '../helpers/auth';
import { useAuthStore } from '@/stores/authStore';

describe('MediaLibraryView', () => {
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

    vi.spyOn(mediaApi, 'listMedia').mockResolvedValue({
      items: [
        {
          id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          fileName: 'abc.png',
          originalFileName: 'logo.png',
          contentType: 'image/png',
          size: 1024,
          url: '/media-files/media/abc.png',
          width: 100,
          height: 100,
          altText: 'Logo',
          title: 'Logo',
          description: null,
          uploadedBy: '11111111-1111-1111-1111-111111111111',
          uploadedAt: '2026-01-01T00:00:00.000Z',
          isDeleted: false,
        },
      ],
      page: 1,
      pageSize: 24,
      totalItems: 1,
      totalPages: 1,
    });
  });

  it('loads media assets in grid view', async () => {
    const wrapper = mount(MediaLibraryView, {
      global: {
        stubs: {
          AppButton: { template: '<button><slot /></button>' },
          AppSpinner: true,
          PaginationBar: true,
        },
      },
    });

    await flushPromises();

    expect(mediaApi.listMedia).toHaveBeenCalledOnce();
    expect(wrapper.text()).toContain('Logo');
    expect(wrapper.findComponent(MediaUploadPanel).exists()).toBe(true);
    expect(wrapper.text()).toContain('Upload media');
  });
});
