using AngularApp4.Model.Hms;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading;

namespace AngularApp4.Data;

public class AppDbContext : DbContext
{
    private static long _nextId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<DoctorAvailabilityException> DoctorAvailabilityExceptions => Set<DoctorAvailabilityException>();
    public DbSet<DoctorBlockedSlot> DoctorBlockedSlots => Set<DoctorBlockedSlot>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<HospitalService> HospitalServices => Set<HospitalService>();
    public DbSet<LabTestMaster> LabTestMasters => Set<LabTestMaster>();
    public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<AngularApp4.Model.Service> Services => Set<AngularApp4.Model.Service>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<PatientClinicalProfile> PatientClinicalProfiles => Set<PatientClinicalProfile>();
    public DbSet<DoctorConsultation> DoctorConsultations => Set<DoctorConsultation>();
    public DbSet<DoctorPrescription> DoctorPrescriptions => Set<DoctorPrescription>();
    public DbSet<DoctorPrescriptionItem> DoctorPrescriptionItems => Set<DoctorPrescriptionItem>();
    public DbSet<DiagnosticRequest> DiagnosticRequests => Set<DiagnosticRequest>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<PatientCaseReport> PatientCaseReports => Set<PatientCaseReport>();
    public DbSet<CareConversationMessage> CareConversationMessages => Set<CareConversationMessage>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<HospitalProfile> HospitalProfiles => Set<HospitalProfile>();
    public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();
    public DbSet<SystemControlSetting> SystemControlSettings => Set<SystemControlSetting>();
    public DbSet<SecuritySetting> SecuritySettings => Set<SecuritySetting>();
    public DbSet<BackupLog> BackupLogs => Set<BackupLog>();
    public DbSet<AppNotification> AppNotifications => Set<AppNotification>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<PatientCategory> PatientCategories => Set<PatientCategory>();
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Bed> Beds => Set<Bed>();
    public DbSet<AppointmentTokenSetting> AppointmentTokenSettings => Set<AppointmentTokenSetting>();
    public DbSet<PatientAdmission> PatientAdmissions => Set<PatientAdmission>();
    public DbSet<AdmissionTransfer> AdmissionTransfers => Set<AdmissionTransfer>();
    public DbSet<MedicineInventoryItem> MedicineInventoryItems => Set<MedicineInventoryItem>();
    public DbSet<InventoryUnit> InventoryUnits => Set<InventoryUnit>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<MedicineMaster> MedicineMasters => Set<MedicineMaster>();
    public DbSet<StockItemMaster> StockItemMasters => Set<StockItemMaster>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StockLocation> StockLocations => Set<StockLocation>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines => Set<PurchaseInvoiceLine>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnLine> PurchaseReturnLines => Set<PurchaseReturnLine>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferLine> StockTransferLines => Set<StockTransferLine>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentLine> StockAdjustmentLines => Set<StockAdjustmentLine>();
    public DbSet<BillingChargeDefinition> BillingChargeDefinitions => Set<BillingChargeDefinition>();
    public DbSet<BillingPaymentMethod> BillingPaymentMethods => Set<BillingPaymentMethod>();
    public DbSet<BillingPartner> BillingPartners => Set<BillingPartner>();
    public DbSet<BillingRule> BillingRules => Set<BillingRule>();
    public DbSet<BillingInvoice> BillingInvoices => Set<BillingInvoice>();
    public DbSet<BillingInvoiceItem> BillingInvoiceItems => Set<BillingInvoiceItem>();
    public DbSet<BillingInvoicePayment> BillingInvoicePayments => Set<BillingInvoicePayment>();
    public DbSet<BillingRefund> BillingRefunds => Set<BillingRefund>();

    public override int SaveChanges()
    {
        AssignMongoNumericIds();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AssignMongoNumericIds();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AssignMongoNumericIds();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AssignMongoNumericIds();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AssignMongoNumericIds()
    {
        foreach (var entry in ChangeTracker.Entries().Where(x => x.State == EntityState.Added))
        {
            var keyProperty = GetLongKeyProperty(entry);
            if (keyProperty is null)
            {
                continue;
            }

            var currentValue = (long)(keyProperty.GetValue(entry.Entity) ?? 0L);
            if (currentValue != 0L)
            {
                continue;
            }

            keyProperty.SetValue(entry.Entity, Interlocked.Increment(ref _nextId));
        }
    }

    private static PropertyInfo? GetLongKeyProperty(EntityEntry entry)
    {
        return entry.Entity.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(x => x.PropertyType == typeof(long) && x.GetCustomAttribute<KeyAttribute>() is not null);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Branch>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(x => new { x.BranchId, x.Name }).IsUnique();
        modelBuilder.Entity<PatientCategory>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.HospitalProfileId);
        modelBuilder.Entity<Patient>()
            .HasIndex(x => x.MedicalRecordNumber)
            .IsUnique();
        modelBuilder.Entity<Doctor>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<DoctorAvailabilityException>().HasIndex(x => new { x.DoctorId, x.StartDate, x.EndDate, x.ExceptionType });
        modelBuilder.Entity<DoctorBlockedSlot>().HasIndex(x => new { x.DoctorId, x.BlockDate, x.StartTime, x.EndTime });
        modelBuilder.Entity<Staff>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<HospitalService>().HasIndex(x => x.ServiceName).IsUnique();
        modelBuilder.Entity<LabTestMaster>().HasIndex(x => new { x.DepartmentName, x.TestName }).IsUnique();
        modelBuilder.Entity<ServicePackage>().HasIndex(x => new { x.Kind, x.PackageName }).IsUnique();
        modelBuilder.Entity<AuditLogEntry>().HasIndex(x => x.CreatedAt);
        modelBuilder.Entity<AuditLogEntry>().HasIndex(x => new { x.Category, x.CreatedAt });
        modelBuilder.Entity<AuditLogEntry>().HasIndex(x => new { x.Action, x.CreatedAt });
        modelBuilder.Entity<AuditLogEntry>().HasIndex(x => x.PerformedByUserId);
        modelBuilder.Entity<BackupLog>().HasIndex(x => x.CreatedAt);
        modelBuilder.Entity<BackupLog>().HasIndex(x => new { x.BackupType, x.CreatedAt });
        modelBuilder.Entity<BackupLog>().HasIndex(x => new { x.Status, x.CreatedAt });
        modelBuilder.Entity<AppNotification>().HasIndex(x => new { x.RecipientUserId, x.IsRead, x.ScheduledForUtc });
        modelBuilder.Entity<AppNotification>().HasIndex(x => new { x.RelatedEntityName, x.RelatedEntityId });
        modelBuilder.Entity<AngularApp4.Model.Service>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<InventoryUnit>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<InventoryCategory>().HasIndex(x => new { x.CategoryType, x.Name }).IsUnique();
        modelBuilder.Entity<MedicineMaster>().HasIndex(x => new { x.MedicineName, x.Brand, x.Strength }).IsUnique();
        modelBuilder.Entity<StockItemMaster>().HasIndex(x => new { x.ItemName, x.ItemType }).IsUnique();
        modelBuilder.Entity<Supplier>().HasIndex(x => x.SupplierCode).IsUnique();
        modelBuilder.Entity<Supplier>().HasIndex(x => x.SupplierName).IsUnique();
        modelBuilder.Entity<StockLocation>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<StockLocation>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<PurchaseOrder>().HasIndex(x => x.OrderNumber).IsUnique();
        modelBuilder.Entity<PurchaseInvoice>().HasIndex(x => new { x.SupplierId, x.InvoiceNumber }).IsUnique();
        modelBuilder.Entity<PurchaseReturn>().HasIndex(x => x.ReturnNumber).IsUnique();
        modelBuilder.Entity<StockTransfer>().HasIndex(x => x.TransferNumber).IsUnique();
        modelBuilder.Entity<StockAdjustment>().HasIndex(x => x.AdjustmentNumber).IsUnique();
        modelBuilder.Entity<BillingChargeDefinition>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<BillingPaymentMethod>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<BillingPartner>().HasIndex(x => new { x.Kind, x.Name }).IsUnique();
        modelBuilder.Entity<BillingPartner>().HasIndex(x => new { x.Kind, x.Code }).IsUnique();
        modelBuilder.Entity<BillingRule>().HasIndex(x => new { x.BillingPartnerId, x.RuleName }).IsUnique();
        modelBuilder.Entity<BillingInvoice>().HasIndex(x => x.InvoiceNumber).IsUnique();
        modelBuilder.Entity<RolePermission>().HasIndex(x => new { x.RoleId, x.ModuleKey }).IsUnique();
        modelBuilder.Entity<Ward>().HasIndex(x => new { x.BranchId, x.Name }).IsUnique();
        modelBuilder.Entity<Bed>().HasIndex(x => new { x.WardId, x.BedNumber }).IsUnique();
        modelBuilder.Entity<PatientAdmission>().HasIndex(x => x.AdmissionNumber).IsUnique();
        modelBuilder.Entity<PatientClinicalProfile>().HasIndex(x => x.PatientId).IsUnique();
        modelBuilder.Entity<DoctorConsultation>().HasIndex(x => x.AppointmentId).IsUnique();
        modelBuilder.Entity<DoctorPrescription>().HasIndex(x => x.AppointmentId);
        modelBuilder.Entity<DiagnosticRequest>().HasIndex(x => new { x.PatientId, x.RequestType, x.CreatedAt });
        modelBuilder.Entity<PatientCaseReport>().HasIndex(x => new { x.AppointmentId, x.CreatedAt });
        modelBuilder.Entity<CareConversationMessage>().HasIndex(x => new { x.AppointmentId, x.CreatedAt });

        modelBuilder.Entity<User>()
            .HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<User>()
            .HasOne(x => x.HospitalProfile)
            .WithMany()
            .HasForeignKey(x => x.HospitalProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Patient>()
            .HasOne(x => x.PatientCategory)
            .WithMany()
            .HasForeignKey(x => x.PatientCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Patient>()
            .HasOne<Patient>()
            .WithMany()
            .HasForeignKey(x => x.MergedIntoPatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientClinicalProfile>()
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DoctorSchedule>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DoctorAvailabilityException>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DoctorBlockedSlot>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Department>()
            .HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Doctor>()
            .HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Doctor>()
            .HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Staff>()
            .HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Staff>()
            .HasOne(x => x.DepartmentMaster)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Ward>()
            .HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ward>()
            .HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Bed>()
            .HasOne(x => x.Ward)
            .WithMany()
            .HasForeignKey(x => x.WardId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Bed>()
            .HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Bed>()
            .HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicineInventoryItem>()
            .HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicineMaster>()
            .HasOne(x => x.InventoryUnit)
            .WithMany()
            .HasForeignKey(x => x.InventoryUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicineMaster>()
            .HasOne(x => x.InventoryCategory)
            .WithMany()
            .HasForeignKey(x => x.InventoryCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockItemMaster>()
            .HasOne(x => x.InventoryUnit)
            .WithMany()
            .HasForeignKey(x => x.InventoryUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockItemMaster>()
            .HasOne(x => x.InventoryCategory)
            .WithMany()
            .HasForeignKey(x => x.InventoryCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockLocation>()
            .HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(x => x.StockLocation)
            .WithMany()
            .HasForeignKey(x => x.StockLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(x => x.PurchaseOrder)
            .WithMany()
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(x => x.MedicineMaster)
            .WithMany()
            .HasForeignKey(x => x.MedicineMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne(x => x.StockItemMaster)
            .WithMany()
            .HasForeignKey(x => x.StockItemMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseInvoice>()
            .HasOne(x => x.PurchaseOrder)
            .WithMany()
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseInvoice>()
            .HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseInvoice>()
            .HasOne(x => x.StockLocation)
            .WithMany()
            .HasForeignKey(x => x.StockLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseInvoiceLine>()
            .HasOne(x => x.PurchaseInvoice)
            .WithMany()
            .HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseInvoiceLine>()
            .HasOne(x => x.PurchaseOrderLine)
            .WithMany()
            .HasForeignKey(x => x.PurchaseOrderLineId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseInvoiceLine>()
            .HasOne(x => x.MedicineMaster)
            .WithMany()
            .HasForeignKey(x => x.MedicineMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseInvoiceLine>()
            .HasOne(x => x.StockItemMaster)
            .WithMany()
            .HasForeignKey(x => x.StockItemMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseReturn>()
            .HasOne(x => x.PurchaseInvoice)
            .WithMany()
            .HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseReturn>()
            .HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseReturnLine>()
            .HasOne(x => x.PurchaseReturn)
            .WithMany()
            .HasForeignKey(x => x.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseReturnLine>()
            .HasOne(x => x.PurchaseInvoiceLine)
            .WithMany()
            .HasForeignKey(x => x.PurchaseInvoiceLineId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseReturnLine>()
            .HasOne(x => x.MedicineMaster)
            .WithMany()
            .HasForeignKey(x => x.MedicineMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseReturnLine>()
            .HasOne(x => x.StockItemMaster)
            .WithMany()
            .HasForeignKey(x => x.StockItemMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockBatch>()
            .HasOne(x => x.StockLocation)
            .WithMany()
            .HasForeignKey(x => x.StockLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockBatch>()
            .HasOne(x => x.MedicineMaster)
            .WithMany()
            .HasForeignKey(x => x.MedicineMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockBatch>()
            .HasOne(x => x.StockItemMaster)
            .WithMany()
            .HasForeignKey(x => x.StockItemMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTransfer>()
            .HasOne(x => x.FromStockLocation)
            .WithMany()
            .HasForeignKey(x => x.FromStockLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTransfer>()
            .HasOne(x => x.ToStockLocation)
            .WithMany()
            .HasForeignKey(x => x.ToStockLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTransferLine>()
            .HasOne(x => x.StockTransfer)
            .WithMany()
            .HasForeignKey(x => x.StockTransferId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockTransferLine>()
            .HasOne(x => x.StockBatch)
            .WithMany()
            .HasForeignKey(x => x.StockBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockAdjustment>()
            .HasOne(x => x.StockLocation)
            .WithMany()
            .HasForeignKey(x => x.StockLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockAdjustmentLine>()
            .HasOne(x => x.StockAdjustment)
            .WithMany()
            .HasForeignKey(x => x.StockAdjustmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockAdjustmentLine>()
            .HasOne(x => x.StockBatch)
            .WithMany()
            .HasForeignKey(x => x.StockBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BillingRule>()
            .HasOne(x => x.BillingPartner)
            .WithMany()
            .HasForeignKey(x => x.BillingPartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BillingInvoice>()
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BillingInvoice>()
            .HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BillingInvoice>()
            .HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BillingInvoice>()
            .HasOne(x => x.BillingPartner)
            .WithMany()
            .HasForeignKey(x => x.BillingPartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BillingInvoice>()
            .HasOne(x => x.BillingRule)
            .WithMany()
            .HasForeignKey(x => x.BillingRuleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BillingInvoiceItem>()
            .HasOne(x => x.BillingInvoice)
            .WithMany()
            .HasForeignKey(x => x.BillingInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BillingInvoiceItem>()
            .HasOne(x => x.BillingChargeDefinition)
            .WithMany()
            .HasForeignKey(x => x.BillingChargeDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BillingInvoicePayment>()
            .HasOne(x => x.BillingInvoice)
            .WithMany()
            .HasForeignKey(x => x.BillingInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BillingInvoicePayment>()
            .HasOne(x => x.BillingPaymentMethod)
            .WithMany()
            .HasForeignKey(x => x.BillingPaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BillingRefund>()
            .HasOne(x => x.BillingInvoice)
            .WithMany()
            .HasForeignKey(x => x.BillingInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BillingRefund>()
            .HasOne(x => x.BillingPaymentMethod)
            .WithMany()
            .HasForeignKey(x => x.BillingPaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientAdmission>()
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientAdmission>()
            .HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientAdmission>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientAdmission>()
            .HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientAdmission>()
            .HasOne(x => x.Ward)
            .WithMany()
            .HasForeignKey(x => x.WardId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientAdmission>()
            .HasOne(x => x.Bed)
            .WithMany()
            .HasForeignKey(x => x.BedId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoctorConsultation>()
            .HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DoctorConsultation>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoctorConsultation>()
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoctorPrescription>()
            .HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DoctorPrescription>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoctorPrescription>()
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoctorPrescriptionItem>()
            .HasOne(x => x.DoctorPrescription)
            .WithMany()
            .HasForeignKey(x => x.DoctorPrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DoctorPrescriptionItem>()
            .HasOne(x => x.MedicineMaster)
            .WithMany()
            .HasForeignKey(x => x.MedicineMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DiagnosticRequest>()
            .HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DiagnosticRequest>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DiagnosticRequest>()
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DiagnosticRequest>()
            .HasOne(x => x.LabTestMaster)
            .WithMany()
            .HasForeignKey(x => x.LabTestMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientDocument>()
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientDocument>()
            .HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientCaseReport>()
            .HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientCaseReport>()
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientCaseReport>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientCaseReport>()
            .HasOne(x => x.PatientDocument)
            .WithMany()
            .HasForeignKey(x => x.PatientDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CareConversationMessage>()
            .HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CareConversationMessage>()
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CareConversationMessage>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CareConversationMessage>()
            .HasOne(x => x.SenderUser)
            .WithMany()
            .HasForeignKey(x => x.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdmissionTransfer>()
            .HasOne(x => x.PatientAdmission)
            .WithMany()
            .HasForeignKey(x => x.PatientAdmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AdmissionTransfer>()
            .HasOne(x => x.FromWard)
            .WithMany()
            .HasForeignKey(x => x.FromWardId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdmissionTransfer>()
            .HasOne(x => x.FromBed)
            .WithMany()
            .HasForeignKey(x => x.FromBedId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdmissionTransfer>()
            .HasOne(x => x.ToWard)
            .WithMany()
            .HasForeignKey(x => x.ToWardId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AdmissionTransfer>()
            .HasOne(x => x.ToBed)
            .WithMany()
            .HasForeignKey(x => x.ToBedId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Patient>().HasIndex(x => x.UserId).IsUnique();

        modelBuilder.Entity<Appointment>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<DoctorAvailabilityException>()
            .Property(x => x.ExceptionType)
            .HasConversion<string>()
            .HasMaxLength(32);

        modelBuilder.Entity<DoctorConsultation>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<DiagnosticRequest>()
            .Property(x => x.RequestType)
            .HasConversion<string>()
            .HasMaxLength(32);

        modelBuilder.Entity<DiagnosticRequest>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<PatientAdmission>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<BillingChargeDefinition>()
            .Property(x => x.ChargeType)
            .HasConversion<string>();

        modelBuilder.Entity<BillingPaymentMethod>()
            .Property(x => x.MethodType)
            .HasConversion<string>();

        modelBuilder.Entity<BillingPartner>()
            .Property(x => x.Kind)
            .HasConversion<string>();

        modelBuilder.Entity<InventoryCategory>()
            .Property(x => x.CategoryType)
            .HasConversion<string>();

        modelBuilder.Entity<ServicePackage>()
            .Property(x => x.Kind)
            .HasConversion<string>();

        modelBuilder.Entity<StockItemMaster>()
            .Property(x => x.ItemType)
            .HasConversion<string>();

        modelBuilder.Entity<StockLocation>()
            .Property(x => x.LocationType)
            .HasConversion<string>();

        modelBuilder.Entity<PurchaseOrder>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<PurchaseInvoice>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<StockTransfer>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<StockAdjustment>()
            .Property(x => x.Reason)
            .HasConversion<string>();

        modelBuilder.Entity<StockAdjustment>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<BillingInvoice>()
            .Property(x => x.PayerType)
            .HasConversion<string>();

        modelBuilder.Entity<BillingInvoice>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<BillingInvoice>()
            .Property(x => x.ClaimStatus)
            .HasConversion<string>();

        modelBuilder.Entity<BillingInvoiceItem>()
            .Property(x => x.ChargeType)
            .HasConversion<string>();

        modelBuilder.Entity<BillingRefund>()
            .Property(x => x.Status)
            .HasConversion<string>();

        ConfigureMongoNumericKeys(modelBuilder);
    }

    private static void ConfigureMongoNumericKeys(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var key = entityType.FindPrimaryKey();
            if (key?.Properties.Count != 1)
            {
                continue;
            }

            if (key.Properties[0].ClrType == typeof(long))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<long>(key.Properties[0].Name)
                    .ValueGeneratedOnAdd()
                    .HasValueGenerator<MongoLongIdValueGenerator>();
            }
            else if (key.Properties[0].ClrType == typeof(int))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<int>(key.Properties[0].Name)
                    .ValueGeneratedOnAdd()
                    .HasValueGenerator<MongoIntIdValueGenerator>();
            }
        }
    }
}

public sealed class MongoLongIdValueGenerator : ValueGenerator<long>
{
    private static long _nextId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;

    public override bool GeneratesTemporaryValues => false;

    public override long Next(EntityEntry entry) => Interlocked.Increment(ref _nextId);
}

public sealed class MongoIntIdValueGenerator : ValueGenerator<int>
{
    private static int _nextId = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % int.MaxValue);

    public override bool GeneratesTemporaryValues => false;

    public override int Next(EntityEntry entry) => Interlocked.Increment(ref _nextId);
}
