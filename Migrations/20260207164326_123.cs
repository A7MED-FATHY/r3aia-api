using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R3AIA.Migrations
{
    /// <inheritdoc />
    public partial class _123 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryStatus",
                table: "MedicineRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VolunteerId",
                table: "MedicineRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicineRequests_VolunteerId",
                table: "MedicineRequests",
                column: "VolunteerId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicineRequests_Volunteers_VolunteerId",
                table: "MedicineRequests",
                column: "VolunteerId",
                principalTable: "Volunteers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicineRequests_Volunteers_VolunteerId",
                table: "MedicineRequests");

            migrationBuilder.DropIndex(
                name: "IX_MedicineRequests_VolunteerId",
                table: "MedicineRequests");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "MedicineRequests");

            migrationBuilder.DropColumn(
                name: "VolunteerId",
                table: "MedicineRequests");
        }
    }
}
