import { ref, type Ref } from 'vue';
import {
  archiveContentEntry,
  deleteContentEntry,
  publishContentEntry,
  restoreContentEntry,
  restoreContentVersion,
  submitContentForReview,
  unpublishContentEntry,
  withdrawContentFromReview,
} from '@/api/content';
import { isConcurrencyConflict, type ConcurrencyConflictProblem } from '@/api/concurrency';
import { ApiError } from '@/api/errors';
import type { ContentEntry } from '@/types/contentEntries';
import type { LifecycleAction } from '@/utils/contentLifecycle';

export function useContentEntryActions(
  entry: Ref<ContentEntry | null>,
  onEntryUpdated: (entry: ContentEntry) => void,
  onConflict: (problem: ConcurrencyConflictProblem) => void,
  onError: (error: unknown, title: string) => void,
) {
  const actionLoading = ref(false);

  async function runLifecycleAction(
    action: LifecycleAction,
    concurrencyToken: number,
    changeSummary?: string,
  ): Promise<boolean> {
    if (!entry.value) {
      return false;
    }

    actionLoading.value = true;

    try {
      const id = entry.value.id;
      let updated: ContentEntry;

      switch (action) {
        case 'submit':
          updated = await submitContentForReview(id, { concurrencyToken });
          break;
        case 'withdraw':
          updated = await withdrawContentFromReview(id, { concurrencyToken });
          break;
        case 'publish':
          updated = await publishContentEntry(id, {
            changeSummary: changeSummary ?? '',
            concurrencyToken,
          });
          break;
        case 'unpublish':
          updated = await unpublishContentEntry(id, {
            changeSummary: changeSummary ?? '',
            concurrencyToken,
          });
          break;
        case 'archive':
          updated = await archiveContentEntry(id, {
            changeSummary: changeSummary ?? '',
            concurrencyToken,
          });
          break;
        case 'restore':
          updated = await restoreContentEntry(id, {
            changeSummary: changeSummary ?? '',
            concurrencyToken,
          });
          break;
        case 'delete':
          await deleteContentEntry(id, { concurrencyToken });
          return true;
        default:
          return false;
      }

      onEntryUpdated(updated);
      return true;
    } catch (error) {
      if (
        error instanceof ApiError &&
        error.isConcurrencyConflict &&
        isConcurrencyConflict(error.problem)
      ) {
        onConflict(error.problem);
        return false;
      }

      onError(error, 'Action failed');
      return false;
    } finally {
      actionLoading.value = false;
    }
  }

  async function restoreVersion(
    versionNumber: number,
    concurrencyToken: number,
    changeSummary: string,
  ): Promise<boolean> {
    if (!entry.value) {
      return false;
    }

    actionLoading.value = true;

    try {
      const updated = await restoreContentVersion(entry.value.id, versionNumber, {
        changeSummary,
        concurrencyToken,
      });
      onEntryUpdated(updated);
      return true;
    } catch (error) {
      if (
        error instanceof ApiError &&
        error.isConcurrencyConflict &&
        isConcurrencyConflict(error.problem)
      ) {
        onConflict(error.problem);
        return false;
      }

      onError(error, 'Failed to restore version');
      return false;
    } finally {
      actionLoading.value = false;
    }
  }

  return {
    actionLoading,
    runLifecycleAction,
    restoreVersion,
  };
}
