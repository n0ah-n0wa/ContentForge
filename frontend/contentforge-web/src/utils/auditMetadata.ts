const SENSITIVE_METADATA_KEYS = new Set([
  'password',
  'passwordhash',
  'newpassword',
  'currentpassword',
  'refreshtoken',
  'accesstoken',
  'token',
  'secret',
  'authorization',
  'apikey',
  'clientsecret',
]);

export function sanitizeAuditMetadataForDisplay(metadata: string | null): string | null {
  if (!metadata?.trim()) {
    return null;
  }

  const trimmed = metadata.trim();
  if (!trimmed.startsWith('{') && !trimmed.startsWith('[')) {
    return trimmed;
  }

  try {
    const parsed: unknown = JSON.parse(trimmed);
    const sanitized = sanitizeValue(parsed);
    return JSON.stringify(sanitized, null, 2);
  } catch {
    return trimmed;
  }
}

function sanitizeValue(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map((item) => sanitizeValue(item));
  }

  if (value && typeof value === 'object') {
    const result: Record<string, unknown> = {};
    for (const [key, nested] of Object.entries(value)) {
      result[key] = SENSITIVE_METADATA_KEYS.has(key.toLowerCase())
        ? '[REDACTED]'
        : sanitizeValue(nested);
    }
    return result;
  }

  return value;
}

export function truncateAuditUserAgent(userAgent: string | null, maxLength = 96): string | null {
  if (!userAgent) {
    return null;
  }

  return userAgent.length <= maxLength ? userAgent : `${userAgent.slice(0, maxLength)}…`;
}
