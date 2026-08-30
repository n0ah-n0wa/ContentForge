import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import ContentEntryEditView from '@/views/content-entries/ContentEntryEditView.vue';
import * as contentApi from '@/api/content';
import * as contentTypesApi from '@/api/contentTypes';
import { ContentStatus } from '@/types/contentEntries';
import { FieldType, createDefaultFieldConfiguration } from '@/types/contentTypes';
import { ApiError } from '@/api/errors';
import { createTestJwt, adminPermissions } from '../helpers/auth';
import { useAuthStore } from '@/stores/authStore';

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

vi.mock('vue-router', () => ({
  useRoute: () => ({
    params: {
      contentTypeSlug: 'article',
      entryId: '33333333-3333-3333-3333-333333333333',
    },
  }),
  useRouter: () => ({
    replace: vi.fn(),
  }),
  onBeforeRouteLeave: vi.fn(),
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
  fields: [
    {
      id: '44444444-4444-4444-4444-444444444444',
      name: 'title',
      fieldType: FieldType.Text,
      displayName: 'Title',
      sortOrder: 1,
      configuration: {
        ...createDefaultFieldConfiguration(FieldType.Text),
        isRequired: true,
      },
    },
  ],
};

const entry = {
  id: '33333333-3333-3333-3333-333333333333',
  contentTypeId: contentType.id,
  slug: 'hello-world',
  status: ContentStatus.Draft,
  draftData: { title: 'Hello' },
  publishedData: null,
  currentVersion: 3,
  concurrencyToken: 4,
  createdBy: '11111111-1111-1111-1111-111111111111',
  updatedBy: '11111111-1111-1111-1111-111111111111',
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-02T00:00:00.000Z',
  publishedAt: null,
  publishedBy: null,
  isDeleted: false,
};

describe('ContentEntryEditView integration', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    seedSession();

    vi.spyOn(contentApi, 'getContentEntry').mockResolvedValue(entry);
    vi.spyOn(contentTypesApi, 'getContentType').mockResolvedValue(contentType);
  });

  it('loads entry metadata and dynamic fields', async () => {
    const wrapper = mount(ContentEntryEditView, {
      global: {
        stubs: {
          AppButton: { template: '<button><slot /></button>' },
          AppSpinner: true,
        },
      },
    });

    await flushPromises();

    expect(contentApi.getContentEntry).toHaveBeenCalledOnce();
    expect(wrapper.text()).toContain('Article');
    expect(wrapper.text()).toContain('Draft');
    expect(wrapper.text()).toContain('3');
    expect((wrapper.findAll('input[type="text"]')[0]?.element as HTMLInputElement).value).toBe(
      'hello-world',
    );
    const titleInput = wrapper
      .findAll('input[type="text"]')
      .find((input) => (input.element as HTMLInputElement).value === 'Hello');
    expect(titleInput).toBeDefined();
  });

  it('shows concurrency conflict panel when save returns 409', async () => {
    const updateSpy = vi.spyOn(contentApi, 'updateContentEntry').mockRejectedValue(
      new ApiError(409, {
        status: 409,
        title: 'Conflict',
        detail: 'The resource was modified by another user.',
        expectedVersion: 4,
        actualVersion: 5,
        updatedAt: '2026-01-03T00:00:00.000Z',
      }),
    );

    const wrapper = mount(ContentEntryEditView, {
      global: {
        stubs: {
          AppButton: { template: '<button><slot /></button>' },
          AppSpinner: true,
        },
      },
    });

    await flushPromises();

    await wrapper.find('input[placeholder="Describe what changed in this draft save"]').setValue(
      'Updated title',
    );

    const titleInput = wrapper
      .findAll('input[type="text"]')
      .find((input) => (input.element as HTMLInputElement).value === 'Hello');
    expect(titleInput).toBeDefined();
    await titleInput!.setValue('Updated');

    const saveButton = wrapper
      .findAll('button')
      .find((button) => button.text() === 'Save draft');
    expect(saveButton).toBeDefined();
    await saveButton!.trigger('click');

    await flushPromises();

    expect(updateSpy).toHaveBeenCalled();
    expect(wrapper.text()).toContain('Concurrency conflict');
    expect(wrapper.text()).toContain('Reload latest');
  });
});
