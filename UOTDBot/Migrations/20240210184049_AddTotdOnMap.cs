using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UOTDBot.Migrations
{
    /// <inheritdoc />
    public partial class AddTotdOnMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "Totd",
                table: "Maps",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Totd",
                table: "Maps");
        }
    }
}
