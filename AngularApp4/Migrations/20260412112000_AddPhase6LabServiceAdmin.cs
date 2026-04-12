using AngularApp4.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AngularApp4.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260412112000_AddPhase6LabServiceAdmin")]
    public partial class AddPhase6LabServiceAdmin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabTestMasters",
                columns: table => new
                {
                    LabTestMasterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SampleType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ReportFormat = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTestMasters", x => x.LabTestMasterId);
                });

            migrationBuilder.CreateTable(
                name: "ServicePackages",
                columns: table => new
                {
                    ServicePackageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PackageName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackages", x => x.ServicePackageId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabTestMasters_DepartmentName_TestName",
                table: "LabTestMasters",
                columns: new[] { "DepartmentName", "TestName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackages_Kind_PackageName",
                table: "ServicePackages",
                columns: new[] { "Kind", "PackageName" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabTestMasters");

            migrationBuilder.DropTable(
                name: "ServicePackages");
        }
    }
}
