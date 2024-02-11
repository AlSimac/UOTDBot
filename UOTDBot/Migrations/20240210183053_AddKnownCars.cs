using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UOTDBot.Migrations
{
    /// <inheritdoc />
    public partial class AddKnownCars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Cars",
                columns: new[] { "Id", "DisplayName", "MapFeaturesId" },
                values: new object[,]
                {
                    { "CarDesert", "DesertCar", null },
                    { "CarRally", "RallyCar", null },
                    { "CarSnow", "SnowCar", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: "CarDesert");

            migrationBuilder.DeleteData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: "CarRally");

            migrationBuilder.DeleteData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: "CarSnow");
        }
    }
}
