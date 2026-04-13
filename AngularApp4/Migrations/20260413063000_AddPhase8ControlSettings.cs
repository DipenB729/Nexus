using AngularApp4.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AngularApp4.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260413063000_AddPhase8ControlSettings")]
    public partial class AddPhase8ControlSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackupLogs",
                columns: table => new
                {
                    BackupLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BackupName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BackupType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriggeredByUserId = table.Column<long>(type: "bigint", nullable: true),
                    TriggeredByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RestoredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RestoredByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupLogs", x => x.BackupLogId);
                });

            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    NotificationSettingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LowStockAlertsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LowStockAlertChannels = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LowStockReminderFrequencyHours = table.Column<int>(type: "int", nullable: false),
                    ExpiryAlertsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ExpiryAlertDays = table.Column<int>(type: "int", nullable: false),
                    ExpiryAlertChannels = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AppointmentRemindersEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AppointmentReminderHoursBefore = table.Column<int>(type: "int", nullable: false),
                    AppointmentReminderChannels = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PaymentDueAlertsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PaymentDueReminderDaysBefore = table.Column<int>(type: "int", nullable: false),
                    PaymentDueAlertChannels = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RecipientEmails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.NotificationSettingId);
                });

            migrationBuilder.CreateTable(
                name: "SecuritySettings",
                columns: table => new
                {
                    SecuritySettingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
                    MinPasswordLength = table.Column<int>(type: "int", nullable: false),
                    RequireUppercase = table.Column<bool>(type: "bit", nullable: false),
                    RequireLowercase = table.Column<bool>(type: "bit", nullable: false),
                    RequireDigit = table.Column<bool>(type: "bit", nullable: false),
                    RequireSpecialCharacter = table.Column<bool>(type: "bit", nullable: false),
                    PasswordExpiryDays = table.Column<int>(type: "int", nullable: false),
                    PermissionReviewIntervalDays = table.Column<int>(type: "int", nullable: false),
                    LastPermissionReviewAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPermissionReviewedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecuritySettings", x => x.SecuritySettingId);
                });

            migrationBuilder.CreateTable(
                name: "SystemControlSettings",
                columns: table => new
                {
                    SystemControlSettingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DefaultCurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    InvoicePrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NextInvoiceNumber = table.Column<int>(type: "int", nullable: false),
                    SmsProviderName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SmsApiUrl = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SmsApiKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SmsSenderId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EmailProviderName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EmailApiUrl = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    EmailApiKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmailFromAddress = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AutoBackupEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AutoBackupTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BackupRetentionCount = table.Column<int>(type: "int", nullable: false),
                    BackupStoragePath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemControlSettings", x => x.SystemControlSettingId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackupLogs_BackupType_CreatedAt",
                table: "BackupLogs",
                columns: new[] { "BackupType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BackupLogs_CreatedAt",
                table: "BackupLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BackupLogs_Status_CreatedAt",
                table: "BackupLogs",
                columns: new[] { "Status", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackupLogs");

            migrationBuilder.DropTable(
                name: "NotificationSettings");

            migrationBuilder.DropTable(
                name: "SecuritySettings");

            migrationBuilder.DropTable(
                name: "SystemControlSettings");
        }
    }
}
