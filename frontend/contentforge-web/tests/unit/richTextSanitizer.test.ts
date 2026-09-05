import { describe, expect, it } from 'vitest';
import { sanitizeRichText, richTextToPlainText } from '@/utils/richTextSanitizer';

describe('richTextSanitizer', () => {
  it('removes script tags and event handler attributes', () => {
    const dirty = '<p>Hello</p><script>alert(1)</script><img src=x onerror="alert(1)" />';
    const clean = sanitizeRichText(dirty);

    expect(clean).toContain('<p>Hello</p>');
    expect(clean).not.toContain('<script');
    expect(clean).not.toContain('onerror');
  });

  it('blocks unsafe link protocols', () => {
    const dirty = '<a href="javascript:alert(1)">Click</a>';
    const clean = sanitizeRichText(dirty);

    expect(clean).not.toContain('javascript:');
  });

  it('blocks protocol-relative hrefs', () => {
    const dirty = '<a href="//evil.example/path">Click</a>';
    const clean = sanitizeRichText(dirty);

    expect(clean).not.toContain('//evil.example');
  });

  it('extracts plain text from sanitized html', () => {
    expect(richTextToPlainText('<p><strong>Title</strong></p>')).toBe('Title');
  });
});
