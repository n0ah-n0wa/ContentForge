import { ApiError } from '@/api/errors';
import { isConcurrencyConflict, describeConcurrencyConflict } from '@/api/concurrency';
import { useNotifications } from '@/composables/useNotifications';

export function useApiErrorHandling() {
  const { notifyError, notifyWarning } = useNotifications();

  function handleError(error: unknown, fallbackTitle = 'Request failed'): void {
    if (error instanceof ApiError) {
      if (error.isConcurrencyConflict && isConcurrencyConflict(error.problem)) {
        notifyWarning('Conflict detected', describeConcurrencyConflict(error.problem));
        return;
      }

      if (error.isUnauthorized) {
        notifyError('Authentication required', error.message);
        return;
      }

      if (error.isForbidden) {
        notifyError('Access denied', error.message);
        return;
      }

      notifyError(error.problem.title ?? fallbackTitle, error.message);
      return;
    }

    notifyError(fallbackTitle, error instanceof Error ? error.message : undefined);
  }

  return {
    handleError,
  };
}
