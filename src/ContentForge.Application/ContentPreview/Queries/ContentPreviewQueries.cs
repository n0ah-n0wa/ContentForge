namespace ContentForge.Application.ContentPreview.Queries;

using ContentForge.Application.Abstractions;
using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.ContentPreview.Models;
using ContentForge.Application.Mapping;
using ContentForge.Domain.Common;

public sealed record GetContentPreviewQuery(string Token);

public sealed class GetContentPreviewQueryHandler
{
    private readonly IContentPreviewTokenRepository _previewTokenRepository;
    private readonly IContentEntryRepository _contentEntryRepository;
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IDateTimeProvider _clock;

    public GetContentPreviewQueryHandler(
        IContentPreviewTokenRepository previewTokenRepository,
        IContentEntryRepository contentEntryRepository,
        IContentTypeRepository contentTypeRepository,
        IDateTimeProvider clock)
    {
        _previewTokenRepository = previewTokenRepository;
        _contentEntryRepository = contentEntryRepository;
        _contentTypeRepository = contentTypeRepository;
        _clock = clock;
    }

    public async Task<ContentPreviewDto> HandleAsync(
        GetContentPreviewQuery query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Token))
        {
            throw new UnauthorizedApplicationException("Preview access is invalid or has expired.");
        }

        var now = _clock.UtcNow;
        var previewToken = await _previewTokenRepository.FindActiveAsync(query.Token, now, cancellationToken)
            ?? throw new UnauthorizedApplicationException("Preview access is invalid or has expired.");

        var entry = await _contentEntryRepository.GetByIdAsync(
                ContentEntryId.From(previewToken.ContentEntryId),
                cancellationToken)
            ?? throw new UnauthorizedApplicationException("Preview access is invalid or has expired.");

        if (entry.IsDeleted)
        {
            throw new UnauthorizedApplicationException("Preview access is invalid or has expired.");
        }

        var contentType = await _contentTypeRepository.GetByIdAsync(entry.ContentTypeId, cancellationToken)
            ?? throw new UnauthorizedApplicationException("Preview access is invalid or has expired.");

        if (!contentType.IsActive)
        {
            throw new UnauthorizedApplicationException("Preview access is invalid or has expired.");
        }

        return ContentEntryMapper.ToPreviewDto(contentType.Slug.Value, entry, contentType, previewToken.ExpiresAt);
    }
}
