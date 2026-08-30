import { describe, expect, it } from 'vitest';
import { ApiError } from '@/api/errors';
import {
  getConfirmationMessage,
  isConfirmationRequiredError,
  isDependentEntriesDeleteError,
} from '@/utils/destructiveChanges';

describe('destructiveChanges utils', () => {
  it('detects confirmation-required schema changes', () => {
    const error = new ApiError(422, {
      status: 422,
      title: 'Unprocessable Entity',
      detail: "Removing field 'summary' requires explicit confirmation because existing content may retain legacy values.",
    });

    expect(isConfirmationRequiredError(error)).toBe(true);
    expect(getConfirmationMessage(error)).toContain('summary');
  });

  it('detects dependent-entry deletion errors', () => {
    const error = new ApiError(422, {
      status: 422,
      title: 'Unprocessable Entity',
      detail: 'Content types with dependent entries require explicit safe deletion confirmation.',
    });

    expect(isDependentEntriesDeleteError(error)).toBe(true);
  });
});
