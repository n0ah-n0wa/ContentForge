import { describe, expect, it } from 'vitest';
import {
  decodeJwtPayload,
  extractPermissionsFromPayload,
  extractPermissionsFromToken,
  isAccessTokenExpired,
} from '@/utils/jwt';
import { createTestJwt, viewerPermissions } from '../helpers/auth';

describe('jwt utils', () => {
  it('extracts permission claims from token payload', () => {
    const token = createTestJwt({
      sub: 'user-1',
      permission: viewerPermissions,
      exp: Math.floor(Date.now() / 1000) + 3600,
    });

    expect(extractPermissionsFromToken(token)).toEqual(viewerPermissions);
  });

  it('detects expired access tokens', () => {
    expect(isAccessTokenExpired(new Date(Date.now() - 60_000).toISOString())).toBe(true);
    expect(isAccessTokenExpired(new Date(Date.now() + 60_000).toISOString())).toBe(false);
  });

  it('returns null for malformed tokens', () => {
    expect(decodeJwtPayload('not-a-jwt')).toBeNull();
    expect(extractPermissionsFromPayload({})).toEqual([]);
  });
});
