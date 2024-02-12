using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UOTDBot.Migrations
{
    /// <inheritdoc />
    public partial class DoMtoN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cars_MapFeatures_MapFeaturesId",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Cars_MapFeaturesId",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "MapFeaturesId",
                table: "Cars");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarMapFeatures");

            migrationBuilder.AddColumn<int>(
                name: "MapFeaturesId",
                table: "Cars",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: "CarDesert",
                column: "MapFeaturesId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: "CarRally",
                column: "MapFeaturesId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: "CarSnow",
                column: "MapFeaturesId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: "CarSport",
                column: "MapFeaturesId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Cars_MapFeaturesId",
                table: "Cars",
                column: "MapFeaturesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cars_MapFeatures_MapFeaturesId",
                table: "Cars",
                column: "MapFeaturesId",
                principalTable: "MapFeatures",
                principalColumn: "Id");
        }
    }
}
