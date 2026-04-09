using AngularApp4.Model.Hms;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<HospitalService> HospitalServices => Set<HospitalService>();
    public DbSet<AngularApp4.Model.Service> Services => Set<AngularApp4.Model.Service>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<HospitalProfile> HospitalProfiles => Set<HospitalProfile>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<PatientCategory> PatientCategories => Set<PatientCategory>();
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Bed> Beds => Set<Bed>();
    public DbSet<AppointmentTokenSetting> AppointmentTokenSettings => Set<AppointmentTokenSetting>();
    public DbSet<PatientAdmission> PatientAdmissions => Set<PatientAdmission>();
    public DbSet<AdmissionTransfer> AdmissionTransfers => Set<AdmissionTransfer>();
    public DbSet<MedicineInventoryItem> MedicineInventoryItems => Set<MedicineInventoryItem>();
    public DbSet<BillingChargeDefinition> BillingChargeDefinitions => Set<BillingChargeDefinition>();
    public DbSet<BillingPaymentMethod> BillingPaymentMethods => Set<BillingPaymentMethod>();
    public DbSet<BillingPartner> BillingPartners => Set<BillingPartner>();
    public DbSet<BillingRule> BillingRules => Set<BillingRule>();
    public DbSet<BillingInvoice> BillingInvoices => Set<BillingInvoice>();
    public DbSet<BillingInvoiceItem> BillingInvoiceItems => Set<BillingInvoiceItem>();
    public DbSet<BillingInvoicePayment> BillingInvoicePayments => Set<BillingInvoicePayment>();
    public DbSet<BillingRefund> BillingRefunds => Set<BillingRefund>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Branch>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(x => new { x.BranchId, x.Name }).IsUnique();
        modelBuilder.Entity<PatientCategory>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Patient>()
            .HasIndex(x => x.MedicalRecordNumber)
            .IsUnique()
            .HasFilter("[MedicalRecordNumber] IS NOT NULL");
        modelBuilder.Entity<Doctor>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Staff>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<HospitalService>().HasIndex(x => x.ServiceName).IsUnique();
        modelBuilder.Entity<AngularApp4.Model.Service>().HasIndex(x => x.Name).IsUnique();
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

        modelBuilder.Entity<User>()
            .HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder.Entity<DoctorSchedule>()
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
    }
}
