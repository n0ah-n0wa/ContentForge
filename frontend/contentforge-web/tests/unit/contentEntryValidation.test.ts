import { describe, expect, it } from 'vitest';
import { FieldType, createDefaultFieldConfiguration } from '@/types/contentTypes';
import type { ContentTypeField } from '@/types/contentTypes';
import {
  createEmptyEntryData,
  serializeEntryDataForApi,
  validateEntryClientSide,
} from '@/utils/contentEntryValidation';

function buildField(overrides: Partial<ContentTypeField> = {}): ContentTypeField {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    name: 'title',
    fieldType: FieldType.Text,
    displayName: 'Title',
    sortOrder: 1,
    configuration: {
      ...createDefaultFieldConfiguration(FieldType.Text),
      isRequired: true,
    },
    ...overrides,
  };
}

describe('contentEntryValidation', () => {
  it('creates default values for all supported field types', () => {
    const fields = [
      buildField({ name: 'text', fieldType: FieldType.Text }),
      buildField({
        name: 'tags',
        fieldType: FieldType.MultiSelect,
        configuration: createDefaultFieldConfiguration(FieldType.MultiSelect),
      }),
      buildField({
        name: 'enabled',
        fieldType: FieldType.Boolean,
        configuration: createDefaultFieldConfiguration(FieldType.Boolean),
      }),
    ];

    const data = createEmptyEntryData(fields);
    expect(data.text).toBe('');
    expect(data.tags).toEqual([]);
    expect(data.enabled).toBe(false);
  });

  it('validates required fields and json shape without enforcing backend rules', () => {
    const fields = [
      buildField({ name: 'title', configuration: { ...createDefaultFieldConfiguration(FieldType.Text), isRequired: true } }),
      buildField({
        name: 'payload',
        fieldType: FieldType.Json,
        displayName: 'Payload',
        configuration: createDefaultFieldConfiguration(FieldType.Json),
      }),
    ];

    const errors = validateEntryClientSide(
      fields,
      { title: '', payload: '{ invalid' },
      { slug: 'entry', changeSummary: '' },
    );

    expect(errors.Slug).toBeUndefined();
    expect(errors.title).toContain('Title is required.');
    expect(errors.ChangeSummary).toContain('Change summary is required.');
    expect(errors.payload?.[0]).toContain('valid JSON');
  });

  it('serializes json fields for api submission', () => {
    const fields = [
      buildField({
        name: 'payload',
        fieldType: FieldType.Json,
        configuration: createDefaultFieldConfiguration(FieldType.Json),
      }),
    ];

    const payload = serializeEntryDataForApi(fields, {
      payload: '{\n  "enabled": true\n}',
    });

    expect(payload.payload).toEqual({ enabled: true });
  });
});
