import { listContentEntries } from '@/api/content';

type RelationOption = { id: string; label: string };

const cache = new Map<string, Promise<RelationOption[]>>();

export function getRelationOptions(contentTypeId: string): Promise<RelationOption[]> {
  const cached = cache.get(contentTypeId);
  if (cached) {
    return cached;
  }

  const request = listContentEntries({
    contentTypeId,
    page: 1,
    pageSize: 100,
    sortBy: 'updatedAt',
    sortDirection: 'desc',
  })
    .then((response) =>
      response.items.map((entry) => ({
        id: entry.id,
        label: entry.slug,
      })),
    )
    .catch((error: unknown) => {
      cache.delete(contentTypeId);
      throw error;
    });

  cache.set(contentTypeId, request);
  return request;
}

export function clearRelationOptionsCache(): void {
  cache.clear();
}
