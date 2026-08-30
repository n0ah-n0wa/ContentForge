namespace ContentForge.Application.ContentTypes;

using ContentForge.Application.ContentTypes.Models;
using ContentForge.Domain.Common;
using ContentForge.Domain.ContentTypes;

internal static class ContentTypeFieldMapper
{
    internal static FieldConfiguration ToDomainConfiguration(FieldConfigurationDto configuration) =>
        FieldConfiguration.Create(
            isRequired: configuration.IsRequired,
            minLength: configuration.MinLength,
            maxLength: configuration.MaxLength,
            minValue: configuration.MinValue,
            maxValue: configuration.MaxValue,
            pattern: configuration.Pattern,
            allowMultiple: configuration.AllowMultiple,
            defaultValue: configuration.DefaultValue,
            options: configuration.Options,
            relationTarget: configuration.RelationTarget is null ? null : ContentTypeId.From(configuration.RelationTarget.Value),
            relationCardinality: configuration.RelationCardinality);
}
