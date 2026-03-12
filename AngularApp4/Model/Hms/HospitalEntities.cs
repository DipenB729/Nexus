using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AngularApp4.Model.Hms;

public class Role
{
    [Key] public long RoleId { get; set; }
    [Required, MaxLength(50)] public string Name { get; set; } = string.Empty;
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
    [MaxLength(20)] public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    [MaxLength(300)] public string? Address { get; set; }
    [MaxLength(10)] public string? BloodGroup { get; set; }
    [MaxLength(20)] public string? EmergencyContact { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class Doctor
{
    [Key] public long DoctorId { get; set; }
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Specialization { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [MaxLength(20)] public string? Phone { get; set; }
    public int ExperienceYears { get; set; }
    [MaxLength(150)] public string? Qualification { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal ConsultationFee { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
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
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Department { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Designation { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [MaxLength(20)] public string? Phone { get; set; }
    public DateTime JoinDate { get; set; } = DateTime.UtcNow.Date;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
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
