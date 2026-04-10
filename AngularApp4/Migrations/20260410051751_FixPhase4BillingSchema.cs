using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AngularApp4.Migrations
{
    public partial class FixPhase4BillingSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedDiscountAmount",
                table: "BillingInvoices",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "BillingPartnerId",
                table: "BillingInvoices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BillingRuleId",
                table: "BillingInvoices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimReferenceNumber",
                table: "BillingInvoices",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimSettledAt",
                table: "BillingInvoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimStatus",
                table: "BillingInvoices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimSubmittedAt",
                table: "BillingInvoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountNotes",
                table: "BillingInvoices",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayerType",
                table: "BillingInvoices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "BillingInvoices",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedDiscountAmount",
                table: "BillingInvoices",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "BillingChargeDefinitions",
                columns: table => new
                {
                    BillingChargeDefinitionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    UnitLabel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DefaultAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingChargeDefinitions", x => x.BillingChargeDefinitionId);
                });

            migrationBuilder.CreateTable(
                name: "BillingPartners",
                columns: table => new
                {
                    BillingPartnerId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ClaimSubmissionMode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPartners", x => x.BillingPartnerId);
                });

            migrationBuilder.CreateTable(
                name: "BillingPaymentMethods",
                columns: table => new
                {
                    BillingPaymentMethodId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MethodType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequiresReference = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPaymentMethods", x => x.BillingPaymentMethodId);
                });

            migrationBuilder.CreateTable(
                name: "BillingInvoiceItems",
                columns: table => new
                {
                    BillingInvoiceItemId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillingInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    BillingChargeDefinitionId = table.Column<long>(type: "bigint", nullable: true),
                    ChargeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingInvoiceItems", x => x.BillingInvoiceItemId);
                    table.ForeignKey(
                        name: "FK_BillingInvoiceItems_BillingChargeDefinitions_BillingChargeDefinitionId",
                        column: x => x.BillingChargeDefinitionId,
                        principalTable: "BillingChargeDefinitions",
                        principalColumn: "BillingChargeDefinitionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingInvoiceItems_BillingInvoices_BillingInvoiceId",
                        column: x => x.BillingInvoiceId,
                        principalTable: "BillingInvoices",
                        principalColumn: "BillingInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingRules",
                columns: table => new
                {
                    BillingRuleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillingPartnerId = table.Column<long>(type: "bigint", nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PolicyName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CoPayPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ClaimSubmissionWindowDays = table.Column<int>(type: "int", nullable: false),
                    RequiresPreApproval = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingRules", x => x.BillingRuleId);
                    table.ForeignKey(
                        name: "FK_BillingRules_BillingPartners_BillingPartnerId",
                        column: x => x.BillingPartnerId,
                        principalTable: "BillingPartners",
                        principalColumn: "BillingPartnerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingInvoicePayments",
                columns: table => new
                {
                    BillingInvoicePaymentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillingInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    BillingPaymentMethodId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingInvoicePayments", x => x.BillingInvoicePaymentId);
                    table.ForeignKey(
                        name: "FK_BillingInvoicePayments_BillingInvoices_BillingInvoiceId",
                        column: x => x.BillingInvoiceId,
                        principalTable: "BillingInvoices",
                        principalColumn: "BillingInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingInvoicePayments_BillingPaymentMethods_BillingPaymentMethodId",
                        column: x => x.BillingPaymentMethodId,
                        principalTable: "BillingPaymentMethods",
                        principalColumn: "BillingPaymentMethodId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingRefunds",
                columns: table => new
                {
                    BillingRefundId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillingInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    BillingPaymentMethodId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingRefunds", x => x.BillingRefundId);
                    table.ForeignKey(
                        name: "FK_BillingRefunds_BillingInvoices_BillingInvoiceId",
                        column: x => x.BillingInvoiceId,
                        principalTable: "BillingInvoices",
                        principalColumn: "BillingInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingRefunds_BillingPaymentMethods_BillingPaymentMethodId",
                        column: x => x.BillingPaymentMethodId,
                        principalTable: "BillingPaymentMethods",
                        principalColumn: "BillingPaymentMethodId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_BillingPartnerId",
                table: "BillingInvoices",
                column: "BillingPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_BillingRuleId",
                table: "BillingInvoices",
                column: "BillingRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingChargeDefinitions_Code",
                table: "BillingChargeDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoiceItems_BillingChargeDefinitionId",
                table: "BillingInvoiceItems",
                column: "BillingChargeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoiceItems_BillingInvoiceId",
                table: "BillingInvoiceItems",
                column: "BillingInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoicePayments_BillingInvoiceId",
                table: "BillingInvoicePayments",
                column: "BillingInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoicePayments_BillingPaymentMethodId",
                table: "BillingInvoicePayments",
                column: "BillingPaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPartners_Kind_Code",
                table: "BillingPartners",
                columns: new[] { "Kind", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPartners_Kind_Name",
                table: "BillingPartners",
                columns: new[] { "Kind", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPaymentMethods_Name",
                table: "BillingPaymentMethods",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingRefunds_BillingInvoiceId",
                table: "BillingRefunds",
                column: "BillingInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRefunds_BillingPaymentMethodId",
                table: "BillingRefunds",
                column: "BillingPaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRules_BillingPartnerId_RuleName",
                table: "BillingRules",
                columns: new[] { "BillingPartnerId", "RuleName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BillingInvoices_BillingPartners_BillingPartnerId",
                table: "BillingInvoices",
                column: "BillingPartnerId",
                principalTable: "BillingPartners",
                principalColumn: "BillingPartnerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BillingInvoices_BillingRules_BillingRuleId",
                table: "BillingInvoices",
                column: "BillingRuleId",
                principalTable: "BillingRules",
                principalColumn: "BillingRuleId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingInvoices_BillingPartners_BillingPartnerId",
                table: "BillingInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_BillingInvoices_BillingRules_BillingRuleId",
                table: "BillingInvoices");

            migrationBuilder.DropTable(
                name: "BillingInvoiceItems");

            migrationBuilder.DropTable(
                name: "BillingInvoicePayments");

            migrationBuilder.DropTable(
                name: "BillingRefunds");

            migrationBuilder.DropTable(
                name: "BillingRules");

            migrationBuilder.DropTable(
                name: "BillingChargeDefinitions");

            migrationBuilder.DropTable(
                name: "BillingPaymentMethods");

            migrationBuilder.DropTable(
                name: "BillingPartners");

            migrationBuilder.DropIndex(
                name: "IX_BillingInvoices_BillingPartnerId",
                table: "BillingInvoices");

            migrationBuilder.DropIndex(
                name: "IX_BillingInvoices_BillingRuleId",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "ApprovedDiscountAmount",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "BillingPartnerId",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "BillingRuleId",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "ClaimReferenceNumber",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "ClaimSettledAt",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "ClaimStatus",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "ClaimSubmittedAt",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "DiscountNotes",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "PayerType",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "RefundedAmount",
                table: "BillingInvoices");

            migrationBuilder.DropColumn(
                name: "RequestedDiscountAmount",
                table: "BillingInvoices");
        }
    }
}
