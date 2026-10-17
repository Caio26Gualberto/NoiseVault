using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoiseVault.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaTranscodeJobId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TranscodeJobId",
                table: "MediaUploads",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TranscodeJobId",
                table: "MediaUploads");
        }
    }
}
