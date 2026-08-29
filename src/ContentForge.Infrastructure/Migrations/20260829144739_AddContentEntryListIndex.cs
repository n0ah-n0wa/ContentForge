#pragma warning disable CA1861
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentEntryListIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_ContentTypeId_IsDeleted_UpdatedAt",
                table: "ContentEntries",
                columns: new[] { "ContentTypeId", "IsDeleted", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContentEntries_ContentTypeId_IsDeleted_UpdatedAt",
                table: "ContentEntries");
        }
    }
}
