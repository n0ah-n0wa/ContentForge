namespace ContentForge.Application.Media;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;

/// <summary>
/// Ensures content data only references existing, non-deleted media assets.
/// </summary>
internal static class MediaReferenceValidator
{
    internal static async Task ValidateAsync(
        ContentType contentType,
        ContentData data,
        IMediaRepository mediaRepository,
        CancellationToken cancellationToken)
    {
        var mediaIds = CollectMediaIds(contentType, data);
        if (mediaIds.Count == 0)
        {
            return;
        }

        var unavailable = await mediaRepository
            .FindUnavailableIdsAsync(mediaIds, cancellationToken)
            .ConfigureAwait(false);

        if (unavailable.Count > 0)
        {
            throw new ApplicationValidationException(
                string.Join(',', unavailable.Select(id => id.ToString("D"))),
                "One or more referenced media assets do not exist or have been deleted.");
        }
    }

    private static List<Guid> CollectMediaIds(ContentType contentType, ContentData data)
    {
        var mediaIds = new List<Guid>();

        foreach (var field in contentType.Fields)
        {
            if (!data.Values.TryGetValue(field.Name.Value, out var value) || value is null)
            {
                continue;
            }

            switch (field.FieldType)
            {
                case FieldType.Media when TryGetMediaId(value, out var mediaId):
                    mediaIds.Add(mediaId);
                    break;
                case FieldType.MediaMultiple when value is IEnumerable<Guid> mediaIdsValue:
                    mediaIds.AddRange(mediaIdsValue);
                    break;
                case FieldType.MediaMultiple:
                    mediaIds.AddRange(CollectMediaIdsFromCollection(value));
                    break;
            }
        }

        return mediaIds;
    }

    private static IEnumerable<Guid> CollectMediaIdsFromCollection(object value)
    {
        if (value is not IEnumerable<object?> items)
        {
            yield break;
        }

        foreach (var item in items)
        {
            if (TryGetMediaId(item, out var mediaId))
            {
                yield return mediaId;
            }
        }
    }

    private static bool TryGetMediaId(object? value, out Guid mediaId)
    {
        switch (value)
        {
            case Guid guid when guid != Guid.Empty:
                mediaId = guid;
                return true;
            case string text when Guid.TryParse(text, out var parsed) && parsed != Guid.Empty:
                mediaId = parsed;
                return true;
            default:
                mediaId = default;
                return false;
        }
    }
}
