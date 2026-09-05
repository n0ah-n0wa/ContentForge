import { apiRequest, getAccessToken, getRefreshHandler } from '@/api/client';
import { ensureApiSession, notifyUnauthorizedSession } from '@/api/authSession';
import { ApiError } from '@/api/errors';
import { env } from '@/config/env';
import type { PaginatedResponse } from '@/types/api';
import type {
  MediaAsset,
  MediaListParams,
  UpdateMediaMetadataRequest,
  UploadMediaMetadata,
} from '@/types/media';

const BASE = '/api/v1/media';

export function listMedia(params: MediaListParams = {}): Promise<PaginatedResponse<MediaAsset>> {
  const query = new URLSearchParams();
  if (params.page) query.set('page', String(params.page));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));
  if (params.sortBy) query.set('sortBy', params.sortBy);
  if (params.sortDirection) query.set('sortDirection', params.sortDirection);
  if (params.search) query.set('search', params.search);
  if (params.contentType) query.set('contentType', params.contentType);
  if (params.includeDeleted !== undefined) {
    query.set('includeDeleted', String(params.includeDeleted));
  }

  const suffix = query.size > 0 ? `?${query.toString()}` : '';
  return apiRequest<PaginatedResponse<MediaAsset>>(`${BASE}${suffix}`);
}

export function getMedia(id: string): Promise<MediaAsset> {
  return apiRequest<MediaAsset>(`${BASE}/${id}`);
}

export function updateMediaMetadata(
  id: string,
  request: UpdateMediaMetadataRequest,
): Promise<MediaAsset> {
  return apiRequest<MediaAsset>(`${BASE}/${id}`, {
    method: 'PUT',
    body: request,
  });
}

export function deleteMedia(id: string): Promise<void> {
  return apiRequest<void>(`${BASE}/${id}`, {
    method: 'DELETE',
  });
}

export async function uploadMedia(
  file: File,
  metadata: UploadMediaMetadata = {},
  onProgress?: (percent: number) => void,
): Promise<MediaAsset> {
  return uploadMediaInternal(file, metadata, onProgress, false);
}

async function uploadMediaInternal(
  file: File,
  metadata: UploadMediaMetadata,
  onProgress: ((percent: number) => void) | undefined,
  hasRetried: boolean,
): Promise<MediaAsset> {
  const sessionIsValid = await ensureApiSession();
  if (!sessionIsValid) {
    notifyUnauthorizedSession('expired');
    throw await ApiError.fromResponse(
      new Response(
        JSON.stringify({
          title: 'Unauthorized',
          status: 401,
          detail: 'Your session has expired. Sign in again.',
        }),
        { status: 401, headers: { 'Content-Type': 'application/problem+json' } },
      ),
    );
  }

  const token = getAccessToken();
  const formData = new FormData();
  formData.append('file', file);
  if (metadata.altText) formData.append('altText', metadata.altText);
  if (metadata.title) formData.append('title', metadata.title);
  if (metadata.description) formData.append('description', metadata.description);

  const baseUrl = env.apiBaseUrl.replace(/\/$/, '');
  const url = `${baseUrl}${BASE}`;

  return new Promise<MediaAsset>((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open('POST', url);
    xhr.setRequestHeader('Accept', 'application/json');
    if (token) {
      xhr.setRequestHeader('Authorization', `Bearer ${token}`);
    }

    xhr.upload.onprogress = (event) => {
      if (!event.lengthComputable || !onProgress) {
        return;
      }

      onProgress(Math.round((event.loaded / event.total) * 100));
    };

    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve(JSON.parse(xhr.responseText) as MediaAsset);
        return;
      }

      if (xhr.status === 401 && !hasRetried) {
        void getRefreshHandler()()
          .then((refreshed) => {
            if (refreshed) {
              resolve(uploadMediaInternal(file, metadata, onProgress, true));
              return;
            }

            notifyUnauthorizedSession('invalid');
            return ApiError.fromResponse(
              new Response(xhr.responseText, {
                status: xhr.status,
                headers: { 'Content-Type': 'application/json' },
              }),
            ).then(reject);
          })
          .catch(reject);
        return;
      }

      void ApiError.fromResponse(
        new Response(xhr.responseText, {
          status: xhr.status,
          headers: { 'Content-Type': 'application/json' },
        }),
      ).then(reject);
    };

    xhr.onerror = () => {
      reject(new Error('Upload failed due to a network error.'));
    };

    xhr.send(formData);
  });
}
