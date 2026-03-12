using AngularApp4.Model;
using AngularApp4.Model.Hms;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<HospitalService> HospitalServices => Set<HospitalService>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Doctor>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Staff>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<HospitalService>().HasIndex(x => x.ServiceName).IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoctorSchedule>()
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Patient>().HasIndex(x => x.UserId).IsUnique();

        modelBuilder.Entity<Appointment>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, Name = "Admin", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Role { RoleId = 2, Name = "User", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
    }
}
