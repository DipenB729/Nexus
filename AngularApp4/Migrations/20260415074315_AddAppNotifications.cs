using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AngularApp4.Migrations
{
    public partial class AddAppNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppNotifications",
                columns: table => new
                {
                    AppNotificationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientUserId = table.Column<long>(type: "bigint", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RelatedEntityName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RelatedEntityId = table.Column<long>(type: "bigint", nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    IsDismissed = table.Column<bool>(type: "bit", nullable: false),
                    ScheduledForUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppNotifications", x => x.AppNotificationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_RelatedEntityName_RelatedEntityId",
                table: "AppNotifications",
                columns: new[] { "RelatedEntityName", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_RecipientUserId_IsRead_ScheduledForUtc",
                table: "AppNotifications",
                columns: new[] { "RecipientUserId", "IsRead", "ScheduledForUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppNotifications");
        }
    }
}
