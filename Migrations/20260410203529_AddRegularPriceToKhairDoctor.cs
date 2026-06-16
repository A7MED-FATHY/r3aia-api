using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R3AIA.Migrations
{
    /// <inheritdoc />
    public partial class AddRegularPriceToKhairDoctor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RegularPrice",
                table: "KhairDoctors",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegularPrice",
                table: "KhairDoctors");
        }
    }
}
