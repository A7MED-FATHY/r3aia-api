using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R3AIA.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDonationCaseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DonationCases",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PatientName",
                table: "DonationCases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DonationCases");

            migrationBuilder.DropColumn(
                name: "PatientName",
                table: "DonationCases");
        }
    }
}
