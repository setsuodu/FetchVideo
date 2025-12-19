using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FetchVideo.Service.Migrations
{
    /// <inheritdoc />
    public partial class Subscribe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                table: "LinkItems");

            migrationBuilder.AlterColumn<int>(
                name: "Duration",
                table: "LinkItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<bool>(
                name: "IsSubscribed",
                table: "LinkItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSubscribed",
                table: "LinkItems");

            migrationBuilder.AlterColumn<int>(
                name: "Duration",
                table: "LinkItems",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 2);

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "LinkItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }
    }
}
