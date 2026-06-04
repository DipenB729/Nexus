using AngularApp4.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.EntityFrameworkCore;

namespace AngularApp4.Services.Hms;

public sealed class EfInventoryAdminService : IInventoryAdminService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _audit;

    public EfInventoryAdminService(AppDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<InventoryDashboardDto> GetDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var nearExpiryDate = today.AddDays(30);

        var batches = await LoadBatchDetailsAsync();
        var supplierDues = await GetSupplierDueSummaryAsync();

        var lowStockAlerts = batches
            .Where(x => x.MinimumStock > 0m && x.QuantityOnHand <= x.MinimumStock)
            .OrderBy(x => x.QuantityOnHand)
            .ThenBy(x => x.ItemName)
            .Take(10)
            .Select(MapInventoryAlert)
            .ToList();

        var nearExpiryAlerts = batches
            .Where(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date >= today && x.ExpiryDate.Value.Date <= nearExpiryDate)
            .OrderBy(x => x.ExpiryDate)
            .ThenBy(x => x.ItemName)
            .Take(10)
            .Select(MapInventoryAlert)
            .ToList();

        return new InventoryDashboardDto
        {
            ActiveLocations = await _db.StockLocations.CountAsync(x => x.IsActive),
            ActiveSuppliers = await _db.Suppliers.CountAsync(x => x.IsActive),
            OpenPurchaseOrders = await _db.PurchaseOrders.CountAsync(x => x.Status == PurchaseOrderStatus.Ordered || x.Status == PurchaseOrderStatus.PartiallyReceived),
            PendingTransfers = await _db.StockTransfers.CountAsync(x => x.Status == TransferStatus.Pending || x.Status == TransferStatus.Approved),
            PendingAdjustments = await _db.StockAdjustments.CountAsync(x => x.Status == ApprovalStatus.Pending),
            LowStockCount = lowStockAlerts.Count,
            NearExpiryCount = batches.Count(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date >= today && x.ExpiryDate.Value.Date <= nearExpiryDate),
            ExpiredCount = batches.Count(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date < today),
            SupplierDueAmount = supplierDues.Sum(x => x.DueAmount),
            LowStockAlerts = lowStockAlerts,
            NearExpiryAlerts = nearExpiryAlerts,
            SupplierDues = supplierDues
        };
    }

    public async Task<IReadOnlyList<InventoryUnitDto>> GetUnitsAsync() =>
        await _db.InventoryUnits
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new InventoryUnitDto
            {
                InventoryUnitId = x.InventoryUnitId,
                Name = x.Name,
                ShortName = x.ShortName,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync();

    public async Task<InventoryUnitDto> SaveUnitAsync(long? inventoryUnitId, SaveInventoryUnitDto dto)
    {
        var entity = inventoryUnitId.HasValue
            ? await _db.InventoryUnits.FirstOrDefaultAsync(x => x.InventoryUnitId == inventoryUnitId.Value)
            : null;

        var isNew = entity is null;
        entity ??= new InventoryUnit { CreatedAt = DateTime.UtcNow };
        entity.Name = NormalizeRequired(dto.Name);
        entity.ShortName = Normalize(dto.ShortName);
        entity.Description = Normalize(dto.Description);
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            _db.InventoryUnits.Add(entity);
        }

        await _db.SaveChangesAsync();
        await WriteInventoryAuditAsync(isNew ? "Created" : "Updated", "Inventory Unit", entity.InventoryUnitId, entity.Name, $"Inventory unit {entity.Name} was {(isNew ? "created" : "updated")}.");
        return (await GetUnitsAsync()).First(x => x.InventoryUnitId == entity.InventoryUnitId);
    }

    public async Task<IReadOnlyList<InventoryCategoryDto>> GetCategoriesAsync() =>
        await _db.InventoryCategories
            .AsNoTracking()
            .OrderBy(x => x.CategoryType)
            .ThenBy(x => x.Name)
            .Select(x => new InventoryCategoryDto
            {
                InventoryCategoryId = x.InventoryCategoryId,
                CategoryType = x.CategoryType.ToString(),
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync();

    public async Task<InventoryCategoryDto> SaveCategoryAsync(long? inventoryCategoryId, SaveInventoryCategoryDto dto)
    {
        var entity = inventoryCategoryId.HasValue
            ? await _db.InventoryCategories.FirstOrDefaultAsync(x => x.InventoryCategoryId == inventoryCategoryId.Value)
            : null;

        var isNew = entity is null;
        entity ??= new InventoryCategory { CreatedAt = DateTime.UtcNow };
        entity.CategoryType = ParseEnum(dto.CategoryType, InventoryCategoryType.Item);
        entity.Name = NormalizeRequired(dto.Name);
        entity.Description = Normalize(dto.Description);
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            _db.InventoryCategories.Add(entity);
        }

        await _db.SaveChangesAsync();
        await WriteInventoryAuditAsync(isNew ? "Created" : "Updated", "Inventory Category", entity.InventoryCategoryId, entity.Name, $"Inventory category {entity.Name} was {(isNew ? "created" : "updated")}.");
        return (await GetCategoriesAsync()).First(x => x.InventoryCategoryId == entity.InventoryCategoryId);
    }

    public async Task<IReadOnlyList<MedicineMasterDto>> GetMedicinesAsync()
    {
        var medicines = await _db.MedicineMasters
            .AsNoTracking()
            .OrderBy(x => x.MedicineName)
            .ThenBy(x => x.Brand)
            .ThenBy(x => x.Strength)
            .ToListAsync();
        var units = await _db.InventoryUnits.AsNoTracking().ToDictionaryAsync(x => x.InventoryUnitId);
        var categories = await _db.InventoryCategories.AsNoTracking().ToDictionaryAsync(x => x.InventoryCategoryId);

        return medicines.Select(x => new MedicineMasterDto
            {
                MedicineMasterId = x.MedicineMasterId,
                MedicineName = x.MedicineName,
                GenericName = x.GenericName,
                Brand = x.Brand,
                UnitName = units.TryGetValue(x.InventoryUnitId, out var unit) ? unit.Name : string.Empty,
                InventoryUnitId = x.InventoryUnitId,
                CategoryName = x.InventoryCategoryId.HasValue && categories.TryGetValue(x.InventoryCategoryId.Value, out var category) ? category.Name : null,
                InventoryCategoryId = x.InventoryCategoryId,
                Strength = x.Strength,
                BatchRequired = x.BatchRequired,
                MinimumStock = x.MinimumStock,
                MaximumStock = x.MaximumStock,
                IsActive = x.IsActive
            })
            .ToList();
    }

    public async Task<MedicineMasterDto> SaveMedicineAsync(long? medicineMasterId, SaveMedicineMasterDto dto)
    {
        if (!await _db.InventoryUnits.AnyAsync(x => x.InventoryUnitId == dto.InventoryUnitId))
        {
            throw new InvalidOperationException("Inventory unit not found.");
        }

        var entity = medicineMasterId.HasValue
            ? await _db.MedicineMasters.FirstOrDefaultAsync(x => x.MedicineMasterId == medicineMasterId.Value)
            : null;

        var isNew = entity is null;
        entity ??= new MedicineMaster { CreatedAt = DateTime.UtcNow };
        entity.InventoryUnitId = dto.InventoryUnitId;
        entity.InventoryCategoryId = dto.InventoryCategoryId;
        entity.MedicineName = NormalizeRequired(dto.MedicineName);
        entity.GenericName = Normalize(dto.GenericName);
        entity.Brand = Normalize(dto.Brand);
        entity.Strength = Normalize(dto.Strength);
        entity.BatchRequired = dto.BatchRequired;
        entity.MinimumStock = Math.Max(dto.MinimumStock, 0m);
        entity.MaximumStock = Math.Max(dto.MaximumStock, 0m);
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            _db.MedicineMasters.Add(entity);
        }

        await _db.SaveChangesAsync();
        await WriteInventoryAuditAsync(isNew ? "Created" : "Updated", "Medicine Master", entity.MedicineMasterId, entity.MedicineName, $"Medicine master {entity.MedicineName} was {(isNew ? "created" : "updated")}.");
        return (await GetMedicinesAsync()).First(x => x.MedicineMasterId == entity.MedicineMasterId);
    }

    public async Task<IReadOnlyList<StockItemMasterDto>> GetItemsAsync()
    {
        var items = await _db.StockItemMasters
            .AsNoTracking()
            .OrderBy(x => x.ItemType)
            .ThenBy(x => x.ItemName)
            .ToListAsync();
        var units = await _db.InventoryUnits.AsNoTracking().ToDictionaryAsync(x => x.InventoryUnitId);
        var categories = await _db.InventoryCategories.AsNoTracking().ToDictionaryAsync(x => x.InventoryCategoryId);

        return items.Select(x => new StockItemMasterDto
            {
                StockItemMasterId = x.StockItemMasterId,
                ItemType = x.ItemType.ToString(),
                ItemName = x.ItemName,
                Specification = x.Specification,
                UnitName = units.TryGetValue(x.InventoryUnitId, out var unit) ? unit.Name : string.Empty,
                InventoryUnitId = x.InventoryUnitId,
                CategoryName = x.InventoryCategoryId.HasValue && categories.TryGetValue(x.InventoryCategoryId.Value, out var category) ? category.Name : null,
                InventoryCategoryId = x.InventoryCategoryId,
                MinimumStock = x.MinimumStock,
                MaximumStock = x.MaximumStock,
                IsActive = x.IsActive
            })
            .ToList();
    }

    public async Task<StockItemMasterDto> SaveItemAsync(long? stockItemMasterId, SaveStockItemMasterDto dto)
    {
        if (!await _db.InventoryUnits.AnyAsync(x => x.InventoryUnitId == dto.InventoryUnitId))
        {
            throw new InvalidOperationException("Inventory unit not found.");
        }

        var entity = stockItemMasterId.HasValue
            ? await _db.StockItemMasters.FirstOrDefaultAsync(x => x.StockItemMasterId == stockItemMasterId.Value)
            : null;

        var isNew = entity is null;
        entity ??= new StockItemMaster { CreatedAt = DateTime.UtcNow };
        entity.InventoryUnitId = dto.InventoryUnitId;
        entity.InventoryCategoryId = dto.InventoryCategoryId;
        entity.ItemType = ParseEnum(dto.ItemType, InventoryItemType.Consumable);
        entity.ItemName = NormalizeRequired(dto.ItemName);
        entity.Specification = Normalize(dto.Specification);
        entity.MinimumStock = Math.Max(dto.MinimumStock, 0m);
        entity.MaximumStock = Math.Max(dto.MaximumStock, 0m);
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            _db.StockItemMasters.Add(entity);
        }

        await _db.SaveChangesAsync();
        await WriteInventoryAuditAsync(isNew ? "Created" : "Updated", "Stock Item", entity.StockItemMasterId, entity.ItemName, $"Stock item {entity.ItemName} was {(isNew ? "created" : "updated")}.");
        return (await GetItemsAsync()).First(x => x.StockItemMasterId == entity.StockItemMasterId);
    }

    public async Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync()
    {
        var suppliers = await _db.Suppliers
            .AsNoTracking()
            .OrderBy(x => x.SupplierName)
            .ToListAsync();
        var purchaseOrders = await _db.PurchaseOrders.AsNoTracking().ToListAsync();
        var invoices = await _db.PurchaseInvoices.AsNoTracking().ToListAsync();
        var returns = await _db.PurchaseReturns.AsNoTracking().ToListAsync();
        var dueBySupplier = (await GetSupplierDueSummaryAsync()).ToDictionary(x => x.SupplierId);

        return suppliers.Select(x => new SupplierDto
        {
            SupplierId = x.SupplierId,
            SupplierName = x.SupplierName,
            SupplierCode = x.SupplierCode,
            ContactPerson = x.ContactPerson,
            ContactPhone = x.ContactPhone,
            ContactEmail = x.ContactEmail,
            Address = x.Address,
            PaymentTermsDays = x.PaymentTermsDays,
            Notes = x.Notes,
            IsActive = x.IsActive,
            PurchaseOrderCount = purchaseOrders.Count(y => y.SupplierId == x.SupplierId),
            PurchaseInvoiceCount = invoices.Count(y => y.SupplierId == x.SupplierId),
            TotalPurchasedAmount = invoices.Where(y => y.SupplierId == x.SupplierId && y.Status != PurchaseInvoiceStatus.Cancelled).Sum(y => y.TotalAmount),
            DueAmount = dueBySupplier.TryGetValue(x.SupplierId, out var due) ? due.DueAmount : 0m,
            LastPurchaseDate = invoices.Where(y => y.SupplierId == x.SupplierId).OrderByDescending(y => y.InvoiceDate).Select(y => (DateTime?)y.InvoiceDate).FirstOrDefault()
        }).ToList();
    }

    public async Task<SupplierDto> SaveSupplierAsync(long? supplierId, SaveSupplierDto dto)
    {
        var entity = supplierId.HasValue
            ? await _db.Suppliers.FirstOrDefaultAsync(x => x.SupplierId == supplierId.Value)
            : null;

        var isNew = entity is null;
        entity ??= new Supplier { CreatedAt = DateTime.UtcNow };
        entity.SupplierName = NormalizeRequired(dto.SupplierName);
        entity.SupplierCode = NormalizeRequired(dto.SupplierCode).ToUpperInvariant();
        entity.ContactPerson = Normalize(dto.ContactPerson);
        entity.ContactPhone = Normalize(dto.ContactPhone);
        entity.ContactEmail = Normalize(dto.ContactEmail);
        entity.Address = Normalize(dto.Address);
        entity.PaymentTermsDays = Math.Max(dto.PaymentTermsDays, 0);
        entity.Notes = Normalize(dto.Notes);
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            _db.Suppliers.Add(entity);
        }

        await _db.SaveChangesAsync();
        await WriteInventoryAuditAsync(isNew ? "Created" : "Updated", "Supplier", entity.SupplierId, entity.SupplierName, $"Supplier {entity.SupplierName} was {(isNew ? "created" : "updated")}.");
        return (await GetSuppliersAsync()).First(x => x.SupplierId == entity.SupplierId);
    }

    public async Task<IReadOnlyList<StockLocationDto>> GetLocationsAsync()
    {
        var locations = await _db.StockLocations
            .AsNoTracking()
            .OrderBy(x => x.LocationType)
            .ThenBy(x => x.Name)
            .ToListAsync();
        var branches = await _db.Branches.AsNoTracking().ToDictionaryAsync(x => x.BranchId);

        return locations.Select(x => new StockLocationDto
            {
                StockLocationId = x.StockLocationId,
                BranchId = x.BranchId,
                BranchName = x.BranchId.HasValue && branches.TryGetValue(x.BranchId.Value, out var branch) ? branch.Name : "Unassigned",
                Name = x.Name,
                Code = x.Code,
                LocationType = x.LocationType.ToString(),
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToList();
    }

    public async Task<StockLocationDto> SaveLocationAsync(long? stockLocationId, SaveStockLocationDto dto)
    {
        if (dto.BranchId.HasValue && !await _db.Branches.AnyAsync(x => x.BranchId == dto.BranchId.Value))
        {
            throw new InvalidOperationException("Branch not found.");
        }

        var entity = stockLocationId.HasValue
            ? await _db.StockLocations.FirstOrDefaultAsync(x => x.StockLocationId == stockLocationId.Value)
            : null;

        var isNew = entity is null;
        entity ??= new StockLocation { CreatedAt = DateTime.UtcNow };
        entity.BranchId = dto.BranchId;
        entity.Name = NormalizeRequired(dto.Name);
        entity.Code = NormalizeRequired(dto.Code).ToUpperInvariant();
        entity.LocationType = ParseEnum(dto.LocationType, StockLocationType.MainStore);
        entity.Description = Normalize(dto.Description);
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            _db.StockLocations.Add(entity);
        }

        await _db.SaveChangesAsync();
        await WriteInventoryAuditAsync(isNew ? "Created" : "Updated", "Stock Location", entity.StockLocationId, entity.Name, $"Stock location {entity.Name} was {(isNew ? "created" : "updated")}.");
        return (await GetLocationsAsync()).First(x => x.StockLocationId == entity.StockLocationId);
    }

    public async Task<IReadOnlyList<StockBatchDto>> GetBatchesAsync() => await LoadBatchDetailsAsync();

    public async Task<IReadOnlyList<PurchaseOrderDto>> GetPurchaseOrdersAsync()
    {
        var headers = await _db.PurchaseOrders
            .AsNoTracking()
            .OrderByDescending(x => x.OrderDate)
            .ThenByDescending(x => x.PurchaseOrderId)
            .ToListAsync();
        var suppliers = await _db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.SupplierId);
        var locations = await _db.StockLocations.AsNoTracking().ToDictionaryAsync(x => x.StockLocationId);

        var lines = await _db.PurchaseOrderLines
            .AsNoTracking()
            .Where(x => headers.Select(h => h.PurchaseOrderId).Contains(x.PurchaseOrderId))
            .OrderBy(x => x.PurchaseOrderLineId)
            .ToListAsync();

        return headers.Select(header =>
        {
            var headerLines = lines.Where(x => x.PurchaseOrderId == header.PurchaseOrderId).ToList();
            return new PurchaseOrderDto
            {
                PurchaseOrderId = header.PurchaseOrderId,
                OrderNumber = header.OrderNumber,
                SupplierId = header.SupplierId,
                SupplierName = suppliers.TryGetValue(header.SupplierId, out var supplier) ? supplier.SupplierName : string.Empty,
                StockLocationId = header.StockLocationId,
                StockLocationName = locations.TryGetValue(header.StockLocationId, out var location) ? location.Name : string.Empty,
                OrderDate = header.OrderDate,
                ExpectedDeliveryDate = header.ExpectedDeliveryDate,
                Status = header.Status.ToString(),
                Notes = header.Notes,
                TotalAmount = headerLines.Sum(x => x.OrderedQuantity * x.UnitCost),
                OrderedQuantity = headerLines.Sum(x => x.OrderedQuantity),
                ReceivedQuantity = headerLines.Sum(x => x.ReceivedQuantity),
                Lines = headerLines.Select(MapPurchaseOrderLine).ToList()
            };
        }).ToList();
    }

    public async Task<PurchaseOrderDto> SavePurchaseOrderAsync(long? purchaseOrderId, SavePurchaseOrderDto dto)
    {
        if (!await _db.Suppliers.AnyAsync(x => x.SupplierId == dto.SupplierId))
        {
            throw new InvalidOperationException("Supplier not found.");
        }

        if (!await _db.StockLocations.AnyAsync(x => x.StockLocationId == dto.StockLocationId))
        {
            throw new InvalidOperationException("Stock location not found.");
        }

        var sanitizedLines = await BuildOrderLinesAsync(dto.Lines);
        if (sanitizedLines.Count == 0)
        {
            throw new InvalidOperationException("At least one purchase order line is required.");
        }

        var entity = purchaseOrderId.HasValue
            ? await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.PurchaseOrderId == purchaseOrderId.Value)
            : null;

        var isNew = entity is null;
        entity ??= new PurchaseOrder
        {
            OrderNumber = await NextDocumentNumberAsync(_db.PurchaseOrders.Select(x => x.OrderNumber), "PO"),
            CreatedAt = DateTime.UtcNow
        };

        if (!isNew && entity.Status == PurchaseOrderStatus.Received)
        {
            throw new InvalidOperationException("Received purchase orders cannot be edited.");
        }

        entity.SupplierId = dto.SupplierId;
        entity.StockLocationId = dto.StockLocationId;
        entity.OrderDate = dto.OrderDate.Date;
        entity.ExpectedDeliveryDate = dto.ExpectedDeliveryDate?.Date;
        entity.Status = ParseEnum(dto.Status, PurchaseOrderStatus.Ordered);
        entity.Notes = Normalize(dto.Notes);
        entity.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            _db.PurchaseOrders.Add(entity);
            await _db.SaveChangesAsync();
        }

        var existingLines = await _db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == entity.PurchaseOrderId).ToListAsync();
        var receivedLookup = existingLines.ToDictionary(x => x.PurchaseOrderLineId, x => x.ReceivedQuantity);
        _db.PurchaseOrderLines.RemoveRange(existingLines);
        await _db.SaveChangesAsync();

        foreach (var line in sanitizedLines)
        {
            _db.PurchaseOrderLines.Add(new PurchaseOrderLine
            {
                PurchaseOrderId = entity.PurchaseOrderId,
                MedicineMasterId = line.MedicineMasterId,
                StockItemMasterId = line.StockItemMasterId,
                ItemName = line.ItemName ?? string.Empty,
                UnitName = line.UnitName ?? string.Empty,
                OrderedQuantity = line.Quantity,
                ReceivedQuantity = line.ReferenceLineId.HasValue && receivedLookup.TryGetValue(line.ReferenceLineId.Value, out var received) ? received : 0m,
                UnitCost = line.UnitCost,
                Notes = line.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        await WriteInventoryAuditAsync(isNew ? "Created" : "Updated", "Purchase Order", entity.PurchaseOrderId, entity.OrderNumber, $"Purchase order {entity.OrderNumber} was {(isNew ? "created" : "updated")}.");
        return (await GetPurchaseOrdersAsync()).First(x => x.PurchaseOrderId == entity.PurchaseOrderId);
    }

    public async Task<PurchaseInvoiceDto> ReceivePurchaseOrderAsync(long purchaseOrderId, ReceivePurchaseOrderDto dto)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.PurchaseOrderId == purchaseOrderId);
        if (order is null)
        {
            throw new InvalidOperationException("Purchase order not found.");
        }

        var orderLines = await _db.PurchaseOrderLines.Where(x => x.PurchaseOrderId == purchaseOrderId).ToListAsync();
        if (orderLines.Count == 0)
        {
            throw new InvalidOperationException("Purchase order has no lines.");
        }

        var receiveLines = new List<(PurchaseOrderLine OrderLine, SaveInventoryLineDto Input)>();
        foreach (var line in dto.Lines ?? Array.Empty<SaveInventoryLineDto>())
        {
            if (line.Quantity <= 0m || !line.ReferenceLineId.HasValue)
            {
                continue;
            }

            var orderLine = orderLines.FirstOrDefault(x => x.PurchaseOrderLineId == line.ReferenceLineId.Value);
            if (orderLine is null)
            {
                throw new InvalidOperationException("A referenced purchase order line was not found.");
            }

            var remaining = Math.Max(orderLine.OrderedQuantity - orderLine.ReceivedQuantity, 0m);
            if (line.Quantity > remaining)
            {
                throw new InvalidOperationException($"Received quantity for {orderLine.ItemName} exceeds the remaining ordered quantity.");
            }

            var batchRequired = orderLine.MedicineMasterId.HasValue &&
                await _db.MedicineMasters.Where(x => x.MedicineMasterId == orderLine.MedicineMasterId.Value).Select(x => x.BatchRequired).FirstOrDefaultAsync();
            if (batchRequired && string.IsNullOrWhiteSpace(line.BatchNumber))
            {
                throw new InvalidOperationException($"Batch number is required for {orderLine.ItemName}.");
            }

            receiveLines.Add((orderLine, line));
        }

        if (receiveLines.Count == 0)
        {
            throw new InvalidOperationException("At least one received line is required.");
        }

        var invoice = new PurchaseInvoice
        {
            PurchaseOrderId = order.PurchaseOrderId,
            SupplierId = order.SupplierId,
            StockLocationId = order.StockLocationId,
            InvoiceNumber = NormalizeRequired(dto.InvoiceNumber),
            InvoiceDate = dto.InvoiceDate.Date,
            DueDate = dto.DueDate?.Date,
            PaidAmount = Math.Max(dto.PaidAmount, 0m),
            Status = PurchaseInvoiceStatus.Open,
            Notes = Normalize(dto.Notes),
            CreatedAt = DateTime.UtcNow
        };
        _db.PurchaseInvoices.Add(invoice);
        await _db.SaveChangesAsync();

        foreach (var (orderLine, input) in receiveLines)
        {
            var unitCost = input.UnitCost > 0m ? input.UnitCost : orderLine.UnitCost;
            var batchNumber = Normalize(input.BatchNumber);
            var expiryDate = input.ExpiryDate?.Date;

            _db.PurchaseInvoiceLines.Add(new PurchaseInvoiceLine
            {
                PurchaseInvoiceId = invoice.PurchaseInvoiceId,
                PurchaseOrderLineId = orderLine.PurchaseOrderLineId,
                MedicineMasterId = orderLine.MedicineMasterId,
                StockItemMasterId = orderLine.StockItemMasterId,
                ItemName = orderLine.ItemName,
                UnitName = orderLine.UnitName,
                BatchNumber = batchNumber,
                ExpiryDate = expiryDate,
                Quantity = input.Quantity,
                UnitCost = unitCost,
                LineTotal = input.Quantity * unitCost,
                Notes = Normalize(input.Notes),
                CreatedAt = DateTime.UtcNow
            });

            orderLine.ReceivedQuantity += input.Quantity;
            orderLine.UpdatedAt = DateTime.UtcNow;

            var batch = await FindBatchAsync(order.StockLocationId, orderLine.MedicineMasterId, orderLine.StockItemMasterId, batchNumber, expiryDate);
            if (batch is null)
            {
                _db.StockBatches.Add(new StockBatch
                {
                    StockLocationId = order.StockLocationId,
                    MedicineMasterId = orderLine.MedicineMasterId,
                    StockItemMasterId = orderLine.StockItemMasterId,
                    BatchNumber = batchNumber,
                    ExpiryDate = expiryDate,
                    QuantityOnHand = input.Quantity,
                    UnitCost = unitCost,
                    LastMovementAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                batch.QuantityOnHand += input.Quantity;
                batch.UnitCost = unitCost > 0m ? unitCost : batch.UnitCost;
                batch.LastMovementAt = DateTime.UtcNow;
                batch.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        invoice.TotalAmount = await _db.PurchaseInvoiceLines.Where(x => x.PurchaseInvoiceId == invoice.PurchaseInvoiceId).SumAsync(x => x.LineTotal);
        invoice.Status = invoice.PaidAmount <= 0m
            ? PurchaseInvoiceStatus.Open
            : invoice.PaidAmount >= invoice.TotalAmount
                ? PurchaseInvoiceStatus.Paid
                : PurchaseInvoiceStatus.Partial;
        invoice.UpdatedAt = DateTime.UtcNow;

        order.Status = orderLines.All(x => x.ReceivedQuantity >= x.OrderedQuantity)
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.PartiallyReceived;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await WriteInventoryAuditAsync("Received", "Purchase Invoice", invoice.PurchaseInvoiceId, invoice.InvoiceNumber, $"Stock receipt {invoice.InvoiceNumber} was recorded for purchase order {order.OrderNumber}.");
        return (await GetPurchaseInvoicesAsync()).First(x => x.PurchaseInvoiceId == invoice.PurchaseInvoiceId);
    }

    public async Task<IReadOnlyList<PurchaseInvoiceDto>> GetPurchaseInvoicesAsync()
    {
        var headers = await _db.PurchaseInvoices
            .AsNoTracking()
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.PurchaseInvoiceId)
            .ToListAsync();
        var orders = await _db.PurchaseOrders.AsNoTracking().ToDictionaryAsync(x => x.PurchaseOrderId);
        var suppliers = await _db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.SupplierId);
        var locations = await _db.StockLocations.AsNoTracking().ToDictionaryAsync(x => x.StockLocationId);
        var returns = await _db.PurchaseReturns.AsNoTracking().ToListAsync();

        var lines = await _db.PurchaseInvoiceLines
            .AsNoTracking()
            .Where(x => headers.Select(h => h.PurchaseInvoiceId).Contains(x.PurchaseInvoiceId))
            .OrderBy(x => x.PurchaseInvoiceLineId)
            .ToListAsync();

        return headers.Select(header =>
        {
            var headerLines = lines.Where(x => x.PurchaseInvoiceId == header.PurchaseInvoiceId).ToList();
            var returnAmount = returns.Where(x => x.PurchaseInvoiceId == header.PurchaseInvoiceId).Sum(x => x.TotalAmount);
            return new PurchaseInvoiceDto
            {
                PurchaseInvoiceId = header.PurchaseInvoiceId,
                PurchaseOrderId = header.PurchaseOrderId,
                OrderNumber = orders.TryGetValue(header.PurchaseOrderId, out var order) ? order.OrderNumber : string.Empty,
                SupplierId = header.SupplierId,
                SupplierName = suppliers.TryGetValue(header.SupplierId, out var supplier) ? supplier.SupplierName : string.Empty,
                StockLocationId = header.StockLocationId,
                StockLocationName = locations.TryGetValue(header.StockLocationId, out var location) ? location.Name : string.Empty,
                InvoiceNumber = header.InvoiceNumber,
                InvoiceDate = header.InvoiceDate,
                DueDate = header.DueDate,
                TotalAmount = header.TotalAmount,
                PaidAmount = header.PaidAmount,
                DueAmount = header.TotalAmount - header.PaidAmount - returnAmount,
                Status = header.Status.ToString(),
                Notes = header.Notes,
                Lines = headerLines.Select(MapPurchaseInvoiceLine).ToList()
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<PurchaseReturnDto>> GetPurchaseReturnsAsync()
    {
        var headers = await _db.PurchaseReturns
            .AsNoTracking()
            .OrderByDescending(x => x.ReturnDate)
            .ThenByDescending(x => x.PurchaseReturnId)
            .ToListAsync();
        var invoices = await _db.PurchaseInvoices.AsNoTracking().ToDictionaryAsync(x => x.PurchaseInvoiceId);
        var suppliers = await _db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.SupplierId);

        var lines = await _db.PurchaseReturnLines
            .AsNoTracking()
            .Where(x => headers.Select(h => h.PurchaseReturnId).Contains(x.PurchaseReturnId))
            .OrderBy(x => x.PurchaseReturnLineId)
            .ToListAsync();

        return headers.Select(header => new PurchaseReturnDto
        {
            PurchaseReturnId = header.PurchaseReturnId,
            PurchaseInvoiceId = header.PurchaseInvoiceId,
            InvoiceNumber = invoices.TryGetValue(header.PurchaseInvoiceId, out var invoice) ? invoice.InvoiceNumber : string.Empty,
            SupplierId = header.SupplierId,
            SupplierName = suppliers.TryGetValue(header.SupplierId, out var supplier) ? supplier.SupplierName : string.Empty,
            ReturnNumber = header.ReturnNumber,
            ReturnDate = header.ReturnDate,
            TotalAmount = header.TotalAmount,
            Notes = header.Notes,
            Lines = lines.Where(x => x.PurchaseReturnId == header.PurchaseReturnId).Select(MapPurchaseReturnLine).ToList()
        }).ToList();
    }

    public async Task<PurchaseReturnDto> SavePurchaseReturnAsync(SavePurchaseReturnDto dto)
    {
        var invoice = await _db.PurchaseInvoices.FirstOrDefaultAsync(x => x.PurchaseInvoiceId == dto.PurchaseInvoiceId);
        if (invoice is null)
        {
            throw new InvalidOperationException("Purchase invoice not found.");
        }

        var invoiceLines = await _db.PurchaseInvoiceLines.Where(x => x.PurchaseInvoiceId == dto.PurchaseInvoiceId).ToListAsync();
        var returnLines = new List<(PurchaseInvoiceLine InvoiceLine, SaveInventoryLineDto Input, StockBatch Batch)>();

        foreach (var line in dto.Lines ?? Array.Empty<SaveInventoryLineDto>())
        {
            if (line.Quantity <= 0m || !line.ReferenceLineId.HasValue)
            {
                continue;
            }

            var invoiceLine = invoiceLines.FirstOrDefault(x => x.PurchaseInvoiceLineId == line.ReferenceLineId.Value);
            if (invoiceLine is null)
            {
                throw new InvalidOperationException("A referenced purchase invoice line was not found.");
            }

            var batchNumber = Normalize(line.BatchNumber) ?? invoiceLine.BatchNumber;
            var expiryDate = line.ExpiryDate?.Date ?? invoiceLine.ExpiryDate?.Date;
            var batch = await FindBatchAsync(invoice.StockLocationId, invoiceLine.MedicineMasterId, invoiceLine.StockItemMasterId, batchNumber, expiryDate);
            if (batch is null || batch.QuantityOnHand < line.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock available for {invoiceLine.ItemName}.");
            }

            returnLines.Add((invoiceLine, line, batch));
        }

        if (returnLines.Count == 0)
        {
            throw new InvalidOperationException("At least one return line is required.");
        }

        var entity = new PurchaseReturn
        {
            PurchaseInvoiceId = invoice.PurchaseInvoiceId,
            SupplierId = invoice.SupplierId,
            ReturnNumber = await NextDocumentNumberAsync(_db.PurchaseReturns.Select(x => x.ReturnNumber), "PR"),
            ReturnDate = dto.ReturnDate.Date,
            Notes = Normalize(dto.Notes),
            CreatedAt = DateTime.UtcNow
        };
        _db.PurchaseReturns.Add(entity);
        await _db.SaveChangesAsync();

        foreach (var (invoiceLine, input, batch) in returnLines)
        {
            var unitCost = input.UnitCost > 0m ? input.UnitCost : invoiceLine.UnitCost;
            _db.PurchaseReturnLines.Add(new PurchaseReturnLine
            {
                PurchaseReturnId = entity.PurchaseReturnId,
                PurchaseInvoiceLineId = invoiceLine.PurchaseInvoiceLineId,
                MedicineMasterId = invoiceLine.MedicineMasterId,
                StockItemMasterId = invoiceLine.StockItemMasterId,
                ItemName = invoiceLine.ItemName,
                BatchNumber = Normalize(input.BatchNumber) ?? invoiceLine.BatchNumber,
                ExpiryDate = input.ExpiryDate?.Date ?? invoiceLine.ExpiryDate?.Date,
                Quantity = input.Quantity,
                UnitCost = unitCost,
                LineTotal = input.Quantity * unitCost,
                Reason = Normalize(input.Reason),
                CreatedAt = DateTime.UtcNow
            });

            batch.QuantityOnHand -= input.Quantity;
            batch.LastMovementAt = DateTime.UtcNow;
            batch.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        entity.TotalAmount = await _db.PurchaseReturnLines.Where(x => x.PurchaseReturnId == entity.PurchaseReturnId).SumAsync(x => x.LineTotal);
        await _db.SaveChangesAsync();

        await WriteInventoryAuditAsync("Created", "Purchase Return", entity.PurchaseReturnId, entity.ReturnNumber, $"Purchase return {entity.ReturnNumber} was recorded for invoice {invoice.InvoiceNumber}.");
        return (await GetPurchaseReturnsAsync()).First(x => x.PurchaseReturnId == entity.PurchaseReturnId);
    }

    public async Task<IReadOnlyList<SupplierDueSummaryDto>> GetSupplierDueSummaryAsync()
    {
        var suppliers = await _db.Suppliers.AsNoTracking().OrderBy(x => x.SupplierName).ToListAsync();
        var invoices = await _db.PurchaseInvoices.AsNoTracking().ToListAsync();
        var returns = await _db.PurchaseReturns.AsNoTracking().ToListAsync();

        return suppliers.Select(supplier =>
        {
            var supplierInvoices = invoices.Where(x => x.SupplierId == supplier.SupplierId && x.Status != PurchaseInvoiceStatus.Cancelled).ToList();
            var supplierReturnAmount = returns
                .Join(invoices.Where(x => x.SupplierId == supplier.SupplierId), x => x.PurchaseInvoiceId, x => x.PurchaseInvoiceId, (purchaseReturn, _) => purchaseReturn.TotalAmount)
                .Sum();

            var totalInvoiceAmount = supplierInvoices.Sum(x => x.TotalAmount);
            var paidAmount = supplierInvoices.Sum(x => x.PaidAmount);
            return new SupplierDueSummaryDto
            {
                SupplierId = supplier.SupplierId,
                SupplierName = supplier.SupplierName,
                SupplierCode = supplier.SupplierCode,
                OpenInvoiceCount = supplierInvoices.Count(x => x.TotalAmount - x.PaidAmount > 0m),
                TotalInvoiceAmount = totalInvoiceAmount,
                PaidAmount = paidAmount,
                ReturnAmount = supplierReturnAmount,
                DueAmount = totalInvoiceAmount - paidAmount - supplierReturnAmount
            };
        }).OrderByDescending(x => x.DueAmount).ThenBy(x => x.SupplierName).ToList();
    }

    public async Task<IReadOnlyList<StockTransferDto>> GetTransfersAsync()
    {
        var headers = await _db.StockTransfers
            .AsNoTracking()
            .OrderByDescending(x => x.TransferDate)
            .ThenByDescending(x => x.StockTransferId)
            .ToListAsync();
        var locations = await _db.StockLocations.AsNoTracking().ToDictionaryAsync(x => x.StockLocationId);

        var lines = await _db.StockTransferLines
            .AsNoTracking()
            .Where(x => headers.Select(h => h.StockTransferId).Contains(x.StockTransferId))
            .OrderBy(x => x.StockTransferLineId)
            .ToListAsync();

        return headers.Select(header =>
        {
            var headerLines = lines.Where(x => x.StockTransferId == header.StockTransferId).ToList();
            return new StockTransferDto
            {
                StockTransferId = header.StockTransferId,
                TransferNumber = header.TransferNumber,
                FromStockLocationId = header.FromStockLocationId,
                FromStockLocationName = locations.TryGetValue(header.FromStockLocationId, out var fromLocation) ? fromLocation.Name : string.Empty,
                ToStockLocationId = header.ToStockLocationId,
                ToStockLocationName = locations.TryGetValue(header.ToStockLocationId, out var toLocation) ? toLocation.Name : string.Empty,
                TransferDate = header.TransferDate,
                Status = header.Status.ToString(),
                Notes = header.Notes,
                ApprovedAt = header.ApprovedAt,
                ReceivedAt = header.ReceivedAt,
                TotalQuantity = headerLines.Sum(x => x.Quantity),
                Lines = headerLines.Select(MapTransferLine).ToList()
            };
        }).ToList();
    }

    public async Task<StockTransferDto> SaveTransferAsync(long? stockTransferId, SaveStockTransferDto dto)
    {
        if (dto.FromStockLocationId == dto.ToStockLocationId)
        {
            throw new InvalidOperationException("Source and destination locations must be different.");
        }

        var sanitizedLines = await BuildTransferLinesAsync(dto.FromStockLocationId, dto.Lines);
        if (sanitizedLines.Count == 0)
        {
            throw new InvalidOperationException("At least one transfer line is required.");
        }

        var entity = stockTransferId.HasValue
            ? await _db.StockTransfers.FirstOrDefaultAsync(x => x.StockTransferId == stockTransferId.Value)
            : null;

        var isNew = entity is null;
        entity ??= new StockTransfer
        {
            TransferNumber = await NextDocumentNumberAsync(_db.StockTransfers.Select(x => x.TransferNumber), "TR"),
            CreatedAt = DateTime.UtcNow
        };

        if (!isNew && entity.Status == TransferStatus.Received)
        {
            throw new InvalidOperationException("Received transfer cannot be edited.");
        }

        entity.FromStockLocationId = dto.FromStockLocationId;
        entity.ToStockLocationId = dto.ToStockLocationId;
        entity.TransferDate = dto.TransferDate.Date;
        entity.Status = TransferStatus.Pending;
        entity.Notes = Normalize(dto.Notes);
        entity.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            _db.StockTransfers.Add(entity);
            await _db.SaveChangesAsync();
        }

        var existingLines = await _db.StockTransferLines.Where(x => x.StockTransferId == entity.StockTransferId).ToListAsync();
        _db.StockTransferLines.RemoveRange(existingLines);
        await _db.SaveChangesAsync();

        foreach (var line in sanitizedLines)
        {
            _db.StockTransferLines.Add(new StockTransferLine
            {
                StockTransferId = entity.StockTransferId,
                StockBatchId = line.StockBatchId!.Value,
                ItemName = line.ItemName!,
                BatchNumber = line.BatchNumber,
                ExpiryDate = line.ExpiryDate?.Date,
                Quantity = line.Quantity,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        await WriteInventoryAuditAsync(isNew ? "Created" : "Updated", "Stock Transfer", entity.StockTransferId, entity.TransferNumber, $"Stock transfer {entity.TransferNumber} was {(isNew ? "created" : "updated")}.");
        return (await GetTransfersAsync()).First(x => x.StockTransferId == entity.StockTransferId);
    }

    public async Task<StockTransferDto> ApproveTransferAsync(long stockTransferId, ApproveStockTransferDto dto)
    {
        var entity = await _db.StockTransfers.FirstOrDefaultAsync(x => x.StockTransferId == stockTransferId);
        if (entity is null)
        {
            throw new InvalidOperationException("Stock transfer not found.");
        }

        entity.Status = TransferStatus.Approved;
        entity.Notes = Normalize(dto.Notes) ?? entity.Notes;
        entity.ApprovedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await WriteInventoryAuditAsync("Approved", "Stock Transfer", entity.StockTransferId, entity.TransferNumber, $"Stock transfer {entity.TransferNumber} was approved.");
        return (await GetTransfersAsync()).First(x => x.StockTransferId == stockTransferId);
    }

    public async Task<StockTransferDto> ReceiveTransferAsync(long stockTransferId, ReceiveStockTransferDto dto)
    {
        var transfer = await _db.StockTransfers.FirstOrDefaultAsync(x => x.StockTransferId == stockTransferId);
        if (transfer is null)
        {
            throw new InvalidOperationException("Stock transfer not found.");
        }

        var lines = await _db.StockTransferLines.Where(x => x.StockTransferId == stockTransferId).ToListAsync();
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Stock transfer has no lines.");
        }

        foreach (var line in lines)
        {
            var sourceBatch = await _db.StockBatches.FirstOrDefaultAsync(x => x.StockBatchId == line.StockBatchId);
            if (sourceBatch is null || sourceBatch.QuantityOnHand < line.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock available for transfer line {line.ItemName}.");
            }

            if (sourceBatch.ExpiryDate.HasValue && sourceBatch.ExpiryDate.Value.Date < DateTime.UtcNow.Date)
            {
                throw new InvalidOperationException($"Transfer batch {line.ItemName} is expired.");
            }

            sourceBatch.QuantityOnHand -= line.Quantity;
            sourceBatch.LastMovementAt = DateTime.UtcNow;
            sourceBatch.UpdatedAt = DateTime.UtcNow;

            var destinationBatch = await FindBatchAsync(
                transfer.ToStockLocationId,
                sourceBatch.MedicineMasterId,
                sourceBatch.StockItemMasterId,
                sourceBatch.BatchNumber,
                sourceBatch.ExpiryDate?.Date);

            if (destinationBatch is null)
            {
                _db.StockBatches.Add(new StockBatch
                {
                    StockLocationId = transfer.ToStockLocationId,
                    MedicineMasterId = sourceBatch.MedicineMasterId,
                    StockItemMasterId = sourceBatch.StockItemMasterId,
                    BatchNumber = sourceBatch.BatchNumber,
                    ExpiryDate = sourceBatch.ExpiryDate?.Date,
                    QuantityOnHand = line.Quantity,
                    UnitCost = sourceBatch.UnitCost,
                    LastMovementAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                destinationBatch.QuantityOnHand += line.Quantity;
                destinationBatch.UnitCost = sourceBatch.UnitCost;
                destinationBatch.LastMovementAt = DateTime.UtcNow;
                destinationBatch.UpdatedAt = DateTime.UtcNow;
            }
        }

        transfer.Status = TransferStatus.Received;
        transfer.Notes = Normalize(dto.Notes) ?? transfer.Notes;
        transfer.ApprovedAt ??= DateTime.UtcNow;
        transfer.ReceivedAt = DateTime.UtcNow;
        transfer.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await WriteInventoryAuditAsync("Received", "Stock Transfer", transfer.StockTransferId, transfer.TransferNumber, $"Stock transfer {transfer.TransferNumber} was received.");
        return (await GetTransfersAsync()).First(x => x.StockTransferId == stockTransferId);
    }

    public async Task<IReadOnlyList<StockAdjustmentDto>> GetAdjustmentsAsync()
    {
        var headers = await _db.StockAdjustments
            .AsNoTracking()
            .OrderByDescending(x => x.AdjustmentDate)
            .ThenByDescending(x => x.StockAdjustmentId)
            .ToListAsync();
        var locations = await _db.StockLocations.AsNoTracking().ToDictionaryAsync(x => x.StockLocationId);

        var lines = await _db.StockAdjustmentLines
            .AsNoTracking()
            .Where(x => headers.Select(h => h.StockAdjustmentId).Contains(x.StockAdjustmentId))
            .OrderBy(x => x.StockAdjustmentLineId)
            .ToListAsync();

        return headers.Select(header =>
        {
            var headerLines = lines.Where(x => x.StockAdjustmentId == header.StockAdjustmentId).ToList();
            return new StockAdjustmentDto
            {
                StockAdjustmentId = header.StockAdjustmentId,
                AdjustmentNumber = header.AdjustmentNumber,
                StockLocationId = header.StockLocationId,
                StockLocationName = locations.TryGetValue(header.StockLocationId, out var location) ? location.Name : string.Empty,
                Reason = header.Reason.ToString(),
                Status = header.Status.ToString(),
                AdjustmentDate = header.AdjustmentDate,
                Notes = header.Notes,
                ApprovedAt = header.ApprovedAt,
                TotalDelta = headerLines.Sum(x => x.QuantityDelta),
                Lines = headerLines.Select(MapAdjustmentLine).ToList()
            };
        }).ToList();
    }

    public async Task<StockAdjustmentDto> SaveAdjustmentAsync(long? stockAdjustmentId, SaveStockAdjustmentDto dto)
    {
        if (!await _db.StockLocations.AnyAsync(x => x.StockLocationId == dto.StockLocationId))
        {
            throw new InvalidOperationException("Stock location not found.");
        }

        var sanitizedLines = await BuildAdjustmentLinesAsync(dto.StockLocationId, dto.Lines);
        if (sanitizedLines.Count == 0)
        {
            throw new InvalidOperationException("At least one adjustment line is required.");
        }

        var entity = stockAdjustmentId.HasValue
            ? await _db.StockAdjustments.FirstOrDefaultAsync(x => x.StockAdjustmentId == stockAdjustmentId.Value)
            : null;

        var isNew = entity is null;
        entity ??= new StockAdjustment
        {
            AdjustmentNumber = await NextDocumentNumberAsync(_db.StockAdjustments.Select(x => x.AdjustmentNumber), "ADJ"),
            CreatedAt = DateTime.UtcNow
        };

        if (!isNew && entity.Status == ApprovalStatus.Approved)
        {
            throw new InvalidOperationException("Approved adjustment cannot be edited.");
        }

        entity.StockLocationId = dto.StockLocationId;
        entity.Reason = ParseEnum(dto.Reason, StockAdjustmentReason.ManualCorrection);
        entity.Status = ApprovalStatus.Pending;
        entity.AdjustmentDate = dto.AdjustmentDate.Date;
        entity.Notes = Normalize(dto.Notes);
        entity.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            _db.StockAdjustments.Add(entity);
            await _db.SaveChangesAsync();
        }

        var existingLines = await _db.StockAdjustmentLines.Where(x => x.StockAdjustmentId == entity.StockAdjustmentId).ToListAsync();
        _db.StockAdjustmentLines.RemoveRange(existingLines);
        await _db.SaveChangesAsync();

        foreach (var line in sanitizedLines)
        {
            _db.StockAdjustmentLines.Add(new StockAdjustmentLine
            {
                StockAdjustmentId = entity.StockAdjustmentId,
                StockBatchId = line.StockBatchId!.Value,
                ItemName = line.ItemName!,
                BatchNumber = line.BatchNumber,
                ExpiryDate = line.ExpiryDate?.Date,
                QuantityDelta = line.Quantity,
                Notes = line.Notes,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        await WriteInventoryAuditAsync(isNew ? "Created" : "Updated", "Stock Adjustment", entity.StockAdjustmentId, entity.AdjustmentNumber, $"Stock adjustment {entity.AdjustmentNumber} was {(isNew ? "created" : "updated")}.");
        return (await GetAdjustmentsAsync()).First(x => x.StockAdjustmentId == entity.StockAdjustmentId);
    }

    public async Task<StockAdjustmentDto> ApproveAdjustmentAsync(long stockAdjustmentId, ApproveStockAdjustmentDto dto)
    {
        var adjustment = await _db.StockAdjustments.FirstOrDefaultAsync(x => x.StockAdjustmentId == stockAdjustmentId);
        if (adjustment is null)
        {
            throw new InvalidOperationException("Stock adjustment not found.");
        }

        var lines = await _db.StockAdjustmentLines.Where(x => x.StockAdjustmentId == stockAdjustmentId).ToListAsync();
        foreach (var line in lines)
        {
            var batch = await _db.StockBatches.FirstOrDefaultAsync(x => x.StockBatchId == line.StockBatchId);
            if (batch is null)
            {
                throw new InvalidOperationException($"Stock batch for {line.ItemName} was not found.");
            }

            if (batch.QuantityOnHand + line.QuantityDelta < 0m)
            {
                throw new InvalidOperationException($"Adjustment for {line.ItemName} would result in negative stock.");
            }

            batch.QuantityOnHand += line.QuantityDelta;
            batch.LastMovementAt = DateTime.UtcNow;
            batch.UpdatedAt = DateTime.UtcNow;
        }

        adjustment.Status = ApprovalStatus.Approved;
        adjustment.Notes = Normalize(dto.Notes) ?? adjustment.Notes;
        adjustment.ApprovedAt = DateTime.UtcNow;
        adjustment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await WriteInventoryAuditAsync("Approved", "Stock Adjustment", adjustment.StockAdjustmentId, adjustment.AdjustmentNumber, $"Stock adjustment {adjustment.AdjustmentNumber} was approved.");
        return (await GetAdjustmentsAsync()).First(x => x.StockAdjustmentId == stockAdjustmentId);
    }

    private async Task<List<StockBatchDto>> LoadBatchDetailsAsync()
    {
        var batches = await _db.StockBatches
            .AsNoTracking()
            .OrderBy(x => x.StockBatchId)
            .ToListAsync();
        var locations = await _db.StockLocations.AsNoTracking().ToDictionaryAsync(x => x.StockLocationId);
        var medicines = await _db.MedicineMasters.AsNoTracking().ToDictionaryAsync(x => x.MedicineMasterId);
        var items = await _db.StockItemMasters.AsNoTracking().ToDictionaryAsync(x => x.StockItemMasterId);
        var units = await _db.InventoryUnits.AsNoTracking().ToDictionaryAsync(x => x.InventoryUnitId);
        var categories = await _db.InventoryCategories.AsNoTracking().ToDictionaryAsync(x => x.InventoryCategoryId);

        var today = DateTime.UtcNow.Date;
        return batches.Select(batch =>
        {
            var medicine = batch.MedicineMasterId.HasValue && medicines.TryGetValue(batch.MedicineMasterId.Value, out var medicineRow)
                ? medicineRow
                : null;
            var item = batch.StockItemMasterId.HasValue && items.TryGetValue(batch.StockItemMasterId.Value, out var itemRow)
                ? itemRow
                : null;
            var itemType = medicine is not null ? "Medicine" : item?.ItemType.ToString() ?? "Item";
            var itemName = medicine?.MedicineName ?? item?.ItemName ?? "Unknown item";
            var unitId = medicine?.InventoryUnitId ?? item?.InventoryUnitId;
            var categoryId = medicine?.InventoryCategoryId ?? item?.InventoryCategoryId;
            var unitName = unitId.HasValue && units.TryGetValue(unitId.Value, out var unit) ? unit.Name : string.Empty;
            var categoryName = categoryId.HasValue && categories.TryGetValue(categoryId.Value, out var category) ? category.Name : null;
            var minimumStock = medicine?.MinimumStock ?? item?.MinimumStock ?? 0m;
            var maximumStock = medicine?.MaximumStock ?? item?.MaximumStock ?? 0m;
            locations.TryGetValue(batch.StockLocationId, out var location);

            return new StockBatchDto
            {
                StockBatchId = batch.StockBatchId,
                StockLocationId = batch.StockLocationId,
                LocationName = location?.Name ?? string.Empty,
                ItemType = itemType,
                MedicineMasterId = batch.MedicineMasterId,
                StockItemMasterId = batch.StockItemMasterId,
                ItemName = itemName,
                UnitName = unitName,
                CategoryName = categoryName,
                BatchNumber = batch.BatchNumber,
                ExpiryDate = batch.ExpiryDate,
                QuantityOnHand = batch.QuantityOnHand,
                UnitCost = batch.UnitCost,
                MinimumStock = minimumStock,
                MaximumStock = maximumStock,
                IsNearExpiry = batch.ExpiryDate.HasValue && batch.ExpiryDate.Value.Date >= today && batch.ExpiryDate.Value.Date <= today.AddDays(30),
                IsExpiredLocked = batch.ExpiryDate.HasValue && batch.ExpiryDate.Value.Date < today
            };
        }).ToList();
    }

    private static InventoryAlertDto MapInventoryAlert(StockBatchDto batch) => new()
    {
        StockBatchId = batch.StockBatchId,
        ItemType = batch.ItemType,
        ItemName = batch.ItemName,
        UnitName = batch.UnitName,
        LocationName = batch.LocationName,
        BatchNumber = batch.BatchNumber,
        ExpiryDate = batch.ExpiryDate,
        QuantityOnHand = batch.QuantityOnHand,
        MinimumStock = batch.MinimumStock,
        IsExpiredLocked = batch.IsExpiredLocked
    };

    private async Task<List<SaveInventoryLineDto>> BuildOrderLinesAsync(IEnumerable<SaveInventoryLineDto> lines)
    {
        var result = new List<SaveInventoryLineDto>();
        foreach (var line in lines ?? Array.Empty<SaveInventoryLineDto>())
        {
            if (line.Quantity <= 0m)
            {
                continue;
            }

            var (itemName, unitName) = await ResolveInventoryIdentityAsync(line.MedicineMasterId, line.StockItemMasterId);
            result.Add(new SaveInventoryLineDto
            {
                ReferenceLineId = line.ReferenceLineId,
                MedicineMasterId = line.MedicineMasterId,
                StockItemMasterId = line.StockItemMasterId,
                ItemName = itemName,
                UnitName = unitName,
                Quantity = line.Quantity,
                UnitCost = Math.Max(line.UnitCost, 0m),
                Notes = Normalize(line.Notes)
            });
        }

        return result;
    }

    private async Task<List<SaveInventoryLineDto>> BuildTransferLinesAsync(long fromStockLocationId, IEnumerable<SaveInventoryLineDto> lines)
    {
        var result = new List<SaveInventoryLineDto>();
        foreach (var line in lines ?? Array.Empty<SaveInventoryLineDto>())
        {
            if (line.Quantity <= 0m || !line.StockBatchId.HasValue)
            {
                continue;
            }

            var batch = await _db.StockBatches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.StockBatchId == line.StockBatchId.Value);

            if (batch is null || batch.StockLocationId != fromStockLocationId)
            {
                throw new InvalidOperationException("One or more transfer lines reference an invalid source batch.");
            }

            var itemName = await ResolveBatchItemNameAsync(batch);

            if (batch.QuantityOnHand < line.Quantity)
            {
                throw new InvalidOperationException($"Transfer quantity for {itemName} exceeds available stock.");
            }

            if (batch.ExpiryDate.HasValue && batch.ExpiryDate.Value.Date < DateTime.UtcNow.Date)
            {
                throw new InvalidOperationException("Expired batches cannot be transferred.");
            }

            result.Add(new SaveInventoryLineDto
            {
                StockBatchId = batch.StockBatchId,
                ItemName = itemName,
                BatchNumber = batch.BatchNumber,
                ExpiryDate = batch.ExpiryDate?.Date,
                Quantity = line.Quantity
            });
        }

        return result;
    }

    private async Task<List<SaveInventoryLineDto>> BuildAdjustmentLinesAsync(long stockLocationId, IEnumerable<SaveInventoryLineDto> lines)
    {
        var result = new List<SaveInventoryLineDto>();
        foreach (var line in lines ?? Array.Empty<SaveInventoryLineDto>())
        {
            if (line.Quantity == 0m || !line.StockBatchId.HasValue)
            {
                continue;
            }

            var batch = await _db.StockBatches
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.StockBatchId == line.StockBatchId.Value);

            if (batch is null || batch.StockLocationId != stockLocationId)
            {
                throw new InvalidOperationException("One or more adjustment lines reference an invalid stock batch.");
            }

            result.Add(new SaveInventoryLineDto
            {
                StockBatchId = batch.StockBatchId,
                ItemName = await ResolveBatchItemNameAsync(batch),
                BatchNumber = batch.BatchNumber,
                ExpiryDate = batch.ExpiryDate?.Date,
                Quantity = line.Quantity,
                Notes = Normalize(line.Notes)
            });
        }

        return result;
    }

    private async Task<(string ItemName, string UnitName)> ResolveInventoryIdentityAsync(long? medicineMasterId, long? stockItemMasterId)
    {
        if (medicineMasterId.HasValue)
        {
            var medicine = await _db.MedicineMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MedicineMasterId == medicineMasterId.Value);

            if (medicine is null)
            {
                throw new InvalidOperationException("Medicine master not found.");
            }

            var unitName = await _db.InventoryUnits
                .AsNoTracking()
                .Where(x => x.InventoryUnitId == medicine.InventoryUnitId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();

            return (medicine.MedicineName, unitName ?? string.Empty);
        }

        if (stockItemMasterId.HasValue)
        {
            var item = await _db.StockItemMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.StockItemMasterId == stockItemMasterId.Value);

            if (item is null)
            {
                throw new InvalidOperationException("Stock item master not found.");
            }

            var unitName = await _db.InventoryUnits
                .AsNoTracking()
                .Where(x => x.InventoryUnitId == item.InventoryUnitId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();

            return (item.ItemName, unitName ?? string.Empty);
        }

        throw new InvalidOperationException("A line item must reference either a medicine or stock item.");
    }

    private async Task<string> ResolveBatchItemNameAsync(StockBatch batch)
    {
        if (batch.MedicineMasterId.HasValue)
        {
            return await _db.MedicineMasters
                .AsNoTracking()
                .Where(x => x.MedicineMasterId == batch.MedicineMasterId.Value)
                .Select(x => x.MedicineName)
                .FirstOrDefaultAsync() ?? "item";
        }

        if (batch.StockItemMasterId.HasValue)
        {
            return await _db.StockItemMasters
                .AsNoTracking()
                .Where(x => x.StockItemMasterId == batch.StockItemMasterId.Value)
                .Select(x => x.ItemName)
                .FirstOrDefaultAsync() ?? "item";
        }

        return "item";
    }

    private async Task<StockBatch?> FindBatchAsync(long stockLocationId, long? medicineMasterId, long? stockItemMasterId, string? batchNumber, DateTime? expiryDate)
    {
        return await _db.StockBatches.FirstOrDefaultAsync(x =>
            x.StockLocationId == stockLocationId &&
            x.MedicineMasterId == medicineMasterId &&
            x.StockItemMasterId == stockItemMasterId &&
            x.BatchNumber == batchNumber &&
            x.ExpiryDate == expiryDate);
    }

    private async Task<string> NextDocumentNumberAsync(IQueryable<string> source, string prefix)
    {
        var numbers = await source.Where(x => x.StartsWith(prefix + "-")).ToListAsync();
        var next = numbers
            .Select(x => x.Split('-', StringSplitOptions.RemoveEmptyEntries).LastOrDefault())
            .Select(x => int.TryParse(x, out var parsed) ? parsed : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefix}-{next:D6}";
    }

    private InventoryLineDto MapPurchaseOrderLine(PurchaseOrderLine line) => new()
    {
        ReferenceLineId = line.PurchaseOrderLineId,
        MedicineMasterId = line.MedicineMasterId,
        StockItemMasterId = line.StockItemMasterId,
        ItemType = line.MedicineMasterId.HasValue ? "Medicine" : "Item",
        ItemName = line.ItemName,
        UnitName = line.UnitName ?? string.Empty,
        Quantity = line.OrderedQuantity,
        ReceivedQuantity = line.ReceivedQuantity,
        UnitCost = line.UnitCost,
        LineTotal = line.OrderedQuantity * line.UnitCost,
        Notes = line.Notes
    };

    private InventoryLineDto MapPurchaseInvoiceLine(PurchaseInvoiceLine line) => new()
    {
        ReferenceLineId = line.PurchaseInvoiceLineId,
        MedicineMasterId = line.MedicineMasterId,
        StockItemMasterId = line.StockItemMasterId,
        ItemType = line.MedicineMasterId.HasValue ? "Medicine" : "Item",
        ItemName = line.ItemName,
        UnitName = line.UnitName ?? string.Empty,
        BatchNumber = line.BatchNumber,
        ExpiryDate = line.ExpiryDate,
        Quantity = line.Quantity,
        UnitCost = line.UnitCost,
        LineTotal = line.LineTotal,
        Notes = line.Notes
    };

    private InventoryLineDto MapPurchaseReturnLine(PurchaseReturnLine line) => new()
    {
        ReferenceLineId = line.PurchaseInvoiceLineId ?? line.PurchaseReturnLineId,
        MedicineMasterId = line.MedicineMasterId,
        StockItemMasterId = line.StockItemMasterId,
        ItemType = line.MedicineMasterId.HasValue ? "Medicine" : "Item",
        ItemName = line.ItemName,
        BatchNumber = line.BatchNumber,
        ExpiryDate = line.ExpiryDate,
        Quantity = line.Quantity,
        UnitCost = line.UnitCost,
        LineTotal = line.LineTotal,
        Reason = line.Reason
    };

    private InventoryLineDto MapTransferLine(StockTransferLine line) => new()
    {
        ReferenceLineId = line.StockTransferLineId,
        StockBatchId = line.StockBatchId,
        ItemName = line.ItemName,
        BatchNumber = line.BatchNumber,
        ExpiryDate = line.ExpiryDate,
        Quantity = line.Quantity
    };

    private InventoryLineDto MapAdjustmentLine(StockAdjustmentLine line) => new()
    {
        ReferenceLineId = line.StockAdjustmentLineId,
        StockBatchId = line.StockBatchId,
        ItemName = line.ItemName,
        BatchNumber = line.BatchNumber,
        ExpiryDate = line.ExpiryDate,
        Quantity = line.QuantityDelta,
        Notes = line.Notes
    };

    private async Task WriteInventoryAuditAsync(string action, string entityName, long entityId, string targetDisplayName, string summary)
    {
        await _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Inventory,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            TargetDisplayName = targetDisplayName,
            Summary = summary
        });
    }

    private static string NormalizeRequired(string? value)
    {
        var normalized = Normalize(value);
        return !string.IsNullOrWhiteSpace(normalized)
            ? normalized
            : throw new InvalidOperationException("A required field was not provided.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
}
