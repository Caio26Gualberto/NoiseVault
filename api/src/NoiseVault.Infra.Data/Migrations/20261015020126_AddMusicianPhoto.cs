using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoiseVault.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMusicianPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoKey",
                table: "MusicianProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoKey",
                table: "MusicianProfiles");
        }
    }
}
