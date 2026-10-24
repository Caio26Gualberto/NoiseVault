using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NoiseVault.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBandPartnerships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BandPartnershipId",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BandPartnerships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequesterBandId = table.Column<int>(type: "integer", nullable: false),
                    TargetBandId = table.Column<int>(type: "integer", nullable: false),
                    LowBandId = table.Column<int>(type: "integer", nullable: false),
                    HighBandId = table.Column<int>(type: "integer", nullable: false),
                    InvitedByUserId = table.Column<int>(type: "integer", nullable: false),
                    RespondedByUserId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_BandPartnerships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BandPartnerships_Bands_RequesterBandId",
                        column: x => x.RequesterBandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BandPartnerships_Bands_TargetBandId",
                        column: x => x.TargetBandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BandPartnerships_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BandPartnershipId",
                table: "Notifications",
                column: "BandPartnershipId");

            migrationBuilder.CreateIndex(
                name: "IX_BandPartnerships_InvitedByUserId",
                table: "BandPartnerships",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BandPartnerships_LowBandId_HighBandId",
                table: "BandPartnerships",
                columns: new[] { "LowBandId", "HighBandId" },
                unique: true,
                filter: "\"Status\" IN (0, 1) AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_BandPartnerships_RequesterBandId_Status",
                table: "BandPartnerships",
                columns: new[] { "RequesterBandId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BandPartnerships_TargetBandId_Status",
                table: "BandPartnerships",
                columns: new[] { "TargetBandId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_BandPartnerships_BandPartnershipId",
                table: "Notifications",
                column: "BandPartnershipId",
                principalTable: "BandPartnerships",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_BandPartnerships_BandPartnershipId",
                table: "Notifications");

            migrationBuilder.DropTable(
                name: "BandPartnerships");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_BandPartnershipId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "BandPartnershipId",
                table: "Notifications");
        }
    }
}
