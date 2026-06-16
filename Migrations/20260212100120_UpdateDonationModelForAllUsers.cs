using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R3AIA.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDonationModelForAllUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_Volunteers_VolunteerId",
                table: "Donations");

            migrationBuilder.AlterColumn<int>(
                name: "VolunteerId",
                table: "Donations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "DonorUserId",
                table: "Donations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_Volunteers_VolunteerId",
                table: "Donations",
                column: "VolunteerId",
                principalTable: "Volunteers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_Volunteers_VolunteerId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "DonorUserId",
                table: "Donations");

            migrationBuilder.AlterColumn<int>(
                name: "VolunteerId",
                table: "Donations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_Volunteers_VolunteerId",
                table: "Donations",
                column: "VolunteerId",
                principalTable: "Volunteers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
