using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UOTDBot.Migrations
{
    /// <inheritdoc />
    public partial class AddCars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "CreatedAt",
                table: "ReportMessages",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "Cars",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    MapFeaturesId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cars", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Cars_MapFeaturesId",
                table: "Cars",
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

            migrationBuilder.AddForeignKey(
                name: "FK_Cars_MapFeatures_MapFeaturesId",
                table: "Cars",
                column: "MapFeaturesId",
                principalTable: "MapFeatures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cars_MapFeatures_MapFeaturesId",
                table: "Cars");

            migrationBuilder.DropTable(
                name: "MapFeatures");

            migrationBuilder.DropTable(
                name: "Cars");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "ReportMessages",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");
        }
    }
}
