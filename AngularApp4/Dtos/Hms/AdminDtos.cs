namespace AngularApp4.Dtos.Hms;

public class DashboardSummaryDto
{
    public int TotalPatients { get; set; }
    public int TodayAppointments { get; set; }
    public decimal TodaySales { get; set; }
    public int LowStockItems { get; set; }
    public int ExpiredMedicines { get; set; }
    public int PendingPayments { get; set; }
    public decimal PendingPaymentAmount { get; set; }
    public int TotalBeds { get; set; }
    public int OccupiedBeds { get; set; }
    public decimal BedOccupancyRate { get; set; }
    public IEnumerable<StockAlertDto> LowStockAlerts { get; set; } = Array.Empty<StockAlertDto>();
    public IEnumerable<StockAlertDto> ExpiredMedicineAlerts { get; set; } = Array.Empty<StockAlertDto>();
    public IEnumerable<PendingPaymentDto> PendingPaymentDetails { get; set; } = Array.Empty<PendingPaymentDto>();
    public IEnumerable<BranchOccupancyDto> BedOccupancyByBranch { get; set; } = Array.Empty<BranchOccupancyDto>();
}

public class StockAlertDto
{
    public long ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }
    public DateTime ExpiryDate { get; set; }
}

public class PendingPaymentDto
{
    public long InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public decimal DueAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class BranchOccupancyDto
{
    public long BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int TotalBeds { get; set; }
    public int OccupiedBeds { get; set; }
    public decimal OccupancyRate { get; set; }
}

public class RolePermissionDto
{
    public string ModuleKey { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class RoleDetailsDto
{
    public long RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemRole { get; set; }
    public IEnumerable<RolePermissionDto> Permissions { get; set; } = Array.Empty<RolePermissionDto>();
}

public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IEnumerable<RolePermissionDto>? Permissions { get; set; }
}

public class UpdateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public IEnumerable<RolePermissionDto> Permissions { get; set; } = Array.Empty<RolePermissionDto>();
}

public class BranchDto
{
    public long BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public int TotalBeds { get; set; }
    public int OccupiedBeds { get; set; }
}

public class HospitalProfileDto
{
    public long HospitalProfileId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StateOrProvince { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string TaxLabel { get; set; } = string.Empty;
    public string TaxRegistrationNumber { get; set; } = string.Empty;
    public decimal TaxPercentage { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string InvoicePrefix { get; set; } = string.Empty;
    public int InvoiceStartingNumber { get; set; }
    public string InvoiceFooterNote { get; set; } = string.Empty;
    public bool MultiBranchEnabled { get; set; }
}

public class OrganizationSettingsDto
{
    public HospitalProfileDto Profile { get; set; } = new();
    public IEnumerable<BranchDto> Branches { get; set; } = Array.Empty<BranchDto>();
}
