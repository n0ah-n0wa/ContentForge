import { describe, expect, it } from 'vitest';
import { formatMediaSize, isImageMedia, resolveMediaUrl } from '@/utils/mediaUrl';

describe('mediaUrl', () => {
  it('resolves relative public urls against the api base', () => {
    expect(resolveMediaUrl('/media-files/media/abc.png')).toContain('/media-files/media/abc.png');
  });

  it('returns absolute urls unchanged', () => {
    expect(resolveMediaUrl('https://cdn.example.com/file.png')).toBe('https://cdn.example.com/file.png');
  });

  it('formats file sizes and detects image content types', () => {
    expect(formatMediaSize(2048)).toBe('2.0 KB');
    expect(isImageMedia('image/png')).toBe(true);
    expect(isImageMedia('application/pdf')).toBe(false);
  });
});
