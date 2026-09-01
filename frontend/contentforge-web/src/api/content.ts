import { apiRequest } from '@/api/client';
import type { PaginatedResponse } from '@/types/api';
import type {
  ContentConcurrencyRequest,
  ContentEntry,
  ContentEntryListParams,
  ContentEntrySearchParams,
  ContentLifecycleRequest,
  ContentPreview,
  ContentPreviewToken,
  ContentVersion,
  ContentVersionComparison,
  CreateContentEntryRequest,
  UpdateContentEntryRequest,
} from '@/types/contentEntries';

const BASE = '/api/v1/content';

export function listContentEntries(
  params: ContentEntryListParams = {},
): Promise<PaginatedResponse<ContentEntry>> {
  const query = new URLSearchParams();
  if (params.page) query.set('page', String(params.page));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));
  if (params.sortBy) query.set('sortBy', params.sortBy);
  if (params.sortDirection) query.set('sortDirection', params.sortDirection);
  if (params.contentTypeId) query.set('contentTypeId', params.contentTypeId);
  if (params.status) query.set('status', params.status);
  if (params.authorId) query.set('authorId', params.authorId);
  if (params.search) query.set('search', params.search);
  if (params.includeDeleted !== undefined) {
    query.set('includeDeleted', String(params.includeDeleted));
  }

  const suffix = query.size > 0 ? `?${query.toString()}` : '';
  return apiRequest<PaginatedResponse<ContentEntry>>(`${BASE}${suffix}`);
}

export function searchContentEntries(
  params: ContentEntrySearchParams = {},
): Promise<PaginatedResponse<ContentEntry>> {
  const query = new URLSearchParams();
  if (params.page) query.set('page', String(params.page));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));
  if (params.sortBy) query.set('sortBy', params.sortBy);
  if (params.sortDirection) query.set('sortDirection', params.sortDirection);
  if (params.keyword) query.set('keyword', params.keyword);
  if (params.contentTypeId) query.set('contentTypeId', params.contentTypeId);
  if (params.status) query.set('status', params.status);
  if (params.authorId) query.set('authorId', params.authorId);
  if (params.createdFrom) query.set('createdFrom', params.createdFrom);
  if (params.createdTo) query.set('createdTo', params.createdTo);

  const suffix = query.size > 0 ? `?${query.toString()}` : '';
  return apiRequest<PaginatedResponse<ContentEntry>>(`${BASE}/search${suffix}`);
}

export function getContentEntry(id: string): Promise<ContentEntry> {
  return apiRequest<ContentEntry>(`${BASE}/${id}`);
}

export function createContentEntry(request: CreateContentEntryRequest): Promise<ContentEntry> {
  return apiRequest<ContentEntry>(BASE, {
    method: 'POST',
    body: request,
  });
}

export function updateContentEntry(
  id: string,
  request: UpdateContentEntryRequest,
): Promise<ContentEntry> {
  return apiRequest<ContentEntry>(`${BASE}/${id}`, {
    method: 'PUT',
    body: request,
  });
}

export function deleteContentEntry(id: string, request: ContentConcurrencyRequest): Promise<void> {
  return apiRequest<void>(`${BASE}/${id}`, {
    method: 'DELETE',
    body: request,
  });
}

export function submitContentForReview(
  id: string,
  request: ContentConcurrencyRequest,
): Promise<ContentEntry> {
  return apiRequest<ContentEntry>(`${BASE}/${id}/submit-for-review`, {
    method: 'POST',
    body: request,
  });
}

export function withdrawContentFromReview(
  id: string,
  request: ContentConcurrencyRequest,
): Promise<ContentEntry> {
  return apiRequest<ContentEntry>(`${BASE}/${id}/withdraw-from-review`, {
    method: 'POST',
    body: request,
  });
}

export function publishContentEntry(
  id: string,
  request: ContentLifecycleRequest,
): Promise<ContentEntry> {
  return apiRequest<ContentEntry>(`${BASE}/${id}/publish`, {
    method: 'POST',
    body: request,
  });
}

export function unpublishContentEntry(
  id: string,
  request: ContentLifecycleRequest,
): Promise<ContentEntry> {
  return apiRequest<ContentEntry>(`${BASE}/${id}/unpublish`, {
    method: 'POST',
    body: request,
  });
}

export function archiveContentEntry(
  id: string,
  request: ContentLifecycleRequest,
): Promise<ContentEntry> {
  return apiRequest<ContentEntry>(`${BASE}/${id}/archive`, {
    method: 'POST',
    body: request,
  });
}

export function restoreContentEntry(
  id: string,
  request: ContentLifecycleRequest,
): Promise<ContentEntry> {
  return apiRequest<ContentEntry>(`${BASE}/${id}/restore`, {
    method: 'POST',
    body: request,
  });
}

export function listContentVersions(entryId: string): Promise<ContentVersion[]> {
  return apiRequest<ContentVersion[]>(`${BASE}/${entryId}/versions`);
}

export function getContentVersion(entryId: string, versionNumber: number): Promise<ContentVersion> {
  return apiRequest<ContentVersion>(`${BASE}/${entryId}/versions/${versionNumber}`);
}

export function compareContentVersions(
  entryId: string,
  left: number,
  right: number,
): Promise<ContentVersionComparison> {
  const query = new URLSearchParams({
    left: String(left),
    right: String(right),
  });
  return apiRequest<ContentVersionComparison>(
    `${BASE}/${entryId}/versions/compare?${query.toString()}`,
  );
}

export function restoreContentVersion(
  entryId: string,
  versionNumber: number,
  request: ContentLifecycleRequest,
): Promise<ContentEntry> {
  return apiRequest<ContentEntry>(`${BASE}/${entryId}/versions/${versionNumber}/restore`, {
    method: 'POST',
    body: request,
  });
}

export function createContentPreviewToken(entryId: string): Promise<ContentPreviewToken> {
  return apiRequest<ContentPreviewToken>(`${BASE}/${entryId}/preview-token`, {
    method: 'POST',
  });
}

export function getContentPreview(token: string): Promise<ContentPreview> {
  return apiRequest<ContentPreview>(`${BASE}/preview/${encodeURIComponent(token)}`, {
    auth: false,
  });
}
