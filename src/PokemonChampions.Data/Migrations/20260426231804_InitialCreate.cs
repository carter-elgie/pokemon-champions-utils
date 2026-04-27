using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokemonChampions.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ability",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShowdownId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NormalizedId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ShortDesc = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Desc = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ability", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Alias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AliasText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    TargetType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TargetId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSetting",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSetting", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Item",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShowdownId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NormalizedId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ShortDesc = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Desc = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsMegaStone = table.Column<bool>(type: "INTEGER", nullable: false),
                    MegaStoneFor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Item", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Move",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShowdownId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NormalizedId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Power = table.Column<int>(type: "INTEGER", nullable: true),
                    Accuracy = table.Column<int>(type: "INTEGER", nullable: true),
                    Pp = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Target = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Flags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ShortDesc = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Desc = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Move", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pokemon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShowdownId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                    NormalizedId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    BaseHp = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseAtk = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseDef = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseSpa = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseSpd = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseSpe = table.Column<int>(type: "INTEGER", nullable: false),
                    Type1 = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Type2 = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Ability0 = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Ability1 = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AbilityH = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsMega = table.Column<bool>(type: "INTEGER", nullable: false),
                    BaseFormShowdownId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FormatBans = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pokemon", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Team",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                    FormatShowdownId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Pokepaste = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Learnset",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PokemonId = table.Column<int>(type: "INTEGER", nullable: false),
                    MoveId = table.Column<int>(type: "INTEGER", nullable: false),
                    Generation = table.Column<int>(type: "INTEGER", nullable: false),
                    Method = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Learnset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Learnset_Move_MoveId",
                        column: x => x.MoveId,
                        principalTable: "Move",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Learnset_Pokemon_PokemonId",
                        column: x => x.PokemonId,
                        principalTable: "Pokemon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsageStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PokemonId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormatShowdownId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UsagePct = table.Column<double>(type: "REAL", nullable: false),
                    RawCount = table.Column<int>(type: "INTEGER", nullable: true),
                    StatsMonth = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageStats_Pokemon_PokemonId",
                        column: x => x.PokemonId,
                        principalTable: "Pokemon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMember",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    PokemonShowdownId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Nickname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Item = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Ability = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Nature = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SpHp = table.Column<int>(type: "INTEGER", nullable: false),
                    SpAtk = table.Column<int>(type: "INTEGER", nullable: false),
                    SpDef = table.Column<int>(type: "INTEGER", nullable: false),
                    SpSpa = table.Column<int>(type: "INTEGER", nullable: false),
                    SpSpd = table.Column<int>(type: "INTEGER", nullable: false),
                    SpSpe = table.Column<int>(type: "INTEGER", nullable: false),
                    IvHp = table.Column<int>(type: "INTEGER", nullable: false),
                    IvAtk = table.Column<int>(type: "INTEGER", nullable: false),
                    IvDef = table.Column<int>(type: "INTEGER", nullable: false),
                    IvSpa = table.Column<int>(type: "INTEGER", nullable: false),
                    IvSpd = table.Column<int>(type: "INTEGER", nullable: false),
                    IvSpe = table.Column<int>(type: "INTEGER", nullable: false),
                    Move1 = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Move2 = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Move3 = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Move4 = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SlotIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLegal = table.Column<bool>(type: "INTEGER", nullable: true),
                    LegalityNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMember_Team_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsageAbility",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StatsId = table.Column<int>(type: "INTEGER", nullable: false),
                    AbilityId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsagePct = table.Column<double>(type: "REAL", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageAbility", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageAbility_Ability_AbilityId",
                        column: x => x.AbilityId,
                        principalTable: "Ability",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsageAbility_UsageStats_StatsId",
                        column: x => x.StatsId,
                        principalTable: "UsageStats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsageItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StatsId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsagePct = table.Column<double>(type: "REAL", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageItem_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsageItem_UsageStats_StatsId",
                        column: x => x.StatsId,
                        principalTable: "UsageStats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsageMove",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StatsId = table.Column<int>(type: "INTEGER", nullable: false),
                    MoveId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsagePct = table.Column<double>(type: "REAL", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageMove", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageMove_Move_MoveId",
                        column: x => x.MoveId,
                        principalTable: "Move",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsageMove_UsageStats_StatsId",
                        column: x => x.StatsId,
                        principalTable: "UsageStats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsageSpread",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StatsId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nature = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SpHp = table.Column<int>(type: "INTEGER", nullable: false),
                    SpAtk = table.Column<int>(type: "INTEGER", nullable: false),
                    SpDef = table.Column<int>(type: "INTEGER", nullable: false),
                    SpSpa = table.Column<int>(type: "INTEGER", nullable: false),
                    SpSpd = table.Column<int>(type: "INTEGER", nullable: false),
                    SpSpe = table.Column<int>(type: "INTEGER", nullable: false),
                    UsagePct = table.Column<double>(type: "REAL", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageSpread", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageSpread_UsageStats_StatsId",
                        column: x => x.StatsId,
                        principalTable: "UsageStats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ability_NormalizedId",
                table: "Ability",
                column: "NormalizedId");

            migrationBuilder.CreateIndex(
                name: "IX_Ability_ShowdownId",
                table: "Ability",
                column: "ShowdownId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alias_AliasText",
                table: "Alias",
                column: "AliasText",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Item_NormalizedId",
                table: "Item",
                column: "NormalizedId");

            migrationBuilder.CreateIndex(
                name: "IX_Item_ShowdownId",
                table: "Item",
                column: "ShowdownId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Learnset_MoveId",
                table: "Learnset",
                column: "MoveId");

            migrationBuilder.CreateIndex(
                name: "IX_Learnset_PokemonId_MoveId_Generation_Method",
                table: "Learnset",
                columns: new[] { "PokemonId", "MoveId", "Generation", "Method" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Move_NormalizedId",
                table: "Move",
                column: "NormalizedId");

            migrationBuilder.CreateIndex(
                name: "IX_Move_ShowdownId",
                table: "Move",
                column: "ShowdownId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pokemon_NormalizedId",
                table: "Pokemon",
                column: "NormalizedId");

            migrationBuilder.CreateIndex(
                name: "IX_Pokemon_ShowdownId",
                table: "Pokemon",
                column: "ShowdownId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Team_Name",
                table: "Team",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_TeamId",
                table: "TeamMember",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageAbility_AbilityId",
                table: "UsageAbility",
                column: "AbilityId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageAbility_StatsId",
                table: "UsageAbility",
                column: "StatsId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageItem_ItemId",
                table: "UsageItem",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageItem_StatsId",
                table: "UsageItem",
                column: "StatsId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageMove_MoveId",
                table: "UsageMove",
                column: "MoveId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageMove_StatsId",
                table: "UsageMove",
                column: "StatsId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageSpread_StatsId",
                table: "UsageSpread",
                column: "StatsId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageStats_PokemonId_FormatShowdownId_StatsMonth_Source",
                table: "UsageStats",
                columns: new[] { "PokemonId", "FormatShowdownId", "StatsMonth", "Source" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alias");

            migrationBuilder.DropTable(
                name: "AppSetting");

            migrationBuilder.DropTable(
                name: "Learnset");

            migrationBuilder.DropTable(
                name: "TeamMember");

            migrationBuilder.DropTable(
                name: "UsageAbility");

            migrationBuilder.DropTable(
                name: "UsageItem");

            migrationBuilder.DropTable(
                name: "UsageMove");

            migrationBuilder.DropTable(
                name: "UsageSpread");

            migrationBuilder.DropTable(
                name: "Team");

            migrationBuilder.DropTable(
                name: "Ability");

            migrationBuilder.DropTable(
                name: "Item");

            migrationBuilder.DropTable(
                name: "Move");

            migrationBuilder.DropTable(
                name: "UsageStats");

            migrationBuilder.DropTable(
                name: "Pokemon");
        }
    }
}
