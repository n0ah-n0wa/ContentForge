import { getMedia } from '@/api/media';
import type { MediaAsset } from '@/types/media';

const cache = new Map<string, Promise<MediaAsset>>();

export function getCachedMediaAsset(id: string): Promise<MediaAsset> {
  const cached = cache.get(id);
  if (cached) {
    return cached;
  }

  const request = getMedia(id);
  cache.set(id, request);
  return request;
}

export function primeMediaAssetCache(asset: MediaAsset): void {
  cache.set(asset.id, Promise.resolve(asset));
}

export function clearMediaAssetCache(): void {
  cache.clear();
}

export async function getCachedMediaAssets(ids: string[]): Promise<MediaAsset[]> {
  if (ids.length === 0) {
    return [];
  }

  return Promise.all(ids.map((id) => getCachedMediaAsset(id)));
}
