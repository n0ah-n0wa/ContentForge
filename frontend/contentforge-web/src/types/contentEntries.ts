/** Matches ContentForge.Domain.Content.ContentStatus serialization order. */
export enum ContentStatus {
  Draft = 0,
  InReview = 1,
  Published = 2,
  Archived = 3,
  Unpublished = 4,
}

export const CONTENT_STATUS_LABELS: Record<ContentStatus, string> = {
  [ContentStatus.Draft]: 'Draft',
  [ContentStatus.InReview]: 'In review',
  [ContentStatus.Published]: 'Published',
  [ContentStatus.Archived]: 'Archived',
  [ContentStatus.Unpublished]: 'Unpublished',
};

export type ContentFieldValue = unknown;

export interface ContentEntry {
  id: string;
  contentTypeId: string;
  slug: string;
  status: ContentStatus;
  draftData: Record<string, ContentFieldValue>;
  publishedData: Record<string, ContentFieldValue> | null;
  currentVersion: number;
  concurrencyToken: number;
  createdBy: string;
  updatedBy: string;
  createdAt: string;
  updatedAt: string;
  publishedAt: string | null;
  publishedBy: string | null;
  isDeleted: boolean;
}

export interface ContentVersion {
  id: string;
  contentEntryId: string;
  versionNumber: number;
  slug: string;
  status: ContentStatus;
  data: Record<string, ContentFieldValue>;
  createdAt: string;
  createdBy: string;
  changeSummary: string;
}

export interface ContentVersionComparison {
  leftVersionNumber: number;
  rightVersionNumber: number;
  changes: Array<{
    fieldName: string;
    oldValue: ContentFieldValue;
    newValue: ContentFieldValue;
  }>;
}

export interface CreateContentEntryRequest {
  contentTypeId: string;
  slug: string;
  data?: Record<string, ContentFieldValue>;
}

export interface UpdateContentEntryRequest {
  slug: string;
  data: Record<string, ContentFieldValue>;
  changeSummary: string;
  concurrencyToken: number;
}

export interface ContentLifecycleRequest {
  changeSummary: string;
  concurrencyToken: number;
}

export interface ContentConcurrencyRequest {
  concurrencyToken: number;
}

export interface ContentEntryListParams {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  contentTypeId?: string;
  status?: string;
  authorId?: string;
  search?: string;
  includeDeleted?: boolean;
}

export interface ContentEntrySearchParams {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  keyword?: string;
  contentTypeId?: string;
  status?: string;
  authorId?: string;
  createdFrom?: string;
  createdTo?: string;
}

export const CONTENT_STATUS_FILTER_OPTIONS = (
  Object.entries(CONTENT_STATUS_LABELS) as Array<[string, string]>
).map(([statusValue, label]) => ({
  value: ContentStatus[Number(statusValue) as ContentStatus],
  label,
}));

export interface ContentPreviewToken {
  token: string;
  expiresAt: string;
  previewPath: string;
}

export interface ContentPreviewField {
  name: string;
  displayName: string;
  fieldType: number;
  sortOrder: number;
}

export interface ContentPreview {
  contentTypeSlug: string;
  slug: string;
  status: ContentStatus;
  data: Record<string, ContentFieldValue>;
  fields: ContentPreviewField[];
  expiresAt: string;
}
