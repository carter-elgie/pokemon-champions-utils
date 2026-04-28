using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokemonChampions.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUsageTeammates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsageTeammate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StatsId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeammateId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsagePct = table.Column<double>(type: "REAL", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageTeammate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageTeammate_Pokemon_TeammateId",
                        column: x => x.TeammateId,
                        principalTable: "Pokemon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsageTeammate_UsageStats_StatsId",
                        column: x => x.StatsId,
                        principalTable: "UsageStats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsageTeammate_StatsId",
                table: "UsageTeammate",
                column: "StatsId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageTeammate_TeammateId",
                table: "UsageTeammate",
                column: "TeammateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsageTeammate");
        }
    }
}
