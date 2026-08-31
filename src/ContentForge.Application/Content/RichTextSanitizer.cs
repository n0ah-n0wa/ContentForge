namespace ContentForge.Application.Content;

using AngleSharp.Dom;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;
using Ganss.Xss;

/// <summary>
/// Server-side HTML allowlist sanitizer for RichText fields. Mirrors the frontend allowlist in richTextSanitizer.ts.
/// </summary>
internal static class RichTextSanitizer
{
    private static readonly string[] _permittedTags =
    [
        "a",
        "b",
        "blockquote",
        "br",
        "code",
        "div",
        "em",
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
        "hr",
        "i",
        "li",
        "ol",
        "p",
        "pre",
        "span",
        "strong",
        "sub",
        "sup",
        "u",
        "ul",
    ];

    private static readonly HtmlSanitizer _sanitizer = CreateSanitizer();

    internal static string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return _sanitizer.Sanitize(html);
    }

    internal static ContentData SanitizeRichTextFields(ContentType contentType, ContentData data)
    {
        var richTextFields = contentType.Fields
            .Where(field => field.FieldType == FieldType.RichText)
            .Select(field => field.Name.Value)
            .ToHashSet(StringComparer.Ordinal);

        if (richTextFields.Count == 0)
        {
            return data;
        }

        var sanitizedValues = new Dictionary<string, object?>(data.Values, StringComparer.Ordinal);
        var changed = false;

        foreach (var fieldName in richTextFields)
        {
            if (!sanitizedValues.TryGetValue(fieldName, out var value) || value is not string html)
            {
                continue;
            }

            var cleaned = Sanitize(html);
            if (!string.Equals(cleaned, html, StringComparison.Ordinal))
            {
                changed = true;
            }

            sanitizedValues[fieldName] = cleaned;
        }

        return changed ? ContentData.FromDictionary(sanitizedValues) : data;
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var options = new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(_permittedTags, StringComparer.OrdinalIgnoreCase),
            AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "class",
                "href",
                "title",
                "target",
                "rel",
            },
            AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "http",
                "https",
                "mailto",
            },
            UriAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "href",
            },
        };

        var sanitizer = new HtmlSanitizer(options);

        sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is IElement element
                && element.NodeName.Equals("a", StringComparison.OrdinalIgnoreCase)
                && element.GetAttribute("target") == "_blank")
            {
                element.SetAttribute("rel", "noopener noreferrer");
            }
        };

        return sanitizer;
    }
}
