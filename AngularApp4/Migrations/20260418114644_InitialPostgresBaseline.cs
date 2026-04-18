using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AngularApp4.Migrations
{
    public partial class InitialPostgresBaseline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppNotifications",
                columns: table => new
                {
                    AppNotificationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecipientUserId = table.Column<long>(type: "bigint", nullable: false),
                    Category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RelatedEntityName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RelatedEntityId = table.Column<long>(type: "bigint", nullable: true),
                    ActionUrl = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    IsDismissed = table.Column<bool>(type: "boolean", nullable: false),
                    ScheduledForUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppNotifications", x => x.AppNotificationId);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    AppointmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    ScheduleId = table.Column<long>(type: "bigint", nullable: true),
                    ServiceId = table.Column<long>(type: "bigint", nullable: true),
                    AppointmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SlotStartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    SlotEndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    TokenNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AdminRemarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.AppointmentId);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentTokenSettings",
                columns: table => new
                {
                    AppointmentTokenSettingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartingNumber = table.Column<int>(type: "integer", nullable: false),
                    NumberPadding = table.Column<int>(type: "integer", nullable: false),
                    ResetDaily = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentTokenSettings", x => x.AppointmentTokenSettingId);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogEntries",
                columns: table => new
                {
                    AuditLogEntryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    EntityId = table.Column<long>(type: "bigint", nullable: true),
                    TargetDisplayName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MetadataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PerformedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    PerformedByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PerformedByRole = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ActorEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEntries", x => x.AuditLogEntryId);
                });

            migrationBuilder.CreateTable(
                name: "BackupLogs",
                columns: table => new
                {
                    BackupLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BackupName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BackupType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: true),
                    TriggeredByUserId = table.Column<long>(type: "bigint", nullable: true),
                    TriggeredByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RestoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RestoredByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupLogs", x => x.BackupLogId);
                });

            migrationBuilder.CreateTable(
                name: "BillingChargeDefinitions",
                columns: table => new
                {
                    BillingChargeDefinitionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChargeType = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    UnitLabel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DefaultAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreditLimit = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ClaimSubmissionMode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MethodType = table.Column<string>(type: "text", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequiresReference = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPaymentMethods", x => x.BillingPaymentMethodId);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    BranchId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TotalBeds = table.Column<int>(type: "integer", nullable: false),
                    OccupiedBeds = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.BranchId);
                });

            migrationBuilder.CreateTable(
                name: "HospitalProfiles",
                columns: table => new
                {
                    HospitalProfileId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HospitalName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AddressLine1 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    City = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    StateOrProvince = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TaxLabel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TaxRegistrationNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TaxPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    InvoicePrefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InvoiceStartingNumber = table.Column<int>(type: "integer", nullable: false),
                    InvoiceFooterNote = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    MultiBranchEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalProfiles", x => x.HospitalProfileId);
                });

            migrationBuilder.CreateTable(
                name: "HospitalServices",
                columns: table => new
                {
                    ServiceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalServices", x => x.ServiceId);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCategories",
                columns: table => new
                {
                    InventoryCategoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryType = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCategories", x => x.InventoryCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "InventoryUnits",
                columns: table => new
                {
                    InventoryUnitId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryUnits", x => x.InventoryUnitId);
                });

            migrationBuilder.CreateTable(
                name: "LabTestMasters",
                columns: table => new
                {
                    LabTestMasterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TestName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DepartmentName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SampleType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReportFormat = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTestMasters", x => x.LabTestMasterId);
                });

            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    NotificationSettingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LowStockAlertsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LowStockAlertChannels = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    LowStockReminderFrequencyHours = table.Column<int>(type: "integer", nullable: false),
                    ExpiryAlertsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiryAlertDays = table.Column<int>(type: "integer", nullable: false),
                    ExpiryAlertChannels = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AppointmentRemindersEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AppointmentReminderHoursBefore = table.Column<int>(type: "integer", nullable: false),
                    AppointmentReminderChannels = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PaymentDueAlertsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentDueReminderDaysBefore = table.Column<int>(type: "integer", nullable: false),
                    PaymentDueAlertChannels = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RecipientEmails = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.NotificationSettingId);
                });

            migrationBuilder.CreateTable(
                name: "PatientCategories",
                columns: table => new
                {
                    PatientCategoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    PriorityOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientCategories", x => x.PatientCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "SecuritySettings",
                columns: table => new
                {
                    SecuritySettingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionTimeoutMinutes = table.Column<int>(type: "integer", nullable: false),
                    MinPasswordLength = table.Column<int>(type: "integer", nullable: false),
                    RequireUppercase = table.Column<bool>(type: "boolean", nullable: false),
                    RequireLowercase = table.Column<bool>(type: "boolean", nullable: false),
                    RequireDigit = table.Column<bool>(type: "boolean", nullable: false),
                    RequireSpecialCharacter = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordExpiryDays = table.Column<int>(type: "integer", nullable: false),
                    PermissionReviewIntervalDays = table.Column<int>(type: "integer", nullable: false),
                    LastPermissionReviewAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastPermissionReviewedByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecuritySettings", x => x.SecuritySettingId);
                });

            migrationBuilder.CreateTable(
                name: "ServicePackages",
                columns: table => new
                {
                    ServicePackageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    PackageName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DepartmentName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackages", x => x.ServicePackageId);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    SupplierId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SupplierCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PaymentTermsDays = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.SupplierId);
                });

            migrationBuilder.CreateTable(
                name: "SystemControlSettings",
                columns: table => new
                {
                    SystemControlSettingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DefaultCurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    InvoicePrefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NextInvoiceNumber = table.Column<int>(type: "integer", nullable: false),
                    SmsProviderName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SmsApiUrl = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SmsApiKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SmsSenderId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EmailProviderName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EmailApiUrl = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    EmailSmtpPort = table.Column<int>(type: "integer", nullable: false),
                    EmailSmtpUsername = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EmailApiKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmailFromAddress = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EmailUseSsl = table.Column<bool>(type: "boolean", nullable: false),
                    DoctorPortalBaseUrl = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    AutoBackupEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoBackupTime = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BackupRetentionCount = table.Column<int>(type: "integer", nullable: false),
                    BackupStoragePath = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemControlSettings", x => x.SystemControlSettingId);
                });

            migrationBuilder.CreateTable(
                name: "BillingRules",
                columns: table => new
                {
                    BillingRuleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillingPartnerId = table.Column<long>(type: "bigint", nullable: false),
                    RuleName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PolicyName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CoPayPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ClaimSubmissionWindowDays = table.Column<int>(type: "integer", nullable: false),
                    RequiresPreApproval = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentId);
                    table.ForeignKey(
                        name: "FK_Departments_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicineInventoryItems",
                columns: table => new
                {
                    MedicineInventoryItemId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    QuantityInStock = table.Column<int>(type: "integer", nullable: false),
                    ReorderLevel = table.Column<int>(type: "integer", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineInventoryItems", x => x.MedicineInventoryItemId);
                    table.ForeignKey(
                        name: "FK_MedicineInventoryItems_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockLocations",
                columns: table => new
                {
                    StockLocationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LocationType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLocations", x => x.StockLocationId);
                    table.ForeignKey(
                        name: "FK_StockLocations_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicineMasters",
                columns: table => new
                {
                    MedicineMasterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventoryUnitId = table.Column<long>(type: "bigint", nullable: false),
                    InventoryCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    MedicineName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    GenericName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Brand = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Strength = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    BatchRequired = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumStock = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaximumStock = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineMasters", x => x.MedicineMasterId);
                    table.ForeignKey(
                        name: "FK_MedicineMasters_InventoryCategories_InventoryCategoryId",
                        column: x => x.InventoryCategoryId,
                        principalTable: "InventoryCategories",
                        principalColumn: "InventoryCategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicineMasters_InventoryUnits_InventoryUnitId",
                        column: x => x.InventoryUnitId,
                        principalTable: "InventoryUnits",
                        principalColumn: "InventoryUnitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockItemMasters",
                columns: table => new
                {
                    StockItemMasterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventoryUnitId = table.Column<long>(type: "bigint", nullable: false),
                    InventoryCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    ItemType = table.Column<string>(type: "text", nullable: false),
                    ItemName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Specification = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    MinimumStock = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaximumStock = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockItemMasters", x => x.StockItemMasterId);
                    table.ForeignKey(
                        name: "FK_StockItemMasters_InventoryCategories_InventoryCategoryId",
                        column: x => x.InventoryCategoryId,
                        principalTable: "InventoryCategories",
                        principalColumn: "InventoryCategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockItemMasters_InventoryUnits_InventoryUnitId",
                        column: x => x.InventoryUnitId,
                        principalTable: "InventoryUnits",
                        principalColumn: "InventoryUnitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PatientCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    MedicalRecordNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    BloodGroup = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    EmergencyContact = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MergedIntoPatientId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientId);
                    table.ForeignKey(
                        name: "FK_Patients_PatientCategories_PatientCategoryId",
                        column: x => x.PatientCategoryId,
                        principalTable: "PatientCategories",
                        principalColumn: "PatientCategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Patients_Patients_MergedIntoPatientId",
                        column: x => x.MergedIntoPatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RolePermissionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    ModuleKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ModuleName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CanView = table.Column<bool>(type: "boolean", nullable: false),
                    CanAdd = table.Column<bool>(type: "boolean", nullable: false),
                    CanEdit = table.Column<bool>(type: "boolean", nullable: false),
                    CanDelete = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.RolePermissionId);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PasswordHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "bytea", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    DoctorId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Specialization = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ExperienceYears = table.Column<int>(type: "integer", nullable: false),
                    Qualification = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    LicenseNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OpdDays = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    OpdStartTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    OpdEndTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ConsultationFee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.DoctorId);
                    table.ForeignKey(
                        name: "FK_Doctors_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doctors_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    StaffId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Designation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Shift = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    JoinDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.StaffId);
                    table.ForeignKey(
                        name: "FK_Staff_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Staff_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wards",
                columns: table => new
                {
                    WardId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    WardType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RoomType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ChargePerDay = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wards", x => x.WardId);
                    table.ForeignKey(
                        name: "FK_Wards_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Wards_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    PurchaseOrderId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    StockLocationId = table.Column<long>(type: "bigint", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.PurchaseOrderId);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_StockLocations_StockLocationId",
                        column: x => x.StockLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "StockLocationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustments",
                columns: table => new
                {
                    StockAdjustmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdjustmentNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StockLocationId = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AdjustmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustments", x => x.StockAdjustmentId);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_StockLocations_StockLocationId",
                        column: x => x.StockLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "StockLocationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                columns: table => new
                {
                    StockTransferId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransferNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FromStockLocationId = table.Column<long>(type: "bigint", nullable: false),
                    ToStockLocationId = table.Column<long>(type: "bigint", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.StockTransferId);
                    table.ForeignKey(
                        name: "FK_StockTransfers_StockLocations_FromStockLocationId",
                        column: x => x.FromStockLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "StockLocationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_StockLocations_ToStockLocationId",
                        column: x => x.ToStockLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "StockLocationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockBatches",
                columns: table => new
                {
                    StockBatchId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockLocationId = table.Column<long>(type: "bigint", nullable: false),
                    MedicineMasterId = table.Column<long>(type: "bigint", nullable: true),
                    StockItemMasterId = table.Column<long>(type: "bigint", nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuantityOnHand = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LastMovementAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBatches", x => x.StockBatchId);
                    table.ForeignKey(
                        name: "FK_StockBatches_MedicineMasters_MedicineMasterId",
                        column: x => x.MedicineMasterId,
                        principalTable: "MedicineMasters",
                        principalColumn: "MedicineMasterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBatches_StockItemMasters_StockItemMasterId",
                        column: x => x.StockItemMasterId,
                        principalTable: "StockItemMasters",
                        principalColumn: "StockItemMasterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBatches_StockLocations_StockLocationId",
                        column: x => x.StockLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "StockLocationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingInvoices",
                columns: table => new
                {
                    BillingInvoiceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    PayerType = table.Column<string>(type: "text", nullable: false),
                    BillingPartnerId = table.Column<long>(type: "bigint", nullable: true),
                    BillingRuleId = table.Column<long>(type: "bigint", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    RequestedDiscountAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ApprovedDiscountAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ClaimStatus = table.Column<string>(type: "text", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastPaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClaimSubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClaimSettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClaimReferenceNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    DiscountNotes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingInvoices", x => x.BillingInvoiceId);
                    table.ForeignKey(
                        name: "FK_BillingInvoices_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingInvoices_BillingPartners_BillingPartnerId",
                        column: x => x.BillingPartnerId,
                        principalTable: "BillingPartners",
                        principalColumn: "BillingPartnerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingInvoices_BillingRules_BillingRuleId",
                        column: x => x.BillingRuleId,
                        principalTable: "BillingRules",
                        principalColumn: "BillingRuleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingInvoices_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingInvoices_Patients_PatientId",
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    MedicalHistory = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Allergies = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ChronicConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CurrentMedications = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: true),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UploadedByRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "DiagnosticRequests",
                columns: table => new
                {
                    DiagnosticRequestId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    RequestType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LabTestMasterId = table.Column<long>(type: "bigint", nullable: true),
                    RequestedItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResultSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "DoctorAvailabilityExceptions",
                columns: table => new
                {
                    DoctorAvailabilityExceptionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    ExceptionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "DoctorBlockedSlots",
                columns: table => new
                {
                    DoctorBlockedSlotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    BlockDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "DoctorConsultations",
                columns: table => new
                {
                    DoctorConsultationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    Symptoms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Diagnosis = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    VitalObservations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Advice = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FollowUpDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "DoctorPrescriptions",
                columns: table => new
                {
                    DoctorPrescriptionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "DoctorSchedules",
                columns: table => new
                {
                    ScheduleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "smallint", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    BreakStartTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    BreakEndTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    SlotDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaxPatientsPerSlot = table.Column<int>(type: "integer", nullable: false),
                    OnlineBookingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorSchedules", x => x.ScheduleId);
                    table.ForeignKey(
                        name: "FK_DoctorSchedules_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Beds",
                columns: table => new
                {
                    BedId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WardId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    BedNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChargePerDay = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    IsOccupied = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beds", x => x.BedId);
                    table.ForeignKey(
                        name: "FK_Beds_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Beds_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Beds_Wards_WardId",
                        column: x => x.WardId,
                        principalTable: "Wards",
                        principalColumn: "WardId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseInvoices",
                columns: table => new
                {
                    PurchaseInvoiceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseOrderId = table.Column<long>(type: "bigint", nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    StockLocationId = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseInvoices", x => x.PurchaseInvoiceId);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "PurchaseOrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_StockLocations_StockLocationId",
                        column: x => x.StockLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "StockLocationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                columns: table => new
                {
                    PurchaseOrderLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseOrderId = table.Column<long>(type: "bigint", nullable: false),
                    MedicineMasterId = table.Column<long>(type: "bigint", nullable: true),
                    StockItemMasterId = table.Column<long>(type: "bigint", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    UnitName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    OrderedQuantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.PurchaseOrderLineId);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_MedicineMasters_MedicineMasterId",
                        column: x => x.MedicineMasterId,
                        principalTable: "MedicineMasters",
                        principalColumn: "MedicineMasterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "PurchaseOrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_StockItemMasters_StockItemMasterId",
                        column: x => x.StockItemMasterId,
                        principalTable: "StockItemMasters",
                        principalColumn: "StockItemMasterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustmentLines",
                columns: table => new
                {
                    StockAdjustmentLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockAdjustmentId = table.Column<long>(type: "bigint", nullable: false),
                    StockBatchId = table.Column<long>(type: "bigint", nullable: false),
                    ItemName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuantityDelta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustmentLines", x => x.StockAdjustmentLineId);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLines_StockAdjustments_StockAdjustmentId",
                        column: x => x.StockAdjustmentId,
                        principalTable: "StockAdjustments",
                        principalColumn: "StockAdjustmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLines_StockBatches_StockBatchId",
                        column: x => x.StockBatchId,
                        principalTable: "StockBatches",
                        principalColumn: "StockBatchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferLines",
                columns: table => new
                {
                    StockTransferLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockTransferId = table.Column<long>(type: "bigint", nullable: false),
                    StockBatchId = table.Column<long>(type: "bigint", nullable: false),
                    ItemName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferLines", x => x.StockTransferLineId);
                    table.ForeignKey(
                        name: "FK_StockTransferLines_StockBatches_StockBatchId",
                        column: x => x.StockBatchId,
                        principalTable: "StockBatches",
                        principalColumn: "StockBatchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferLines_StockTransfers_StockTransferId",
                        column: x => x.StockTransferId,
                        principalTable: "StockTransfers",
                        principalColumn: "StockTransferId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingInvoiceItems",
                columns: table => new
                {
                    BillingInvoiceItemId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillingInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    BillingChargeDefinitionId = table.Column<long>(type: "bigint", nullable: true),
                    ChargeType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingInvoiceItems", x => x.BillingInvoiceItemId);
                    table.ForeignKey(
                        name: "FK_BillingInvoiceItems_BillingChargeDefinitions_BillingChargeD~",
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
                name: "BillingInvoicePayments",
                columns: table => new
                {
                    BillingInvoicePaymentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillingInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    BillingPaymentMethodId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                        name: "FK_BillingInvoicePayments_BillingPaymentMethods_BillingPayment~",
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillingInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    BillingPaymentMethodId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "DoctorPrescriptionItems",
                columns: table => new
                {
                    DoctorPrescriptionItemId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorPrescriptionId = table.Column<long>(type: "bigint", nullable: false),
                    MedicineMasterId = table.Column<long>(type: "bigint", nullable: true),
                    MedicineName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Dosage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Frequency = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Duration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorPrescriptionItems", x => x.DoctorPrescriptionItemId);
                    table.ForeignKey(
                        name: "FK_DoctorPrescriptionItems_DoctorPrescriptions_DoctorPrescript~",
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

            migrationBuilder.CreateTable(
                name: "PatientAdmissions",
                columns: table => new
                {
                    PatientAdmissionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdmissionNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: true),
                    DoctorId = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    WardId = table.Column<long>(type: "bigint", nullable: false),
                    BedId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AdmissionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedDischargeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DischargeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DischargeSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DischargeApprovedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    DischargeApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "PurchaseReturns",
                columns: table => new
                {
                    PurchaseReturnId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    ReturnNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturns", x => x.PurchaseReturnId);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_PurchaseInvoices_PurchaseInvoiceId",
                        column: x => x.PurchaseInvoiceId,
                        principalTable: "PurchaseInvoices",
                        principalColumn: "PurchaseInvoiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseInvoiceLines",
                columns: table => new
                {
                    PurchaseInvoiceLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseOrderLineId = table.Column<long>(type: "bigint", nullable: true),
                    MedicineMasterId = table.Column<long>(type: "bigint", nullable: true),
                    StockItemMasterId = table.Column<long>(type: "bigint", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    UnitName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseInvoiceLines", x => x.PurchaseInvoiceLineId);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoiceLines_MedicineMasters_MedicineMasterId",
                        column: x => x.MedicineMasterId,
                        principalTable: "MedicineMasters",
                        principalColumn: "MedicineMasterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoiceLines_PurchaseInvoices_PurchaseInvoiceId",
                        column: x => x.PurchaseInvoiceId,
                        principalTable: "PurchaseInvoices",
                        principalColumn: "PurchaseInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoiceLines_PurchaseOrderLines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalTable: "PurchaseOrderLines",
                        principalColumn: "PurchaseOrderLineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoiceLines_StockItemMasters_StockItemMasterId",
                        column: x => x.StockItemMasterId,
                        principalTable: "StockItemMasters",
                        principalColumn: "StockItemMasterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionTransfers",
                columns: table => new
                {
                    AdmissionTransferId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientAdmissionId = table.Column<long>(type: "bigint", nullable: false),
                    FromWardId = table.Column<long>(type: "bigint", nullable: true),
                    FromBedId = table.Column<long>(type: "bigint", nullable: true),
                    ToWardId = table.Column<long>(type: "bigint", nullable: false),
                    ToBedId = table.Column<long>(type: "bigint", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "PurchaseReturnLines",
                columns: table => new
                {
                    PurchaseReturnLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseReturnId = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseInvoiceLineId = table.Column<long>(type: "bigint", nullable: true),
                    MedicineMasterId = table.Column<long>(type: "bigint", nullable: true),
                    StockItemMasterId = table.Column<long>(type: "bigint", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturnLines", x => x.PurchaseReturnLineId);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLines_MedicineMasters_MedicineMasterId",
                        column: x => x.MedicineMasterId,
                        principalTable: "MedicineMasters",
                        principalColumn: "MedicineMasterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLines_PurchaseInvoiceLines_PurchaseInvoiceLin~",
                        column: x => x.PurchaseInvoiceLineId,
                        principalTable: "PurchaseInvoiceLines",
                        principalColumn: "PurchaseInvoiceLineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLines_PurchaseReturns_PurchaseReturnId",
                        column: x => x.PurchaseReturnId,
                        principalTable: "PurchaseReturns",
                        principalColumn: "PurchaseReturnId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLines_StockItemMasters_StockItemMasterId",
                        column: x => x.StockItemMasterId,
                        principalTable: "StockItemMasters",
                        principalColumn: "StockItemMasterId",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "IX_AppNotifications_RecipientUserId_IsRead_ScheduledForUtc",
                table: "AppNotifications",
                columns: new[] { "RecipientUserId", "IsRead", "ScheduledForUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_RelatedEntityName_RelatedEntityId",
                table: "AppNotifications",
                columns: new[] { "RelatedEntityName", "RelatedEntityId" });

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

            migrationBuilder.CreateIndex(
                name: "IX_Beds_BranchId",
                table: "Beds",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Beds_DepartmentId",
                table: "Beds",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Beds_WardId_BedNumber",
                table: "Beds",
                columns: new[] { "WardId", "BedNumber" },
                unique: true);

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
                name: "IX_BillingInvoices_AppointmentId",
                table: "BillingInvoices",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_BillingPartnerId",
                table: "BillingInvoices",
                column: "BillingPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_BillingRuleId",
                table: "BillingInvoices",
                column: "BillingRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_BranchId",
                table: "BillingInvoices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_InvoiceNumber",
                table: "BillingInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_PatientId",
                table: "BillingInvoices",
                column: "PatientId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Code",
                table: "Branches",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_BranchId_Code",
                table: "Departments",
                columns: new[] { "BranchId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_BranchId_Name",
                table: "Departments",
                columns: new[] { "BranchId", "Name" },
                unique: true);

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
                name: "IX_DoctorAvailabilityExceptions_DoctorId_StartDate_EndDate_Exc~",
                table: "DoctorAvailabilityExceptions",
                columns: new[] { "DoctorId", "StartDate", "EndDate", "ExceptionType" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorBlockedSlots_DoctorId_BlockDate_StartTime_EndTime",
                table: "DoctorBlockedSlots",
                columns: new[] { "DoctorId", "BlockDate", "StartTime", "EndTime" });

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
                name: "IX_Doctors_BranchId",
                table: "Doctors",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_DepartmentId",
                table: "Doctors",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_Email",
                table: "Doctors",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSchedules_DoctorId",
                table: "DoctorSchedules",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_HospitalServices_ServiceName",
                table: "HospitalServices",
                column: "ServiceName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_CategoryType_Name",
                table: "InventoryCategories",
                columns: new[] { "CategoryType", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryUnits_Name",
                table: "InventoryUnits",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabTestMasters_DepartmentName_TestName",
                table: "LabTestMasters",
                columns: new[] { "DepartmentName", "TestName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicineInventoryItems_BranchId",
                table: "MedicineInventoryItems",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineMasters_InventoryCategoryId",
                table: "MedicineMasters",
                column: "InventoryCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineMasters_InventoryUnitId",
                table: "MedicineMasters",
                column: "InventoryUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineMasters_MedicineName_Brand_Strength",
                table: "MedicineMasters",
                columns: new[] { "MedicineName", "Brand", "Strength" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_PatientCategories_Name",
                table: "PatientCategories",
                column: "Name",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Patients_MedicalRecordNumber",
                table: "Patients",
                column: "MedicalRecordNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_MergedIntoPatientId",
                table: "Patients",
                column: "MergedIntoPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PatientCategoryId",
                table: "Patients",
                column: "PatientCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserId",
                table: "Patients",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceLines_MedicineMasterId",
                table: "PurchaseInvoiceLines",
                column: "MedicineMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceLines_PurchaseInvoiceId",
                table: "PurchaseInvoiceLines",
                column: "PurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceLines_PurchaseOrderLineId",
                table: "PurchaseInvoiceLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceLines_StockItemMasterId",
                table: "PurchaseInvoiceLines",
                column: "StockItemMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_PurchaseOrderId",
                table: "PurchaseInvoices",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_StockLocationId",
                table: "PurchaseInvoices",
                column: "StockLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_SupplierId_InvoiceNumber",
                table: "PurchaseInvoices",
                columns: new[] { "SupplierId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_MedicineMasterId",
                table: "PurchaseOrderLines",
                column: "MedicineMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_PurchaseOrderId",
                table: "PurchaseOrderLines",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_StockItemMasterId",
                table: "PurchaseOrderLines",
                column: "StockItemMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_StockLocationId",
                table: "PurchaseOrders",
                column: "StockLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLines_MedicineMasterId",
                table: "PurchaseReturnLines",
                column: "MedicineMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLines_PurchaseInvoiceLineId",
                table: "PurchaseReturnLines",
                column: "PurchaseInvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLines_PurchaseReturnId",
                table: "PurchaseReturnLines",
                column: "PurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLines_StockItemMasterId",
                table: "PurchaseReturnLines",
                column: "StockItemMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_PurchaseInvoiceId",
                table: "PurchaseReturns",
                column: "PurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_ReturnNumber",
                table: "PurchaseReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_SupplierId",
                table: "PurchaseReturns",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_ModuleKey",
                table: "RolePermissions",
                columns: new[] { "RoleId", "ModuleKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackages_Kind_PackageName",
                table: "ServicePackages",
                columns: new[] { "Kind", "PackageName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_Name",
                table: "Services",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_BranchId",
                table: "Staff",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_DepartmentId",
                table: "Staff",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_Email",
                table: "Staff",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLines_StockAdjustmentId",
                table: "StockAdjustmentLines",
                column: "StockAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLines_StockBatchId",
                table: "StockAdjustmentLines",
                column: "StockBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_AdjustmentNumber",
                table: "StockAdjustments",
                column: "AdjustmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_StockLocationId",
                table: "StockAdjustments",
                column: "StockLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_MedicineMasterId",
                table: "StockBatches",
                column: "MedicineMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_StockItemMasterId",
                table: "StockBatches",
                column: "StockItemMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_StockLocationId",
                table: "StockBatches",
                column: "StockLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItemMasters_InventoryCategoryId",
                table: "StockItemMasters",
                column: "InventoryCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItemMasters_InventoryUnitId",
                table: "StockItemMasters",
                column: "InventoryUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItemMasters_ItemName_ItemType",
                table: "StockItemMasters",
                columns: new[] { "ItemName", "ItemType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLocations_BranchId",
                table: "StockLocations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLocations_Code",
                table: "StockLocations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLocations_Name",
                table: "StockLocations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLines_StockBatchId",
                table: "StockTransferLines",
                column: "StockBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLines_StockTransferId",
                table: "StockTransferLines",
                column: "StockTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_FromStockLocationId",
                table: "StockTransfers",
                column: "FromStockLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ToStockLocationId",
                table: "StockTransfers",
                column: "ToStockLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TransferNumber",
                table: "StockTransfers",
                column: "TransferNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierCode",
                table: "Suppliers",
                column: "SupplierCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierName",
                table: "Suppliers",
                column: "SupplierName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Wards_BranchId_Name",
                table: "Wards",
                columns: new[] { "BranchId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wards_DepartmentId",
                table: "Wards",
                column: "DepartmentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionTransfers");

            migrationBuilder.DropTable(
                name: "AppNotifications");

            migrationBuilder.DropTable(
                name: "AppointmentTokenSettings");

            migrationBuilder.DropTable(
                name: "AuditLogEntries");

            migrationBuilder.DropTable(
                name: "BackupLogs");

            migrationBuilder.DropTable(
                name: "BillingInvoiceItems");

            migrationBuilder.DropTable(
                name: "BillingInvoicePayments");

            migrationBuilder.DropTable(
                name: "BillingRefunds");

            migrationBuilder.DropTable(
                name: "DiagnosticRequests");

            migrationBuilder.DropTable(
                name: "DoctorAvailabilityExceptions");

            migrationBuilder.DropTable(
                name: "DoctorBlockedSlots");

            migrationBuilder.DropTable(
                name: "DoctorConsultations");

            migrationBuilder.DropTable(
                name: "DoctorPrescriptionItems");

            migrationBuilder.DropTable(
                name: "DoctorSchedules");

            migrationBuilder.DropTable(
                name: "HospitalProfiles");

            migrationBuilder.DropTable(
                name: "HospitalServices");

            migrationBuilder.DropTable(
                name: "MedicineInventoryItems");

            migrationBuilder.DropTable(
                name: "NotificationSettings");

            migrationBuilder.DropTable(
                name: "PatientClinicalProfiles");

            migrationBuilder.DropTable(
                name: "PatientDocuments");

            migrationBuilder.DropTable(
                name: "PurchaseReturnLines");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SecuritySettings");

            migrationBuilder.DropTable(
                name: "ServicePackages");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "Staff");

            migrationBuilder.DropTable(
                name: "StockAdjustmentLines");

            migrationBuilder.DropTable(
                name: "StockTransferLines");

            migrationBuilder.DropTable(
                name: "SystemControlSettings");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "PatientAdmissions");

            migrationBuilder.DropTable(
                name: "BillingChargeDefinitions");

            migrationBuilder.DropTable(
                name: "BillingInvoices");

            migrationBuilder.DropTable(
                name: "BillingPaymentMethods");

            migrationBuilder.DropTable(
                name: "LabTestMasters");

            migrationBuilder.DropTable(
                name: "DoctorPrescriptions");

            migrationBuilder.DropTable(
                name: "PurchaseInvoiceLines");

            migrationBuilder.DropTable(
                name: "PurchaseReturns");

            migrationBuilder.DropTable(
                name: "StockAdjustments");

            migrationBuilder.DropTable(
                name: "StockBatches");

            migrationBuilder.DropTable(
                name: "StockTransfers");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Beds");

            migrationBuilder.DropTable(
                name: "BillingRules");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines");

            migrationBuilder.DropTable(
                name: "PurchaseInvoices");

            migrationBuilder.DropTable(
                name: "Wards");

            migrationBuilder.DropTable(
                name: "BillingPartners");

            migrationBuilder.DropTable(
                name: "PatientCategories");

            migrationBuilder.DropTable(
                name: "MedicineMasters");

            migrationBuilder.DropTable(
                name: "StockItemMasters");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "InventoryCategories");

            migrationBuilder.DropTable(
                name: "InventoryUnits");

            migrationBuilder.DropTable(
                name: "StockLocations");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Branches");
        }
    }
}
