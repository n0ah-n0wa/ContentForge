import { describe, expect, it } from 'vitest';
import { resolveSafeInternalPath } from '@/utils/navigationSafety';

describe('resolveSafeInternalPath', () => {
  it('accepts relative app paths', () => {
    expect(resolveSafeInternalPath('/dashboard')).toBe('/dashboard');
    expect(resolveSafeInternalPath('/content/articles/123?tab=versions')).toBe(
      '/content/articles/123?tab=versions',
    );
  });

  it('rejects absolute and protocol-relative URLs', () => {
    expect(resolveSafeInternalPath('https://evil.example/phish')).toBe('/dashboard');
    expect(resolveSafeInternalPath('//evil.example/phish')).toBe('/dashboard');
    expect(resolveSafeInternalPath('http://evil.example')).toBe('/dashboard');
  });

  it('falls back for missing or invalid values', () => {
    expect(resolveSafeInternalPath(undefined)).toBe('/dashboard');
    expect(resolveSafeInternalPath('dashboard')).toBe('/dashboard');
    expect(resolveSafeInternalPath('/\\evil')).toBe('/dashboard');
  });
});
