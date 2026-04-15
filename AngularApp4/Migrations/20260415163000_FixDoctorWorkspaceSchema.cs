using System;
using AngularApp4.Data;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AngularApp4.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260415163000_FixDoctorWorkspaceSchema")]
    public partial class FixDoctorWorkspaceSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoctorAvailabilityExceptions",
                columns: table => new
                {
                    DoctorAvailabilityExceptionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    ExceptionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAvailabilityExceptions", x => x.DoctorAvailabilityExceptionId);
                    table.ForeignKey(
                        name: "FK_DoctorAvailabilityExceptions_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorConsultations",
                columns: table => new
                {
                    DoctorConsultationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    Symptoms = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Diagnosis = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    VitalObservations = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Advice = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FollowUpDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorConsultations", x => x.DoctorConsultationId);
                    table.ForeignKey(
                        name: "FK_DoctorConsultations_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoctorConsultations_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoctorConsultations_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosticRequests",
                columns: table => new
                {
                    DiagnosticRequestId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LabTestMasterId = table.Column<long>(type: "bigint", nullable: true),
                    RequestedItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResultSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticRequests", x => x.DiagnosticRequestId);
                    table.ForeignKey(
                        name: "FK_DiagnosticRequests_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiagnosticRequests_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiagnosticRequests_LabTestMasters_LabTestMasterId",
                        column: x => x.LabTestMasterId,
                        principalTable: "LabTestMasters",
                        principalColumn: "LabTestMasterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiagnosticRequests_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientClinicalProfiles",
                columns: table => new
                {
                    PatientClinicalProfileId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    MedicalHistory = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Allergies = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChronicConditions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CurrentMedications = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientClinicalProfiles", x => x.PatientClinicalProfileId);
                    table.ForeignKey(
                        name: "FK_PatientClinicalProfiles_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientDocuments",
                columns: table => new
                {
                    PatientDocumentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedByRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientDocuments", x => x.PatientDocumentId);
                    table.ForeignKey(
                        name: "FK_PatientDocuments_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientDocuments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorPrescriptions",
                columns: table => new
                {
                    DoctorPrescriptionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorPrescriptions", x => x.DoctorPrescriptionId);
                    table.ForeignKey(
                        name: "FK_DoctorPrescriptions_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoctorPrescriptions_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoctorPrescriptions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DoctorPrescriptionItems",
                columns: table => new
                {
                    DoctorPrescriptionItemId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorPrescriptionId = table.Column<long>(type: "bigint", nullable: false),
                    MedicineMasterId = table.Column<long>(type: "bigint", nullable: true),
                    MedicineName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Frequency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Duration = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorPrescriptionItems", x => x.DoctorPrescriptionItemId);
                    table.ForeignKey(
                        name: "FK_DoctorPrescriptionItems_DoctorPrescriptions_DoctorPrescriptionId",
                        column: x => x.DoctorPrescriptionId,
                        principalTable: "DoctorPrescriptions",
                        principalColumn: "DoctorPrescriptionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DoctorPrescriptionItems_MedicineMasters_MedicineMasterId",
                        column: x => x.MedicineMasterId,
                        principalTable: "MedicineMasters",
                        principalColumn: "MedicineMasterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticRequests_AppointmentId",
                table: "DiagnosticRequests",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticRequests_DoctorId",
                table: "DiagnosticRequests",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticRequests_LabTestMasterId",
                table: "DiagnosticRequests",
                column: "LabTestMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticRequests_PatientId_RequestType_CreatedAt",
                table: "DiagnosticRequests",
                columns: new[] { "PatientId", "RequestType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilityExceptions_DoctorId_StartDate_EndDate_ExceptionType",
                table: "DoctorAvailabilityExceptions",
                columns: new[] { "DoctorId", "StartDate", "EndDate", "ExceptionType" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorConsultations_AppointmentId",
                table: "DoctorConsultations",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorConsultations_DoctorId",
                table: "DoctorConsultations",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorConsultations_PatientId",
                table: "DoctorConsultations",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPrescriptionItems_DoctorPrescriptionId",
                table: "DoctorPrescriptionItems",
                column: "DoctorPrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPrescriptionItems_MedicineMasterId",
                table: "DoctorPrescriptionItems",
                column: "MedicineMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPrescriptions_AppointmentId",
                table: "DoctorPrescriptions",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPrescriptions_DoctorId",
                table: "DoctorPrescriptions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPrescriptions_PatientId",
                table: "DoctorPrescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientClinicalProfiles_PatientId",
                table: "PatientClinicalProfiles",
                column: "PatientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientDocuments_AppointmentId",
                table: "PatientDocuments",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDocuments_PatientId",
                table: "PatientDocuments",
                column: "PatientId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiagnosticRequests");

            migrationBuilder.DropTable(
                name: "DoctorAvailabilityExceptions");

            migrationBuilder.DropTable(
                name: "DoctorConsultations");

            migrationBuilder.DropTable(
                name: "DoctorPrescriptionItems");

            migrationBuilder.DropTable(
                name: "PatientClinicalProfiles");

            migrationBuilder.DropTable(
                name: "PatientDocuments");

            migrationBuilder.DropTable(
                name: "DoctorPrescriptions");
        }
    }
}
