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

        var totalPatients = await _db.Patients.CountAsync();
        var todayAppointments = await _db.Appointments.CountAsync(x => x.AppointmentDate.Date == today);
        var todaySales = await _db.BillingInvoices
            .Where(x => x.LastPaymentDate.HasValue && x.LastPaymentDate.Value.Date == today && x.Status != InvoiceStatus.Cancelled)
            .SumAsync(x => (decimal?)x.AmountPaid) ?? 0m;

        var stockBatchQuery =
            from batch in _db.StockBatches.AsNoTracking()
            join location in _db.StockLocations.AsNoTracking() on batch.StockLocationId equals location.StockLocationId
            join branchRow in _db.Branches.AsNoTracking() on location.BranchId equals branchRow.BranchId into branchJoin
            from branch in branchJoin.DefaultIfEmpty()
            join medicineRow in _db.MedicineMasters.AsNoTracking() on batch.MedicineMasterId equals medicineRow.MedicineMasterId into medicineJoin
            from medicine in medicineJoin.DefaultIfEmpty()
            join itemRow in _db.StockItemMasters.AsNoTracking() on batch.StockItemMasterId equals itemRow.StockItemMasterId into itemJoin
            from item in itemJoin.DefaultIfEmpty()
            where batch.QuantityOnHand > 0
            select new
            {
                batch.StockBatchId,
                Name = medicine != null ? medicine.MedicineName : item != null ? item.ItemName : "Unknown item",
                BranchName = branch != null ? branch.Name : location.Name,
                QuantityOnHand = batch.QuantityOnHand,
                ReorderLevel = medicine != null ? medicine.MinimumStock : item != null ? item.MinimumStock : 0m,
                batch.ExpiryDate
            };

        var lowStockQuery = stockBatchQuery
            .Where(x => x.QuantityOnHand <= x.ReorderLevel && (!x.ExpiryDate.HasValue || x.ExpiryDate.Value.Date >= today));

        var expiredMedicineQuery = stockBatchQuery
            .Where(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date < today);

        var pendingInvoiceRows = await _db.BillingInvoices
            .AsNoTracking()
            .Where(x => x.Status == InvoiceStatus.Pending || x.Status == InvoiceStatus.Partial)
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.InvoiceDate)
            .Take(6)
            .ToListAsync();

        var patientIds = pendingInvoiceRows
            .Where(x => x.PatientId.HasValue)
            .Select(x => x.PatientId!.Value)
            .Distinct()
            .ToList();

        var patientMap = await _db.Patients
            .AsNoTracking()
            .Where(x => patientIds.Contains(x.PatientId))
            .ToDictionaryAsync(x => x.PatientId, cancellationToken: default);

        var userIds = patientMap.Values.Select(x => x.UserId).Distinct().ToList();
        var userMap = await _db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, cancellationToken: default);

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

        var bedOccupancyGroups = await _db.Beds
            .AsNoTracking()
            .Where(x => x.IsActive)
            .GroupBy(x => x.BranchId)
            .Select(group => new
            {
                BranchId = group.Key,
                TotalBeds = group.Count(),
                OccupiedBeds = group.Count(x => x.IsOccupied)
            })
            .ToListAsync();

        List<BranchOccupancyDto> branchOccupancy;
        if (bedOccupancyGroups.Count != 0)
        {
            var branches = await _db.Branches
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToDictionaryAsync(x => x.BranchId);

            branchOccupancy = bedOccupancyGroups
                .Where(x => branches.ContainsKey(x.BranchId))
                .Select(x =>
                {
                    var branch = branches[x.BranchId];
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
            branchOccupancy = await _db.Branches
                .AsNoTracking()
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
                .ToListAsync();
        }

        var totalBeds = branchOccupancy.Sum(x => x.TotalBeds);
        var occupiedBeds = branchOccupancy.Sum(x => x.OccupiedBeds);

        var summary = new DashboardSummaryDto
        {
            TotalPatients = totalPatients,
            TodayAppointments = todayAppointments,
            TodaySales = todaySales,
            LowStockItems = await lowStockQuery.CountAsync(),
            ExpiredMedicines = await expiredMedicineQuery.CountAsync(),
            PendingPayments = pendingInvoices.Count,
            PendingPaymentAmount = pendingInvoices.Sum(x => x.DueAmount),
            TotalBeds = totalBeds,
            OccupiedBeds = occupiedBeds,
            BedOccupancyRate = totalBeds == 0 ? 0 : Math.Round((decimal)occupiedBeds / totalBeds * 100m, 1),
            LowStockAlerts = await lowStockQuery
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
                .ToListAsync(),
            ExpiredMedicineAlerts = await expiredMedicineQuery
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
                .ToListAsync(),
            PendingPaymentDetails = pendingInvoices,
            BedOccupancyByBranch = branchOccupancy
        };

        return Ok(ApiResponse<DashboardSummaryDto>.Ok(summary));
    }
}
