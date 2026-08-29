namespace ContentForge.Infrastructure.Persistence.Converters;

using System.Text.Json;
using System.Text.Json.Serialization;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;
using ContentForge.Domain.Common;

internal static class JsonPersistence
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

internal sealed record FieldConfigurationDto(
    bool IsRequired,
    string? DefaultValue,
    int? MinLength,
    int? MaxLength,
    decimal? MinValue,
    decimal? MaxValue,
    string? Pattern,
    IReadOnlyList<string> Options,
    Guid? RelationTarget,
    string? RelationCardinality,
    bool AllowMultiple);

internal sealed record ContentSnapshotDto(
    string Slug,
    Dictionary<string, object?> Data,
    string Status);

internal static class PersistenceJsonConverter
{
    internal static string SerializeContentData(ContentData data) =>
        JsonSerializer.Serialize(data.Values, JsonPersistence.Options);

    internal static ContentData DeserializeContentData(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ContentData.Empty;
        }

        var values = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonPersistence.Options)
            ?? new Dictionary<string, object?>(StringComparer.Ordinal);

        var normalized = values.ToDictionary(
            pair => pair.Key,
            pair => NormalizeJsonValue(pair.Value),
            StringComparer.Ordinal);

        return ContentData.FromDictionary(normalized);
    }

    internal static string SerializeFieldConfiguration(FieldConfiguration configuration)
    {
        var dto = new FieldConfigurationDto(
            configuration.IsRequired,
            configuration.DefaultValue,
            configuration.MinLength,
            configuration.MaxLength,
            configuration.MinValue,
            configuration.MaxValue,
            configuration.Pattern,
            configuration.Options.ToList(),
            configuration.RelationTarget?.Value,
            configuration.RelationCardinality?.ToString(),
            configuration.AllowMultiple);

        return JsonSerializer.Serialize(dto, JsonPersistence.Options);
    }

    internal static FieldConfiguration DeserializeFieldConfiguration(string json)
    {
        var dto = JsonSerializer.Deserialize<FieldConfigurationDto>(json, JsonPersistence.Options)
            ?? throw new InvalidOperationException("Field configuration payload is invalid.");

        RelationCardinality? cardinality = null;
        if (!string.IsNullOrWhiteSpace(dto.RelationCardinality)
            && Enum.TryParse<RelationCardinality>(dto.RelationCardinality, out var parsed))
        {
            cardinality = parsed;
        }

        ContentTypeId? relationTarget = dto.RelationTarget is { } targetId && targetId != Guid.Empty
            ? ContentTypeId.From(targetId)
            : null;

        return FieldConfiguration.Create(
            dto.IsRequired,
            dto.DefaultValue,
            dto.MinLength,
            dto.MaxLength,
            dto.MinValue,
            dto.MaxValue,
            dto.Pattern,
            dto.Options,
            relationTarget,
            cardinality,
            dto.AllowMultiple);
    }

    internal static string SerializeContentSnapshot(ContentSnapshot snapshot)
    {
        var dto = new ContentSnapshotDto(
            snapshot.Slug.Value,
            snapshot.Data.Values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            snapshot.Status.ToString());

        return JsonSerializer.Serialize(dto, JsonPersistence.Options);
    }

    internal static ContentSnapshot DeserializeContentSnapshot(string json)
    {
        var dto = JsonSerializer.Deserialize<ContentSnapshotDto>(json, JsonPersistence.Options)
            ?? throw new InvalidOperationException("Content snapshot payload is invalid.");

        return new ContentSnapshot(
            Slug.Create(dto.Slug),
            ContentData.FromDictionary(
                dto.Data.ToDictionary(
                    pair => pair.Key,
                    pair => NormalizeJsonValue(pair.Value),
                    StringComparer.Ordinal)),
            Enum.Parse<ContentStatus>(dto.Status));
    }

    private static object? NormalizeJsonValue(object? value)
    {
        if (value is not JsonElement element)
        {
            return value;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(item => NormalizeJsonValue(item)).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => NormalizeJsonValue(property.Value),
                StringComparer.Ordinal),
            _ => element.GetRawText(),
        };
    }
}
