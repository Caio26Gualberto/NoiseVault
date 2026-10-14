using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NoiseVault.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class Archive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArchiveEntryId",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Profile",
                table: "MediaUploads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ArchiveEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AuthorUserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: true),
                    Day = table.Column<int>(type: "integer", nullable: true),
                    SortDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Venue = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    City = table.Column<int>(type: "integer", nullable: true),
                    CityName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CoverMediaUploadId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchiveEntries_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArchiveAttendances",
                columns: table => new
                {
                    ArchiveEntryId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveAttendances", x => new { x.ArchiveEntryId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ArchiveAttendances_ArchiveEntries_ArchiveEntryId",
                        column: x => x.ArchiveEntryId,
                        principalTable: "ArchiveEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArchiveAttendances_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArchiveBandEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArchiveEntryId = table.Column<int>(type: "integer", nullable: false),
                    BandId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveBandEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchiveBandEntries_ArchiveEntries_ArchiveEntryId",
                        column: x => x.ArchiveEntryId,
                        principalTable: "ArchiveEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArchiveBandEntries_Bands_BandId",
                        column: x => x.BandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArchiveComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArchiveEntryId = table.Column<int>(type: "integer", nullable: false),
                    AuthorUserId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchiveComments_ArchiveEntries_ArchiveEntryId",
                        column: x => x.ArchiveEntryId,
                        principalTable: "ArchiveEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArchiveComments_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArchiveLikes",
                columns: table => new
                {
                    ArchiveEntryId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveLikes", x => new { x.ArchiveEntryId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ArchiveLikes_ArchiveEntries_ArchiveEntryId",
                        column: x => x.ArchiveEntryId,
                        principalTable: "ArchiveEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArchiveLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArchiveMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArchiveEntryId = table.Column<int>(type: "integer", nullable: false),
                    MediaUploadId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchiveMedia_ArchiveEntries_ArchiveEntryId",
                        column: x => x.ArchiveEntryId,
                        principalTable: "ArchiveEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArchiveMedia_MediaUploads_MediaUploadId",
                        column: x => x.MediaUploadId,
                        principalTable: "MediaUploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ArchiveEntryId",
                table: "Posts",
                column: "ArchiveEntryId",
                unique: true,
                filter: "\"ArchiveEntryId\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveAttendances_ArchiveEntryId_CreatedAt",
                table: "ArchiveAttendances",
                columns: new[] { "ArchiveEntryId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveAttendances_UserId",
                table: "ArchiveAttendances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveBandEntries_ArchiveEntryId_Order",
                table: "ArchiveBandEntries",
                columns: new[] { "ArchiveEntryId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveBandEntries_BandId",
                table: "ArchiveBandEntries",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveComments_ArchiveEntryId_CreatedAt",
                table: "ArchiveComments",
                columns: new[] { "ArchiveEntryId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveComments_AuthorUserId",
                table: "ArchiveComments",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveEntries_AuthorUserId",
                table: "ArchiveEntries",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveEntries_SortDate",
                table: "ArchiveEntries",
                column: "SortDate");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveEntries_Year",
                table: "ArchiveEntries",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveLikes_UserId",
                table: "ArchiveLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveMedia_ArchiveEntryId_Order",
                table: "ArchiveMedia",
                columns: new[] { "ArchiveEntryId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveMedia_MediaUploadId",
                table: "ArchiveMedia",
                column: "MediaUploadId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_ArchiveEntries_ArchiveEntryId",
                table: "Posts",
                column: "ArchiveEntryId",
                principalTable: "ArchiveEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_ArchiveEntries_ArchiveEntryId",
                table: "Posts");

            migrationBuilder.DropTable(
                name: "ArchiveAttendances");

            migrationBuilder.DropTable(
                name: "ArchiveBandEntries");

            migrationBuilder.DropTable(
                name: "ArchiveComments");

            migrationBuilder.DropTable(
                name: "ArchiveLikes");

            migrationBuilder.DropTable(
                name: "ArchiveMedia");

            migrationBuilder.DropTable(
                name: "ArchiveEntries");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ArchiveEntryId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ArchiveEntryId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Profile",
                table: "MediaUploads");
        }
    }
}
