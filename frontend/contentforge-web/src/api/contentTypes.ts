import { apiRequest } from '@/api/client';
import type { PaginatedResponse } from '@/types/api';
import type {
  AddContentTypeFieldRequest,
  ContentType,
  ContentTypeListParams,
  CreateContentTypeRequest,
  UpdateContentTypeFieldRequest,
  UpdateContentTypeRequest,
} from '@/types/contentTypes';

const BASE = '/api/v1/content-types';

export function listContentTypes(
  params: ContentTypeListParams = {},
): Promise<PaginatedResponse<ContentType>> {
  const query = new URLSearchParams();
  if (params.page) query.set('page', String(params.page));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));
  if (params.sortBy) query.set('sortBy', params.sortBy);
  if (params.sortDirection) query.set('sortDirection', params.sortDirection);
  if (params.isActive !== undefined) query.set('isActive', String(params.isActive));
  if (params.search) query.set('search', params.search);

  const suffix = query.size > 0 ? `?${query.toString()}` : '';
  return apiRequest<PaginatedResponse<ContentType>>(`${BASE}${suffix}`);
}

export function getContentType(id: string): Promise<ContentType> {
  return apiRequest<ContentType>(`${BASE}/${id}`);
}

export function createContentType(request: CreateContentTypeRequest): Promise<ContentType> {
  return apiRequest<ContentType>(BASE, {
    method: 'POST',
    body: request,
  });
}

export function updateContentType(
  id: string,
  request: UpdateContentTypeRequest,
): Promise<ContentType> {
  return apiRequest<ContentType>(`${BASE}/${id}`, {
    method: 'PUT',
    body: request,
  });
}

export function deleteContentType(id: string, confirmedSafeDeletion: boolean): Promise<void> {
  return apiRequest<void>(`${BASE}/${id}`, {
    method: 'DELETE',
    body: { confirmedSafeDeletion },
  });
}

export function deactivateContentType(id: string): Promise<ContentType> {
  return apiRequest<ContentType>(`${BASE}/${id}/deactivate`, {
    method: 'POST',
  });
}

export function addContentTypeField(
  contentTypeId: string,
  request: AddContentTypeFieldRequest,
): Promise<ContentType> {
  return apiRequest<ContentType>(`${BASE}/${contentTypeId}/fields`, {
    method: 'POST',
    body: request,
  });
}

export function updateContentTypeField(
  contentTypeId: string,
  fieldName: string,
  request: UpdateContentTypeFieldRequest,
): Promise<ContentType> {
  return apiRequest<ContentType>(
    `${BASE}/${contentTypeId}/fields/${encodeURIComponent(fieldName)}`,
    {
      method: 'PUT',
      body: request,
    },
  );
}

export function removeContentTypeField(
  contentTypeId: string,
  fieldName: string,
  confirmed: boolean,
): Promise<ContentType> {
  return apiRequest<ContentType>(
    `${BASE}/${contentTypeId}/fields/${encodeURIComponent(fieldName)}`,
    {
      method: 'DELETE',
      body: { confirmed },
    },
  );
}

export function renameContentTypeField(
  contentTypeId: string,
  fieldName: string,
  newName: string,
  confirmed: boolean,
): Promise<ContentType> {
  return apiRequest<ContentType>(
    `${BASE}/${contentTypeId}/fields/${encodeURIComponent(fieldName)}/name`,
    {
      method: 'PUT',
      body: { newName, confirmed },
    },
  );
}
