using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UOTDBot.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MapId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MapUid = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "TEXT", nullable: false),
                    DownloadUrl = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorTime = table.Column<int>(type: "INTEGER", nullable: false),
                    FileSize = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Totd = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Format = table.Column<string>(type: "TEXT", nullable: false),
                    Emotes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportConfiguration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoThread = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConfigurationId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportChannels_ReportConfiguration_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "ReportConfiguration",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ConfigurationId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportUsers_ReportConfiguration_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "ReportConfiguration",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    OriginalChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    MapId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<int>(type: "INTEGER", nullable: true),
                    DMId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportMessages_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportMessages_ReportChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "ReportChannels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReportMessages_ReportUsers_DMId",
                        column: x => x.DMId,
                        principalTable: "ReportUsers",
                        principalColumn: "Id");
                });

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

            migrationBuilder.InsertData(
                table: "Cars",
                columns: new[] { "Id", "DisplayName", "MapFeaturesId" },
                values: new object[,]
                {
                    { "CarDesert", "DesertCar", null },
                    { "CarRally", "RallyCar", null },
                    { "CarSnow", "SnowCar", null },
                    { "CarSport", "StadiumCar", null }
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

            migrationBuilder.CreateIndex(
                name: "IX_ReportChannels_ConfigurationId",
                table: "ReportChannels",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMessages_DMId",
                table: "ReportMessages",
                column: "DMId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMessages_ChannelId",
                table: "ReportMessages",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportMessages_MapId",
                table: "ReportMessages",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportUsers_ConfigurationId",
                table: "ReportUsers",
                column: "ConfigurationId");

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
                name: "ReportMessages");

            migrationBuilder.DropTable(
                name: "ReportChannels");

            migrationBuilder.DropTable(
                name: "ReportUsers");

            migrationBuilder.DropTable(
                name: "ReportConfiguration");

            migrationBuilder.DropTable(
                name: "MapFeatures");

            migrationBuilder.DropTable(
                name: "Cars");

            migrationBuilder.DropTable(
                name: "Maps");
        }
    }
}
