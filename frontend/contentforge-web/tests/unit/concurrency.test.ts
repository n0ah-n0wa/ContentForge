import { describe, expect, it } from 'vitest';
import { describeConcurrencyConflict, isConcurrencyConflict } from '@/api/concurrency';

describe('concurrency conflict contract', () => {
  it('accepts a structured 409 payload from the API', () => {
    const payload = {
      status: 409,
      title: 'Conflict',
      detail: 'The resource was modified by another user.',
      expectedVersion: 3,
      actualVersion: 4,
      updatedAt: '2026-08-30T09:00:00+00:00',
    };

    expect(isConcurrencyConflict(payload)).toBe(true);
    expect(describeConcurrencyConflict(payload)).toContain('version 3');
    expect(describeConcurrencyConflict(payload)).toContain('current is 4');
  });

  it('rejects unstructured errors', () => {
    expect(isConcurrencyConflict({ status: 409, title: 'Conflict' })).toBe(false);
    expect(isConcurrencyConflict({ status: 422, expectedVersion: 1, actualVersion: 2, updatedAt: '' })).toBe(false);
  });
});
