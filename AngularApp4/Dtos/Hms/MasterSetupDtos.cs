namespace AngularApp4.Dtos.Hms;

public class LookupDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class StatusUpdateDto
{
    public bool IsActive { get; set; }
}

public class DepartmentDto
{
    public long DepartmentId { get; set; }
    public long BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class SaveDepartmentDto
{
    public long BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class DoctorMasterDto
{
    public long DoctorId { get; set; }
    public long? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int ExperienceYears { get; set; }
    public string? Qualification { get; set; }
    public string? OpdDays { get; set; }
    public TimeSpan? OpdStartTime { get; set; }
    public TimeSpan? OpdEndTime { get; set; }
    public decimal ConsultationFee { get; set; }
    public bool IsActive { get; set; }
}

public class SaveDoctorDto
{
    public long? BranchId { get; set; }
    public long? DepartmentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int ExperienceYears { get; set; }
    public string? Qualification { get; set; }
    public string? OpdDays { get; set; }
    public TimeSpan? OpdStartTime { get; set; }
    public TimeSpan? OpdEndTime { get; set; }
    public decimal ConsultationFee { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StaffMasterDto
{
    public long StaffId { get; set; }
    public long? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string Designation { get; set; } = string.Empty;
    public string? Shift { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime JoinDate { get; set; }
    public bool IsActive { get; set; }
}

public class SaveStaffDto
{
    public long? BranchId { get; set; }
    public long? DepartmentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string Designation { get; set; } = string.Empty;
    public string? Shift { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime JoinDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PatientCategoryDto
{
    public long PatientCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PriorityOrder { get; set; }
    public bool IsActive { get; set; }
}

public class SavePatientCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PriorityOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class WardDto
{
    public long WardId { get; set; }
    public long BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WardType { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public decimal ChargePerDay { get; set; }
    public bool IsActive { get; set; }
}

public class SaveWardDto
{
    public long BranchId { get; set; }
    public long? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WardType { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public decimal ChargePerDay { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BedDto
{
    public long BedId { get; set; }
    public long WardId { get; set; }
    public string WardName { get; set; } = string.Empty;
    public long BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string BedNumber { get; set; } = string.Empty;
    public decimal ChargePerDay { get; set; }
    public bool IsOccupied { get; set; }
    public bool IsActive { get; set; }
}

public class SaveBedDto
{
    public long WardId { get; set; }
    public long BranchId { get; set; }
    public long? DepartmentId { get; set; }
    public string BedNumber { get; set; } = string.Empty;
    public decimal ChargePerDay { get; set; }
    public bool IsOccupied { get; set; }
    public bool IsActive { get; set; } = true;
}
