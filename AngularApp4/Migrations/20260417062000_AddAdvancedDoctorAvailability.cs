using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AngularApp4.Migrations
{
    public partial class AddAdvancedDoctorAvailability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "BreakEndTime",
                table: "DoctorSchedules",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "BreakStartTime",
                table: "DoctorSchedules",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnlineBookingEnabled",
                table: "DoctorSchedules",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "DoctorBlockedSlots",
                columns: table => new
                {
                    DoctorBlockedSlotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    BlockDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorBlockedSlots", x => x.DoctorBlockedSlotId);
                    table.ForeignKey(
                        name: "FK_DoctorBlockedSlots_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorBlockedSlots_DoctorId_BlockDate_StartTime_EndTime",
                table: "DoctorBlockedSlots",
                columns: new[] { "DoctorId", "BlockDate", "StartTime", "EndTime" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorBlockedSlots");

            migrationBuilder.DropColumn(
                name: "BreakEndTime",
                table: "DoctorSchedules");

            migrationBuilder.DropColumn(
                name: "BreakStartTime",
                table: "DoctorSchedules");

            migrationBuilder.DropColumn(
                name: "OnlineBookingEnabled",
                table: "DoctorSchedules");
        }
    }
}
