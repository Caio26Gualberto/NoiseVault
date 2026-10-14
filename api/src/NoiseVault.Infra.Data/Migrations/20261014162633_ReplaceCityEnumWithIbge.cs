using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoiseVault.Infra.Data.Migrations
{
    public partial class ReplaceCityEnumWithIbge : Migration
    {
        private static readonly string[] CityTables = ["Users", "Bands", "Events", "ArchiveEntries"];

        private const string EnumToIbge =
            "CASE \"City\" WHEN 0 THEN 3547809 WHEN 1 THEN 3548708 WHEN 2 THEN 3548807 WHEN 3 THEN 3513801 " +
            "WHEN 4 THEN 3529401 WHEN 5 THEN 3543303 WHEN 6 THEN 3544103 WHEN 7 THEN 3550308 ELSE NULL END";

        private const string IbgeToEnum =
            "CASE \"CityId\" WHEN 3547809 THEN 0 WHEN 3548708 THEN 1 WHEN 3548807 THEN 2 WHEN 3513801 THEN 3 " +
            "WHEN 3529401 THEN 4 WHEN 3543303 THEN 5 WHEN 3544103 THEN 6 WHEN 3550308 THEN 7 ELSE 8 END";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    SearchName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Cities",
                columns: ["Id", "Name", "Uf", "SearchName"],
                values: new object[,]
                {
                    { 3547809, "Santo André", "SP", "santo andre" },
                    { 3548708, "São Bernardo do Campo", "SP", "sao bernardo do campo" },
                    { 3548807, "São Caetano do Sul", "SP", "sao caetano do sul" },
                    { 3513801, "Diadema", "SP", "diadema" },
                    { 3529401, "Mauá", "SP", "maua" },
                    { 3543303, "Ribeirão Pires", "SP", "ribeirao pires" },
                    { 3544103, "Rio Grande da Serra", "SP", "rio grande da serra" },
                    { 3550308, "São Paulo", "SP", "sao paulo" },
                });

            foreach (var table in CityTables)
                migrationBuilder.AddColumn<int>(name: "CityId", table: table, type: "integer", nullable: true);

            foreach (var table in new[] { "Users", "Bands" })
                migrationBuilder.AddColumn<string>(name: "CityName", table: table, type: "character varying(80)", maxLength: 80, nullable: true);

            foreach (var table in CityTables)
            {
                migrationBuilder.Sql($"UPDATE \"{table}\" SET \"CityId\" = {EnumToIbge} WHERE \"City\" IS NOT NULL;");
                migrationBuilder.DropColumn(name: "City", table: table);
                migrationBuilder.CreateIndex(name: $"IX_{table}_CityId", table: table, column: "CityId");
                migrationBuilder.AddForeignKey(
                    name: $"FK_{table}_Cities_CityId",
                    table: table,
                    column: "CityId",
                    principalTable: "Cities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            }

            migrationBuilder.CreateIndex(name: "IX_Cities_SearchName", table: "Cities", column: "SearchName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in CityTables)
            {
                migrationBuilder.DropForeignKey(name: $"FK_{table}_Cities_CityId", table: table);
                migrationBuilder.DropIndex(name: $"IX_{table}_CityId", table: table);

                if (table == "Events")
                    migrationBuilder.AddColumn<int>(name: "City", table: table, type: "integer", nullable: false, defaultValue: 8);
                else
                    migrationBuilder.AddColumn<int>(name: "City", table: table, type: "integer", nullable: true);

                migrationBuilder.Sql(table == "Events"
                    ? $"UPDATE \"{table}\" SET \"City\" = {IbgeToEnum};"
                    : $"UPDATE \"{table}\" SET \"City\" = {IbgeToEnum} WHERE \"CityId\" IS NOT NULL;");

                migrationBuilder.DropColumn(name: "CityId", table: table);
            }

            foreach (var table in new[] { "Users", "Bands" })
                migrationBuilder.DropColumn(name: "CityName", table: table);

            migrationBuilder.DropTable(name: "Cities");
        }
    }
}
