using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AngularApp4.Migrations
{
    public partial class AddSmtpDoctorPortalConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DoctorPortalBaseUrl",
                table: "SystemControlSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EmailSmtpPort",
                table: "SystemControlSettings",
                type: "int",
                nullable: false,
                defaultValue: 587);

            migrationBuilder.AddColumn<string>(
                name: "EmailSmtpUsername",
                table: "SystemControlSettings",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EmailUseSsl",
                table: "SystemControlSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoctorPortalBaseUrl",
                table: "SystemControlSettings");

            migrationBuilder.DropColumn(
                name: "EmailSmtpPort",
                table: "SystemControlSettings");

            migrationBuilder.DropColumn(
                name: "EmailSmtpUsername",
                table: "SystemControlSettings");

            migrationBuilder.DropColumn(
                name: "EmailUseSsl",
                table: "SystemControlSettings");
        }
    }
}
