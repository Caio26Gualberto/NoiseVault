using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoiseVault.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDistance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ApproxLatitude",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApproxLocationUpdatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ApproxLongitude",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReachKm",
                table: "MusicianProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"MusicianProfiles\" SET \"ReachKm\" = CASE \"Reach\" WHEN 0 THEN 10 WHEN 1 THEN 30 WHEN 2 THEN 60 ELSE NULL END;");

            migrationBuilder.DropColumn(
                name: "Reach",
                table: "MusicianProfiles");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Cities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Cities",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApproxLatitude",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApproxLocationUpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApproxLongitude",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReachKm",
                table: "MusicianProfiles");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Cities");

            migrationBuilder.AddColumn<int>(
                name: "Reach",
                table: "MusicianProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
