using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadafraApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Products",
                newName: "StartingPrice");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Products",
                newName: "AuctionEndTime");

            migrationBuilder.AddColumn<string>(
                name: "Collection",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsNumbered",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Numbering",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Publisher",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Collection",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsNumbered",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Numbering",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Publisher",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "StartingPrice",
                table: "Products",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "AuctionEndTime",
                table: "Products",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Products",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
