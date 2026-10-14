using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NoiseVault.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhotoTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MediaUploadId = table.Column<int>(type: "integer", nullable: false),
                    X = table.Column<double>(type: "double precision", nullable: false),
                    Y = table.Column<double>(type: "double precision", nullable: false),
                    TaggedUserId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    TaggedByUserId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_PhotoTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhotoTags_MediaUploads_MediaUploadId",
                        column: x => x.MediaUploadId,
                        principalTable: "MediaUploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoTags_Users_TaggedByUserId",
                        column: x => x.TaggedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhotoTags_Users_TaggedUserId",
                        column: x => x.TaggedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_MediaUploadId",
                table: "PhotoTags",
                column: "MediaUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_MediaUploadId_TaggedUserId",
                table: "PhotoTags",
                columns: new[] { "MediaUploadId", "TaggedUserId" },
                unique: true,
                filter: "\"TaggedUserId\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_TaggedByUserId",
                table: "PhotoTags",
                column: "TaggedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_TaggedUserId",
                table: "PhotoTags",
                column: "TaggedUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhotoTags");
        }
    }
}
