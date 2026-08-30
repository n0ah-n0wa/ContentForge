import { ApiError } from '@/api/errors';

const CONFIRMATION_HINT = 'requires explicit confirmation';

export function isConfirmationRequiredError(error: unknown): boolean {
  if (!(error instanceof ApiError)) {
    return false;
  }

  const detail = error.problem.detail ?? error.message;
  return detail.includes(CONFIRMATION_HINT);
}

export function getConfirmationMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.problem.detail ?? error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'This change may affect existing content. Confirm to continue.';
}

export function isDependentEntriesDeleteError(error: unknown): boolean {
  if (!(error instanceof ApiError)) {
    return false;
  }

  const detail = error.problem.detail ?? error.message;
  return detail.toLowerCase().includes('dependent entries');
}
