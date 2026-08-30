import { FieldType } from '@/types/contentTypes';

export interface FieldConfigurationCapabilities {
  required: boolean;
  defaultValue: boolean;
  minLength: boolean;
  maxLength: boolean;
  minValue: boolean;
  maxValue: boolean;
  pattern: boolean;
  options: boolean;
  relation: boolean;
  allowMultiple: boolean;
}

export interface FieldTypeDefinition {
  type: FieldType;
  label: string;
  description: string;
  capabilities: FieldConfigurationCapabilities;
  requiresOptions: boolean;
  requiresRelation: boolean;
  requiresAllowMultiple: boolean;
}

const textLikeCapabilities: FieldConfigurationCapabilities = {
  required: true,
  defaultValue: true,
  minLength: true,
  maxLength: true,
  minValue: false,
  maxValue: false,
  pattern: true,
  options: false,
  relation: false,
  allowMultiple: false,
};

const numericCapabilities: FieldConfigurationCapabilities = {
  required: true,
  defaultValue: true,
  minLength: false,
  maxLength: false,
  minValue: true,
  maxValue: true,
  pattern: false,
  options: false,
  relation: false,
  allowMultiple: false,
};

const basicCapabilities: FieldConfigurationCapabilities = {
  required: true,
  defaultValue: true,
  minLength: false,
  maxLength: false,
  minValue: false,
  maxValue: false,
  pattern: false,
  options: false,
  relation: false,
  allowMultiple: false,
};

export const FIELD_TYPE_DEFINITIONS: FieldTypeDefinition[] = [
  {
    type: FieldType.Text,
    label: 'Text',
    description: 'Short single-line text.',
    capabilities: textLikeCapabilities,
    requiresOptions: false,
    requiresRelation: false,
    requiresAllowMultiple: false,
  },
  {
    type: FieldType.LongText,
    label: 'Long text',
    description: 'Multi-line plain text.',
    capabilities: textLikeCapabilities,
    requiresOptions: false,
    requiresRelation: false,
    requiresAllowMultiple: false,
  },
  {
    type: FieldType.RichText,
    label: 'Rich text',
    description: 'Formatted rich text content.',
    capabilities: textLikeCapabilities,
    requiresOptions: false,
    requiresRelation: false,
    requiresAllowMultiple: false,
  },
  {
    type: FieldType.Integer,
    label: 'Integer',
    description: 'Whole number value.',
    capabilities: numericCapabilities,
    requiresOptions: false,
    requiresRelation: false,
    requiresAllowMultiple: false,
  },
  {
    type: FieldType.Decimal,
    label: 'Decimal',
    description: 'Decimal number value.',
    capabilities: numericCapabilities,
    requiresOptions: false,
    requiresRelation: false,
    requiresAllowMultiple: false,
  },
  {
    type: FieldType.Boolean,
    label: 'Boolean',
    description: 'True or false toggle.',
    capabilities: basicCapabilities,
    requiresOptions: false,
    requiresRelation: false,
    requiresAllowMultiple: false,
  },
  {
    type: FieldType.Date,
    label: 'Date',
    description: 'Calendar date without time.',
    capabilities: basicCapabilities,
    requiresOptions: false,
    requiresRelation: false,
    requiresAllowMultiple: false,
  },
  {
    type: FieldType.DateTime,
    label: 'Date & time',
    description: 'Date with time component.',
    capabilities: basicCapabilities,
    requiresOptions: false,
    requiresRelation: false,
    requiresAllowMultiple: false,
  },
  {
    type: FieldType.Media,
    label: 'Media',
    description: 'Single media asset reference.',
    capabilities: { ...basicCapabilities, defaultValue: false },
    requiresOptions: false,
    requiresRelation: false,
    requiresAllowMultiple: false,
  },
  {
    type: FieldType.MediaMultiple,
    label: 'Media (multiple)',
    description: 'Multiple media asset references.',
    capabilities: { ...basicCapabilities, defaultValue: false, allowMultiple: true },
    requiresOptions: false,
    requiresRelation: false,
    requiresAllowMultiple: true,
  },
  {
    type: FieldType.Relation,
    label: 'Relation',
    description: 'Reference to another content type.',
    capabilities: { ...basicCapabilities, defaultValue: false, relation: true },
    requiresOptions: false,
    requiresRelation: true,
    requiresAllowMultiple: false,
  },
  {
    type: FieldType.RelationMultiple,
    label: 'Relation (multiple)',
    description: 'Multiple references to another content type.',
    capabilities: { ...basicCapabilities, defaultValue: false, relation: true },
    requiresOptions: false,
    requiresRelation: true,
    requiresAllowMultiple: false,
  },
  {
    type: FieldType.Select,
    label: 'Select',
    description: 'Single choice from predefined options.',
    capabilities: { ...basicCapabilities, options: true },
    requiresOptions: true,
    requiresRelation: false,
    requiresAllowMultiple: false,
  },
  {
    type: FieldType.MultiSelect,
    label: 'Multi-select',
    description: 'Multiple choices from predefined options.',
    capabilities: { ...basicCapabilities, options: true },
    requiresOptions: true,
    requiresRelation: false,
    requiresAllowMultiple: false,
  },
  {
    type: FieldType.Json,
    label: 'JSON',
    description: 'Structured JSON payload.',
    capabilities: { ...basicCapabilities, pattern: false },
    requiresOptions: false,
    requiresRelation: false,
    requiresAllowMultiple: false,
  },
];

const definitionByType = new Map(FIELD_TYPE_DEFINITIONS.map((definition) => [definition.type, definition]));

export function getFieldTypeDefinition(fieldType: FieldType): FieldTypeDefinition {
  const definition = definitionByType.get(fieldType);
  if (!definition) {
    throw new Error(`Unsupported field type: ${fieldType}`);
  }

  return definition;
}

export function getFieldTypeLabel(fieldType: FieldType): string {
  return getFieldTypeDefinition(fieldType).label;
}
