namespace AngularApp4.Dtos.Hms;

public class DoctorWorkspaceAppointmentDetailDto
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
    public long PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string? PatientEmail { get; set; }
    public string? PatientPhone { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? BloodGroup { get; set; }
    public string? EmergencyContact { get; set; }
    public string? Address { get; set; }
    public string? ServiceName { get; set; }
    public string? DepartmentName { get; set; }
    public PatientClinicalProfileDto ClinicalProfile { get; set; } = new();
    public DoctorConsultationDto Consultation { get; set; } = new();
    public List<DoctorPrescriptionDto> PreviousPrescriptions { get; set; } = new();
    public List<DiagnosticRequestDto> DiagnosticRequests { get; set; } = new();
    public List<PatientAppointmentHistoryDto> PastAppointments { get; set; } = new();
    public List<PatientAdmissionHistoryDto> AdmissionHistory { get; set; } = new();
    public List<PatientDocumentDto> UploadedReports { get; set; } = new();
}

public class PatientClinicalProfileDto
{
    public string? MedicalHistory { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicConditions { get; set; }
    public string? CurrentMedications { get; set; }
}

public class DoctorConsultationDto
{
    public long? DoctorConsultationId { get; set; }
    public string? Symptoms { get; set; }
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }
    public string? VitalObservations { get; set; }
    public string? Advice { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime? UpdatedAt { get; set; }
}

public class SaveDoctorConsultationDto
{
    public string? Symptoms { get; set; }
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }
    public string? VitalObservations { get; set; }
    public string? Advice { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public string? Status { get; set; }
}

public class DoctorPrescriptionDto
{
    public long DoctorPrescriptionId { get; set; }
    public long AppointmentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
    public List<DoctorPrescriptionItemDto> Items { get; set; } = new();
}

public class DoctorPrescriptionItemDto
{
    public long? MedicineMasterId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public string? Instructions { get; set; }
}

public class SaveDoctorPrescriptionDto
{
    public string? Notes { get; set; }
    public List<DoctorPrescriptionItemDto> Items { get; set; } = new();
}

public class DiagnosticRequestDto
{
    public long DiagnosticRequestId { get; set; }
    public long AppointmentId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public long? LabTestMasterId { get; set; }
    public string RequestedItemName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string? ResultSummary { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SaveDiagnosticRequestDto
{
    public string RequestType { get; set; } = "Lab";
    public long? LabTestMasterId { get; set; }
    public string RequestedItemName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class PatientAppointmentHistoryDto
{
    public long AppointmentId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string? Diagnosis { get; set; }
    public string? ServiceName { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PatientAdmissionHistoryDto
{
    public long PatientAdmissionId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? WardName { get; set; }
    public string? BedNumber { get; set; }
    public string? Reason { get; set; }
}

public class PatientDocumentDto
{
    public long PatientDocumentId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string UploadedByRole { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class DoctorAvailabilityExceptionDto
{
    public long DoctorAvailabilityExceptionId { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public class SaveDoctorAvailabilityExceptionDto
{
    public string ExceptionType { get; set; } = "Unavailable";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class DoctorAvailabilityWorkspaceDto
{
    public List<DoctorScheduleDto> Schedules { get; set; } = new();
    public List<DoctorAvailabilityExceptionDto> Exceptions { get; set; } = new();
}

public class MedicineSearchResultDto
{
    public long MedicineMasterId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Strength { get; set; }
    public string? DosageForm { get; set; }
}
