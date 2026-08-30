import { describe, expect, it } from 'vitest';
import { FIELD_TYPE_DEFINITIONS, getFieldTypeDefinition } from '@/config/fieldTypeDefinitions';
import { FieldType } from '@/types/contentTypes';

describe('fieldTypeDefinitions', () => {
  it('defines every field type from the specification', () => {
    const expectedTypes = [
      FieldType.Text,
      FieldType.LongText,
      FieldType.RichText,
      FieldType.Integer,
      FieldType.Decimal,
      FieldType.Boolean,
      FieldType.Date,
      FieldType.DateTime,
      FieldType.Media,
      FieldType.MediaMultiple,
      FieldType.Relation,
      FieldType.RelationMultiple,
      FieldType.Select,
      FieldType.MultiSelect,
      FieldType.Json,
    ];

    expect(FIELD_TYPE_DEFINITIONS.map((definition) => definition.type)).toEqual(expectedTypes);
  });

  it('requires options for select fields', () => {
    expect(getFieldTypeDefinition(FieldType.Select).requiresOptions).toBe(true);
    expect(getFieldTypeDefinition(FieldType.MultiSelect).requiresOptions).toBe(true);
  });

  it('requires relation settings for relation fields', () => {
    expect(getFieldTypeDefinition(FieldType.Relation).requiresRelation).toBe(true);
    expect(getFieldTypeDefinition(FieldType.RelationMultiple).requiresRelation).toBe(true);
  });
});
