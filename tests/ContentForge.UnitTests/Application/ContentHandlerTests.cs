namespace ContentForge.UnitTests.Application;

using ContentForge.Application.Abstractions.Persistence;
using ContentForge.Application.Common.Concurrency;
using ContentForge.Application.Common.Exceptions;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Common.Sorting;
using ContentForge.Application.Content.Commands;
using ContentForge.Application.Content.Queries;
using ContentForge.Application.ContentTypes.Commands;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.Common;
using ContentForge.Domain.Content;
using ContentForge.Domain.ContentTypes;
using ContentForge.UnitTests.Domain;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

public sealed class ContentTypeHandlerTests
{
    [Fact]
    public async Task CreateContentTypeCommandHandler_ValidRequest_CreatesContentType()
    {
        var repository = Substitute.For<IContentTypeRepository>();
        repository.ExistsByNameAsync(Arg.Any<FieldName>(), Arg.Any<CancellationToken>()).Returns(false);
        repository.ExistsBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateContentTypeCommandHandler(
            repository,
            RepositorySubstituteExtensions.CreateUnitOfWork(),
            ApplicationTestData.CreateCurrentUser(ApplicationTestData.EditorUserId, ApplicationTestData.AdministratorRole),
            ApplicationTestData.CreateClock(),
            RepositorySubstituteExtensions.CreateAuditService(),
            new CreateContentTypeCommandValidator());

        var result = await handler.HandleAsync(
            new CreateContentTypeCommand("Page", "Page", "page", "Static pages"),
            CancellationToken.None);

        result.Name.Should().Be("Page");
        result.Slug.Should().Be("page");
        await repository.Received(1).AddAsync(Arg.Any<ContentType>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateContentTypeCommandHandler_DuplicateSlug_ThrowsValidationException()
    {
        var repository = Substitute.For<IContentTypeRepository>();
        repository.ExistsByNameAsync(Arg.Any<FieldName>(), Arg.Any<CancellationToken>()).Returns(false);
        repository.ExistsBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateContentTypeCommandHandler(
            repository,
            RepositorySubstituteExtensions.CreateUnitOfWork(),
            ApplicationTestData.CreateCurrentUser(ApplicationTestData.EditorUserId, ApplicationTestData.AdministratorRole),
            ApplicationTestData.CreateClock(),
            RepositorySubstituteExtensions.CreateAuditService(),
            new CreateContentTypeCommandValidator());

        var action = () => handler.HandleAsync(
            new CreateContentTypeCommand("Page", "Page", "page", null),
            CancellationToken.None);

        await action.Should().ThrowAsync<ApplicationValidationException>();
    }
}

public sealed class ContentEntryHandlerTests
{
    [Fact]
    public async Task CreateContentEntryCommandHandler_ValidRequest_CreatesDraftEntry()
    {
        var contentType = DomainTestData.CreateArticleType();
        var contentTypeRepository = Substitute.For<IContentTypeRepository>();
        contentTypeRepository.GetByIdAsync(contentType.Id, Arg.Any<CancellationToken>()).Returns(contentType);

        var entryRepository = Substitute.For<IContentEntryRepository>();
        entryRepository.ExistsBySlugAsync(contentType.Id, Arg.Any<Slug>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateContentEntryCommandHandler(
            contentTypeRepository,
            entryRepository,
            RepositorySubstituteExtensions.CreateUnitOfWork(),
            ApplicationTestData.CreateCurrentUser(ApplicationTestData.AuthorUserId, ApplicationTestData.AuthorRole),
            ApplicationTestData.CreateClock(),
            RepositorySubstituteExtensions.CreateAuditService(),
            new CreateContentEntryCommandValidator());

        var result = await handler.HandleAsync(
            new CreateContentEntryCommand(contentType.Id.Value, "hello-world", DomainTestData.CreateValidArticleData().Values.ToDictionary()),
            CancellationToken.None);

        result.Status.Should().Be(ContentStatus.Draft);
        result.Slug.Should().Be("hello-world");
        await entryRepository.Received(1).AddAsync(Arg.Any<ContentEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateContentEntryCommandHandler_AuthorEditingOthersContent_ThrowsForbidden()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);

        var contentTypeRepository = Substitute.For<IContentTypeRepository>();
        contentTypeRepository.GetByIdAsync(entry.ContentTypeId, Arg.Any<CancellationToken>()).Returns(contentType);

        var entryRepository = Substitute.For<IContentEntryRepository>();
        entryRepository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        var handler = new UpdateContentEntryCommandHandler(
            contentTypeRepository,
            entryRepository,
            RepositorySubstituteExtensions.CreateUnitOfWork(),
            ApplicationTestData.CreateCurrentUser(ApplicationTestData.OtherAuthorUserId, ApplicationTestData.AuthorRole),
            ApplicationTestData.CreateClock(),
            RepositorySubstituteExtensions.CreateAuditService(),
            new UpdateContentEntryCommandValidator());

        var action = () => handler.HandleAsync(
            new UpdateContentEntryCommand(
                entry.Id.Value,
                "hello-world",
                DomainTestData.CreateValidArticleData().Values.ToDictionary(),
                "update",
                new ConcurrencyRequest(entry.ConcurrencyToken.Value)),
            CancellationToken.None);

        await action.Should().ThrowAsync<ForbiddenApplicationException>();
    }

    [Fact]
    public async Task PublishContentCommandHandler_FromInReview_PublishesEntry()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);
        entry.SubmitForReview(contentType, ApplicationTestData.AuthorUserId, entry.ConcurrencyToken, ApplicationTestData.Timestamp);

        var contentTypeRepository = Substitute.For<IContentTypeRepository>();
        contentTypeRepository.GetByIdAsync(entry.ContentTypeId, Arg.Any<CancellationToken>()).Returns(contentType);

        var entryRepository = Substitute.For<IContentEntryRepository>();
        entryRepository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        var handler = new PublishContentCommandHandler(
            contentTypeRepository,
            entryRepository,
            RepositorySubstituteExtensions.CreateUnitOfWork(),
            ApplicationTestData.CreateCurrentUser(ApplicationTestData.EditorUserId, ApplicationTestData.EditorRole),
            ApplicationTestData.CreateClock(),
            RepositorySubstituteExtensions.CreateAuditService(),
            new PublishContentCommandValidator());

        var result = await handler.HandleAsync(
            new PublishContentCommand(entry.Id.Value, "Published first version", new ConcurrencyRequest(entry.ConcurrencyToken.Value)),
            CancellationToken.None);

        result.Status.Should().Be(ContentStatus.Published);
        result.PublishedData.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreContentVersionCommandHandler_ValidVersion_RestoresDraft()
    {
        var contentType = DomainTestData.CreateArticleType();
        var entry = DomainTestData.CreateDraftEntry(contentType);

        entry.UpdateDraft(
            contentType,
            ContentData.FromDictionary(new Dictionary<string, object?> { ["title"] = "Version One", ["body"] = "<p>Body</p>", ["publishedAt"] = ApplicationTestData.Timestamp }),
            entry.Slug,
            ApplicationTestData.AuthorUserId,
            entry.ConcurrencyToken,
            "first version",
            ApplicationTestData.Timestamp);

        entry.UpdateDraft(
            contentType,
            ContentData.FromDictionary(new Dictionary<string, object?> { ["title"] = "Version Two", ["body"] = "<p>Body</p>", ["publishedAt"] = ApplicationTestData.Timestamp }),
            entry.Slug,
            ApplicationTestData.AuthorUserId,
            entry.ConcurrencyToken,
            "second version",
            ApplicationTestData.Timestamp);

        var versionToRestore = entry.Versions.Single(version => version.ChangeSummary == "first version");

        var contentTypeRepository = Substitute.For<IContentTypeRepository>();
        contentTypeRepository.GetByIdAsync(entry.ContentTypeId, Arg.Any<CancellationToken>()).Returns(contentType);

        var entryRepository = Substitute.For<IContentEntryRepository>();
        entryRepository.GetByIdAsync(entry.Id, Arg.Any<CancellationToken>()).Returns(entry);

        var handler = new RestoreContentVersionCommandHandler(
            contentTypeRepository,
            entryRepository,
            RepositorySubstituteExtensions.CreateUnitOfWork(),
            ApplicationTestData.CreateCurrentUser(ApplicationTestData.EditorUserId, ApplicationTestData.EditorRole),
            ApplicationTestData.CreateClock(),
            RepositorySubstituteExtensions.CreateAuditService(),
            new RestoreContentVersionCommandValidator());

        var result = await handler.HandleAsync(
            new RestoreContentVersionCommand(
                entry.Id.Value,
                versionToRestore.VersionNumber.Value,
                "restore title",
                new ConcurrencyRequest(entry.ConcurrencyToken.Value)),
            CancellationToken.None);

        result.Status.Should().Be(ContentStatus.Draft);
        result.DraftData["title"].Should().Be("Version One");
    }

    [Fact]
    public async Task ListContentEntriesQueryHandler_UnsupportedSort_ThrowsUnsupportedQueryParameterException()
    {
        var handler = new ListContentEntriesQueryHandler(
            Substitute.For<IContentEntryRepository>(),
            ApplicationTestData.CreateCurrentUser(ApplicationTestData.EditorUserId, ApplicationTestData.EditorRole));

        var action = () => handler.HandleAsync(
            new ListContentEntriesQuery(
                new ContentEntryListCriteria(
                    new PaginationRequest(),
                    new SortRequest("unsupported"))),
            CancellationToken.None);

        await action.Should().ThrowAsync<ContentForge.Application.Common.Filtering.UnsupportedQueryParameterException>();
    }
}
