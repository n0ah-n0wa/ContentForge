import type { ApiClient } from './apiClient';

/**
 * Tracks resources created during a test so they can be removed afterward.
 * Cleanup is best-effort and ordered to respect FK-like dependencies.
 */
export class CleanupRegistry {
  private readonly mediaIds: string[] = [];
  private readonly entryIds: string[] = [];
  private readonly contentTypeIds: string[] = [];
  private readonly userIds: string[] = [];

  trackMedia(id: string): void {
    this.mediaIds.push(id);
  }

  trackEntry(id: string): void {
    this.entryIds.push(id);
  }

  trackContentType(id: string): void {
    this.contentTypeIds.push(id);
  }

  trackUser(id: string): void {
    this.userIds.push(id);
  }

  async run(adminApi: ApiClient): Promise<void> {
    for (const id of [...this.entryIds].reverse()) {
      try {
        const entry = await adminApi.request<{ concurrencyToken: number }>(
          'GET',
          `/api/v1/content/${id}`,
        );
        await adminApi.request('DELETE', `/api/v1/content/${id}`, {
          concurrencyToken: entry.concurrencyToken,
        });
      } catch {
        // Best-effort cleanup.
      }
    }

    for (const id of [...this.mediaIds].reverse()) {
      await adminApi.request('DELETE', `/api/v1/media/${id}`).catch(() => undefined);
    }

    for (const id of [...this.contentTypeIds].reverse()) {
      await adminApi.request('DELETE', `/api/v1/content-types/${id}`).catch(() => undefined);
    }

    for (const id of [...this.userIds].reverse()) {
      await adminApi.request('POST', `/api/v1/users/${id}/disable`).catch(() => undefined);
    }

    this.mediaIds.length = 0;
    this.entryIds.length = 0;
    this.contentTypeIds.length = 0;
    this.userIds.length = 0;
  }
}
