using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FetchVideo.Service.Migrations
{
    /// <inheritdoc />
    public partial class InitGlobalConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    TriggerTimesJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledItems_Key",
                table: "ScheduledItems",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledItems");
        }
    }
}
