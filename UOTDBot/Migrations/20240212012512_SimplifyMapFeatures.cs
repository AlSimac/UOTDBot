using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UOTDBot.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyMapFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarMapFeatures");

            migrationBuilder.DropTable(
                name: "MapFeatures");

            migrationBuilder.AddColumn<string>(
                name: "Features",
                table: "Maps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Features",
                table: "Maps");

            migrationBuilder.CreateTable(
                name: "MapFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DefaultCarId = table.Column<string>(type: "TEXT", nullable: false),
                    MapId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MapFeatures_Cars_DefaultCarId",
                        column: x => x.DefaultCarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MapFeatures_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarMapFeatures",
                columns: table => new
                {
                    GatesId = table.Column<string>(type: "TEXT", nullable: false),
                    MapFeaturesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarMapFeatures", x => new { x.GatesId, x.MapFeaturesId });
                    table.ForeignKey(
                        name: "FK_CarMapFeatures_Cars_GatesId",
                        column: x => x.GatesId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CarMapFeatures_MapFeatures_MapFeaturesId",
                        column: x => x.MapFeaturesId,
                        principalTable: "MapFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarMapFeatures_MapFeaturesId",
                table: "CarMapFeatures",
                column: "MapFeaturesId");

            migrationBuilder.CreateIndex(
                name: "IX_MapFeatures_DefaultCarId",
                table: "MapFeatures",
                column: "DefaultCarId");

            migrationBuilder.CreateIndex(
                name: "IX_MapFeatures_MapId",
                table: "MapFeatures",
                column: "MapId",
                unique: true);
        }
    }
}
