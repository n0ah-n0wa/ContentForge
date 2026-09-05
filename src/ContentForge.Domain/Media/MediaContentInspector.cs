namespace ContentForge.Domain.Media;

using System.Text;
using ContentForge.Domain.Common;

/// <summary>
/// Validates uploaded binary content against declared media types using magic-byte signatures.
/// </summary>
public static class MediaContentInspector
{
    public static void EnsureMatchesDeclaredType(ReadOnlySpan<byte> content, string extension)
    {
        var normalizedExtension = MediaUploadRules.NormalizeExtension(extension);

        if (!MatchesSignature(content, normalizedExtension))
        {
            throw new DomainValidationException(
                nameof(extension),
                "File content does not match the declared media type.");
        }

        if (normalizedExtension == ".svg")
        {
            EnsureSafeSvg(content);
        }

        if (normalizedExtension == ".txt")
        {
            EnsurePlainText(content);
        }
    }

    private static bool MatchesSignature(ReadOnlySpan<byte> content, string extension) =>
        extension switch
        {
            ".jpg" or ".jpeg" => content.Length >= 3
                && content[0] == 0xFF
                && content[1] == 0xD8
                && content[2] == 0xFF,
            ".png" => content.Length >= 8
                && content[0] == 0x89
                && content[1] == (byte)'P'
                && content[2] == (byte)'N'
                && content[3] == (byte)'G'
                && content[4] == 0x0D
                && content[5] == 0x0A
                && content[6] == 0x1A
                && content[7] == 0x0A,
            ".gif" => content.Length >= 6
                && content[0] == (byte)'G'
                && content[1] == (byte)'I'
                && content[2] == (byte)'F'
                && content[3] == (byte)'8'
                && (content[4] == (byte)'7' || content[4] == (byte)'9')
                && content[5] == (byte)'a',
            ".webp" => content.Length >= 12
                && content[0] == (byte)'R'
                && content[1] == (byte)'I'
                && content[2] == (byte)'F'
                && content[3] == (byte)'F'
                && content[8] == (byte)'W'
                && content[9] == (byte)'E'
                && content[10] == (byte)'B'
                && content[11] == (byte)'P',
            ".pdf" => content.Length >= 4
                && content[0] == (byte)'%'
                && content[1] == (byte)'P'
                && content[2] == (byte)'D'
                && content[3] == (byte)'F',
            ".txt" => content.Length > 0,
            ".mp4" => content.Length >= 12
                && content[4] == (byte)'f'
                && content[5] == (byte)'t'
                && content[6] == (byte)'y'
                && content[7] == (byte)'p',
            ".webm" => content.Length >= 4
                && content[0] == 0x1A
                && content[1] == 0x45
                && content[2] == 0xDF
                && content[3] == 0xA3,
            ".mp3" => content.Length >= 3
                && ((content[0] == (byte)'I' && content[1] == (byte)'D' && content[2] == (byte)'3')
                    || (content[0] == 0xFF && (content[1] & 0xE0) == 0xE0)),
            ".svg" => LooksLikeSvg(content),
            _ => false,
        };

    private static bool LooksLikeSvg(ReadOnlySpan<byte> content)
    {
        var text = Encoding.UTF8.GetString(content).TrimStart();
        return text.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureSafeSvg(ReadOnlySpan<byte> content)
    {
        var text = Encoding.UTF8.GetString(content);
        if (ContainsUnsafeSvgMarker(text))
        {
            throw new DomainValidationException(
                "content",
                "SVG uploads must not contain executable script content.");
        }
    }

    private static bool ContainsUnsafeSvgMarker(string text)
    {
        ReadOnlySpan<string> forbidden =
        [
            "<script",
            "</script",
            "javascript:",
            "vbscript:",
            "data:text/html",
            "data:image/svg+xml",
            "<foreignobject",
            "<iframe",
            "<embed",
            "<object",
        ];

        foreach (var marker in forbidden)
        {
            if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Block inline event handlers (onload=, onerror=, onclick=, …).
        for (var index = 0; index < text.Length - 2; index++)
        {
            if (text[index] is not ('o' or 'O') || text[index + 1] is not ('n' or 'N'))
            {
                continue;
            }

            var nameStart = index + 2;
            var cursor = nameStart;
            while (cursor < text.Length && char.IsLetter(text[cursor]))
            {
                cursor++;
            }

            if (cursor == nameStart)
            {
                continue;
            }

            while (cursor < text.Length && char.IsWhiteSpace(text[cursor]))
            {
                cursor++;
            }

            if (cursor < text.Length && text[cursor] == '=')
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsurePlainText(ReadOnlySpan<byte> content)
    {
        if (content.Contains((byte)0))
        {
            throw new DomainValidationException(
                "content",
                "Text uploads must not contain null bytes.");
        }
    }
}
