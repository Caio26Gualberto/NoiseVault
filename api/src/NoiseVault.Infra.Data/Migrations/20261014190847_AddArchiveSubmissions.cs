using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoiseVault.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArchiveSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArchiveEntries_AuthorUserId",
                table: "ArchiveEntries");

            migrationBuilder.AddColumn<bool>(
                name: "IsSubmission",
                table: "ArchiveEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ArchiveEntries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "ArchiveEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByUserId",
                table: "ArchiveEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ArchiveEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveEntries_AuthorUserId_Status",
                table: "ArchiveEntries",
                columns: new[] { "AuthorUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveEntries_Status_CreatedAt",
                table: "ArchiveEntries",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArchiveEntries_AuthorUserId_Status",
                table: "ArchiveEntries");

            migrationBuilder.DropIndex(
                name: "IX_ArchiveEntries_Status_CreatedAt",
                table: "ArchiveEntries");

            migrationBuilder.DropColumn(
                name: "IsSubmission",
                table: "ArchiveEntries");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ArchiveEntries");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "ArchiveEntries");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "ArchiveEntries");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ArchiveEntries");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveEntries_AuthorUserId",
                table: "ArchiveEntries",
                column: "AuthorUserId");
        }
    }
}
