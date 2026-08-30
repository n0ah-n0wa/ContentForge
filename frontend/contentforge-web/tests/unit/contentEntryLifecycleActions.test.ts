import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import ContentEntryLifecycleActions from '@/components/content-entries/ContentEntryLifecycleActions.vue';
import * as contentApi from '@/api/content';
import { ContentStatus } from '@/types/contentEntries';
import { createTestJwt, adminPermissions } from '../helpers/auth';
import { useAuthStore } from '@/stores/authStore';

const entry = {
  id: '33333333-3333-3333-3333-333333333333',
  contentTypeId: '22222222-2222-2222-2222-222222222222',
  slug: 'hello-world',
  status: ContentStatus.InReview,
  draftData: { title: 'Hello' },
  publishedData: null,
  currentVersion: 2,
  concurrencyToken: 3,
  createdBy: '11111111-1111-1111-1111-111111111111',
  updatedBy: '11111111-1111-1111-1111-111111111111',
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-02T00:00:00.000Z',
  publishedAt: null,
  publishedBy: null,
  isDeleted: false,
};

function seedSession(): void {
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
}

describe('ContentEntryLifecycleActions', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    seedSession();
    vi.restoreAllMocks();
  });

  it('shows publish action for in-review entries when user can publish', () => {
    const wrapper = mount(ContentEntryLifecycleActions, {
      props: {
        entry,
        concurrencyToken: entry.concurrencyToken,
      },
      global: {
        stubs: {
          AppButton: { template: '<button @click="$emit(\'click\')"><slot /></button>' },
        },
      },
    });

    expect(wrapper.text()).toContain('Publish');
    expect(wrapper.text()).toContain('Withdraw from review');
  });

  it('requires confirmation and change summary before publishing', async () => {
    vi.spyOn(contentApi, 'publishContentEntry').mockResolvedValue({
      ...entry,
      status: ContentStatus.Published,
      concurrencyToken: 4,
    });

    const wrapper = mount(ContentEntryLifecycleActions, {
      props: {
        entry,
        concurrencyToken: entry.concurrencyToken,
      },
    });

    const publishButton = wrapper
      .findAll('button')
      .find((button) => button.text() === 'Publish');
    expect(publishButton).toBeDefined();
    await publishButton!.trigger('click');
    expect(wrapper.text()).toContain('Publish "hello-world"');

    const confirmButton = wrapper
      .findAll('button')
      .find((button) => button.text() === 'Confirm');
    expect(confirmButton?.attributes('disabled')).toBeDefined();

    await wrapper.find('input[placeholder="Describe why this action is being taken"]').setValue(
      'Ready to go live',
    );
    await confirmButton!.trigger('click');
    await flushPromises();

    expect(contentApi.publishContentEntry).toHaveBeenCalledWith(entry.id, {
      changeSummary: 'Ready to go live',
      concurrencyToken: 3,
    });
  });
});
