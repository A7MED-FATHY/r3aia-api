using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R3AIA.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanionSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanionUserId",
                table: "SanadSettings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SanadSettings_CompanionUserId",
                table: "SanadSettings",
                column: "CompanionUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SanadSettings_AspNetUsers_CompanionUserId",
                table: "SanadSettings",
                column: "CompanionUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SanadSettings_AspNetUsers_CompanionUserId",
                table: "SanadSettings");

            migrationBuilder.DropIndex(
                name: "IX_SanadSettings_CompanionUserId",
                table: "SanadSettings");

            migrationBuilder.DropColumn(
                name: "CompanionUserId",
                table: "SanadSettings");
        }
    }
}
