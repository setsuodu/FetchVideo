using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FetchVideo.Service.Migrations
{
    /// <inheritdoc />
    public partial class LinkItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "LinkItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "LinkItems");
        }
    }
}
