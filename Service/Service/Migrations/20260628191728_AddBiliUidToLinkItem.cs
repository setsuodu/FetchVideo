using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FetchVideo.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddBiliUidToLinkItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BiliUid",
                table: "LinkItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UidFetchedAt",
                table: "LinkItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UidStatus",
                table: "LinkItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BiliUid",
                table: "LinkItems");

            migrationBuilder.DropColumn(
                name: "UidFetchedAt",
                table: "LinkItems");

            migrationBuilder.DropColumn(
                name: "UidStatus",
                table: "LinkItems");
        }
    }
}
