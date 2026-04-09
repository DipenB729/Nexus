using AngularApp4.Model.Hms;

namespace AngularApp4.Dtos.Hms;

public class PatientOverviewDto
{
    public long PatientId { get; set; }
    public long UserId { get; set; }
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public long? PatientCategoryId { get; set; }
    public string PatientCategoryName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? BloodGroup { get; set; }
    public string? EmergencyContact { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public long? MergedIntoPatientId { get; set; }
    public bool IsMerged { get; set; }
    public int AppointmentCount { get; set; }
    public int ActiveAdmissions { get; set; }
    public int DuplicateGroupSize { get; set; }
}

public class SavePatientDetailsDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public long? PatientCategoryId { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? BloodGroup { get; set; }
    public string? EmergencyContact { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreatePatientDto : SavePatientDetailsDto
{
    public string Password { get; set; } = string.Empty;
}

public class MergePatientRecordsDto
{
    public long SourcePatientId { get; set; }
    public long TargetPatientId { get; set; }
    public string? Notes { get; set; }
}

public class AppointmentAdminDto
{
    public long AppointmentId { get; set; }
    public long PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public long DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public long? ScheduleId { get; set; }
    public long? ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public TimeSpan SlotStartTime { get; set; }
    public TimeSpan SlotEndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TokenNumber { get; set; }
    public string? Reason { get; set; }
    public string? AdminRemarks { get; set; }
}

public class ManageAppointmentDto
{
    public AppointmentStatus Status { get; set; }
    public long? DoctorId { get; set; }
    public long? ScheduleId { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public TimeSpan? SlotStartTime { get; set; }
    public TimeSpan? SlotEndTime { get; set; }
    public string? AdminRemarks { get; set; }
}

public class AppointmentTokenSettingsDto
{
    public long AppointmentTokenSettingId { get; set; }
    public string Prefix { get; set; } = "OPD";
    public int StartingNumber { get; set; } = 1;
    public int NumberPadding { get; set; } = 3;
    public bool ResetDaily { get; set; } = true;
}

public class AdmissionTransferDto
{
    public long AdmissionTransferId { get; set; }
    public string? FromWardName { get; set; }
    public string? FromBedNumber { get; set; }
    public string ToWardName { get; set; } = string.Empty;
    public string ToBedNumber { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public string? Notes { get; set; }
}

public class AdmissionRecordDto
{
    public long PatientAdmissionId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public long PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public long? AppointmentId { get; set; }
    public long? DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public long BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public long WardId { get; set; }
    public string WardName { get; set; } = string.Empty;
    public long BedId { get; set; }
    public string BedNumber { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime? ExpectedDischargeDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public string? DischargeSummary { get; set; }
    public DateTime? DischargeApprovedAt { get; set; }
    public IEnumerable<AdmissionTransferDto> Transfers { get; set; } = Array.Empty<AdmissionTransferDto>();
}

public class CreateAdmissionDto
{
    public long PatientId { get; set; }
    public long? AppointmentId { get; set; }
    public long? DoctorId { get; set; }
    public long WardId { get; set; }
    public long BedId { get; set; }
    public DateTime AdmissionDate { get; set; }
    public DateTime? ExpectedDischargeDate { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

public class TransferAdmissionDto
{
    public long WardId { get; set; }
    public long BedId { get; set; }
    public DateTime TransferDate { get; set; }
    public string? Notes { get; set; }
}

public class ApproveDischargeDto
{
    public DateTime DischargeDate { get; set; }
    public string DischargeSummary { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
