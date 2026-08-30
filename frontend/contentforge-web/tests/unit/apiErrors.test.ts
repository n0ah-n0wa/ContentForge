import { describe, expect, it } from 'vitest';
import { ApiError, getValidationMessages, isValidationProblem } from '@/api/errors';

describe('ApiError', () => {
  it('parses validation problem details', async () => {
    const response = new Response(
      JSON.stringify({
        title: 'Validation failed',
        status: 422,
        detail: 'One or more fields are invalid.',
        errors: {
          email: ['Email is required.'],
        },
      }),
      {
        status: 422,
        headers: { 'Content-Type': 'application/problem+json' },
      },
    );

    const error = await ApiError.fromResponse(response);

    expect(error.isValidationError).toBe(true);
    expect(isValidationProblem(error.problem)).toBe(true);
    expect(getValidationMessages(error.problem)).toContain('Email is required.');
  });
});
