using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AngularApp4.Model.Hms;

public enum DoctorAvailabilityExceptionType
{
    Leave,
    Unavailable,
    Holiday
}

public class DoctorAvailabilityException
{
    [Key] public long DoctorAvailabilityExceptionId { get; set; }
    public long DoctorId { get; set; }
    public DoctorAvailabilityExceptionType ExceptionType { get; set; } = DoctorAvailabilityExceptionType.Unavailable;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    [MaxLength(300)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Doctor? Doctor { get; set; }
}

public class DoctorBlockedSlot
{
    [Key] public long DoctorBlockedSlotId { get; set; }
    public long DoctorId { get; set; }
    public DateTime BlockDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    [MaxLength(300)] public string? Reason { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Doctor? Doctor { get; set; }
}

public class PatientClinicalProfile
{
    [Key] public long PatientClinicalProfileId { get; set; }
    public long PatientId { get; set; }
    [MaxLength(4000)] public string? MedicalHistory { get; set; }
    [MaxLength(2000)] public string? Allergies { get; set; }
    [MaxLength(2000)] public string? ChronicConditions { get; set; }
    [MaxLength(2000)] public string? CurrentMedications { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Patient? Patient { get; set; }
}

public enum DoctorConsultationStatus
{
    Draft,
    Completed,
    FollowUpPlanned
}

public class DoctorConsultation
{
    [Key] public long DoctorConsultationId { get; set; }
    public long AppointmentId { get; set; }
    public long DoctorId { get; set; }
    public long PatientId { get; set; }
    [MaxLength(4000)] public string? Symptoms { get; set; }
    [MaxLength(4000)] public string? Diagnosis { get; set; }
    [MaxLength(4000)] public string? Notes { get; set; }
    [MaxLength(2000)] public string? VitalObservations { get; set; }
    [MaxLength(2000)] public string? Advice { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public DoctorConsultationStatus Status { get; set; } = DoctorConsultationStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Appointment? Appointment { get; set; }
    public Doctor? Doctor { get; set; }
    public Patient? Patient { get; set; }
}

public class DoctorPrescription
{
    [Key] public long DoctorPrescriptionId { get; set; }
    public long AppointmentId { get; set; }
    public long DoctorId { get; set; }
    public long PatientId { get; set; }
    [MaxLength(2000)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Appointment? Appointment { get; set; }
    public Doctor? Doctor { get; set; }
    public Patient? Patient { get; set; }
}

public class DoctorPrescriptionItem
{
    [Key] public long DoctorPrescriptionItemId { get; set; }
    public long DoctorPrescriptionId { get; set; }
    public long? MedicineMasterId { get; set; }
    [Required, MaxLength(200)] public string MedicineName { get; set; } = string.Empty;
    [MaxLength(100)] public string? Dosage { get; set; }
    [MaxLength(100)] public string? Frequency { get; set; }
    [MaxLength(100)] public string? Duration { get; set; }
    [MaxLength(500)] public string? Instructions { get; set; }
    public int SortOrder { get; set; }

    public DoctorPrescription? DoctorPrescription { get; set; }
    public MedicineMaster? MedicineMaster { get; set; }
}

public enum DiagnosticRequestType
{
    Lab,
    Radiology
}

public enum DiagnosticRequestStatus
{
    Requested,
    InProgress,
    Completed,
    Cancelled
}

public class DiagnosticRequest
{
    [Key] public long DiagnosticRequestId { get; set; }
    public long AppointmentId { get; set; }
    public long DoctorId { get; set; }
    public long PatientId { get; set; }
    public DiagnosticRequestType RequestType { get; set; } = DiagnosticRequestType.Lab;
    public long? LabTestMasterId { get; set; }
    [Required, MaxLength(200)] public string RequestedItemName { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Remarks { get; set; }
    [MaxLength(1000)] public string? ResultSummary { get; set; }
    public DiagnosticRequestStatus Status { get; set; } = DiagnosticRequestStatus.Requested;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Appointment? Appointment { get; set; }
    public Doctor? Doctor { get; set; }
    public Patient? Patient { get; set; }
    public LabTestMaster? LabTestMaster { get; set; }
}

public class PatientDocument
{
    [Key] public long PatientDocumentId { get; set; }
    public long PatientId { get; set; }
    public long? AppointmentId { get; set; }
    [Required, MaxLength(120)] public string Category { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string FileUrl { get; set; } = string.Empty;
    [MaxLength(500)] public string? Notes { get; set; }
    [MaxLength(50)] public string UploadedByRole { get; set; } = "Patient";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Patient? Patient { get; set; }
    public Appointment? Appointment { get; set; }
}

public class PatientCaseReport
{
    [Key] public long PatientCaseReportId { get; set; }
    public long AppointmentId { get; set; }
    public long PatientId { get; set; }
    public long DoctorId { get; set; }
    [Required, MaxLength(4000)] public string Symptoms { get; set; } = string.Empty;
    [MaxLength(2000)] public string? PreviousReportSummary { get; set; }
    public long? PatientDocumentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Appointment? Appointment { get; set; }
    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
    public PatientDocument? PatientDocument { get; set; }
}

public class CareConversationMessage
{
    [Key] public long CareConversationMessageId { get; set; }
    public long AppointmentId { get; set; }
    public long PatientId { get; set; }
    public long DoctorId { get; set; }
    public long SenderUserId { get; set; }
    [Required, MaxLength(30)] public string SenderRole { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string SenderName { get; set; } = string.Empty;
    [Required, MaxLength(2000)] public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Appointment? Appointment { get; set; }
    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
    public User? SenderUser { get; set; }
}
