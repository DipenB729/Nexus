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
    public DbSet<MedicineInventoryItem> MedicineInventoryItems => Set<MedicineInventoryItem>();
    public DbSet<BillingInvoice> BillingInvoices => Set<BillingInvoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Branch>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(x => new { x.BranchId, x.Name }).IsUnique();
        modelBuilder.Entity<PatientCategory>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Doctor>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Staff>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<HospitalService>().HasIndex(x => x.ServiceName).IsUnique();
        modelBuilder.Entity<AngularApp4.Model.Service>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<BillingInvoice>().HasIndex(x => x.InvoiceNumber).IsUnique();
        modelBuilder.Entity<RolePermission>().HasIndex(x => new { x.RoleId, x.ModuleKey }).IsUnique();
        modelBuilder.Entity<Ward>().HasIndex(x => new { x.BranchId, x.Name }).IsUnique();
        modelBuilder.Entity<Bed>().HasIndex(x => new { x.WardId, x.BedNumber }).IsUnique();

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

        modelBuilder.Entity<Patient>().HasIndex(x => x.UserId).IsUnique();

        modelBuilder.Entity<Appointment>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<BillingInvoice>()
            .Property(x => x.Status)
            .HasConversion<string>();
    }
}
