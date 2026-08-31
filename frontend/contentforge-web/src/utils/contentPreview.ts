import { createContentPreviewToken } from '@/api/content';

export async function openContentPreview(entryId: string): Promise<void> {
  const issued = await createContentPreviewToken(entryId);
  const previewUrl = `${window.location.origin}/preview/${encodeURIComponent(issued.token)}`;
  window.open(previewUrl, '_blank', 'noopener,noreferrer');
}
