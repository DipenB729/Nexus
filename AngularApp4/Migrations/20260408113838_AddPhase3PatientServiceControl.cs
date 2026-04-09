using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AngularApp4.Migrations
{
    public partial class AddPhase3PatientServiceControl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Patients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MedicalRecordNumber",
                table: "Patients",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MergedIntoPatientId",
                table: "Patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Patients",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenNumber",
                table: "Appointments",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppointmentTokenSettings",
                columns: table => new
                {
                    AppointmentTokenSettingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartingNumber = table.Column<int>(type: "int", nullable: false),
                    NumberPadding = table.Column<int>(type: "int", nullable: false),
                    ResetDaily = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentTokenSettings", x => x.AppointmentTokenSettingId);
                });

            migrationBuilder.CreateTable(
                name: "PatientAdmissions",
                columns: table => new
                {
                    PatientAdmissionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdmissionNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: true),
                    DoctorId = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    WardId = table.Column<long>(type: "bigint", nullable: false),
                    BedId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedDischargeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DischargeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DischargeSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DischargeApprovedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    DischargeApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAdmissions", x => x.PatientAdmissionId);
                    table.ForeignKey(
                        name: "FK_PatientAdmissions_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAdmissions_Beds_BedId",
                        column: x => x.BedId,
                        principalTable: "Beds",
                        principalColumn: "BedId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAdmissions_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAdmissions_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAdmissions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAdmissions_Wards_WardId",
                        column: x => x.WardId,
                        principalTable: "Wards",
                        principalColumn: "WardId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionTransfers",
                columns: table => new
                {
                    AdmissionTransferId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientAdmissionId = table.Column<long>(type: "bigint", nullable: false),
                    FromWardId = table.Column<long>(type: "bigint", nullable: true),
                    FromBedId = table.Column<long>(type: "bigint", nullable: true),
                    ToWardId = table.Column<long>(type: "bigint", nullable: false),
                    ToBedId = table.Column<long>(type: "bigint", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionTransfers", x => x.AdmissionTransferId);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_Beds_FromBedId",
                        column: x => x.FromBedId,
                        principalTable: "Beds",
                        principalColumn: "BedId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_Beds_ToBedId",
                        column: x => x.ToBedId,
                        principalTable: "Beds",
                        principalColumn: "BedId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_PatientAdmissions_PatientAdmissionId",
                        column: x => x.PatientAdmissionId,
                        principalTable: "PatientAdmissions",
                        principalColumn: "PatientAdmissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_Wards_FromWardId",
                        column: x => x.FromWardId,
                        principalTable: "Wards",
                        principalColumn: "WardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_Wards_ToWardId",
                        column: x => x.ToWardId,
                        principalTable: "Wards",
                        principalColumn: "WardId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_MedicalRecordNumber",
                table: "Patients",
                column: "MedicalRecordNumber",
                unique: true,
                filter: "[MedicalRecordNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_MergedIntoPatientId",
                table: "Patients",
                column: "MergedIntoPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionTransfers_FromBedId",
                table: "AdmissionTransfers",
                column: "FromBedId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionTransfers_FromWardId",
                table: "AdmissionTransfers",
                column: "FromWardId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionTransfers_PatientAdmissionId",
                table: "AdmissionTransfers",
                column: "PatientAdmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionTransfers_ToBedId",
                table: "AdmissionTransfers",
                column: "ToBedId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionTransfers_ToWardId",
                table: "AdmissionTransfers",
                column: "ToWardId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdmissions_AdmissionNumber",
                table: "PatientAdmissions",
                column: "AdmissionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdmissions_AppointmentId",
                table: "PatientAdmissions",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdmissions_BedId",
                table: "PatientAdmissions",
                column: "BedId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdmissions_BranchId",
                table: "PatientAdmissions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdmissions_DoctorId",
                table: "PatientAdmissions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdmissions_PatientId",
                table: "PatientAdmissions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdmissions_WardId",
                table: "PatientAdmissions",
                column: "WardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Patients_MergedIntoPatientId",
                table: "Patients",
                column: "MergedIntoPatientId",
                principalTable: "Patients",
                principalColumn: "PatientId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Patients_MergedIntoPatientId",
                table: "Patients");

            migrationBuilder.DropTable(
                name: "AdmissionTransfers");

            migrationBuilder.DropTable(
                name: "AppointmentTokenSettings");

            migrationBuilder.DropTable(
                name: "PatientAdmissions");

            migrationBuilder.DropIndex(
                name: "IX_Patients_MedicalRecordNumber",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_MergedIntoPatientId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "MedicalRecordNumber",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "MergedIntoPatientId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "TokenNumber",
                table: "Appointments");
        }
    }
}
