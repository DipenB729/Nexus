namespace AngularApp4.Dtos.Hms;

public class MonitoringReportsDto
{
    public PatientReportDto PatientReport { get; set; } = new();
    public BillingReportDto BillingReport { get; set; } = new();
    public DoctorRevenueReportDto DoctorRevenueReport { get; set; } = new();
    public PharmacySalesReportDto PharmacySalesReport { get; set; } = new();
    public PurchaseReportDto PurchaseReport { get; set; } = new();
    public StockBalanceReportDto StockBalanceReport { get; set; } = new();
    public ExpiryReportDto ExpiryReport { get; set; } = new();
    public BedOccupancyReportDto BedOccupancyReport { get; set; } = new();
}

public class ReportCountRowDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PatientBranchActivityDto
{
    public long BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int DistinctPatients { get; set; }
    public int ActiveAdmissions { get; set; }
}

public class PatientReportDto
{
    public int TotalPatients { get; set; }
    public int ActivePatients { get; set; }
    public int NewPatientsLast30Days { get; set; }
    public int MergedPatients { get; set; }
    public IEnumerable<ReportCountRowDto> CategoryRows { get; set; } = Array.Empty<ReportCountRowDto>();
    public IEnumerable<PatientBranchActivityDto> BranchRows { get; set; } = Array.Empty<PatientBranchActivityDto>();
}

public class BillingStatusRowDto
{
    public string Status { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal Amount { get; set; }
}

public class BillingInvoiceReportRowDto
{
    public long BillingInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DueAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
}

public class BillingReportDto
{
    public int TotalInvoices { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalDue { get; set; }
    public decimal TotalRefunded { get; set; }
    public IEnumerable<BillingStatusRowDto> StatusRows { get; set; } = Array.Empty<BillingStatusRowDto>();
    public IEnumerable<BillingInvoiceReportRowDto> RecentInvoices { get; set; } = Array.Empty<BillingInvoiceReportRowDto>();
}

public class DoctorRevenueRowDto
{
    public long DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int AppointmentCount { get; set; }
    public int LinkedInvoiceCount { get; set; }
    public decimal RecognizedRevenue { get; set; }
    public decimal ConsultationEstimate { get; set; }
    public decimal ReportedRevenue { get; set; }
}

public class DoctorRevenueReportDto
{
    public int ActiveDoctors { get; set; }
    public int DoctorsWithRevenue { get; set; }
    public decimal TotalRecognizedRevenue { get; set; }
    public decimal TotalConsultationEstimate { get; set; }
    public IEnumerable<DoctorRevenueRowDto> Rows { get; set; } = Array.Empty<DoctorRevenueRowDto>();
}

public class PharmacyMovementRowDto
{
    public long StockLocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public decimal ReceivedValue { get; set; }
    public decimal ReturnedValue { get; set; }
    public decimal TransferOutValue { get; set; }
    public decimal AdjustmentOutValue { get; set; }
    public decimal CurrentStockValue { get; set; }
}

public class PharmacySalesReportDto
{
    public string BasisNote { get; set; } = string.Empty;
    public int PharmacyLocations { get; set; }
    public decimal ReceivedValue { get; set; }
    public decimal ReturnedValue { get; set; }
    public decimal TransferOutValue { get; set; }
    public decimal AdjustmentOutValue { get; set; }
    public decimal CurrentStockValue { get; set; }
    public IEnumerable<PharmacyMovementRowDto> Rows { get; set; } = Array.Empty<PharmacyMovementRowDto>();
}

public class PurchaseSupplierReportRowDto
{
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalPurchased { get; set; }
    public decimal DueAmount { get; set; }
    public decimal ReturnAmount { get; set; }
}

public class PurchaseInvoiceReportRowDto
{
    public long PurchaseInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DueAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
}

public class PurchaseReportDto
{
    public int PurchaseOrders { get; set; }
    public int PurchaseInvoices { get; set; }
    public decimal TotalPurchased { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalDue { get; set; }
    public decimal TotalReturned { get; set; }
    public IEnumerable<PurchaseSupplierReportRowDto> SupplierRows { get; set; } = Array.Empty<PurchaseSupplierReportRowDto>();
    public IEnumerable<PurchaseInvoiceReportRowDto> RecentInvoices { get; set; } = Array.Empty<PurchaseInvoiceReportRowDto>();
}

public class StockBalanceRowDto
{
    public long StockLocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int BatchCount { get; set; }
    public int ItemCount { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal StockValue { get; set; }
    public int LowStockBatches { get; set; }
}

public class StockBalanceReportDto
{
    public int ActiveLocations { get; set; }
    public int ActiveBatches { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal StockValue { get; set; }
    public int LowStockBatches { get; set; }
    public IEnumerable<StockBalanceRowDto> LocationRows { get; set; } = Array.Empty<StockBalanceRowDto>();
}

public class ExpiryBatchRowDto
{
    public long StockBatchId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal StockValue { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ExpiryReportDto
{
    public int NearExpiryCount { get; set; }
    public int ExpiredCount { get; set; }
    public decimal NearExpiryValue { get; set; }
    public decimal ExpiredValue { get; set; }
    public IEnumerable<ExpiryBatchRowDto> Rows { get; set; } = Array.Empty<ExpiryBatchRowDto>();
}

public class WardOccupancyRowDto
{
    public long WardId { get; set; }
    public string WardName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int TotalBeds { get; set; }
    public int OccupiedBeds { get; set; }
    public decimal OccupancyRate { get; set; }
}

public class BedOccupancyReportDto
{
    public int TotalBeds { get; set; }
    public int OccupiedBeds { get; set; }
    public int AvailableBeds { get; set; }
    public decimal OccupancyRate { get; set; }
    public IEnumerable<BranchOccupancyDto> BranchRows { get; set; } = Array.Empty<BranchOccupancyDto>();
    public IEnumerable<WardOccupancyRowDto> WardRows { get; set; } = Array.Empty<WardOccupancyRowDto>();
}

public class AuditLogEntryDto
{
    public long AuditLogEntryId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public long? EntityId { get; set; }
    public string? TargetDisplayName { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public long? PerformedByUserId { get; set; }
    public string? PerformedByName { get; set; }
    public string? PerformedByRole { get; set; }
    public string? ActorEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditOverviewDto
{
    public int TotalEntries { get; set; }
    public int Last24Hours { get; set; }
    public int UniqueActors { get; set; }
    public int LoginEvents { get; set; }
    public int BillingEvents { get; set; }
    public int StockAdjustmentEvents { get; set; }
    public int RefundEvents { get; set; }
}

public class AuditMonitoringDto
{
    public AuditOverviewDto Overview { get; set; } = new();
    public IEnumerable<AuditLogEntryDto> ChangeTrail { get; set; } = Array.Empty<AuditLogEntryDto>();
    public IEnumerable<AuditLogEntryDto> LoginHistory { get; set; } = Array.Empty<AuditLogEntryDto>();
    public IEnumerable<AuditLogEntryDto> StockAdjustmentHistory { get; set; } = Array.Empty<AuditLogEntryDto>();
    public IEnumerable<AuditLogEntryDto> BillingChanges { get; set; } = Array.Empty<AuditLogEntryDto>();
    public IEnumerable<AuditLogEntryDto> RefundApprovals { get; set; } = Array.Empty<AuditLogEntryDto>();
}
