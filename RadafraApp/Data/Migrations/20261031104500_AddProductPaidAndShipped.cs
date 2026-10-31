using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadafraApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPaidAndShipped : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Pagado",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Enviado",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pagado",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Enviado",
                table: "Products");
        }
    }
}
