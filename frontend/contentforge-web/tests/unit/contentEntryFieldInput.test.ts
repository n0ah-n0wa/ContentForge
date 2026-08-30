import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import ContentEntryFieldInput from '@/components/content-entries/ContentEntryFieldInput.vue';
import { FieldType, createDefaultFieldConfiguration } from '@/types/contentTypes';

const baseField = {
  id: '11111111-1111-1111-1111-111111111111',
  name: 'title',
  displayName: 'Title',
  sortOrder: 1,
  configuration: createDefaultFieldConfiguration(FieldType.Text),
};

describe('ContentEntryFieldInput', () => {
  it('renders a text input for text fields', async () => {
    const wrapper = mount(ContentEntryFieldInput, {
      props: {
        field: { ...baseField, fieldType: FieldType.Text },
        modelValue: 'Hello',
      },
    });

    const input = wrapper.find('input[type="text"]');
    expect(input.exists()).toBe(true);
    expect((input.element as HTMLInputElement).value).toBe('Hello');

    await input.setValue('Updated');
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['Updated']);
  });

  it('renders select options from field configuration', () => {
    const wrapper = mount(ContentEntryFieldInput, {
      props: {
        field: {
          ...baseField,
          name: 'category',
          fieldType: FieldType.Select,
          configuration: {
            ...createDefaultFieldConfiguration(FieldType.Select),
            options: ['News', 'Blog'],
          },
        },
        modelValue: '',
      },
    });

    const options = wrapper.findAll('select option');
    expect(options.some((option) => option.text() === 'News')).toBe(true);
    expect(options.some((option) => option.text() === 'Blog')).toBe(true);
  });

  it('shows validation errors', () => {
    const wrapper = mount(ContentEntryFieldInput, {
      props: {
        field: { ...baseField, fieldType: FieldType.LongText },
        modelValue: '',
        errors: ['Title is required.'],
      },
    });

    expect(wrapper.text()).toContain('Title is required.');
    expect(wrapper.classes()).toContain('entry-field--invalid');
  });
});
