using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R3AIA.Migrations
{
    /// <inheritdoc />
    public partial class FixGeographyRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Pharmacies_CityId",
                table: "Pharmacies",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacies_GovernorateId",
                table: "Pharmacies",
                column: "GovernorateId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_CityId",
                table: "Patients",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_GovernorateId",
                table: "Patients",
                column: "GovernorateId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_CityId",
                table: "Doctors",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_GovernorateId",
                table: "Doctors",
                column: "GovernorateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Cities_CityId",
                table: "Doctors",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Governorates_GovernorateId",
                table: "Doctors",
                column: "GovernorateId",
                principalTable: "Governorates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Cities_CityId",
                table: "Patients",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Governorates_GovernorateId",
                table: "Patients",
                column: "GovernorateId",
                principalTable: "Governorates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pharmacies_Cities_CityId",
                table: "Pharmacies",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pharmacies_Governorates_GovernorateId",
                table: "Pharmacies",
                column: "GovernorateId",
                principalTable: "Governorates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Cities_CityId",
                table: "Doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Governorates_GovernorateId",
                table: "Doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Cities_CityId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Governorates_GovernorateId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Pharmacies_Cities_CityId",
                table: "Pharmacies");

            migrationBuilder.DropForeignKey(
                name: "FK_Pharmacies_Governorates_GovernorateId",
                table: "Pharmacies");

            migrationBuilder.DropIndex(
                name: "IX_Pharmacies_CityId",
                table: "Pharmacies");

            migrationBuilder.DropIndex(
                name: "IX_Pharmacies_GovernorateId",
                table: "Pharmacies");

            migrationBuilder.DropIndex(
                name: "IX_Patients_CityId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_GovernorateId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_CityId",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_GovernorateId",
                table: "Doctors");
        }
    }
}
