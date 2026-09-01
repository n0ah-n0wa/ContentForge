import { FieldType, type ContentTypeField } from '@/types/contentTypes';
import type { ContentFieldValue } from '@/types/contentEntries';
import { sanitizeRichText } from '@/utils/richTextSanitizer';

const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export function isEmptyFieldValue(value: ContentFieldValue, fieldType: FieldType): boolean {
  if (value === null || value === undefined) {
    return true;
  }

  if (typeof value === 'string') {
    return value.trim().length === 0;
  }

  if (Array.isArray(value)) {
    return value.length === 0;
  }

  if (fieldType === FieldType.Boolean) {
    return false;
  }

  if (fieldType === FieldType.Json) {
    return value === null;
  }

  return false;
}

export function createDefaultFieldValue(field: ContentTypeField): ContentFieldValue {
  const defaultValue = field.configuration.defaultValue;
  if (defaultValue !== null && defaultValue !== '') {
    return coerceDefaultValue(field.fieldType, defaultValue);
  }

  switch (field.fieldType) {
    case FieldType.Boolean:
      return false;
    case FieldType.MultiSelect:
    case FieldType.MediaMultiple:
    case FieldType.RelationMultiple:
      return [];
    case FieldType.Integer:
    case FieldType.Decimal:
      return null;
    case FieldType.Json:
      return '';
    default:
      return '';
  }
}

function coerceDefaultValue(fieldType: FieldType, raw: string): ContentFieldValue {
  switch (fieldType) {
    case FieldType.Boolean:
      return raw === 'true';
    case FieldType.Integer:
    case FieldType.Decimal: {
      const parsed = Number(raw);
      return Number.isFinite(parsed) ? parsed : null;
    }
    case FieldType.MultiSelect:
      return raw
        .split(',')
        .map((item) => item.trim())
        .filter(Boolean);
    default:
      return raw;
  }
}

export function createEmptyEntryData(
  fields: ContentTypeField[],
): Record<string, ContentFieldValue> {
  return Object.fromEntries(fields.map((field) => [field.name, createDefaultFieldValue(field)]));
}

export function normalizeEntryData(
  fields: ContentTypeField[],
  data: Record<string, ContentFieldValue>,
): Record<string, ContentFieldValue> {
  const normalized: Record<string, ContentFieldValue> = {};

  for (const field of fields) {
    const value = data[field.name];
    normalized[field.name] = normalizeFieldValue(field, value);
  }

  return normalized;
}

function normalizeFieldValue(field: ContentTypeField, value: ContentFieldValue): ContentFieldValue {
  switch (field.fieldType) {
    case FieldType.Text:
    case FieldType.LongText:
      return typeof value === 'string' ? value : value == null ? '' : String(value);
    case FieldType.RichText:
      return typeof value === 'string' ? sanitizeRichText(value) : '';
    case FieldType.Integer: {
      if (value === '' || value === null || value === undefined) return null;
      const whole = Number(value);
      return Number.isInteger(whole) ? whole : value;
    }
    case FieldType.Decimal: {
      if (value === '' || value === null || value === undefined) return null;
      const decimal = Number(value);
      return Number.isFinite(decimal) ? decimal : value;
    }
    case FieldType.Boolean:
      return Boolean(value);
    case FieldType.Date:
    case FieldType.DateTime:
      return typeof value === 'string' ? value : '';
    case FieldType.Select:
      return typeof value === 'string' ? value : '';
    case FieldType.MultiSelect:
      return Array.isArray(value) ? value.map(String) : [];
    case FieldType.Media:
    case FieldType.Relation:
      return typeof value === 'string' ? value.trim() : value == null ? '' : String(value);
    case FieldType.MediaMultiple:
    case FieldType.RelationMultiple:
      return Array.isArray(value) ? value.map(String).filter(Boolean) : [];
    case FieldType.Json:
      if (typeof value === 'string') {
        return value.trim();
      }
      if (value === null || value === undefined) {
        return '';
      }
      return JSON.stringify(value, null, 2);
    default:
      return value;
  }
}

export interface ClientValidationOptions {
  slug?: string;
  changeSummary?: string;
}

/**
 * Lightweight client-side checks for immediate feedback.
 * Authoritative validation always comes from the server.
 */
export function validateEntryClientSide(
  fields: ContentTypeField[],
  data: Record<string, ContentFieldValue>,
  options: ClientValidationOptions = {},
): Record<string, string[]> {
  const errors: Record<string, string[]> = {};

  if (options.slug !== undefined && !options.slug.trim()) {
    errors.Slug = ['Slug is required.'];
  }

  if (options.changeSummary !== undefined && !options.changeSummary.trim()) {
    errors.ChangeSummary = ['Change summary is required.'];
  }

  for (const field of fields) {
    const value = data[field.name];
    const fieldErrors: string[] = [];

    if (field.configuration.isRequired && isEmptyFieldValue(value, field.fieldType)) {
      fieldErrors.push(`${field.displayName} is required.`);
    }

    if (!isEmptyFieldValue(value, field.fieldType)) {
      fieldErrors.push(...getFieldShapeErrors(field, value));
    }

    if (fieldErrors.length > 0) {
      errors[field.name] = fieldErrors;
    }
  }

  return errors;
}

function getFieldShapeErrors(field: ContentTypeField, value: ContentFieldValue): string[] {
  switch (field.fieldType) {
    case FieldType.Integer:
      if (value !== null && value !== '' && !Number.isInteger(Number(value))) {
        return [`${field.displayName} must be a whole number.`];
      }
      return [];
    case FieldType.Decimal:
      if (value !== null && value !== '' && !Number.isFinite(Number(value))) {
        return [`${field.displayName} must be a number.`];
      }
      return [];
    case FieldType.Json:
      if (typeof value === 'string' && value.trim()) {
        try {
          JSON.parse(value);
        } catch {
          return [`${field.displayName} must be valid JSON.`];
        }
      }
      return [];
    case FieldType.Media:
    case FieldType.Relation:
      if (typeof value === 'string' && value.trim() && !GUID_PATTERN.test(value.trim())) {
        return [`${field.displayName} must be a valid identifier.`];
      }
      return [];
    case FieldType.MediaMultiple:
    case FieldType.RelationMultiple:
      if (Array.isArray(value)) {
        const invalid = value.some(
          (item) => typeof item === 'string' && item && !GUID_PATTERN.test(item),
        );
        if (invalid) {
          return [`${field.displayName} contains an invalid identifier.`];
        }
      }
      return [];
    default:
      return [];
  }
}

export function mergeValidationErrors(
  clientErrors: Record<string, string[]>,
  serverErrors: Record<string, string[]> | undefined,
): Record<string, string[]> {
  const merged: Record<string, string[]> = { ...clientErrors };

  if (!serverErrors) {
    return merged;
  }

  for (const [key, messages] of Object.entries(serverErrors)) {
    merged[key] = [...(merged[key] ?? []), ...messages];
  }

  return merged;
}

export function hasValidationErrors(errors: Record<string, string[]>): boolean {
  return Object.values(errors).some((messages) => messages.length > 0);
}

export function serializeEntryDataForApi(
  fields: ContentTypeField[],
  data: Record<string, ContentFieldValue>,
): Record<string, ContentFieldValue> {
  const normalized = normalizeEntryData(fields, data);
  const payload: Record<string, ContentFieldValue> = {};

  for (const field of fields) {
    const value = normalized[field.name];
    if (field.fieldType === FieldType.Json && typeof value === 'string') {
      const trimmed = value.trim();
      payload[field.name] = trimmed ? JSON.parse(trimmed) : null;
      continue;
    }

    if (
      (field.fieldType === FieldType.Integer || field.fieldType === FieldType.Decimal) &&
      (value === '' || value === null)
    ) {
      if (!field.configuration.isRequired) {
        continue;
      }
    }

    if (isEmptyFieldValue(value, field.fieldType) && !field.configuration.isRequired) {
      continue;
    }

    payload[field.name] = value;
  }

  return payload;
}

export function snapshotsEqual(
  left: Record<string, ContentFieldValue>,
  right: Record<string, ContentFieldValue>,
): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}
