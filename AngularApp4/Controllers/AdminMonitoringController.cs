using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using AngularApp4.Services.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/admin/monitoring")]
[Authorize(Policy = "AdminOnly")]
public class AdminMonitoringController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminMonitoringController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("reports")]
    public async Task<ActionResult<ApiResponse<MonitoringReportsDto>>> GetReports(CancellationToken cancellationToken)
    {
        var payload = await BuildReportsAsync(cancellationToken);
        return Ok(ApiResponse<MonitoringReportsDto>.Ok(payload));
    }

    [HttpGet("audit")]
    public async Task<ActionResult<ApiResponse<AuditMonitoringDto>>> GetAudit(CancellationToken cancellationToken)
    {
        var entries = await _db.AuditLogEntries
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.AuditLogEntryId)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var payload = new AuditMonitoringDto
        {
            Overview = new AuditOverviewDto
            {
                TotalEntries = entries.Count,
                Last24Hours = entries.Count(x => x.CreatedAt >= now.AddHours(-24)),
                UniqueActors = entries
                    .Select(GetActorKey)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                LoginEvents = entries.Count(x => x.Category == AuditLogCategories.Authentication),
                BillingEvents = entries.Count(x => x.Category == AuditLogCategories.Billing),
                StockAdjustmentEvents = entries.Count(x => x.Category == AuditLogCategories.StockAdjustment),
                RefundEvents = entries.Count(x => x.Category == AuditLogCategories.RefundApproval)
            },
            ChangeTrail = entries.Take(30).Select(MapAuditEntry).ToList(),
            LoginHistory = entries
                .Where(x => x.Category == AuditLogCategories.Authentication)
                .Take(20)
                .Select(MapAuditEntry)
                .ToList(),
            StockAdjustmentHistory = entries
                .Where(x =>
                    x.Category == AuditLogCategories.StockAdjustment ||
                    (x.Category == AuditLogCategories.Inventory &&
                     string.Equals(x.EntityName, "Stock Adjustment", StringComparison.OrdinalIgnoreCase)))
                .Take(20)
                .Select(MapAuditEntry)
                .ToList(),
            BillingChanges = entries
                .Where(x => x.Category == AuditLogCategories.Billing)
                .Take(20)
                .Select(MapAuditEntry)
                .ToList(),
            RefundApprovals = entries
                .Where(x => x.Category == AuditLogCategories.RefundApproval)
                .Take(20)
                .Select(MapAuditEntry)
                .ToList()
        };

        return Ok(ApiResponse<AuditMonitoringDto>.Ok(payload));
    }

    private async Task<MonitoringReportsDto> BuildReportsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var last30Days = now.AddDays(-30);
        var expiryThreshold = today.AddDays(30);

        var branches = await _db.Branches.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var users = await _db.Users.AsNoTracking().ToListAsync(cancellationToken);
        var patients = await _db.Patients.AsNoTracking().ToListAsync(cancellationToken);
        var patientCategories = await _db.PatientCategories.AsNoTracking().OrderBy(x => x.PriorityOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        var admissions = await _db.PatientAdmissions.AsNoTracking().ToListAsync(cancellationToken);
        var doctors = await _db.Doctors.AsNoTracking().ToListAsync(cancellationToken);
        var appointments = await _db.Appointments.AsNoTracking().ToListAsync(cancellationToken);
        var wards = await _db.Wards.AsNoTracking().ToListAsync(cancellationToken);
        var beds = await _db.Beds.AsNoTracking().ToListAsync(cancellationToken);
        var invoices = await _db.BillingInvoices.AsNoTracking().ToListAsync(cancellationToken);
        var purchaseOrdersCount = await _db.PurchaseOrders.AsNoTracking().CountAsync(cancellationToken);
        var purchaseInvoices = await _db.PurchaseInvoices.AsNoTracking().ToListAsync(cancellationToken);
        var purchaseReturns = await _db.PurchaseReturns.AsNoTracking().ToListAsync(cancellationToken);
        var suppliers = await _db.Suppliers.AsNoTracking().ToListAsync(cancellationToken);
        var stockLocations = await _db.StockLocations.AsNoTracking().ToListAsync(cancellationToken);
        var stockBatches = await _db.StockBatches.AsNoTracking().ToListAsync(cancellationToken);
        var transferHeaders = await _db.StockTransfers.AsNoTracking().ToListAsync(cancellationToken);
        var transferLines = await _db.StockTransferLines.AsNoTracking().ToListAsync(cancellationToken);
        var adjustmentHeaders = await _db.StockAdjustments.AsNoTracking().ToListAsync(cancellationToken);
        var adjustmentLines = await _db.StockAdjustmentLines.AsNoTracking().ToListAsync(cancellationToken);
        var medicineMasters = await _db.MedicineMasters.AsNoTracking().ToListAsync(cancellationToken);
        var stockItems = await _db.StockItemMasters.AsNoTracking().ToListAsync(cancellationToken);

        var userNamesByUserId = users.ToDictionary(x => x.UserId, x => x.FullName);
        var branchById = branches.ToDictionary(x => x.BranchId);
        var patientNamesByPatientId = patients.ToDictionary(
            x => x.PatientId,
            x => userNamesByUserId.TryGetValue(x.UserId, out var fullName) ? fullName : $"Patient #{x.PatientId}");
        var supplierById = suppliers.ToDictionary(x => x.SupplierId);
        var locationById = stockLocations.ToDictionary(x => x.StockLocationId);
        var medicineById = medicineMasters.ToDictionary(x => x.MedicineMasterId);
        var stockItemById = stockItems.ToDictionary(x => x.StockItemMasterId);
        var batchById = stockBatches.ToDictionary(x => x.StockBatchId);
        var purchaseInvoiceById = purchaseInvoices.ToDictionary(x => x.PurchaseInvoiceId);

        var patientReport = BuildPatientReport(branches, patients, patientCategories, admissions, last30Days);
        var billingReport = BuildBillingReport(invoices, patientNamesByPatientId, branchById);
        var doctorRevenueReport = BuildDoctorRevenueReport(doctors, appointments, invoices);
        var pharmacySalesReport = BuildPharmacySalesReport(stockLocations, purchaseInvoices, purchaseReturns, purchaseInvoiceById, transferHeaders, transferLines, adjustmentHeaders, adjustmentLines, stockBatches, batchById, branchById);
        var purchaseReport = BuildPurchaseReport(purchaseOrdersCount, purchaseInvoices, purchaseReturns, suppliers, supplierById, locationById);
        var stockBalanceReport = BuildStockBalanceReport(stockLocations, stockBatches, medicineById, stockItemById, branchById);
        var expiryReport = BuildExpiryReport(today, expiryThreshold, stockBatches, medicineById, stockItemById, locationById);
        var bedOccupancyReport = BuildBedOccupancyReport(wards, beds, branchById);

        return new MonitoringReportsDto
        {
            PatientReport = patientReport,
            BillingReport = billingReport,
            DoctorRevenueReport = doctorRevenueReport,
            PharmacySalesReport = pharmacySalesReport,
            PurchaseReport = purchaseReport,
            StockBalanceReport = stockBalanceReport,
            ExpiryReport = expiryReport,
            BedOccupancyReport = bedOccupancyReport
        };
    }

    private static PatientReportDto BuildPatientReport(
        IReadOnlyList<Branch> branches,
        IReadOnlyList<Patient> patients,
        IReadOnlyList<PatientCategory> patientCategories,
        IReadOnlyList<PatientAdmission> admissions,
        DateTime last30Days)
    {
        return new PatientReportDto
        {
            TotalPatients = patients.Count,
            ActivePatients = patients.Count(x => x.IsActive),
            NewPatientsLast30Days = patients.Count(x => x.CreatedAt >= last30Days),
            MergedPatients = patients.Count(x => x.MergedIntoPatientId.HasValue),
            CategoryRows = patientCategories
                .Select(category => new ReportCountRowDto
                {
                    Label = category.Name,
                    Count = patients.Count(patient => patient.PatientCategoryId == category.PatientCategoryId && !patient.MergedIntoPatientId.HasValue)
                })
                .Where(x => x.Count > 0)
                .ToList(),
            BranchRows = branches
                .Select(branch =>
                {
                    var branchAdmissions = admissions.Where(x => x.BranchId == branch.BranchId).ToList();
                    return new PatientBranchActivityDto
                    {
                        BranchId = branch.BranchId,
                        BranchName = branch.Name,
                        DistinctPatients = branchAdmissions.Select(x => x.PatientId).Distinct().Count(),
                        ActiveAdmissions = branchAdmissions.Count(x => x.Status is AdmissionStatus.Active or AdmissionStatus.DischargePending)
                    };
                })
                .Where(x => x.DistinctPatients > 0 || x.ActiveAdmissions > 0)
                .ToList()
        };
    }

    private static BillingReportDto BuildBillingReport(
        IReadOnlyList<BillingInvoice> invoices,
        IReadOnlyDictionary<long, string> patientNamesByPatientId,
        IReadOnlyDictionary<long, Branch> branchById)
    {
        return new BillingReportDto
        {
            TotalInvoices = invoices.Count,
            TotalBilled = invoices.Sum(x => x.TotalAmount),
            TotalCollected = invoices.Sum(x => x.AmountPaid),
            TotalDue = invoices.Sum(GetInvoiceDueAmount),
            TotalRefunded = invoices.Sum(x => x.RefundedAmount),
            StatusRows = invoices
                .GroupBy(x => x.Status)
                .Select(group => new BillingStatusRowDto
                {
                    Status = group.Key.ToString(),
                    InvoiceCount = group.Count(),
                    Amount = group.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.Amount)
                .ToList(),
            RecentInvoices = invoices
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.BillingInvoiceId)
                .Take(8)
                .Select(invoice => new BillingInvoiceReportRowDto
                {
                    BillingInvoiceId = invoice.BillingInvoiceId,
                    InvoiceNumber = invoice.InvoiceNumber,
                    PatientName = invoice.PatientId.HasValue && patientNamesByPatientId.TryGetValue(invoice.PatientId.Value, out var patientName)
                        ? patientName
                        : "Unknown patient",
                    BranchName = invoice.BranchId.HasValue && branchById.TryGetValue(invoice.BranchId.Value, out var branch)
                        ? branch.Name
                        : "Unassigned",
                    TotalAmount = invoice.TotalAmount,
                    DueAmount = GetInvoiceDueAmount(invoice),
                    Status = invoice.Status.ToString(),
                    InvoiceDate = invoice.InvoiceDate
                })
                .ToList()
        };
    }

    private static DoctorRevenueReportDto BuildDoctorRevenueReport(
        IReadOnlyList<Doctor> doctors,
        IReadOnlyList<Appointment> appointments,
        IReadOnlyList<BillingInvoice> invoices)
    {
        var activeDoctors = doctors.Where(x => x.IsActive).OrderBy(x => x.FullName).ToList();
        var rows = activeDoctors
            .Select(doctor =>
            {
                var doctorAppointments = appointments.Where(x => x.DoctorId == doctor.DoctorId && x.Status != AppointmentStatus.Cancelled).ToList();
                var appointmentIds = doctorAppointments.Select(x => x.AppointmentId).ToHashSet();
                var linkedInvoices = invoices.Where(x => x.AppointmentId.HasValue && appointmentIds.Contains(x.AppointmentId.Value)).ToList();
                var recognizedRevenue = linkedInvoices.Sum(x => Math.Max(x.TotalAmount - x.ApprovedDiscountAmount - x.RefundedAmount, 0m));
                var consultationEstimate = doctorAppointments.Count * doctor.ConsultationFee;

                return new DoctorRevenueRowDto
                {
                    DoctorId = doctor.DoctorId,
                    DoctorName = doctor.FullName,
                    Specialization = doctor.Specialization,
                    AppointmentCount = doctorAppointments.Count,
                    LinkedInvoiceCount = linkedInvoices.Count,
                    RecognizedRevenue = recognizedRevenue,
                    ConsultationEstimate = consultationEstimate,
                    ReportedRevenue = recognizedRevenue > 0m ? recognizedRevenue : consultationEstimate
                };
            })
            .OrderByDescending(x => x.ReportedRevenue)
            .ThenBy(x => x.DoctorName)
            .ToList();

        return new DoctorRevenueReportDto
        {
            ActiveDoctors = activeDoctors.Count,
            DoctorsWithRevenue = rows.Count(x => x.ReportedRevenue > 0m),
            TotalRecognizedRevenue = rows.Sum(x => x.RecognizedRevenue),
            TotalConsultationEstimate = rows.Sum(x => x.ConsultationEstimate),
            Rows = rows
        };
    }

    private static PharmacySalesReportDto BuildPharmacySalesReport(
        IReadOnlyList<StockLocation> stockLocations,
        IReadOnlyList<PurchaseInvoice> purchaseInvoices,
        IReadOnlyList<PurchaseReturn> purchaseReturns,
        IReadOnlyDictionary<long, PurchaseInvoice> purchaseInvoiceById,
        IReadOnlyList<StockTransfer> transferHeaders,
        IReadOnlyList<StockTransferLine> transferLines,
        IReadOnlyList<StockAdjustment> adjustmentHeaders,
        IReadOnlyList<StockAdjustmentLine> adjustmentLines,
        IReadOnlyList<StockBatch> stockBatches,
        IReadOnlyDictionary<long, StockBatch> batchById,
        IReadOnlyDictionary<long, Branch> branchById)
    {
        var locations = stockLocations.Where(x => x.IsActive && x.LocationType == StockLocationType.PharmacyStore).OrderBy(x => x.Name).ToList();
        var rows = locations
            .Select(location =>
            {
                var receivedValue = purchaseInvoices.Where(x => x.StockLocationId == location.StockLocationId).Sum(x => x.TotalAmount);
                var returnedValue = purchaseReturns
                    .Where(x => purchaseInvoiceById.TryGetValue(x.PurchaseInvoiceId, out var purchaseInvoice) && purchaseInvoice.StockLocationId == location.StockLocationId)
                    .Sum(x => x.TotalAmount);
                var transferOutValue = transferLines
                    .Where(line =>
                        transferHeaders.Any(header =>
                            header.StockTransferId == line.StockTransferId &&
                            header.FromStockLocationId == location.StockLocationId &&
                            header.Status is TransferStatus.Approved or TransferStatus.Received))
                    .Sum(line => line.Quantity * (batchById.TryGetValue(line.StockBatchId, out var batch) ? batch.UnitCost : 0m));
                var adjustmentOutValue = adjustmentLines
                    .Where(line =>
                        line.QuantityDelta < 0m &&
                        adjustmentHeaders.Any(header =>
                            header.StockAdjustmentId == line.StockAdjustmentId &&
                            header.StockLocationId == location.StockLocationId &&
                            header.Status == ApprovalStatus.Approved))
                    .Sum(line => Math.Abs(line.QuantityDelta) * (batchById.TryGetValue(line.StockBatchId, out var batch) ? batch.UnitCost : 0m));
                var currentStockValue = stockBatches
                    .Where(batch => batch.StockLocationId == location.StockLocationId && batch.QuantityOnHand > 0m)
                    .Sum(batch => batch.QuantityOnHand * batch.UnitCost);

                return new PharmacyMovementRowDto
                {
                    StockLocationId = location.StockLocationId,
                    LocationName = location.Name,
                    BranchName = location.BranchId.HasValue && branchById.TryGetValue(location.BranchId.Value, out var branch)
                        ? branch.Name
                        : "Central",
                    ReceivedValue = receivedValue,
                    ReturnedValue = returnedValue,
                    TransferOutValue = transferOutValue,
                    AdjustmentOutValue = adjustmentOutValue,
                    CurrentStockValue = currentStockValue
                };
            })
            .ToList();

        return new PharmacySalesReportDto
        {
            BasisNote = "Pharmacy sales use inventory movement proxies because the current schema does not yet contain a dispense ledger.",
            PharmacyLocations = locations.Count,
            ReceivedValue = rows.Sum(x => x.ReceivedValue),
            ReturnedValue = rows.Sum(x => x.ReturnedValue),
            TransferOutValue = rows.Sum(x => x.TransferOutValue),
            AdjustmentOutValue = rows.Sum(x => x.AdjustmentOutValue),
            CurrentStockValue = rows.Sum(x => x.CurrentStockValue),
            Rows = rows
        };
    }

    private static PurchaseReportDto BuildPurchaseReport(
        int purchaseOrdersCount,
        IReadOnlyList<PurchaseInvoice> purchaseInvoices,
        IReadOnlyList<PurchaseReturn> purchaseReturns,
        IReadOnlyList<Supplier> suppliers,
        IReadOnlyDictionary<long, Supplier> supplierById,
        IReadOnlyDictionary<long, StockLocation> locationById)
    {
        return new PurchaseReportDto
        {
            PurchaseOrders = purchaseOrdersCount,
            PurchaseInvoices = purchaseInvoices.Count,
            TotalPurchased = purchaseInvoices.Sum(x => x.TotalAmount),
            TotalPaid = purchaseInvoices.Sum(x => x.PaidAmount),
            TotalDue = purchaseInvoices.Sum(x => Math.Max(x.TotalAmount - x.PaidAmount, 0m)),
            TotalReturned = purchaseReturns.Sum(x => x.TotalAmount),
            SupplierRows = suppliers
                .Select(supplier =>
                {
                    var supplierInvoices = purchaseInvoices.Where(x => x.SupplierId == supplier.SupplierId).ToList();
                    var supplierReturns = purchaseReturns.Where(x => x.SupplierId == supplier.SupplierId).ToList();

                    return new PurchaseSupplierReportRowDto
                    {
                        SupplierId = supplier.SupplierId,
                        SupplierName = supplier.SupplierName,
                        InvoiceCount = supplierInvoices.Count,
                        TotalPurchased = supplierInvoices.Sum(x => x.TotalAmount),
                        DueAmount = supplierInvoices.Sum(x => Math.Max(x.TotalAmount - x.PaidAmount, 0m)),
                        ReturnAmount = supplierReturns.Sum(x => x.TotalAmount)
                    };
                })
                .Where(x => x.InvoiceCount > 0 || x.ReturnAmount > 0m)
                .OrderByDescending(x => x.TotalPurchased)
                .ThenBy(x => x.SupplierName)
                .ToList(),
            RecentInvoices = purchaseInvoices
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.PurchaseInvoiceId)
                .Take(8)
                .Select(invoice => new PurchaseInvoiceReportRowDto
                {
                    PurchaseInvoiceId = invoice.PurchaseInvoiceId,
                    InvoiceNumber = invoice.InvoiceNumber,
                    SupplierName = supplierById.TryGetValue(invoice.SupplierId, out var supplier) ? supplier.SupplierName : "Unknown supplier",
                    LocationName = locationById.TryGetValue(invoice.StockLocationId, out var location) ? location.Name : "Unknown location",
                    TotalAmount = invoice.TotalAmount,
                    DueAmount = Math.Max(invoice.TotalAmount - invoice.PaidAmount, 0m),
                    Status = invoice.Status.ToString(),
                    InvoiceDate = invoice.InvoiceDate
                })
                .ToList()
        };
    }

    private static StockBalanceReportDto BuildStockBalanceReport(
        IReadOnlyList<StockLocation> stockLocations,
        IReadOnlyList<StockBatch> stockBatches,
        IReadOnlyDictionary<long, MedicineMaster> medicineById,
        IReadOnlyDictionary<long, StockItemMaster> stockItemById,
        IReadOnlyDictionary<long, Branch> branchById)
    {
        var activeBatches = stockBatches.Where(x => x.QuantityOnHand > 0m).ToList();
        var rows = stockLocations
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(location =>
            {
                var locationBatches = activeBatches.Where(batch => batch.StockLocationId == location.StockLocationId).ToList();
                return new StockBalanceRowDto
                {
                    StockLocationId = location.StockLocationId,
                    LocationName = location.Name,
                    BranchName = location.BranchId.HasValue && branchById.TryGetValue(location.BranchId.Value, out var branch)
                        ? branch.Name
                        : "Central",
                    BatchCount = locationBatches.Count,
                    ItemCount = locationBatches.Select(GetBatchItemKey).Distinct().Count(),
                    QuantityOnHand = locationBatches.Sum(batch => batch.QuantityOnHand),
                    StockValue = locationBatches.Sum(batch => batch.QuantityOnHand * batch.UnitCost),
                    LowStockBatches = locationBatches.Count(batch => IsLowStockBatch(batch, medicineById, stockItemById))
                };
            })
            .Where(x => x.BatchCount > 0)
            .ToList();

        return new StockBalanceReportDto
        {
            ActiveLocations = rows.Count,
            ActiveBatches = activeBatches.Count,
            QuantityOnHand = activeBatches.Sum(x => x.QuantityOnHand),
            StockValue = activeBatches.Sum(x => x.QuantityOnHand * x.UnitCost),
            LowStockBatches = activeBatches.Count(batch => IsLowStockBatch(batch, medicineById, stockItemById)),
            LocationRows = rows
        };
    }

    private static ExpiryReportDto BuildExpiryReport(
        DateTime today,
        DateTime expiryThreshold,
        IReadOnlyList<StockBatch> stockBatches,
        IReadOnlyDictionary<long, MedicineMaster> medicineById,
        IReadOnlyDictionary<long, StockItemMaster> stockItemById,
        IReadOnlyDictionary<long, StockLocation> locationById)
    {
        var rows = stockBatches
            .Where(x => x.QuantityOnHand > 0m && x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date <= expiryThreshold)
            .Select(batch =>
            {
                var expiryDate = batch.ExpiryDate!.Value.Date;
                return new ExpiryBatchRowDto
                {
                    StockBatchId = batch.StockBatchId,
                    ItemName = GetBatchItemName(batch, medicineById, stockItemById),
                    LocationName = locationById.TryGetValue(batch.StockLocationId, out var location) ? location.Name : "Unknown location",
                    BatchNumber = string.IsNullOrWhiteSpace(batch.BatchNumber) ? "NA" : batch.BatchNumber!,
                    ExpiryDate = expiryDate,
                    QuantityOnHand = batch.QuantityOnHand,
                    StockValue = batch.QuantityOnHand * batch.UnitCost,
                    Status = expiryDate < today ? "Expired" : "Near expiry"
                };
            })
            .OrderBy(x => x.ExpiryDate)
            .ThenBy(x => x.ItemName)
            .Take(20)
            .ToList();

        return new ExpiryReportDto
        {
            NearExpiryCount = rows.Count(x => x.Status == "Near expiry"),
            ExpiredCount = rows.Count(x => x.Status == "Expired"),
            NearExpiryValue = rows.Where(x => x.Status == "Near expiry").Sum(x => x.StockValue),
            ExpiredValue = rows.Where(x => x.Status == "Expired").Sum(x => x.StockValue),
            Rows = rows
        };
    }

    private static BedOccupancyReportDto BuildBedOccupancyReport(
        IReadOnlyList<Ward> wards,
        IReadOnlyList<Bed> beds,
        IReadOnlyDictionary<long, Branch> branchById)
    {
        var activeBeds = beds.Where(x => x.IsActive).ToList();
        var occupiedBeds = activeBeds.Count(x => x.IsOccupied);
        var branchRows = activeBeds
            .GroupBy(x => x.BranchId)
            .Select(group =>
            {
                branchById.TryGetValue(group.Key, out var branch);
                var totalBeds = group.Count();
                var occupied = group.Count(x => x.IsOccupied);
                return new BranchOccupancyDto
                {
                    BranchId = group.Key,
                    BranchName = branch?.Name ?? "Unknown branch",
                    TotalBeds = totalBeds,
                    OccupiedBeds = occupied,
                    OccupancyRate = CalculateRate(occupied, totalBeds)
                };
            })
            .OrderByDescending(x => x.OccupancyRate)
            .ThenBy(x => x.BranchName)
            .ToList();
        var wardRows = activeBeds
            .GroupBy(x => x.WardId)
            .Select(group =>
            {
                var bed = group.First();
                return new WardOccupancyRowDto
                {
                    WardId = group.Key,
                    WardName = wards.FirstOrDefault(x => x.WardId == group.Key)?.Name ?? $"Ward #{group.Key}",
                    BranchName = branchById.TryGetValue(bed.BranchId, out var branch) ? branch.Name : "Unknown branch",
                    TotalBeds = group.Count(),
                    OccupiedBeds = group.Count(x => x.IsOccupied),
                    OccupancyRate = CalculateRate(group.Count(x => x.IsOccupied), group.Count())
                };
            })
            .OrderByDescending(x => x.OccupancyRate)
            .ThenBy(x => x.WardName)
            .ToList();

        return new BedOccupancyReportDto
        {
            TotalBeds = activeBeds.Count,
            OccupiedBeds = occupiedBeds,
            AvailableBeds = Math.Max(activeBeds.Count - occupiedBeds, 0),
            OccupancyRate = CalculateRate(occupiedBeds, activeBeds.Count),
            BranchRows = branchRows,
            WardRows = wardRows
        };
    }

    private static AuditLogEntryDto MapAuditEntry(AuditLogEntry entry)
    {
        return new AuditLogEntryDto
        {
            AuditLogEntryId = entry.AuditLogEntryId,
            Category = entry.Category,
            Action = entry.Action,
            EntityName = entry.EntityName,
            EntityId = entry.EntityId,
            TargetDisplayName = entry.TargetDisplayName,
            Summary = entry.Summary,
            MetadataJson = entry.MetadataJson,
            PerformedByUserId = entry.PerformedByUserId,
            PerformedByName = entry.PerformedByName,
            PerformedByRole = entry.PerformedByRole,
            ActorEmail = entry.ActorEmail,
            IpAddress = entry.IpAddress,
            UserAgent = entry.UserAgent,
            CreatedAt = entry.CreatedAt
        };
    }

    private static string? GetActorKey(AuditLogEntry entry)
    {
        if (entry.PerformedByUserId.HasValue)
        {
            return $"user:{entry.PerformedByUserId.Value}";
        }

        if (!string.IsNullOrWhiteSpace(entry.ActorEmail))
        {
            return $"email:{entry.ActorEmail}";
        }

        return string.IsNullOrWhiteSpace(entry.PerformedByName) ? null : $"name:{entry.PerformedByName}";
    }

    private static decimal GetInvoiceDueAmount(BillingInvoice invoice)
    {
        return Math.Max(invoice.TotalAmount - invoice.ApprovedDiscountAmount - invoice.AmountPaid, 0m);
    }

    private static decimal CalculateRate(int occupied, int total)
    {
        if (total <= 0)
        {
            return 0m;
        }

        return decimal.Round(occupied * 100m / total, 2);
    }

    private static bool IsLowStockBatch(
        StockBatch batch,
        IReadOnlyDictionary<long, MedicineMaster> medicineById,
        IReadOnlyDictionary<long, StockItemMaster> stockItemById)
    {
        decimal minimumStock = 0m;

        if (batch.MedicineMasterId.HasValue && medicineById.TryGetValue(batch.MedicineMasterId.Value, out var medicine))
        {
            minimumStock = medicine.MinimumStock;
        }
        else if (batch.StockItemMasterId.HasValue && stockItemById.TryGetValue(batch.StockItemMasterId.Value, out var item))
        {
            minimumStock = item.MinimumStock;
        }

        return minimumStock > 0m && batch.QuantityOnHand <= minimumStock;
    }

    private static string GetBatchItemName(
        StockBatch batch,
        IReadOnlyDictionary<long, MedicineMaster> medicineById,
        IReadOnlyDictionary<long, StockItemMaster> stockItemById)
    {
        if (batch.MedicineMasterId.HasValue && medicineById.TryGetValue(batch.MedicineMasterId.Value, out var medicine))
        {
            return medicine.MedicineName;
        }

        if (batch.StockItemMasterId.HasValue && stockItemById.TryGetValue(batch.StockItemMasterId.Value, out var item))
        {
            return item.ItemName;
        }

        return $"Stock batch #{batch.StockBatchId}";
    }

    private static string GetBatchItemKey(StockBatch batch)
    {
        if (batch.MedicineMasterId.HasValue)
        {
            return $"M:{batch.MedicineMasterId.Value}";
        }

        if (batch.StockItemMasterId.HasValue)
        {
            return $"I:{batch.StockItemMasterId.Value}";
        }

        return $"B:{batch.StockBatchId}";
    }
}
