namespace AngularApp4.Dtos.Hms;

public class CareThreadSummaryDto
{
    public long AppointmentId { get; set; }
    public long PatientId { get; set; }
    public long DoctorId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? DoctorSpecialization { get; set; }
    public string? MedicalRecordNumber { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan SlotStartTime { get; set; }
    public TimeSpan SlotEndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public string? LastMessage { get; set; }
    public int ReportCount { get; set; }
}

public class CareThreadDetailDto
{
    public CareThreadSummaryDto Thread { get; set; } = new();
    public List<CareConversationMessageDto> Messages { get; set; } = new();
    public List<PatientCaseReportDto> Reports { get; set; } = new();
    public DoctorConsultationDto Consultation { get; set; } = new();
    public List<DoctorPrescriptionDto> Prescriptions { get; set; } = new();
    public List<DiagnosticRequestDto> DiagnosticRequests { get; set; } = new();
}

public class CareConversationMessageDto
{
    public long CareConversationMessageId { get; set; }
    public long AppointmentId { get; set; }
    public string SenderRole { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SaveCareConversationMessageDto
{
    public string Message { get; set; } = string.Empty;
}

public class PatientCaseReportDto
{
    public long PatientCaseReportId { get; set; }
    public long AppointmentId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string? PreviousReportSummary { get; set; }
    public PatientDocumentDto? Document { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SavePatientCaseReportDto
{
    public string Symptoms { get; set; } = string.Empty;
    public string? PreviousReportSummary { get; set; }
    public string? ReportTitle { get; set; }
    public string? ReportUrl { get; set; }
    public string? ReportCategory { get; set; }
    public string? ReportNotes { get; set; }
}

public class SavePatientCaseReportFormDto
{
    public string Symptoms { get; set; } = string.Empty;
    public string? PreviousReportSummary { get; set; }
    public string? ReportTitle { get; set; }
    public string? ReportUrl { get; set; }
    public string? ReportCategory { get; set; }
    public string? ReportNotes { get; set; }
    public IFormFile? Attachment { get; set; }
}
