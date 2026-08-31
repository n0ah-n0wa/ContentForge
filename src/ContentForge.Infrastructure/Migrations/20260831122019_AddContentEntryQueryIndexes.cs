#pragma warning disable CA1861
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentEntryQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_ContentTypeId_IsDeleted_PublishedAt",
                table: "ContentEntries",
                columns: new[] { "ContentTypeId", "IsDeleted", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_PublishedAt",
                table: "ContentEntries",
                column: "PublishedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContentEntries_ContentTypeId_IsDeleted_PublishedAt",
                table: "ContentEntries");

            migrationBuilder.DropIndex(
                name: "IX_ContentEntries_PublishedAt",
                table: "ContentEntries");
        }
    }
}
