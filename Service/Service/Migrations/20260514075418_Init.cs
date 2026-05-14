using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FetchVideo.Service.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LinkItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RoomId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsSubscribed = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 2),
                    LastRecordedAt = table.Column<DateTime>(type: "DATETIME", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    TriggerTimesJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleConfigs_Key",
                table: "ScheduleConfigs",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LinkItems");

            migrationBuilder.DropTable(
                name: "ScheduleConfigs");
        }
    }
}
