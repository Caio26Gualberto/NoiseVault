using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NoiseVault.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnsaio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BandOpeningId",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BandOpeningId",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BandOpenings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BandId = table.Column<int>(type: "integer", nullable: false),
                    Instrument = table.Column<int>(type: "integer", nullable: false),
                    OtherName = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Goal = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_BandOpenings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BandOpenings_Bands_BandId",
                        column: x => x.BandId,
                        principalTable: "Bands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MusicianProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    IsLookingForBand = table.Column<bool>(type: "boolean", nullable: false),
                    LookingSince = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Goal = table.Column<int>(type: "integer", nullable: false),
                    Commitment = table.Column<int>(type: "integer", nullable: false),
                    Reach = table.Column<int>(type: "integer", nullable: false),
                    HasGear = table.Column<bool>(type: "boolean", nullable: false),
                    HasTransport = table.Column<bool>(type: "boolean", nullable: false),
                    HasRehearsalSpace = table.Column<bool>(type: "boolean", nullable: false),
                    Influences = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_MusicianProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicianProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BandOpeningInterests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BandOpeningId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BandInvitationId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_BandOpeningInterests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BandOpeningInterests_BandInvitations_BandInvitationId",
                        column: x => x.BandInvitationId,
                        principalTable: "BandInvitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BandOpeningInterests_BandOpenings_BandOpeningId",
                        column: x => x.BandOpeningId,
                        principalTable: "BandOpenings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BandOpeningInterests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MusicianAvailability",
                columns: table => new
                {
                    MusicianProfileId = table.Column<int>(type: "integer", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicianAvailability", x => new { x.MusicianProfileId, x.Day, x.Period });
                    table.ForeignKey(
                        name: "FK_MusicianAvailability_MusicianProfiles_MusicianProfileId",
                        column: x => x.MusicianProfileId,
                        principalTable: "MusicianProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusicianGenres",
                columns: table => new
                {
                    MusicianProfileId = table.Column<int>(type: "integer", nullable: false),
                    Genre = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicianGenres", x => new { x.MusicianProfileId, x.Genre });
                    table.ForeignKey(
                        name: "FK_MusicianGenres_MusicianProfiles_MusicianProfileId",
                        column: x => x.MusicianProfileId,
                        principalTable: "MusicianProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusicianInstruments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MusicianProfileId = table.Column<int>(type: "integer", nullable: false),
                    Instrument = table.Column<int>(type: "integer", nullable: false),
                    OtherName = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicianInstruments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicianInstruments_MusicianProfiles_MusicianProfileId",
                        column: x => x.MusicianProfileId,
                        principalTable: "MusicianProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusicianSampleLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MusicianProfileId = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicianSampleLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicianSampleLinks_MusicianProfiles_MusicianProfileId",
                        column: x => x.MusicianProfileId,
                        principalTable: "MusicianProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_BandOpeningId",
                table: "Posts",
                column: "BandOpeningId",
                unique: true,
                filter: "\"BandOpeningId\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BandOpeningId",
                table: "Notifications",
                column: "BandOpeningId");

            migrationBuilder.CreateIndex(
                name: "IX_BandOpeningInterests_BandInvitationId",
                table: "BandOpeningInterests",
                column: "BandInvitationId");

            migrationBuilder.CreateIndex(
                name: "IX_BandOpeningInterests_BandOpeningId_UserId",
                table: "BandOpeningInterests",
                columns: new[] { "BandOpeningId", "UserId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_BandOpeningInterests_UserId",
                table: "BandOpeningInterests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BandOpenings_BandId_Status",
                table: "BandOpenings",
                columns: new[] { "BandId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BandOpenings_Status_ExpiresAt",
                table: "BandOpenings",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MusicianInstruments_Instrument_MusicianProfileId",
                table: "MusicianInstruments",
                columns: new[] { "Instrument", "MusicianProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_MusicianInstruments_MusicianProfileId",
                table: "MusicianInstruments",
                column: "MusicianProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicianProfiles_IsLookingForBand_LookingSince",
                table: "MusicianProfiles",
                columns: new[] { "IsLookingForBand", "LookingSince" });

            migrationBuilder.CreateIndex(
                name: "IX_MusicianProfiles_UserId",
                table: "MusicianProfiles",
                column: "UserId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MusicianSampleLinks_MusicianProfileId",
                table: "MusicianSampleLinks",
                column: "MusicianProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_BandOpenings_BandOpeningId",
                table: "Notifications",
                column: "BandOpeningId",
                principalTable: "BandOpenings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_BandOpenings_BandOpeningId",
                table: "Posts",
                column: "BandOpeningId",
                principalTable: "BandOpenings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_BandOpenings_BandOpeningId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_BandOpenings_BandOpeningId",
                table: "Posts");

            migrationBuilder.DropTable(
                name: "BandOpeningInterests");

            migrationBuilder.DropTable(
                name: "MusicianAvailability");

            migrationBuilder.DropTable(
                name: "MusicianGenres");

            migrationBuilder.DropTable(
                name: "MusicianInstruments");

            migrationBuilder.DropTable(
                name: "MusicianSampleLinks");

            migrationBuilder.DropTable(
                name: "BandOpenings");

            migrationBuilder.DropTable(
                name: "MusicianProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Posts_BandOpeningId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_BandOpeningId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "BandOpeningId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "BandOpeningId",
                table: "Notifications");
        }
    }
}
