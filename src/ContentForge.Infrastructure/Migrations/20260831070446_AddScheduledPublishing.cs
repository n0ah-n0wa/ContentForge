using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace ContentForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledPublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledPublishAt",
                table: "ContentEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledUnpublishAt",
                table: "ContentEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScheduledJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_ScheduledPublishAt",
                table: "ContentEntries",
                column: "ScheduledPublishAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEntries_ScheduledUnpublishAt",
                table: "ContentEntries",
                column: "ScheduledUnpublishAt");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledJobs");

            migrationBuilder.DropIndex(
                name: "IX_ContentEntries_ScheduledPublishAt",
                table: "ContentEntries");

            migrationBuilder.DropIndex(
                name: "IX_ContentEntries_ScheduledUnpublishAt",
                table: "ContentEntries");

            migrationBuilder.DropColumn(
                name: "ScheduledPublishAt",
                table: "ContentEntries");

            migrationBuilder.DropColumn(
                name: "ScheduledUnpublishAt",
                table: "ContentEntries");
        }
    }
}
