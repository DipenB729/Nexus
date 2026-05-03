using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AngularApp4.Migrations
{
    public partial class AddHospitalAdminAssignment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "HospitalProfileId",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_HospitalProfileId",
                table: "Users",
                column: "HospitalProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_HospitalProfiles_HospitalProfileId",
                table: "Users",
                column: "HospitalProfileId",
                principalTable: "HospitalProfiles",
                principalColumn: "HospitalProfileId",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_HospitalProfiles_HospitalProfileId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_HospitalProfileId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HospitalProfileId",
                table: "Users");
        }
    }
}
