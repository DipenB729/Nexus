using AngularApp4.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AngularApp4.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260413045500_AddPhase7ReportsMonitoring")]
    public partial class AddPhase7ReportsMonitoring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogEntries",
                columns: table => new
                {
                    AuditLogEntryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    EntityId = table.Column<long>(type: "bigint", nullable: true),
                    TargetDisplayName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PerformedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    PerformedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PerformedByRole = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ActorEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEntries", x => x.AuditLogEntryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_Action_CreatedAt",
                table: "AuditLogEntries",
                columns: new[] { "Action", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_Category_CreatedAt",
                table: "AuditLogEntries",
                columns: new[] { "Category", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_CreatedAt",
                table: "AuditLogEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_PerformedByUserId",
                table: "AuditLogEntries",
                column: "PerformedByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogEntries");
        }
    }
}
