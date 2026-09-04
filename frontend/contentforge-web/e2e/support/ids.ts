import { randomBytes } from 'node:crypto';

/** Deterministic unique suffixes for isolated E2E data (no shared counters). */

export function uniqueSuffix(prefix = 'e2e'): string {
  const stamp = Date.now().toString(36);
  const rand = randomBytes(4).toString('hex');
  return `${prefix}-${stamp}-${rand}`;
}

export function uniqueEmail(role: string): string {
  return `${uniqueSuffix(role)}@contentforge.e2e`;
}

export function uniqueSlug(prefix = 'entry'): string {
  return uniqueSuffix(prefix)
    .toLowerCase()
    .replace(/[^a-z0-9-]/g, '-');
}
