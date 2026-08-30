namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ContentForge.Application.Audit.Models;
using ContentForge.Application.Common.Pagination;
using ContentForge.Application.Content.Models;
using ContentForge.Application.ContentTypes.Models;
using ContentForge.Application.Media.Models;
using ContentForge.Application.Users.Models;
using ContentForge.Domain.Audit;
using ContentForge.Domain.Authorization;
using ContentForge.Domain.ContentTypes;
using ContentForge.Infrastructure.Persistence;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[Collection(PersistenceTests.Name)]
public sealed class AuditApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public AuditApiIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        await _databaseFixture.ResetDatabaseAsync();
        _factory = new ContentForgeWebApplicationFactory();
        _client = _factory.CreateClient();
        await AuthTestSeeder.SeedAsync(_factory.Services);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CriticalOperations_CreateExpectedAuditRecordsWithCorrelationAndActor()
    {
        const string correlationId = "audit-integration-correlation-001";
        var adminToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);

        var loginAudits = await ListAuditAsync(adminToken, AuditAction.LoginSucceeded, "User", AuthTestConstants.AdminUserId.ToString());
        loginAudits.Should().ContainSingle(entry =>
            entry.UserId == AuthTestConstants.AdminUserId
            && entry.EntityId == AuthTestConstants.AdminUserId.ToString()
            && entry.CorrelationId != null);

        using var failedLogin = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email = "missing@contentforge.test", password = "DefinitelyWrongPassword123!" }),
        };
        failedLogin.Headers.Add("X-Correlation-ID", correlationId);
        var failedResponse = await _client.SendAsync(failedLogin);
        failedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var failedAudits = await ListAuditAsync(adminToken, AuditAction.LoginFailed, "User", "missing@contentforge.test");
        failedAudits.Should().ContainSingle(entry =>
            entry.UserId == null
            && entry.CorrelationId == correlationId
            && entry.Metadata != null
            && !entry.Metadata.Contains("DefinitelyWrongPassword123!", StringComparison.Ordinal));

        var createdUser = await CreateUserAsync(adminToken, correlationId);
        (await ListAuditAsync(adminToken, AuditAction.UserCreated, "User", createdUser.Id.ToString()))
            .Should().ContainSingle(entry => entry.UserId == AuthTestConstants.AdminUserId && entry.CorrelationId == correlationId);

        await UpdateUserRoleAsync(adminToken, createdUser.Id, RoleName.Author, correlationId);
        (await ListAuditAsync(adminToken, AuditAction.UserRoleChanged, "User", createdUser.Id.ToString()))
            .Should().ContainSingle(entry =>
                entry.Metadata != null
                && entry.Metadata.Contains("Author", StringComparison.Ordinal)
                && entry.CorrelationId == correlationId);

        await DisableUserAsync(adminToken, createdUser.Id, correlationId);
        (await ListAuditAsync(adminToken, AuditAction.UserDisabled, "User", createdUser.Id.ToString()))
            .Should().ContainSingle(entry => entry.CorrelationId == correlationId);

        var contentType = await CreateContentTypeWithFieldsAsync(adminToken, correlationId);
        (await ListAuditAsync(adminToken, AuditAction.ContentTypeCreated, "ContentType", contentType.Id.ToString()))
            .Should().ContainSingle(entry => entry.CorrelationId == correlationId);
        (await ListAuditAsync(adminToken, AuditAction.ContentTypeUpdated, "ContentType", contentType.Id.ToString()))
            .Should().Contain(entry => entry.Metadata != null && entry.Metadata.Contains("fieldAdded", StringComparison.Ordinal));

        var authorToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AuthorEmail,
            AuthTestConstants.AuthorPassword);
        var editorToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.EditorEmail,
            AuthTestConstants.EditorPassword);

        var draft = await CreateDraftAsync(authorToken, contentType.Id, $"audit-{Guid.NewGuid():N}", correlationId);
        (await ListAuditAsync(adminToken, AuditAction.ContentCreated, "ContentEntry", draft.Id.ToString()))
            .Should().ContainSingle(entry => entry.UserId == AuthTestConstants.AuthorUserId && entry.CorrelationId == correlationId);

        var updated = await UpdateDraftAsync(authorToken, draft, "Updated title", correlationId);
        (await ListAuditAsync(adminToken, AuditAction.ContentUpdated, "ContentEntry", draft.Id.ToString()))
            .Should().ContainSingle(entry => entry.CorrelationId == correlationId);

        var submitted = await SubmitForReviewAsync(authorToken, updated, correlationId);
        (await ListAuditAsync(adminToken, AuditAction.ContentSubmittedForReview, "ContentEntry", draft.Id.ToString()))
            .Should().ContainSingle(entry => entry.CorrelationId == correlationId);

        var published = await PublishAsync(editorToken, submitted, correlationId);
        (await ListAuditAsync(adminToken, AuditAction.ContentPublished, "ContentEntry", draft.Id.ToString()))
            .Should().ContainSingle(entry => entry.UserId == AuthTestConstants.EditorUserId && entry.CorrelationId == correlationId);

        var unpublished = await UnpublishAsync(editorToken, published, correlationId);
        (await ListAuditAsync(adminToken, AuditAction.ContentUnpublished, "ContentEntry", draft.Id.ToString()))
            .Should().ContainSingle(entry => entry.CorrelationId == correlationId);

        var resubmitted = await SubmitForReviewAsync(authorToken, unpublished, correlationId);
        var republished = await PublishAsync(editorToken, resubmitted, correlationId);

        var archived = await ArchiveAsync(editorToken, republished, correlationId);
        (await ListAuditAsync(adminToken, AuditAction.ContentArchived, "ContentEntry", draft.Id.ToString()))
            .Should().ContainSingle(entry => entry.CorrelationId == correlationId);

        _ = await RestoreAsync(editorToken, archived, correlationId);
        (await ListAuditAsync(adminToken, AuditAction.ContentRestored, "ContentEntry", draft.Id.ToString()))
            .Should().ContainSingle(entry => entry.CorrelationId == correlationId);

        var media = await UploadMediaAsync(editorToken, correlationId);
        (await ListAuditAsync(adminToken, AuditAction.MediaUploaded, "MediaAsset", media.Id.ToString()))
            .Should().ContainSingle(entry => entry.CorrelationId == correlationId);

        await DeleteMediaAsync(editorToken, media.Id, correlationId);
        (await ListAuditAsync(adminToken, AuditAction.MediaDeleted, "MediaAsset", media.Id.ToString()))
            .Should().ContainSingle(entry => entry.CorrelationId == correlationId);
    }

    [Fact]
    public async Task AuditLogs_AreImmutableAtPersistenceLayer()
    {
        var adminToken = await ApiTestHelper.LoginAsync(
            _client,
            AuthTestConstants.AdminEmail,
            AuthTestConstants.AdminPassword);

        var audits = await ListAuditAsync(adminToken, AuditAction.LoginSucceeded, "User", AuthTestConstants.AdminUserId.ToString());
        var auditId = audits.Should().ContainSingle().Subject.Id;

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await dbContext.AuditLogs.SingleAsync(log => log.Id == auditId);
        entity.Metadata = "tampered";

        var action = () => dbContext.SaveChangesAsync();
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*immutable*");
    }

    private async Task<UserDto> CreateUserAsync(string token, string correlationId)
    {
        var email = $"audit-user-{Guid.NewGuid():N}@contentforge.test";
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/users",
            token,
            JsonContent.Create(new
            {
                email,
                displayName = "Audit User",
                password = "SecurePassword123!",
                role = RoleName.Viewer.ToString(),
            }),
            correlationId);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<UserDto>())!;
    }

    private async Task UpdateUserRoleAsync(string token, Guid userId, RoleName role, string correlationId)
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Put,
            $"/api/v1/users/{userId}",
            token,
            JsonContent.Create(new { displayName = "Audit User", role = role.ToString() }),
            correlationId);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task DisableUserAsync(string token, Guid userId, string correlationId)
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/users/{userId}/disable",
            token,
            correlationId: correlationId);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<ContentTypeDto> CreateContentTypeWithFieldsAsync(string token, string correlationId)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var createRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/content-types",
            token,
            JsonContent.Create(new
            {
                name = $"audit{suffix}",
                displayName = "Audit Type",
                slug = $"audit-{suffix}",
                description = "Audit coverage",
            }),
            correlationId);

        var createResponse = await _client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var contentType = (await createResponse.Content.ReadFromJsonAsync<ContentTypeDto>())!;

        using var fieldRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/content-types/{contentType.Id}/fields",
            token,
            JsonContent.Create(new
            {
                name = "title",
                fieldType = FieldType.Text,
                displayName = "Title",
                sortOrder = 0,
                configuration = ContentTypeApiScenario.BuildConfiguration(required: true),
            }),
            correlationId);

        var fieldResponse = await _client.SendAsync(fieldRequest);
        fieldResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await fieldResponse.Content.ReadFromJsonAsync<ContentTypeDto>())!;
    }

    private async Task<ContentEntryDto> CreateDraftAsync(string token, Guid contentTypeId, string slug, string correlationId)
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/v1/content",
            token,
            JsonContent.Create(new
            {
                contentTypeId,
                slug,
                data = new Dictionary<string, object?> { ["title"] = "Audit draft" },
            }),
            correlationId);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private async Task<ContentEntryDto> UpdateDraftAsync(string token, ContentEntryDto entry, string title, string correlationId)
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Put,
            $"/api/v1/content/{entry.Id}",
            token,
            JsonContent.Create(new
            {
                slug = entry.Slug,
                changeSummary = "audit update",
                concurrencyToken = entry.ConcurrencyToken,
                data = new Dictionary<string, object?> { ["title"] = title },
            }),
            correlationId);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private async Task<ContentEntryDto> SubmitForReviewAsync(string token, ContentEntryDto entry, string correlationId)
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/content/{entry.Id}/submit-for-review",
            token,
            JsonContent.Create(new { concurrencyToken = entry.ConcurrencyToken }),
            correlationId);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private async Task<ContentEntryDto> PublishAsync(string token, ContentEntryDto entry, string correlationId)
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/content/{entry.Id}/publish",
            token,
            JsonContent.Create(new { changeSummary = "audit publish", concurrencyToken = entry.ConcurrencyToken }),
            correlationId);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private async Task<ContentEntryDto> UnpublishAsync(string token, ContentEntryDto entry, string correlationId)
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/content/{entry.Id}/unpublish",
            token,
            JsonContent.Create(new { changeSummary = "audit unpublish", concurrencyToken = entry.ConcurrencyToken }),
            correlationId);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private async Task<ContentEntryDto> ArchiveAsync(string token, ContentEntryDto entry, string correlationId)
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/content/{entry.Id}/archive",
            token,
            JsonContent.Create(new { changeSummary = "audit archive", concurrencyToken = entry.ConcurrencyToken }),
            correlationId);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private async Task<ContentEntryDto> RestoreAsync(string token, ContentEntryDto entry, string correlationId)
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/content/{entry.Id}/restore",
            token,
            JsonContent.Create(new { changeSummary = "audit restore", concurrencyToken = entry.ConcurrencyToken }),
            correlationId);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ContentEntryDto>())!;
    }

    private async Task<MediaAssetDto> UploadMediaAsync(string token, string correlationId)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("audit-media"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "audit.txt");

        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/media", token, content, correlationId);
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<MediaAssetDto>())!;
    }

    private async Task DeleteMediaAsync(string token, Guid mediaId, string correlationId)
    {
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Delete,
            $"/api/v1/media/{mediaId}",
            token,
            correlationId: correlationId);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<IReadOnlyList<AuditLogEntryDto>> ListAuditAsync(
        string token,
        AuditAction action,
        string entityType,
        string entityId)
    {
        using var request = ApiTestHelper.CreateAuthenticatedRequest(
            HttpMethod.Get,
            $"/api/v1/audit?action={action}&entityType={entityType}&entityId={entityId}&page=1&pageSize=50",
            token);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PaginatedResult<AuditLogEntryDto>>();
        return page!.Items;
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string url,
        string token,
        HttpContent? content = null,
        string? correlationId = null)
    {
        var request = ApiTestHelper.CreateAuthenticatedRequest(method, url, token, content);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.Add("X-Correlation-ID", correlationId);
        }

        return request;
    }
}
