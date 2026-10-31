using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadafraApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Equipo",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Sin especificar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Equipo",
                table: "Products");
        }
    }
}
