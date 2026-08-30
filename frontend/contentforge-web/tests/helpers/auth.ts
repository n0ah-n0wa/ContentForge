export function createTestJwt(payload: Record<string, unknown>): string {
  const encode = (value: Record<string, unknown>): string =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode(payload)}.signature`;
}

export const viewerPermissions = [
  'content.read',
  'contentType.read',
  'media.read',
  'content.version.read',
];

export const adminPermissions = [
  ...viewerPermissions,
  'user.read',
  'audit.read',
  'content.create',
  'content.update',
];
