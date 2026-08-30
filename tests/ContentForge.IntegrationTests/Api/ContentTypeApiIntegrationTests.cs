namespace ContentForge.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using ContentForge.Application.ContentTypes.Models;
using ContentForge.Domain.ContentTypes;
using ContentForge.IntegrationTests.Auth;
using ContentForge.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

[Collection(PersistenceTests.Name)]
public sealed class ContentTypeApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlPersistenceFixture _databaseFixture;
    private ContentForgeWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private ContentTypeApiScenario _scenario = null!;

    public ContentTypeApiIntegrationTests(PostgreSqlPersistenceFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        await _databaseFixture.ResetDatabaseAsync();
        _factory = new ContentForgeWebApplicationFactory();
        _client = _factory.CreateClient();
        await AuthTestSeeder.SeedAsync(_factory.Services);
        _scenario = new ContentTypeApiScenario(_client);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Create_ValidContentType_Returns201()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var contentType = await _scenario.CreateContentTypeAsync(adminToken, suffix);

        contentType.Name.Should().Be($"type{suffix}");
        contentType.Slug.Should().Be($"dynamic-{suffix}");
        contentType.IsActive.Should().BeTrue();
        contentType.Fields.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ExistingContentType_Returns200()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var created = await _scenario.CreateContentTypeAsync(adminToken);

        var response = await _scenario.SendAsync(HttpMethod.Get, $"/api/v1/content-types/{created.Id}", adminToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loaded = await response.Content.ReadFromJsonAsync<ContentTypeDto>();
        loaded!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task List_ReturnsPaginatedResults()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var created = await _scenario.CreateContentTypeAsync(adminToken);

        var response = await _scenario.SendAsync(
            HttpMethod.Get,
            $"/api/v1/content-types?page=1&pageSize=10&sortBy=name",
            adminToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PaginatedContentTypes>();
        payload!.Items.Should().Contain(item => item.Id == created.Id);
    }

    [Fact]
    public async Task Update_ValidContentType_Returns200()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var created = await _scenario.CreateContentTypeAsync(adminToken);

        var response = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content-types/{created.Id}",
            adminToken,
            new
            {
                displayName = "Updated Type",
                slug = created.Slug,
                description = "Updated description",
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ContentTypeDto>();
        updated!.DisplayName.Should().Be("Updated Type");
        updated.Version.Should().BeGreaterThan(created.Version);
    }

    [Fact]
    public async Task Deactivate_ActiveContentType_Returns200AndSetsInactive()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var created = await _scenario.CreateContentTypeAsync(adminToken);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content-types/{created.Id}/deactivate",
            adminToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var deactivated = await response.Content.ReadFromJsonAsync<ContentTypeDto>();
        deactivated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_WithoutDependentEntries_Returns204()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var created = await _scenario.CreateContentTypeAsync(adminToken);

        var response = await _scenario.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/content-types/{created.Id}",
            adminToken,
            new { confirmedSafeDeletion = false });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithDependentEntriesWithoutConfirmation_Returns422()
    {
        var contentScenario = new ContentApiScenario(_client);
        var (contentType, adminToken, authorToken, _) = await contentScenario.SeedAsync();
        await contentScenario.CreateDraftAsync(authorToken, contentType.Id, $"entry-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/content-types/{contentType.Id}",
            adminToken,
            new { confirmedSafeDeletion = false });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Delete_WithDependentEntriesWithConfirmation_Returns204()
    {
        var contentScenario = new ContentApiScenario(_client);
        var (contentType, adminToken, authorToken, _) = await contentScenario.SeedAsync();
        await contentScenario.CreateDraftAsync(authorToken, contentType.Id, $"entry-{Guid.NewGuid():N}");

        var response = await _scenario.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/content-types/{contentType.Id}",
            adminToken,
            new { confirmedSafeDeletion = true });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Create_DuplicateName_Returns422()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var name = $"dupName{suffix}";
        await _scenario.CreateContentTypeAsync(adminToken, suffix, name: name, slug: $"slug-a-{suffix}");

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            "/api/v1/content-types",
            adminToken,
            new
            {
                name,
                displayName = "Duplicate",
                slug = $"slug-b-{suffix}",
                description = (string?)null,
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_DuplicateSlug_Returns422()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var slug = $"dup-slug-{suffix}";
        await _scenario.CreateContentTypeAsync(adminToken, suffix, name: $"nameA{suffix}", slug: slug);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            "/api/v1/content-types",
            adminToken,
            new
            {
                name = $"nameB{suffix}",
                displayName = "Duplicate Slug",
                slug,
                description = (string?)null,
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Update_DuplicateSlug_Returns422()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var first = await _scenario.CreateContentTypeAsync(adminToken, suffix, name: $"firstType{suffix}", slug: $"first-{suffix}");
        var second = await _scenario.CreateContentTypeAsync(adminToken, suffix, name: $"secondType{suffix}", slug: $"second-{suffix}");

        var response = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content-types/{second.Id}",
            adminToken,
            new
            {
                displayName = second.DisplayName,
                slug = first.Slug,
                description = second.Description,
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AddField_AllSupportedFieldTypes_Succeed()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var targetType = await _scenario.CreateContentTypeAsync(adminToken);
        var contentType = await _scenario.CreateContentTypeAsync(adminToken);

        var fieldTypes = new (string Name, FieldType Type, bool Required, IReadOnlyList<string>? Options, bool AllowMultiple)[]
        {
            ("textField", FieldType.Text, false, null, false),
            ("longTextField", FieldType.LongText, false, null, false),
            ("richTextField", FieldType.RichText, false, null, false),
            ("integerField", FieldType.Integer, false, null, false),
            ("decimalField", FieldType.Decimal, false, null, false),
            ("booleanField", FieldType.Boolean, false, null, false),
            ("dateField", FieldType.Date, false, null, false),
            ("dateTimeField", FieldType.DateTime, false, null, false),
            ("mediaField", FieldType.Media, false, null, false),
            ("mediaMultipleField", FieldType.MediaMultiple, false, null, true),
            ("relationField", FieldType.Relation, false, null, false),
            ("relationMultipleField", FieldType.RelationMultiple, false, null, false),
            ("selectField", FieldType.Select, false, new[] { "a", "b" }, false),
            ("multiSelectField", FieldType.MultiSelect, false, new[] { "x", "y" }, false),
            ("jsonField", FieldType.Json, false, null, false),
        };

        var sortOrder = 1;
        foreach (var field in fieldTypes)
        {
            Guid? relationTarget = field.Type is FieldType.Relation or FieldType.RelationMultiple
                ? targetType.Id
                : null;
            RelationCardinality? cardinality = field.Type is FieldType.Relation or FieldType.RelationMultiple
                ? RelationCardinality.ManyToOne
                : null;

            contentType = await _scenario.AddFieldAsync(
                adminToken,
                contentType.Id,
                field.Name,
                field.Type,
                sortOrder++,
                field.Required,
                field.Options,
                relationTarget,
                cardinality,
                field.AllowMultiple);
        }

        contentType.Fields.Should().HaveCount(fieldTypes.Length);
        contentType.Fields.Select(field => field.FieldType).Should().BeEquivalentTo(fieldTypes.Select(field => field.Type));
    }

    [Fact]
    public async Task AddField_DuplicateFieldName_Returns422()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var contentType = await _scenario.CreateContentTypeAsync(adminToken);
        await _scenario.AddFieldAsync(adminToken, contentType.Id, "title", FieldType.Text, 1);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content-types/{contentType.Id}/fields",
            adminToken,
            new
            {
                name = "title",
                fieldType = FieldType.Text,
                displayName = "Duplicate",
                sortOrder = 2,
                configuration = ContentTypeApiScenario.BuildConfiguration(required: false),
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AddField_SelectWithoutOptions_Returns422()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var contentType = await _scenario.CreateContentTypeAsync(adminToken);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content-types/{contentType.Id}/fields",
            adminToken,
            new
            {
                name = "category",
                fieldType = FieldType.Select,
                displayName = "Category",
                sortOrder = 1,
                configuration = ContentTypeApiScenario.BuildConfiguration(required: false),
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AddField_InvalidRelationTarget_Returns422()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var contentType = await _scenario.CreateContentTypeAsync(adminToken);

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            $"/api/v1/content-types/{contentType.Id}/fields",
            adminToken,
            new
            {
                name = "related",
                fieldType = FieldType.Relation,
                displayName = "Related",
                sortOrder = 1,
                configuration = ContentTypeApiScenario.BuildConfiguration(
                    required: false,
                    relationTarget: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    relationCardinality: RelationCardinality.ManyToOne),
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RemoveField_WithoutConfirmation_Returns422()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var contentType = await _scenario.CreateContentTypeAsync(adminToken);
        await _scenario.AddFieldAsync(adminToken, contentType.Id, "subtitle", FieldType.Text, 1);

        var response = await _scenario.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/content-types/{contentType.Id}/fields/subtitle",
            adminToken,
            new { confirmed = false });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RemoveField_WithConfirmation_Returns200()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var contentType = await _scenario.CreateContentTypeAsync(adminToken);
        await _scenario.AddFieldAsync(adminToken, contentType.Id, "subtitle", FieldType.Text, 1);

        var response = await _scenario.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/content-types/{contentType.Id}/fields/subtitle",
            adminToken,
            new { confirmed = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ContentTypeDto>();
        updated!.Fields.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateField_RequiredWithoutConfirmation_Returns422()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var contentType = await _scenario.CreateContentTypeAsync(adminToken);
        await _scenario.AddFieldAsync(adminToken, contentType.Id, "summary", FieldType.Text, 1, required: false);

        var response = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content-types/{contentType.Id}/fields/summary",
            adminToken,
            new
            {
                displayName = "Summary",
                sortOrder = 1,
                configuration = ContentTypeApiScenario.BuildConfiguration(required: true),
                confirmedDestructiveChange = false,
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateField_RequiredWithConfirmation_Returns200()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var contentType = await _scenario.CreateContentTypeAsync(adminToken);
        await _scenario.AddFieldAsync(adminToken, contentType.Id, "summary", FieldType.Text, 1, required: false);

        var response = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content-types/{contentType.Id}/fields/summary",
            adminToken,
            new
            {
                displayName = "Summary Required",
                sortOrder = 1,
                configuration = ContentTypeApiScenario.BuildConfiguration(required: true),
                confirmedDestructiveChange = true,
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ContentTypeDto>();
        updated!.Fields.Single().Configuration.IsRequired.Should().BeTrue();
        updated.Fields.Single().DisplayName.Should().Be("Summary Required");
    }

    [Fact]
    public async Task RenameField_WithoutConfirmation_Returns422()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var contentType = await _scenario.CreateContentTypeAsync(adminToken);
        await _scenario.AddFieldAsync(adminToken, contentType.Id, "headline", FieldType.Text, 1);

        var response = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content-types/{contentType.Id}/fields/headline/name",
            adminToken,
            new { newName = "title", confirmed = false });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RenameField_WithConfirmation_Returns200()
    {
        var (adminToken, _, _, _) = await _scenario.LoginAllAsync();
        var contentType = await _scenario.CreateContentTypeAsync(adminToken);
        await _scenario.AddFieldAsync(adminToken, contentType.Id, "headline", FieldType.Text, 1);

        var response = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content-types/{contentType.Id}/fields/headline/name",
            adminToken,
            new { newName = "title", confirmed = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ContentTypeDto>();
        updated!.Fields.Single().Name.Should().Be("title");
    }

    [Fact]
    public async Task Create_AsViewer_Returns403()
    {
        var (_, _, _, viewerToken) = await _scenario.LoginAllAsync();

        var response = await _scenario.SendAsync(
            HttpMethod.Post,
            "/api/v1/content-types",
            viewerToken,
            new
            {
                name = "viewerType",
                displayName = "Viewer Type",
                slug = $"viewer-{Guid.NewGuid():N}",
                description = (string?)null,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_AsAuthor_Returns403()
    {
        var (adminToken, authorToken, _, _) = await _scenario.LoginAllAsync();
        var created = await _scenario.CreateContentTypeAsync(adminToken);

        var response = await _scenario.SendAsync(
            HttpMethod.Put,
            $"/api/v1/content-types/{created.Id}",
            authorToken,
            new
            {
                displayName = "Author Update",
                slug = created.Slug,
                description = created.Description,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_AsEditor_Returns403()
    {
        var (adminToken, _, editorToken, _) = await _scenario.LoginAllAsync();
        var created = await _scenario.CreateContentTypeAsync(adminToken);

        var response = await _scenario.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/content-types/{created.Id}",
            editorToken,
            new { confirmedSafeDeletion = false });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_AsAuthor_Returns200()
    {
        var (adminToken, authorToken, _, _) = await _scenario.LoginAllAsync();
        await _scenario.CreateContentTypeAsync(adminToken);

        var response = await _scenario.SendAsync(HttpMethod.Get, "/api/v1/content-types", authorToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record PaginatedContentTypes(
        IReadOnlyList<ContentTypeDto> Items,
        int Page,
        int PageSize,
        int TotalItems);
}
