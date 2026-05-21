using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "AdminOnly")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("admin-summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetAdminSummary()
    {
        var today = DateTime.Today;

        var patients = await _db.Patients.AsNoTracking().ToListAsync();
        var users = await _db.Users.AsNoTracking().ToListAsync();
        var appointments = await _db.Appointments.AsNoTracking().ToListAsync();
        var invoices = await _db.BillingInvoices.AsNoTracking().ToListAsync();
        var batches = await _db.StockBatches.AsNoTracking().ToListAsync();
        var locations = await _db.StockLocations.AsNoTracking().ToListAsync();
        var branches = await _db.Branches.AsNoTracking().ToListAsync();
        var medicines = await _db.MedicineMasters.AsNoTracking().ToListAsync();
        var stockItems = await _db.StockItemMasters.AsNoTracking().ToListAsync();
        var beds = await _db.Beds.AsNoTracking().ToListAsync();

        var totalPatients = patients.Count;
        var todayAppointments = appointments.Count(x => x.AppointmentDate.Date == today);
        var todaySales = invoices
            .Where(x => x.LastPaymentDate.HasValue && x.LastPaymentDate.Value.Date == today && x.Status != InvoiceStatus.Cancelled)
            .Sum(x => x.AmountPaid);

        var branchMap = branches.ToDictionary(x => x.BranchId);
        var locationMap = locations.ToDictionary(x => x.StockLocationId);
        var medicineMap = medicines.ToDictionary(x => x.MedicineMasterId);
        var stockItemMap = stockItems.ToDictionary(x => x.StockItemMasterId);

        var stockBatchRows = batches
            .Where(x => x.QuantityOnHand > 0)
            .Select(batch =>
            {
                locationMap.TryGetValue(batch.StockLocationId, out var location);
                var branchName = location?.BranchId is long branchId && branchMap.TryGetValue(branchId, out var branch)
                    ? branch.Name
                    : location?.Name ?? "Unassigned";
                var medicine = batch.MedicineMasterId.HasValue && medicineMap.TryGetValue(batch.MedicineMasterId.Value, out var medicineRow)
                    ? medicineRow
                    : null;
                var item = batch.StockItemMasterId.HasValue && stockItemMap.TryGetValue(batch.StockItemMasterId.Value, out var itemRow)
                    ? itemRow
                    : null;

                return new
                {
                    batch.StockBatchId,
                    Name = medicine?.MedicineName ?? item?.ItemName ?? "Unknown item",
                    BranchName = branchName,
                    batch.QuantityOnHand,
                    ReorderLevel = medicine?.MinimumStock ?? item?.MinimumStock ?? 0m,
                    batch.ExpiryDate
                };
            })
            .ToList();

        var lowStockRows = stockBatchRows
            .Where(x => x.QuantityOnHand <= x.ReorderLevel && (!x.ExpiryDate.HasValue || x.ExpiryDate.Value.Date >= today))
            .ToList();

        var expiredMedicineRows = stockBatchRows
            .Where(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date < today)
            .ToList();

        var pendingInvoiceRows = invoices
            .Where(x => x.Status == InvoiceStatus.Pending || x.Status == InvoiceStatus.Partial)
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.InvoiceDate)
            .Take(6)
            .ToList();

        var patientMap = patients.ToDictionary(x => x.PatientId);
        var userMap = users.ToDictionary(x => x.UserId);

        var pendingInvoices = pendingInvoiceRows.Select(invoice =>
        {
            var patientName = "Unknown patient";
            if (invoice.PatientId.HasValue &&
                patientMap.TryGetValue(invoice.PatientId.Value, out var patient) &&
                userMap.TryGetValue(patient.UserId, out var user))
            {
                patientName = user.FullName;
            }

            return new PendingPaymentDto
            {
                InvoiceId = invoice.BillingInvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                PatientName = patientName,
                DueAmount = Math.Max(invoice.TotalAmount - invoice.ApprovedDiscountAmount - invoice.AmountPaid, 0m),
                DueDate = invoice.DueDate,
                Status = invoice.Status.ToString()
            };
        }).ToList();

        var bedOccupancyGroups = beds
            .Where(x => x.IsActive)
            .GroupBy(x => x.BranchId)
            .Select(group => new
            {
                BranchId = group.Key,
                TotalBeds = group.Count(),
                OccupiedBeds = group.Count(x => x.IsOccupied)
            })
            .ToList();

        List<BranchOccupancyDto> branchOccupancy;
        if (bedOccupancyGroups.Count != 0)
        {
            var activeBranches = branches
                .Where(x => x.IsActive)
                .ToDictionary(x => x.BranchId);

            branchOccupancy = bedOccupancyGroups
                .Where(x => activeBranches.ContainsKey(x.BranchId))
                .Select(x =>
                {
                    var branch = activeBranches[x.BranchId];
                    return new BranchOccupancyDto
                    {
                        BranchId = x.BranchId,
                        BranchName = branch.Name,
                        TotalBeds = x.TotalBeds,
                        OccupiedBeds = x.OccupiedBeds,
                        OccupancyRate = x.TotalBeds == 0 ? 0 : Math.Round((decimal)x.OccupiedBeds / x.TotalBeds * 100m, 1)
                    };
                })
                .OrderByDescending(x => x.TotalBeds)
                .ThenBy(x => x.BranchName)
                .ToList();
        }
        else
        {
            branchOccupancy = branches
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.Name)
                .Select(x => new BranchOccupancyDto
                {
                    BranchId = x.BranchId,
                    BranchName = x.Name,
                    TotalBeds = x.TotalBeds,
                    OccupiedBeds = x.OccupiedBeds,
                    OccupancyRate = x.TotalBeds == 0 ? 0 : Math.Round((decimal)x.OccupiedBeds / x.TotalBeds * 100m, 1)
                })
                .ToList();
        }

        var totalBeds = branchOccupancy.Sum(x => x.TotalBeds);
        var occupiedBeds = branchOccupancy.Sum(x => x.OccupiedBeds);

        var summary = new DashboardSummaryDto
        {
            TotalPatients = totalPatients,
            TodayAppointments = todayAppointments,
            TodaySales = todaySales,
            LowStockItems = lowStockRows.Count,
            ExpiredMedicines = expiredMedicineRows.Count,
            PendingPayments = pendingInvoices.Count,
            PendingPaymentAmount = pendingInvoices.Sum(x => x.DueAmount),
            TotalBeds = totalBeds,
            OccupiedBeds = occupiedBeds,
            BedOccupancyRate = totalBeds == 0 ? 0 : Math.Round((decimal)occupiedBeds / totalBeds * 100m, 1),
            LowStockAlerts = lowStockRows
                .OrderBy(x => x.QuantityOnHand)
                .Take(5)
                .Select(x => new StockAlertDto
                {
                    ItemId = x.StockBatchId,
                    Name = x.Name,
                    BranchName = x.BranchName,
                    QuantityInStock = (int)Math.Round(x.QuantityOnHand, MidpointRounding.AwayFromZero),
                    ReorderLevel = (int)Math.Round(x.ReorderLevel, MidpointRounding.AwayFromZero),
                    ExpiryDate = x.ExpiryDate ?? today
                })
                .ToList(),
            ExpiredMedicineAlerts = expiredMedicineRows
                .OrderBy(x => x.ExpiryDate)
                .Take(5)
                .Select(x => new StockAlertDto
                {
                    ItemId = x.StockBatchId,
                    Name = x.Name,
                    BranchName = x.BranchName,
                    QuantityInStock = (int)Math.Round(x.QuantityOnHand, MidpointRounding.AwayFromZero),
                    ReorderLevel = (int)Math.Round(x.ReorderLevel, MidpointRounding.AwayFromZero),
                    ExpiryDate = x.ExpiryDate ?? today
                })
                .ToList(),
            PendingPaymentDetails = pendingInvoices,
            BedOccupancyByBranch = branchOccupancy
        };

        return Ok(ApiResponse<DashboardSummaryDto>.Ok(summary));
    }
}
