#pragma warning disable CA1861

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentPreviewTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentPreviewTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPreviewTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPreviewTokens_ContentEntryId_RevokedAt_ExpiresAt",
                table: "ContentPreviewTokens",
                columns: new[] { "ContentEntryId", "RevokedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPreviewTokens_TokenHash",
                table: "ContentPreviewTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentPreviewTokens");
        }
    }
}
