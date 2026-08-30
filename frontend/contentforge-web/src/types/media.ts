export interface MediaAsset {
  id: string;
  fileName: string;
  originalFileName: string;
  contentType: string;
  size: number;
  url: string | null;
  width: number | null;
  height: number | null;
  altText: string | null;
  title: string | null;
  description: string | null;
  uploadedBy: string;
  uploadedAt: string;
  isDeleted: boolean;
}

export interface MediaListParams {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  search?: string;
  contentType?: string;
  includeDeleted?: boolean;
}

export interface UpdateMediaMetadataRequest {
  altText?: string | null;
  title?: string | null;
  description?: string | null;
}

export interface UploadMediaMetadata {
  altText?: string;
  title?: string;
  description?: string;
}

export const MEDIA_MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

export const MEDIA_CONTENT_TYPE_FILTERS = [
  { value: '', label: 'All types' },
  { value: 'image/', label: 'Images' },
  { value: 'video/', label: 'Video' },
  { value: 'audio/', label: 'Audio' },
  { value: 'application/', label: 'Documents' },
  { value: 'text/', label: 'Text' },
] as const;
