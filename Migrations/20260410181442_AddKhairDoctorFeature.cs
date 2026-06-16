using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace R3AIA.Migrations
{
    /// <inheritdoc />
    public partial class AddKhairDoctorFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KhairDoctors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    ConsultationType = table.Column<int>(type: "int", nullable: false),
                    DiscountedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FreeDailyLimit = table.Column<int>(type: "int", nullable: false),
                    BioNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    RatingCount = table.Column<int>(type: "int", nullable: false),
                    TotalFreeConsultations = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhairDoctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KhairDoctors_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KhairAppointmentSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhairDoctorId = table.Column<int>(type: "int", nullable: false),
                    SlotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    IsBooked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhairAppointmentSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KhairAppointmentSlots_KhairDoctors_KhairDoctorId",
                        column: x => x.KhairDoctorId,
                        principalTable: "KhairDoctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KhairBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    KhairDoctorId = table.Column<int>(type: "int", nullable: false),
                    SlotId = table.Column<int>(type: "int", nullable: false),
                    PatientNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PatientRating = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhairBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KhairBookings_KhairAppointmentSlots_SlotId",
                        column: x => x.SlotId,
                        principalTable: "KhairAppointmentSlots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KhairBookings_KhairDoctors_KhairDoctorId",
                        column: x => x.KhairDoctorId,
                        principalTable: "KhairDoctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KhairBookings_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_KhairAppointmentSlots_KhairDoctorId",
                table: "KhairAppointmentSlots",
                column: "KhairDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_KhairBookings_KhairDoctorId",
                table: "KhairBookings",
                column: "KhairDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_KhairBookings_PatientId",
                table: "KhairBookings",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_KhairBookings_SlotId",
                table: "KhairBookings",
                column: "SlotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KhairDoctors_DoctorId",
                table: "KhairDoctors",
                column: "DoctorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KhairBookings");

            migrationBuilder.DropTable(
                name: "KhairAppointmentSlots");

            migrationBuilder.DropTable(
                name: "KhairDoctors");
        }
    }
}
