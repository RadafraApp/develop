using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadafraApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImageData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ImagePath",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "Products",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImageUpdatedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImageUpdatedAt",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "ImagePath",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
