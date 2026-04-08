using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AngularApp4.Model.Hms;

public class Role
{
    [Key] public long RoleId { get; set; }
    [Required, MaxLength(50)] public string Name { get; set; } = string.Empty;
    [MaxLength(250)] public string? Description { get; set; }
    public bool IsSystemRole { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class User
{
    [Key] public long UserId { get; set; }
    public long RoleId { get; set; }
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [MaxLength(20)] public string? Phone { get; set; }
    [Required] public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    [Required] public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Role? Role { get; set; }
}

public class Patient
{
    [Key] public long PatientId { get; set; }
    public long UserId { get; set; }
    public long? PatientCategoryId { get; set; }
    [MaxLength(20)] public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    [MaxLength(300)] public string? Address { get; set; }
    [MaxLength(10)] public string? BloodGroup { get; set; }
    [MaxLength(20)] public string? EmergencyContact { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public PatientCategory? PatientCategory { get; set; }
}

public class Doctor
{
    [Key] public long DoctorId { get; set; }
    public long? BranchId { get; set; }
    public long? DepartmentId { get; set; }
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Specialization { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [MaxLength(20)] public string? Phone { get; set; }
    public int ExperienceYears { get; set; }
    [MaxLength(150)] public string? Qualification { get; set; }
    [MaxLength(80)] public string? OpdDays { get; set; }
    public TimeSpan? OpdStartTime { get; set; }
    public TimeSpan? OpdEndTime { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal ConsultationFee { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Branch? Branch { get; set; }
    public Department? Department { get; set; }
}

public class DoctorSchedule
{
    [Key] public long ScheduleId { get; set; }
    public long DoctorId { get; set; }
    [Range(1, 7)] public byte DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int SlotDurationMinutes { get; set; }
    public int MaxPatientsPerSlot { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Doctor? Doctor { get; set; }
}

public class Staff
{
    [Key] public long StaffId { get; set; }
    public long? BranchId { get; set; }
    public long? DepartmentId { get; set; }
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
    [MaxLength(40)] public string? EmployeeCode { get; set; }
    [Required, MaxLength(100)] public string Department { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Designation { get; set; } = string.Empty;
    [MaxLength(50)] public string? Shift { get; set; }
    [Required, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [MaxLength(20)] public string? Phone { get; set; }
    public DateTime JoinDate { get; set; } = DateTime.UtcNow.Date;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Branch? Branch { get; set; }
    public Department? DepartmentMaster { get; set; }
}

public class HospitalService
{
    [Key] public long ServiceId { get; set; }
    [Required, MaxLength(150)] public string ServiceName { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Price { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class RolePermission
{
    [Key] public long RolePermissionId { get; set; }
    public long RoleId { get; set; }
    [Required, MaxLength(80)] public string ModuleKey { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string ModuleName { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Role? Role { get; set; }
}

public class HospitalProfile
{
    [Key] public long HospitalProfileId { get; set; }
    [Required, MaxLength(150)] public string HospitalName { get; set; } = string.Empty;
    [MaxLength(500)] public string? LogoUrl { get; set; }
    [MaxLength(300)] public string AddressLine1 { get; set; } = string.Empty;
    [MaxLength(150)] public string City { get; set; } = string.Empty;
    [MaxLength(150)] public string StateOrProvince { get; set; } = string.Empty;
    [MaxLength(20)] public string PostalCode { get; set; } = string.Empty;
    [MaxLength(100)] public string Country { get; set; } = string.Empty;
    [MaxLength(150)] public string ContactEmail { get; set; } = string.Empty;
    [MaxLength(30)] public string ContactPhone { get; set; } = string.Empty;
    [MaxLength(50)] public string TaxLabel { get; set; } = "VAT";
    [MaxLength(50)] public string TaxRegistrationNumber { get; set; } = string.Empty;
    [Column(TypeName = "decimal(5,2)")] public decimal TaxPercentage { get; set; }
    [MaxLength(10)] public string CurrencyCode { get; set; } = "NPR";
    [MaxLength(20)] public string InvoicePrefix { get; set; } = "INV";
    public int InvoiceStartingNumber { get; set; } = 1001;
    [MaxLength(300)] public string InvoiceFooterNote { get; set; } = string.Empty;
    public bool MultiBranchEnabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Branch
{
    [Key] public long BranchId { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Code { get; set; } = string.Empty;
    [MaxLength(250)] public string Address { get; set; } = string.Empty;
    [MaxLength(30)] public string ContactPhone { get; set; } = string.Empty;
    [MaxLength(150)] public string ContactEmail { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public int TotalBeds { get; set; }
    public int OccupiedBeds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class Department
{
    [Key] public long DepartmentId { get; set; }
    public long BranchId { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Code { get; set; } = string.Empty;
    [MaxLength(250)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Branch? Branch { get; set; }
}

public class PatientCategory
{
    [Key] public long PatientCategoryId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(250)] public string? Description { get; set; }
    public int PriorityOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class Ward
{
    [Key] public long WardId { get; set; }
    public long BranchId { get; set; }
    public long? DepartmentId { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string WardType { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string RoomType { get; set; } = string.Empty;
    [Column(TypeName = "decimal(10,2)")] public decimal ChargePerDay { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Branch? Branch { get; set; }
    public Department? Department { get; set; }
}

public class Bed
{
    [Key] public long BedId { get; set; }
    public long WardId { get; set; }
    public long BranchId { get; set; }
    public long? DepartmentId { get; set; }
    [Required, MaxLength(50)] public string BedNumber { get; set; } = string.Empty;
    [Column(TypeName = "decimal(10,2)")] public decimal ChargePerDay { get; set; }
    public bool IsOccupied { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Ward? Ward { get; set; }
    public Branch? Branch { get; set; }
    public Department? Department { get; set; }
}

public class MedicineInventoryItem
{
    [Key] public long MedicineInventoryItemId { get; set; }
    public long? BranchId { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [MaxLength(100)] public string Category { get; set; } = string.Empty;
    [MaxLength(60)] public string BatchNumber { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }
    public DateTime ExpiryDate { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Branch? Branch { get; set; }
}

public enum InvoiceStatus
{
    Pending,
    Partial,
    Paid,
    Cancelled
}

public class BillingInvoice
{
    [Key] public long BillingInvoiceId { get; set; }
    [Required, MaxLength(30)] public string InvoiceNumber { get; set; } = string.Empty;
    public long? PatientId { get; set; }
    public long? AppointmentId { get; set; }
    public long? BranchId { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal AmountPaid { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? DueDate { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    [MaxLength(250)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Patient? Patient { get; set; }
    public Branch? Branch { get; set; }
    public Appointment? Appointment { get; set; }
}

public enum AppointmentStatus
{
    Pending,
    Approved,
    Rescheduled,
    Cancelled,
    Completed
}

public class Appointment
{
    [Key] public long AppointmentId { get; set; }
    public long PatientId { get; set; }
    public long DoctorId { get; set; }
    public long? ScheduleId { get; set; }
    public long? ServiceId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan SlotStartTime { get; set; }
    public TimeSpan SlotEndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    [MaxLength(500)] public string? Reason { get; set; }
    [MaxLength(500)] public string? AdminRemarks { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
