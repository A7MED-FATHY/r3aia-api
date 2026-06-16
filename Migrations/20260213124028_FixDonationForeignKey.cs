using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R3AIA.Migrations
{
    /// <inheritdoc />
    public partial class FixDonationForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_DonationCases_DonationCaseId",
                table: "Donations");

            migrationBuilder.DropIndex(
                name: "IX_Donations_DonationCaseId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "DonationCaseId",
                table: "Donations");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_CaseId",
                table: "Donations",
                column: "CaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_DonationCases_CaseId",
                table: "Donations",
                column: "CaseId",
                principalTable: "DonationCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_DonationCases_CaseId",
                table: "Donations");

            migrationBuilder.DropIndex(
                name: "IX_Donations_CaseId",
                table: "Donations");

            migrationBuilder.AddColumn<int>(
                name: "DonationCaseId",
                table: "Donations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Donations_DonationCaseId",
                table: "Donations",
                column: "DonationCaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_DonationCases_DonationCaseId",
                table: "Donations",
                column: "DonationCaseId",
                principalTable: "DonationCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
