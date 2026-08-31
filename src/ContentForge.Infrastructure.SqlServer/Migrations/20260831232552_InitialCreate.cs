using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ContentForge.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentPreviewTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPreviewTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UploadedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Media", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockedUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DraftDataJson = table.Column<string>(type: "text", nullable: false),
                    PublishedSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    CurrentVersion = table.Column<int>(type: "int", nullable: false),
                    ConcurrencyToken = table.Column<long>(type: "bigint", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledPublishAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ScheduledUnpublishAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentEntries_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContentTypeFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypeFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTypeFields_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentEntryRelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentEntryRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentEntryRelations_ContentEntries_SourceEntryId",
                        column: x => x.SourceEntryId,
                        principalTable: "ContentEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentEntryRelations_ContentEntries_TargetEntryId",
                        column: x => x.TargetEntryId,
                        principalTable: "ContentEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentVersions_ContentEntries_ContentEntryId",
                        column: x => x.ContentEntryId,
                        principalTable: "ContentEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-000000000001"), "content.read" },
                    { new Guid("22222222-2222-2222-2222-000000000002"), "content.create" },
                    { new Guid("22222222-2222-2222-2222-000000000003"), "content.update" },
                    { new Guid("22222222-2222-2222-2222-000000000004"), "content.delete" },
                    { new Guid("22222222-2222-2222-2222-000000000005"), "content.publish" },
                    { new Guid("22222222-2222-2222-2222-000000000006"), "content.archive" },
                    { new Guid("22222222-2222-2222-2222-000000000007"), "content.restore" },
                    { new Guid("22222222-2222-2222-2222-000000000008"), "content.review" },
                    { new Guid("22222222-2222-2222-2222-000000000009"), "content.version.read" },
                    { new Guid("22222222-2222-2222-2222-000000000010"), "content.version.restore" },
                    { new Guid("22222222-2222-2222-2222-000000000011"), "contentType.read" },
                    { new Guid("22222222-2222-2222-2222-000000000012"), "contentType.create" },
                    { new Guid("22222222-2222-2222-2222-000000000013"), "contentType.update" },
                    { new Guid("22222222-2222-2222-2222-000000000014"), "contentType.delete" },
                    { new Guid("22222222-2222-2222-2222-000000000015"), "media.read" },
                    { new Guid("22222222-2222-2222-2222-000000000016"), "media.upload" },
                    { new Guid("22222222-2222-2222-2222-000000000017"), "media.update" },
                    { new Guid("22222222-2222-2222-2222-000000000018"), "media.delete" },
                    { new Guid("22222222-2222-2222-2222-000000000019"), "user.read" },
                    { new Guid("22222222-2222-2222-2222-000000000020"), "user.create" },
                    { new Guid("22222222-2222-2222-2222-000000000021"), "user.update" },
                    { new Guid("22222222-2222-2222-2222-000000000022"), "user.disable" },
                    { new Guid("22222222-2222-2222-2222-000000000023"), "audit.read" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "Administrator" },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "Editor" },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "Author" },
                    { new Guid("11111111-1111-1111-1111-111111111104"), "Viewer" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-000000000001"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000002"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000003"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000004"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000005"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000006"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000007"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000008"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000009"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000010"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000011"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000012"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000013"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000014"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000015"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000016"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000017"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000018"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000019"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000020"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000021"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000022"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000023"), new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("22222222-2222-2222-2222-000000000001"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000002"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000003"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000004"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000005"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000006"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000007"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000008"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000009"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000010"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000011"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000015"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000016"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000017"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000018"), new Guid("11111111-1111-1111-1111-111111111102") },
                    { new Guid("22222222-2222-2222-2222-000000000001"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-000000000002"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-000000000003"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-000000000008"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-000000000009"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-000000000011"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-000000000015"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-000000000016"), new Guid("11111111-1111-1111-1111-111111111103") },
                    { new Guid("22222222-2222-2222-2222-000000000001"), new Guid("11111111-1111-1111-1111-111111111104") },
                    { new Guid("22222222-2222-2222-2222-000000000009"), new Guid("11111111-1111-1111-1111-111111111104") },
                    { new Guid("22222222-2222-2222-2222-000000000011"), new Guid("11111111-1111-1111-1111-111111111104") },
                    { new Guid("22222222-2222-2222-2222-000000000015"), new Guid("11111111-1111-1111-1111-111111111104") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CorrelationId",
                table: "AuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_ContentTypeId",
                table: "ContentEntries",
                column: "ContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_ContentTypeId_IsDeleted_PublishedAt",
                table: "ContentEntries",
                columns: new[] { "ContentTypeId", "IsDeleted", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_ContentTypeId_IsDeleted_UpdatedAt",
                table: "ContentEntries",
                columns: new[] { "ContentTypeId", "IsDeleted", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_ContentTypeId_Slug",
                table: "ContentEntries",
                columns: new[] { "ContentTypeId", "Slug" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_CreatedAt",
                table: "ContentEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_CreatedBy",
                table: "ContentEntries",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_IsDeleted",
                table: "ContentEntries",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_PublishedAt",
                table: "ContentEntries",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_ScheduledPublishAt",
                table: "ContentEntries",
                column: "ScheduledPublishAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_ScheduledUnpublishAt",
                table: "ContentEntries",
                column: "ScheduledUnpublishAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_Slug",
                table: "ContentEntries",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_Status",
                table: "ContentEntries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_UpdatedAt",
                table: "ContentEntries",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntryRelations_SourceEntryId",
                table: "ContentEntryRelations",
                column: "SourceEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntryRelations_SourceEntryId_FieldName",
                table: "ContentEntryRelations",
                columns: new[] { "SourceEntryId", "FieldName" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntryRelations_SourceEntryId_TargetEntryId_FieldName",
                table: "ContentEntryRelations",
                columns: new[] { "SourceEntryId", "TargetEntryId", "FieldName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntryRelations_TargetEntryId",
                table: "ContentEntryRelations",
                column: "TargetEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPreviewTokens_ContentEntryId_RevokedAt_ExpiresAt",
                table: "ContentPreviewTokens",
                columns: new[] { "ContentEntryId", "RevokedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPreviewTokens_TokenHash",
                table: "ContentPreviewTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeFields_ContentTypeId_Name",
                table: "ContentTypeFields",
                columns: new[] { "ContentTypeId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeFields_ContentTypeId_SortOrder",
                table: "ContentTypeFields",
                columns: new[] { "ContentTypeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_CreatedAt",
                table: "ContentTypes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_IsActive",
                table: "ContentTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_Name",
                table: "ContentTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_Slug",
                table: "ContentTypes",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_UpdatedAt",
                table: "ContentTypes",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentVersions_ContentEntryId",
                table: "ContentVersions",
                column: "ContentEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentVersions_ContentEntryId_VersionNumber",
                table: "ContentVersions",
                columns: new[] { "ContentEntryId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentVersions_CreatedAt",
                table: "ContentVersions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Media_ContentType",
                table: "Media",
                column: "ContentType");

            migrationBuilder.CreateIndex(
                name: "IX_Media_IsDeleted",
                table: "Media",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Media_UploadedAt",
                table: "Media",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Media_UploadedBy",
                table: "Media",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledJobs_ContentEntryId_JobType_Status",
                table: "ScheduledJobs",
                columns: new[] { "ContentEntryId", "JobType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledJobs_IdempotencyKey",
                table: "ScheduledJobs",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledJobs_LockedUntil",
                table: "ScheduledJobs",
                column: "LockedUntil");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledJobs_Status_ScheduledAt",
                table: "ScheduledJobs",
                columns: new[] { "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ContentEntryRelations");

            migrationBuilder.DropTable(
                name: "ContentPreviewTokens");

            migrationBuilder.DropTable(
                name: "ContentTypeFields");

            migrationBuilder.DropTable(
                name: "ContentVersions");

            migrationBuilder.DropTable(
                name: "Media");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "ScheduledJobs");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "ContentEntries");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ContentTypes");
        }
    }
}
