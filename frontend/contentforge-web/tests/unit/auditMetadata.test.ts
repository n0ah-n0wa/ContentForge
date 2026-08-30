import { describe, expect, it } from 'vitest';
import { sanitizeAuditMetadataForDisplay, truncateAuditUserAgent } from '@/utils/auditMetadata';

describe('auditMetadata utils', () => {
  it('redacts sensitive metadata keys before display', () => {
    const sanitized = sanitizeAuditMetadataForDisplay(
      JSON.stringify({
        previousRole: 'Viewer',
        password: 'secret-value',
        refreshToken: 'token-value',
      }),
    );

    expect(sanitized).toContain('Viewer');
    expect(sanitized).toContain('[REDACTED]');
    expect(sanitized).not.toContain('secret-value');
    expect(sanitized).not.toContain('token-value');
  });

  it('truncates long user agent strings', () => {
    const longAgent = 'Mozilla/'.padEnd(120, 'x');
    expect(truncateAuditUserAgent(longAgent)).toMatch(/…$/);
    expect(truncateAuditUserAgent('short-agent')).toBe('short-agent');
  });
});
