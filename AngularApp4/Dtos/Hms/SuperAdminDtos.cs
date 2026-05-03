namespace AngularApp4.Dtos.Hms;

public sealed class SuperAdminSummaryDto
{
    public int TotalHospitals { get; set; }
    public int ActiveBranches { get; set; }
    public int TotalAdmins { get; set; }
    public int ActiveAdmins { get; set; }
    public int TotalUsers { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public sealed class SuperAdminHospitalDto
{
    public HospitalProfileDto Profile { get; set; } = new();
    public IEnumerable<BranchDto> Branches { get; set; } = Array.Empty<BranchDto>();
}

public sealed class SuperAdminUserDto
{
    public long UserId { get; set; }
    public long? HospitalProfileId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class CreateAdminUserDto
{
    public long HospitalProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;
}

public sealed class UpdateAdminStatusDto
{
    public bool IsActive { get; set; }
}

public sealed class UpsertBranchDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public int TotalBeds { get; set; }
    public int OccupiedBeds { get; set; }
}
