/**
 * Returns a same-origin relative path suitable for post-login redirects.
 * Rejects absolute URLs, protocol-relative URLs, and non-path values.
 */
export function resolveSafeInternalPath(candidate: unknown, fallback = '/dashboard'): string {
  if (typeof candidate !== 'string') {
    return fallback;
  }

  const trimmed = candidate.trim();
  if (!trimmed.startsWith('/') || trimmed.startsWith('//')) {
    return fallback;
  }

  // Block scheme-like paths such as "/\\evil" or encoded tricks that leave the app.
  if (trimmed.includes('\\') || /^\/[a-z]+:/i.test(trimmed)) {
    return fallback;
  }

  return trimmed;
}
