const PERMISSION_CLAIM = 'permission';

export interface JwtPayload {
  sub?: string;
  exp?: number;
  permission?: string | string[];
  [key: string]: unknown;
}

export function decodeJwtPayload(token: string): JwtPayload | null {
  const segments = token.split('.');
  if (segments.length !== 3) {
    return null;
  }

  try {
    const normalized = segments[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=');
    const json = atob(padded);
    return JSON.parse(json) as JwtPayload;
  } catch {
    return null;
  }
}

export function extractPermissionsFromToken(token: string | null | undefined): string[] {
  if (!token) {
    return [];
  }

  const payload = decodeJwtPayload(token);
  if (!payload) {
    return [];
  }

  return extractPermissionsFromPayload(payload);
}

export function extractPermissionsFromPayload(payload: JwtPayload): string[] {
  const claim = payload[PERMISSION_CLAIM] ?? payload.permission;
  if (Array.isArray(claim)) {
    return claim.filter((value): value is string => typeof value === 'string');
  }

  if (typeof claim === 'string') {
    return [claim];
  }

  return [];
}

export function isAccessTokenExpired(
  expiresAt: string | null | undefined,
  bufferMs = 30_000,
): boolean {
  if (!expiresAt) {
    return true;
  }

  const expiry = Date.parse(expiresAt);
  if (Number.isNaN(expiry)) {
    return true;
  }

  return Date.now() >= expiry - bufferMs;
}

export function getTokenExpiryIso(token: string): string | null {
  const payload = decodeJwtPayload(token);
  if (!payload?.exp) {
    return null;
  }

  return new Date(payload.exp * 1000).toISOString();
}
