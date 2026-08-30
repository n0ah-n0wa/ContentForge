namespace ContentForge.UnitTests.Application;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Application.Content.Queries;
using ContentForge.Application.PublicContent.Queries;
using ContentForge.Domain.Content;
using ContentForge.UnitTests.Domain;
using FluentAssertions;
using NSubstitute;

public sealed class PublicContentHandlerTests
{
    [Fact]
    public async Task GetPublicContentBySlugQueryHandler_UnpublishedEntry_ThrowsNotFound()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);

        var contentTypeRepository = Substitute.For<IContentTypeRepository>();
        contentTypeRepository.GetBySlugAsync(contentType.Slug, Arg.Any<CancellationToken>()).Returns(contentType);

        var entryRepository = Substitute.For<IContentEntryRepository>();
        entryRepository.GetBySlugAsync(contentType.Id, entry.Slug, Arg.Any<CancellationToken>()).Returns(entry);

        var handler = new GetPublicContentBySlugQueryHandler(contentTypeRepository, entryRepository);

        var action = () => handler.HandleAsync(
            new GetPublicContentBySlugQuery("article", "hello-world"),
            CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundApplicationException>();
    }

    [Fact]
    public async Task GetPublicContentBySlugQueryHandler_PublishedEntry_ReturnsPublicDto()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);
        entry.SubmitForReview(DomainTestData.User1, entry.ConcurrencyToken, DomainTestData.Timestamp);
        entry.Publish(contentType, DomainTestData.User1, entry.ConcurrencyToken, "publish", DomainTestData.Timestamp);

        var contentTypeRepository = Substitute.For<IContentTypeRepository>();
        contentTypeRepository.GetBySlugAsync(contentType.Slug, Arg.Any<CancellationToken>()).Returns(contentType);

        var entryRepository = Substitute.For<IContentEntryRepository>();
        entryRepository.GetBySlugAsync(contentType.Id, entry.Slug, Arg.Any<CancellationToken>()).Returns(entry);

        var handler = new GetPublicContentBySlugQueryHandler(contentTypeRepository, entryRepository);

        var result = await handler.HandleAsync(
            new GetPublicContentBySlugQuery("article", "hello-world"),
            CancellationToken.None);

        result.ContentTypeSlug.Should().Be("article");
        result.Slug.Should().Be("hello-world");
        result.Data.Should().ContainKey("title");
        result.Data.Should().NotContainKey("_audit");
    }

    [Fact]
    public async Task GetPublicContentBySlugQueryHandler_InReviewEntry_ThrowsNotFound()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);
        entry.SubmitForReview(DomainTestData.User1, entry.ConcurrencyToken, DomainTestData.Timestamp);

        var contentTypeRepository = Substitute.For<IContentTypeRepository>();
        contentTypeRepository.GetBySlugAsync(contentType.Slug, Arg.Any<CancellationToken>()).Returns(contentType);

        var entryRepository = Substitute.For<IContentEntryRepository>();
        entryRepository.GetBySlugAsync(contentType.Id, entry.Slug, Arg.Any<CancellationToken>()).Returns(entry);

        var handler = new GetPublicContentBySlugQueryHandler(contentTypeRepository, entryRepository);

        var action = () => handler.HandleAsync(
            new GetPublicContentBySlugQuery("article", "hello-world"),
            CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundApplicationException>();
    }

    [Fact]
    public async Task GetPublicContentBySlugQueryHandler_InactiveContentType_ThrowsNotFound()
    {
        var contentType = DomainTestData.CreateArticleType();
        contentType.Deactivate(DomainTestData.User1, DomainTestData.Timestamp);

        var contentTypeRepository = Substitute.For<IContentTypeRepository>();
        contentTypeRepository.GetBySlugAsync(contentType.Slug, Arg.Any<CancellationToken>()).Returns(contentType);

        var handler = new GetPublicContentBySlugQueryHandler(
            contentTypeRepository,
            Substitute.For<IContentEntryRepository>());

        var action = () => handler.HandleAsync(
            new GetPublicContentBySlugQuery("article", "hello-world"),
            CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundApplicationException>();
    }

    [Fact]
    public async Task ListPublicContentQueryHandler_UnsupportedFilter_ThrowsUnsupportedQueryParameterException()
    {
        var handler = new ListPublicContentQueryHandler(
            Substitute.For<IContentTypeRepository>(),
            Substitute.For<IContentEntryRepository>());

        var action = () => handler.HandleAsync(
            new ListPublicContentQuery(
                new PublicContentListCriteria(
                    new PaginationRequest(),
                    SortRequest.Default("publishedAt"),
                    "article",
                    UnsupportedFilters: new Dictionary<string, string?> { ["status"] = "draft" })),
            CancellationToken.None);

        await action.Should().ThrowAsync<ContentForge.Application.Common.Filtering.UnsupportedQueryParameterException>();
    }
}

public sealed class PaginationContractTests
{
    [Fact]
    public void PaginationRequest_ClampsPageSizeToMaximum()
    {
        var pagination = new PaginationRequest(1, 500);

        pagination.NormalizedPageSize.Should().Be(PaginationDefaults.MaxPageSize);
    }

    [Fact]
    public void PaginatedResult_TotalPages_CalculatesCorrectly()
    {
        var result = new PaginatedResult<string>(["a"], 1, 20, 45);

        result.TotalPages.Should().Be(3);
    }
}
