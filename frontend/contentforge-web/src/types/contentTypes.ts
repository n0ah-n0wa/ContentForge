/** Matches ContentForge.Domain.ContentTypes.FieldType serialization order. */
export enum FieldType {
  Text = 0,
  LongText = 1,
  RichText = 2,
  Integer = 3,
  Decimal = 4,
  Boolean = 5,
  Date = 6,
  DateTime = 7,
  Media = 8,
  MediaMultiple = 9,
  Relation = 10,
  RelationMultiple = 11,
  Select = 12,
  MultiSelect = 13,
  Json = 14,
}

/** Matches ContentForge.Domain.ContentTypes.RelationCardinality serialization order. */
export enum RelationCardinality {
  OneToOne = 0,
  ManyToOne = 1,
  OneToMany = 2,
  ManyToMany = 3,
}

export interface FieldConfiguration {
  isRequired: boolean;
  minLength: number | null;
  maxLength: number | null;
  minValue: number | null;
  maxValue: number | null;
  pattern: string | null;
  allowMultiple: boolean;
  defaultValue: string | null;
  options: string[];
  relationTarget: string | null;
  relationCardinality: RelationCardinality | null;
}

export interface ContentTypeField {
  id: string;
  name: string;
  fieldType: FieldType;
  displayName: string;
  sortOrder: number;
  configuration: FieldConfiguration;
}

export interface ContentType {
  id: string;
  name: string;
  displayName: string;
  description: string | null;
  slug: string;
  isActive: boolean;
  version: number;
  createdBy: string;
  updatedBy: string;
  createdAt: string;
  updatedAt: string;
  fields: ContentTypeField[];
  fieldCount: number;
}

export interface CreateContentTypeRequest {
  name: string;
  displayName: string;
  slug: string;
  description?: string | null;
}

export interface UpdateContentTypeRequest {
  displayName: string;
  slug: string;
  description?: string | null;
}

export interface AddContentTypeFieldRequest {
  name: string;
  fieldType: FieldType;
  displayName: string;
  sortOrder: number;
  configuration: FieldConfiguration;
}

export interface UpdateContentTypeFieldRequest {
  displayName: string;
  sortOrder: number;
  configuration: FieldConfiguration;
  confirmedDestructiveChange: boolean;
}

export interface ContentTypeListParams {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  isActive?: boolean;
  search?: string;
}

export function createDefaultFieldConfiguration(fieldType: FieldType): FieldConfiguration {
  return {
    isRequired: false,
    minLength: null,
    maxLength: null,
    minValue: null,
    maxValue: null,
    pattern: null,
    allowMultiple: fieldType === FieldType.MediaMultiple,
    defaultValue: null,
    options: [],
    relationTarget: null,
    relationCardinality: null,
  };
}

export const RELATION_CARDINALITY_LABELS: Record<RelationCardinality, string> = {
  [RelationCardinality.OneToOne]: 'One to one',
  [RelationCardinality.ManyToOne]: 'Many to one',
  [RelationCardinality.OneToMany]: 'One to many',
  [RelationCardinality.ManyToMany]: 'Many to many',
};
