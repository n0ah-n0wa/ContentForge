import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import MediaFieldSelector from '@/components/media/MediaFieldSelector.vue';
import * as mediaApi from '@/api/media';

vi.mock('@/components/media/MediaPickerDialog.vue', () => ({
  default: {
    name: 'MediaPickerDialog',
    template: '<div />',
  },
}));

describe('MediaFieldSelector', () => {
  beforeEach(() => {
    vi.spyOn(mediaApi, 'getMedia').mockResolvedValue({
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
    });
  });

  it('loads and displays the selected media asset', async () => {
    const wrapper = mount(MediaFieldSelector, {
      props: {
        modelValue: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      },
      global: {
        stubs: {
          AppButton: { template: '<button><slot /></button>' },
        },
      },
    });

    await flushPromises();

    expect(mediaApi.getMedia).toHaveBeenCalledWith('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    expect(wrapper.text()).toContain('Logo');
    expect(wrapper.text()).toContain('Choose media asset');
  });
});
