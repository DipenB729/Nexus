using AngularApp4.Model.Hms;

namespace AngularApp4.Dtos.Hms;

public class AppointmentCreateDto
{
    public long DoctorId { get; set; }
    public long? ScheduleId { get; set; }
    public long? ServiceId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan SlotStartTime { get; set; }
    public TimeSpan SlotEndTime { get; set; }
    public string? Reason { get; set; }
}

public class AppointmentRescheduleDto
{
    public long DoctorId { get; set; }
    public long? ScheduleId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan SlotStartTime { get; set; }
    public TimeSpan SlotEndTime { get; set; }
    public string? Reason { get; set; }
}

public class AppointmentStatusUpdateDto
{
    public AppointmentStatus Status { get; set; }
    public string? AdminRemarks { get; set; }
}

public class PatientAppointmentSummaryDto
{
    public long AppointmentId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan SlotStartTime { get; set; }
    public TimeSpan SlotEndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TokenNumber { get; set; }
    public string? Reason { get; set; }
    public string? AdminRemarks { get; set; }
    public long DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string? DoctorSpecialization { get; set; }
    public long? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public decimal? ServicePrice { get; set; }
}

public class DoctorAppointmentSummaryDto
{
    public long AppointmentId { get; set; }
    public long PatientId { get; set; }
    public long? ScheduleId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string? PatientGender { get; set; }
    public DateTime? PatientDateOfBirth { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan SlotStartTime { get; set; }
    public TimeSpan SlotEndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TokenNumber { get; set; }
    public string? Reason { get; set; }
    public string? AdminRemarks { get; set; }
    public long DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string? DoctorSpecialization { get; set; }
    public long? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public decimal? ServicePrice { get; set; }
}

public class DoctorAppointmentStatusUpdateDto
{
    public AppointmentStatus Status { get; set; }
}

public class DoctorManageAppointmentDto
{
    public AppointmentStatus Status { get; set; }
    public long? ScheduleId { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public TimeSpan? SlotStartTime { get; set; }
    public TimeSpan? SlotEndTime { get; set; }
    public string? Reason { get; set; }
    public string? AdminRemarks { get; set; }
}
