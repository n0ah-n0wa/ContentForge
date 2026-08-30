import { env } from '@/config/env';

/** Resolves a public media URL returned by the API. Never constructs paths from storage keys. */
export function resolveMediaUrl(publicUrl: string | null | undefined): string | null {
  if (!publicUrl) {
    return null;
  }

  if (publicUrl.startsWith('http://') || publicUrl.startsWith('https://')) {
    return publicUrl;
  }

  const baseUrl = env.apiBaseUrl.replace(/\/$/, '');
  const normalizedPath = publicUrl.startsWith('/') ? publicUrl : `/${publicUrl}`;
  return `${baseUrl}${normalizedPath}`;
}

export function formatMediaSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function isImageMedia(contentType: string): boolean {
  return contentType.startsWith('image/');
}
