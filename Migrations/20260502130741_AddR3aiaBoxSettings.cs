using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R3AIA.Migrations
{
    /// <inheritdoc />
    public partial class AddR3aiaBoxSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "R3aiaBoxSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AvailableQuantity = table.Column<int>(type: "int", nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetailedDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppDownloadLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_R3aiaBoxSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "R3aiaBoxImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    R3aiaBoxSettingId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_R3aiaBoxImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_R3aiaBoxImages_R3aiaBoxSettings_R3aiaBoxSettingId",
                        column: x => x.R3aiaBoxSettingId,
                        principalTable: "R3aiaBoxSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_R3aiaBoxImages_R3aiaBoxSettingId",
                table: "R3aiaBoxImages",
                column: "R3aiaBoxSettingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "R3aiaBoxImages");

            migrationBuilder.DropTable(
                name: "R3aiaBoxSettings");
        }
    }
}
