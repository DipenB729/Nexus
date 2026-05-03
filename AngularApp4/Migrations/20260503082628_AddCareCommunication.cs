using System;
using AngularApp4.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AngularApp4.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260503082628_AddCareCommunication")]
    public partial class AddCareCommunication : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareConversationMessages",
                columns: table => new
                {
                    CareConversationMessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    SenderUserId = table.Column<long>(type: "bigint", nullable: false),
                    SenderRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SenderName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareConversationMessages", x => x.CareConversationMessageId);
                    table.ForeignKey(
                        name: "FK_CareConversationMessages_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CareConversationMessages_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareConversationMessages_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareConversationMessages_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientCaseReports",
                columns: table => new
                {
                    PatientCaseReportId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    Symptoms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PreviousReportSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PatientDocumentId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientCaseReports", x => x.PatientCaseReportId);
                    table.ForeignKey(
                        name: "FK_PatientCaseReports_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientCaseReports_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientCaseReports_PatientDocuments_PatientDocumentId",
                        column: x => x.PatientDocumentId,
                        principalTable: "PatientDocuments",
                        principalColumn: "PatientDocumentId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientCaseReports_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareConversationMessages_AppointmentId_CreatedAt",
                table: "CareConversationMessages",
                columns: new[] { "AppointmentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CareConversationMessages_DoctorId",
                table: "CareConversationMessages",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_CareConversationMessages_PatientId",
                table: "CareConversationMessages",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_CareConversationMessages_SenderUserId",
                table: "CareConversationMessages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCaseReports_AppointmentId_CreatedAt",
                table: "PatientCaseReports",
                columns: new[] { "AppointmentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientCaseReports_DoctorId",
                table: "PatientCaseReports",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCaseReports_PatientDocumentId",
                table: "PatientCaseReports",
                column: "PatientDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCaseReports_PatientId",
                table: "PatientCaseReports",
                column: "PatientId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CareConversationMessages");
            migrationBuilder.DropTable(name: "PatientCaseReports");
        }
    }
}
