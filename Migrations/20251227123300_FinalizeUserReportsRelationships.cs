using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R3AIA.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeUserReportsRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportedPatientId",
                table: "UserReports");

            migrationBuilder.AlterColumn<string>(
                name: "ReporterId",
                table: "UserReports",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AdminActionNotes",
                table: "UserReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportedUserId",
                table: "UserReports",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "UserReports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ReportedUserId",
                table: "UserReports",
                column: "ReportedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ReporterId",
                table: "UserReports",
                column: "ReporterId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserReports_ApplicationUser_ReportedUserId",
                table: "UserReports",
                column: "ReportedUserId",
                principalTable: "ApplicationUser",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserReports_ApplicationUser_ReporterId",
                table: "UserReports",
                column: "ReporterId",
                principalTable: "ApplicationUser",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserReports_ApplicationUser_ReportedUserId",
                table: "UserReports");

            migrationBuilder.DropForeignKey(
                name: "FK_UserReports_ApplicationUser_ReporterId",
                table: "UserReports");

            migrationBuilder.DropIndex(
                name: "IX_UserReports_ReportedUserId",
                table: "UserReports");

            migrationBuilder.DropIndex(
                name: "IX_UserReports_ReporterId",
                table: "UserReports");

            migrationBuilder.DropColumn(
                name: "AdminActionNotes",
                table: "UserReports");

            migrationBuilder.DropColumn(
                name: "ReportedUserId",
                table: "UserReports");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserReports");

            migrationBuilder.AlterColumn<string>(
                name: "ReporterId",
                table: "UserReports",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "ReportedPatientId",
                table: "UserReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
