using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NoiseVault.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailOnBandInvite",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: true),
                    ActorCount = table.Column<int>(type: "integer", nullable: false),
                    PostId = table.Column<int>(type: "integer", nullable: true),
                    EventId = table.Column<int>(type: "integer", nullable: true),
                    ArchiveEntryId = table.Column<int>(type: "integer", nullable: true),
                    BandId = table.Column<int>(type: "integer", nullable: true),
                    BandInvitationId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_ArchiveEntries_ArchiveEntryId",
                        column: x => x.ArchiveEntryId,
                        principalTable: "ArchiveEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_BandInvitations_BandInvitationId",
                        column: x => x.BandInvitationId,
                        principalTable: "BandInvitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Bands_BandId",
                        column: x => x.BandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ActorUserId",
                table: "Notifications",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ArchiveEntryId",
                table: "Notifications",
                column: "ArchiveEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BandId",
                table: "Notifications",
                column: "BandId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BandInvitationId",
                table: "Notifications",
                column: "BandInvitationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_EventId",
                table: "Notifications",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_LastActivityAt",
                table: "Notifications",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PostId",
                table: "Notifications",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_ReadAt_LastActivityAt",
                table: "Notifications",
                columns: new[] { "UserId", "ReadAt", "LastActivityAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropColumn(
                name: "EmailOnBandInvite",
                table: "Users");
        }
    }
}
