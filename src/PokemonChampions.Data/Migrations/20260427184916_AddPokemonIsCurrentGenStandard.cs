using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokemonChampions.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPokemonIsCurrentGenStandard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentGenStandard",
                table: "Pokemon",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCurrentGenStandard",
                table: "Pokemon");
        }
    }
}
