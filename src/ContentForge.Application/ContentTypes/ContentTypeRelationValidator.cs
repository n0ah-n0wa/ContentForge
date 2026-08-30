namespace ContentForge.Application.ContentTypes;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.ContentTypes.Models;
using ContentForge.Domain.Common;
using ContentForge.Domain.ContentTypes;

internal static class ContentTypeRelationValidator
{
    internal static async Task EnsureRelationTargetExistsAsync(
        FieldType fieldType,
        FieldConfigurationDto configuration,
        ContentTypeId currentContentTypeId,
        IContentTypeRepository repository,
        CancellationToken cancellationToken)
    {
        if (fieldType is not (FieldType.Relation or FieldType.RelationMultiple))
        {
            return;
        }

        if (configuration.RelationTarget is null)
        {
            return;
        }

        var targetId = ContentTypeId.From(configuration.RelationTarget.Value);
        if (targetId == currentContentTypeId)
        {
            throw new ApplicationValidationException(
                nameof(configuration.RelationTarget),
                "Relation fields cannot target the same content type.");
        }

        var target = await repository.GetByIdAsync(targetId, cancellationToken);
        if (target is null)
        {
            throw new ApplicationValidationException(
                nameof(configuration.RelationTarget),
                "The specified relation target content type does not exist.");
        }
    }
}
