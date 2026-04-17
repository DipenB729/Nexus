namespace AngularApp4.Dtos.Hms;

public class RegisterRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class ForgotPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordResponseDto
{
    public string? ResetCodePreview { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ResetPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string ResetCode { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class PatientProfileDto
{
    public long PatientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? MedicalRecordNumber { get; set; }
    public string? PatientCategoryName { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? BloodGroup { get; set; }
    public string? EmergencyContact { get; set; }
}

public class DoctorProfileDto
{
    public long DoctorId { get; set; }
    public long? DepartmentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Specialization { get; set; }
    public string? LicenseNumber { get; set; }
    public int ExperienceYears { get; set; }
    public string? Qualification { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? BranchName { get; set; }
    public string? DepartmentName { get; set; }
    public string? Bio { get; set; }
    public string? Address { get; set; }
    public string? OpdDays { get; set; }
    public TimeSpan? OpdStartTime { get; set; }
    public TimeSpan? OpdEndTime { get; set; }
}

public class UpdatePatientProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? BloodGroup { get; set; }
    public string? EmergencyContact { get; set; }
}

public class UpdateDoctorProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public long? DepartmentId { get; set; }
    public string? Specialization { get; set; }
    public int ExperienceYears { get; set; }
    public string? Qualification { get; set; }
    public string? LicenseNumber { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? Bio { get; set; }
    public string? Address { get; set; }
}

public class DoctorProfilePhotoDto
{
    public string PhotoUrl { get; set; } = string.Empty;
}

public class DoctorProfileDepartmentOptionDto
{
    public long DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
}

public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
