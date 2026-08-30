import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import FieldConfigurationForm from '@/components/content-types/FieldConfigurationForm.vue';
import { FieldType, createDefaultFieldConfiguration } from '@/types/contentTypes';

const relationTargets = [
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
];

describe('FieldConfigurationForm', () => {
  it('renders text validation controls for text fields', () => {
    const wrapper = mount(FieldConfigurationForm, {
      props: {
        fieldType: FieldType.Text,
        relationTargets,
        configuration: createDefaultFieldConfiguration(FieldType.Text),
      },
    });

    expect(wrapper.text()).toContain('Minimum length');
    expect(wrapper.text()).toContain('Validation pattern');
    expect(wrapper.find('textarea').exists()).toBe(false);
  });

  it('renders option and relation controls based on field type configuration', () => {
    const selectWrapper = mount(FieldConfigurationForm, {
      props: {
        fieldType: FieldType.Select,
        relationTargets,
        configuration: {
          ...createDefaultFieldConfiguration(FieldType.Select),
          options: ['A', 'B'],
        },
      },
    });

    expect(selectWrapper.text()).toContain('Options (one per line)');
    expect(selectWrapper.find('textarea').exists()).toBe(true);

    const relationWrapper = mount(FieldConfigurationForm, {
      props: {
        fieldType: FieldType.Relation,
        relationTargets,
        configuration: createDefaultFieldConfiguration(FieldType.Relation),
      },
    });

    expect(relationWrapper.text()).toContain('Relation target content type');
    expect(relationWrapper.text()).toContain('Relation cardinality');
  });
});
