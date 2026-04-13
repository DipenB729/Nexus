using System.Data;
using AngularApp4.Dtos.Hms;
using AngularApp4.Model.Hms;
using Microsoft.Data.SqlClient;

namespace AngularApp4.Services.Hms;

public class InventoryAdminService : IInventoryAdminService
{
    private readonly string _connectionString;
    private readonly IAuditLogService _audit;

    public InventoryAdminService(IConfiguration configuration, IAuditLogService audit)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Default database connection is not configured.");
        _audit = audit;
    }

    public async Task<InventoryDashboardDto> GetDashboardAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = CreateCommand(connection, "dbo.usp_Inventory_GetDashboard");
        await using var reader = await command.ExecuteReaderAsync();

        var dashboard = new InventoryDashboardDto();
        if (await reader.ReadAsync())
        {
            dashboard = MapDashboardHeader(reader);
        }

        var lowStockAlerts = new List<InventoryAlertDto>();
        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                lowStockAlerts.Add(MapInventoryAlert(reader));
            }
        }

        var nearExpiryAlerts = new List<InventoryAlertDto>();
        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                nearExpiryAlerts.Add(MapInventoryAlert(reader));
            }
        }

        var supplierDues = new List<SupplierDueSummaryDto>();
        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                supplierDues.Add(MapSupplierDueSummary(reader));
            }
        }

        dashboard.LowStockAlerts = lowStockAlerts;
        dashboard.NearExpiryAlerts = nearExpiryAlerts;
        dashboard.SupplierDues = supplierDues;
        return dashboard;
    }

    public async Task<IReadOnlyList<InventoryUnitDto>> GetUnitsAsync() =>
        await QueryListAsync("dbo.usp_Inventory_GetUnits", null, MapInventoryUnit);

    public async Task<InventoryUnitDto> SaveUnitAsync(long? inventoryUnitId, SaveInventoryUnitDto dto)
    {
        var id = await ExecuteSaveAsync("dbo.usp_Inventory_SaveUnit", "@InventoryUnitId", command =>
        {
            AddInputOutputId(command, "@InventoryUnitId", inventoryUnitId);
            AddParameter(command, "@Name", NormalizeRequired(dto.Name));
            AddParameter(command, "@ShortName", Normalize(dto.ShortName));
            AddParameter(command, "@Description", Normalize(dto.Description));
            AddParameter(command, "@IsActive", dto.IsActive);
        });

        var result = await FindRequiredAsync(GetUnitsAsync, x => x.InventoryUnitId == id, "Inventory unit not found after save.");
        await WriteInventoryAuditAsync(
            inventoryUnitId.HasValue ? "Updated" : "Created",
            "Inventory Unit",
            result.InventoryUnitId,
            result.Name,
            $"Inventory unit {result.Name} was {(inventoryUnitId.HasValue ? "updated" : "created")}.");
        return result;
    }

    public async Task<IReadOnlyList<InventoryCategoryDto>> GetCategoriesAsync() =>
        await QueryListAsync("dbo.usp_Inventory_GetCategories", null, MapInventoryCategory);

    public async Task<InventoryCategoryDto> SaveCategoryAsync(long? inventoryCategoryId, SaveInventoryCategoryDto dto)
    {
        var id = await ExecuteSaveAsync("dbo.usp_Inventory_SaveCategory", "@InventoryCategoryId", command =>
        {
            AddInputOutputId(command, "@InventoryCategoryId", inventoryCategoryId);
            AddParameter(command, "@CategoryType", ParseEnum(dto.CategoryType, InventoryCategoryType.Item).ToString());
            AddParameter(command, "@Name", NormalizeRequired(dto.Name));
            AddParameter(command, "@Description", Normalize(dto.Description));
            AddParameter(command, "@IsActive", dto.IsActive);
        });

        var result = await FindRequiredAsync(GetCategoriesAsync, x => x.InventoryCategoryId == id, "Inventory category not found after save.");
        await WriteInventoryAuditAsync(
            inventoryCategoryId.HasValue ? "Updated" : "Created",
            "Inventory Category",
            result.InventoryCategoryId,
            result.Name,
            $"Inventory category {result.Name} was {(inventoryCategoryId.HasValue ? "updated" : "created")}.");
        return result;
    }

    public async Task<IReadOnlyList<MedicineMasterDto>> GetMedicinesAsync() =>
        await QueryListAsync("dbo.usp_Inventory_GetMedicines", null, MapMedicine);

    public async Task<MedicineMasterDto> SaveMedicineAsync(long? medicineMasterId, SaveMedicineMasterDto dto)
    {
        var id = await ExecuteSaveAsync("dbo.usp_Inventory_SaveMedicine", "@MedicineMasterId", command =>
        {
            AddInputOutputId(command, "@MedicineMasterId", medicineMasterId);
            AddParameter(command, "@MedicineName", NormalizeRequired(dto.MedicineName));
            AddParameter(command, "@GenericName", Normalize(dto.GenericName));
            AddParameter(command, "@Brand", Normalize(dto.Brand));
            AddParameter(command, "@InventoryUnitId", dto.InventoryUnitId);
            AddParameter(command, "@InventoryCategoryId", ToDbValue(dto.InventoryCategoryId));
            AddParameter(command, "@Strength", Normalize(dto.Strength));
            AddParameter(command, "@BatchRequired", dto.BatchRequired);
            AddParameter(command, "@MinimumStock", Math.Max(dto.MinimumStock, 0m));
            AddParameter(command, "@MaximumStock", Math.Max(dto.MaximumStock, 0m));
            AddParameter(command, "@IsActive", dto.IsActive);
        });

        var result = await FindRequiredAsync(GetMedicinesAsync, x => x.MedicineMasterId == id, "Medicine master not found after save.");
        await WriteInventoryAuditAsync(
            medicineMasterId.HasValue ? "Updated" : "Created",
            "Medicine Master",
            result.MedicineMasterId,
            result.MedicineName,
            $"Medicine master {result.MedicineName} was {(medicineMasterId.HasValue ? "updated" : "created")}.");
        return result;
    }

    public async Task<IReadOnlyList<StockItemMasterDto>> GetItemsAsync() =>
        await QueryListAsync("dbo.usp_Inventory_GetItems", null, MapItem);

    public async Task<StockItemMasterDto> SaveItemAsync(long? stockItemMasterId, SaveStockItemMasterDto dto)
    {
        var id = await ExecuteSaveAsync("dbo.usp_Inventory_SaveItem", "@StockItemMasterId", command =>
        {
            AddInputOutputId(command, "@StockItemMasterId", stockItemMasterId);
            AddParameter(command, "@ItemType", ParseEnum(dto.ItemType, InventoryItemType.Consumable).ToString());
            AddParameter(command, "@ItemName", NormalizeRequired(dto.ItemName));
            AddParameter(command, "@Specification", Normalize(dto.Specification));
            AddParameter(command, "@InventoryUnitId", dto.InventoryUnitId);
            AddParameter(command, "@InventoryCategoryId", ToDbValue(dto.InventoryCategoryId));
            AddParameter(command, "@MinimumStock", Math.Max(dto.MinimumStock, 0m));
            AddParameter(command, "@MaximumStock", Math.Max(dto.MaximumStock, 0m));
            AddParameter(command, "@IsActive", dto.IsActive);
        });

        var result = await FindRequiredAsync(GetItemsAsync, x => x.StockItemMasterId == id, "Stock item master not found after save.");
        await WriteInventoryAuditAsync(
            stockItemMasterId.HasValue ? "Updated" : "Created",
            "Stock Item",
            result.StockItemMasterId,
            result.ItemName,
            $"Stock item {result.ItemName} was {(stockItemMasterId.HasValue ? "updated" : "created")}.");
        return result;
    }

    public async Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync() =>
        await QueryListAsync("dbo.usp_Inventory_GetSuppliers", null, MapSupplier);

    public async Task<SupplierDto> SaveSupplierAsync(long? supplierId, SaveSupplierDto dto)
    {
        var id = await ExecuteSaveAsync("dbo.usp_Inventory_SaveSupplier", "@SupplierId", command =>
        {
            AddInputOutputId(command, "@SupplierId", supplierId);
            AddParameter(command, "@SupplierName", NormalizeRequired(dto.SupplierName));
            AddParameter(command, "@SupplierCode", NormalizeRequired(dto.SupplierCode).ToUpperInvariant());
            AddParameter(command, "@ContactPerson", Normalize(dto.ContactPerson));
            AddParameter(command, "@ContactPhone", Normalize(dto.ContactPhone));
            AddParameter(command, "@ContactEmail", Normalize(dto.ContactEmail));
            AddParameter(command, "@Address", Normalize(dto.Address));
            AddParameter(command, "@PaymentTermsDays", Math.Max(dto.PaymentTermsDays, 0));
            AddParameter(command, "@Notes", Normalize(dto.Notes));
            AddParameter(command, "@IsActive", dto.IsActive);
        });

        var result = await FindRequiredAsync(GetSuppliersAsync, x => x.SupplierId == id, "Supplier not found after save.");
        await WriteInventoryAuditAsync(
            supplierId.HasValue ? "Updated" : "Created",
            "Supplier",
            result.SupplierId,
            result.SupplierName,
            $"Supplier {result.SupplierName} was {(supplierId.HasValue ? "updated" : "created")}.");
        return result;
    }

    public async Task<IReadOnlyList<StockLocationDto>> GetLocationsAsync() =>
        await QueryListAsync("dbo.usp_Inventory_GetLocations", null, MapLocation);

    public async Task<StockLocationDto> SaveLocationAsync(long? stockLocationId, SaveStockLocationDto dto)
    {
        var id = await ExecuteSaveAsync("dbo.usp_Inventory_SaveLocation", "@StockLocationId", command =>
        {
            AddInputOutputId(command, "@StockLocationId", stockLocationId);
            AddParameter(command, "@BranchId", ToDbValue(dto.BranchId));
            AddParameter(command, "@Name", NormalizeRequired(dto.Name));
            AddParameter(command, "@Code", NormalizeRequired(dto.Code).ToUpperInvariant());
            AddParameter(command, "@LocationType", ParseEnum(dto.LocationType, StockLocationType.MainStore).ToString());
            AddParameter(command, "@Description", Normalize(dto.Description));
            AddParameter(command, "@IsActive", dto.IsActive);
        });

        var result = await FindRequiredAsync(GetLocationsAsync, x => x.StockLocationId == id, "Stock location not found after save.");
        await WriteInventoryAuditAsync(
            stockLocationId.HasValue ? "Updated" : "Created",
            "Stock Location",
            result.StockLocationId,
            result.Name,
            $"Stock location {result.Name} was {(stockLocationId.HasValue ? "updated" : "created")}.");
        return result;
    }

    public async Task<IReadOnlyList<StockBatchDto>> GetBatchesAsync() =>
        await QueryListAsync("dbo.usp_Inventory_GetBatches", null, MapBatch);

    public async Task<IReadOnlyList<PurchaseOrderDto>> GetPurchaseOrdersAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = CreateCommand(connection, "dbo.usp_Inventory_GetPurchaseOrders");
        await using var reader = await command.ExecuteReaderAsync();
        return await ReadHeaderWithLinesAsync(reader, "PurchaseOrderId", MapPurchaseOrderHeader);
    }

    public async Task<PurchaseOrderDto> SavePurchaseOrderAsync(long? purchaseOrderId, SavePurchaseOrderDto dto)
    {
        var id = await ExecuteSaveAsync("dbo.usp_Inventory_SavePurchaseOrder", "@PurchaseOrderId", command =>
        {
            AddInputOutputId(command, "@PurchaseOrderId", purchaseOrderId);
            AddParameter(command, "@SupplierId", dto.SupplierId);
            AddParameter(command, "@StockLocationId", dto.StockLocationId);
            AddParameter(command, "@OrderDate", dto.OrderDate.Date);
            AddParameter(command, "@ExpectedDeliveryDate", dto.ExpectedDeliveryDate?.Date);
            AddParameter(command, "@Status", ParseEnum(dto.Status, PurchaseOrderStatus.Ordered).ToString());
            AddParameter(command, "@Notes", Normalize(dto.Notes));
            command.Parameters.Add(CreateLineTableParameter(dto.Lines));
        });

        var result = await FindRequiredAsync(GetPurchaseOrdersAsync, x => x.PurchaseOrderId == id, "Purchase order not found after save.");
        await WriteInventoryAuditAsync(
            purchaseOrderId.HasValue ? "Updated" : "Created",
            "Purchase Order",
            result.PurchaseOrderId,
            result.OrderNumber,
            $"Purchase order {result.OrderNumber} was {(purchaseOrderId.HasValue ? "updated" : "created")}.");
        return result;
    }

    public async Task<PurchaseInvoiceDto> ReceivePurchaseOrderAsync(long purchaseOrderId, ReceivePurchaseOrderDto dto)
    {
        var id = await ExecuteSaveAsync("dbo.usp_Inventory_ReceivePurchaseOrder", "@PurchaseInvoiceId", command =>
        {
            AddInputOutputId(command, "@PurchaseInvoiceId", null);
            AddParameter(command, "@PurchaseOrderId", purchaseOrderId);
            AddParameter(command, "@InvoiceNumber", NormalizeRequired(dto.InvoiceNumber));
            AddParameter(command, "@InvoiceDate", dto.InvoiceDate.Date);
            AddParameter(command, "@DueDate", dto.DueDate?.Date);
            AddParameter(command, "@PaidAmount", Math.Max(dto.PaidAmount, 0m));
            AddParameter(command, "@Notes", Normalize(dto.Notes));
            command.Parameters.Add(CreateLineTableParameter(dto.Lines));
        });

        var result = await FindRequiredAsync(GetPurchaseInvoicesAsync, x => x.PurchaseInvoiceId == id, "Purchase invoice not found after receiving stock.");
        await WriteInventoryAuditAsync(
            "Received",
            "Purchase Invoice",
            result.PurchaseInvoiceId,
            result.InvoiceNumber,
            $"Stock receipt {result.InvoiceNumber} was recorded for purchase order {result.OrderNumber}.");
        return result;
    }

    public async Task<IReadOnlyList<PurchaseInvoiceDto>> GetPurchaseInvoicesAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = CreateCommand(connection, "dbo.usp_Inventory_GetPurchaseInvoices");
        await using var reader = await command.ExecuteReaderAsync();
        return await ReadHeaderWithLinesAsync(reader, "PurchaseInvoiceId", MapPurchaseInvoiceHeader);
    }

    public async Task<IReadOnlyList<PurchaseReturnDto>> GetPurchaseReturnsAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = CreateCommand(connection, "dbo.usp_Inventory_GetPurchaseReturns");
        await using var reader = await command.ExecuteReaderAsync();
        return await ReadHeaderWithLinesAsync(reader, "PurchaseReturnId", MapPurchaseReturnHeader);
    }

    public async Task<PurchaseReturnDto> SavePurchaseReturnAsync(SavePurchaseReturnDto dto)
    {
        var id = await ExecuteSaveAsync("dbo.usp_Inventory_SavePurchaseReturn", "@PurchaseReturnId", command =>
        {
            AddInputOutputId(command, "@PurchaseReturnId", null);
            AddParameter(command, "@PurchaseInvoiceId", dto.PurchaseInvoiceId);
            AddParameter(command, "@ReturnDate", dto.ReturnDate.Date);
            AddParameter(command, "@Notes", Normalize(dto.Notes));
            command.Parameters.Add(CreateLineTableParameter(dto.Lines));
        });

        var result = await FindRequiredAsync(GetPurchaseReturnsAsync, x => x.PurchaseReturnId == id, "Purchase return not found after save.");
        await WriteInventoryAuditAsync(
            "Created",
            "Purchase Return",
            result.PurchaseReturnId,
            result.ReturnNumber,
            $"Purchase return {result.ReturnNumber} was recorded for invoice {result.InvoiceNumber}.");
        return result;
    }

    public async Task<IReadOnlyList<SupplierDueSummaryDto>> GetSupplierDueSummaryAsync() =>
        await QueryListAsync("dbo.usp_Inventory_GetSupplierDueSummary", null, MapSupplierDueSummary);

    public async Task<IReadOnlyList<StockTransferDto>> GetTransfersAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = CreateCommand(connection, "dbo.usp_Inventory_GetTransfers");
        await using var reader = await command.ExecuteReaderAsync();
        return await ReadHeaderWithLinesAsync(reader, "StockTransferId", MapTransferHeader);
    }

    public async Task<StockTransferDto> SaveTransferAsync(long? stockTransferId, SaveStockTransferDto dto)
    {
        var id = await ExecuteSaveAsync("dbo.usp_Inventory_SaveTransfer", "@StockTransferId", command =>
        {
            AddInputOutputId(command, "@StockTransferId", stockTransferId);
            AddParameter(command, "@FromStockLocationId", dto.FromStockLocationId);
            AddParameter(command, "@ToStockLocationId", dto.ToStockLocationId);
            AddParameter(command, "@TransferDate", dto.TransferDate.Date);
            AddParameter(command, "@Notes", Normalize(dto.Notes));
            command.Parameters.Add(CreateLineTableParameter(dto.Lines));
        });

        var result = await FindRequiredAsync(GetTransfersAsync, x => x.StockTransferId == id, "Stock transfer not found after save.");
        await WriteInventoryAuditAsync(
            stockTransferId.HasValue ? "Updated" : "Created",
            "Stock Transfer",
            result.StockTransferId,
            result.TransferNumber,
            $"Stock transfer {result.TransferNumber} was {(stockTransferId.HasValue ? "updated" : "created")}.");
        return result;
    }

    public async Task<StockTransferDto> ApproveTransferAsync(long stockTransferId, ApproveStockTransferDto dto)
    {
        await ExecuteNonQueryAsync("dbo.usp_Inventory_ApproveTransfer", command =>
        {
            AddParameter(command, "@StockTransferId", stockTransferId);
            AddParameter(command, "@Notes", Normalize(dto.Notes));
        });

        var result = await FindRequiredAsync(GetTransfersAsync, x => x.StockTransferId == stockTransferId, "Stock transfer not found after approval.");
        await WriteInventoryAuditAsync("Approved", "Stock Transfer", result.StockTransferId, result.TransferNumber, $"Stock transfer {result.TransferNumber} was approved.");
        return result;
    }

    public async Task<StockTransferDto> ReceiveTransferAsync(long stockTransferId, ReceiveStockTransferDto dto)
    {
        await ExecuteNonQueryAsync("dbo.usp_Inventory_ReceiveTransfer", command =>
        {
            AddParameter(command, "@StockTransferId", stockTransferId);
            AddParameter(command, "@Notes", Normalize(dto.Notes));
        });

        var result = await FindRequiredAsync(GetTransfersAsync, x => x.StockTransferId == stockTransferId, "Stock transfer not found after receipt.");
        await WriteInventoryAuditAsync("Received", "Stock Transfer", result.StockTransferId, result.TransferNumber, $"Stock transfer {result.TransferNumber} was received.");
        return result;
    }

    public async Task<IReadOnlyList<StockAdjustmentDto>> GetAdjustmentsAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = CreateCommand(connection, "dbo.usp_Inventory_GetAdjustments");
        await using var reader = await command.ExecuteReaderAsync();
        return await ReadHeaderWithLinesAsync(reader, "StockAdjustmentId", MapAdjustmentHeader);
    }

    public async Task<StockAdjustmentDto> SaveAdjustmentAsync(long? stockAdjustmentId, SaveStockAdjustmentDto dto)
    {
        var id = await ExecuteSaveAsync("dbo.usp_Inventory_SaveAdjustment", "@StockAdjustmentId", command =>
        {
            AddInputOutputId(command, "@StockAdjustmentId", stockAdjustmentId);
            AddParameter(command, "@StockLocationId", dto.StockLocationId);
            AddParameter(command, "@Reason", ParseEnum(dto.Reason, StockAdjustmentReason.ManualCorrection).ToString());
            AddParameter(command, "@AdjustmentDate", dto.AdjustmentDate.Date);
            AddParameter(command, "@Notes", Normalize(dto.Notes));
            command.Parameters.Add(CreateLineTableParameter(dto.Lines));
        });

        var result = await FindRequiredAsync(GetAdjustmentsAsync, x => x.StockAdjustmentId == id, "Stock adjustment not found after save.");
        await WriteStockAdjustmentAuditAsync(
            stockAdjustmentId.HasValue ? "Updated" : "Created",
            result.StockAdjustmentId,
            result.AdjustmentNumber,
            $"Stock adjustment {result.AdjustmentNumber} was {(stockAdjustmentId.HasValue ? "updated" : "created")}.");
        return result;
    }

    public async Task<StockAdjustmentDto> ApproveAdjustmentAsync(long stockAdjustmentId, ApproveStockAdjustmentDto dto)
    {
        await ExecuteNonQueryAsync("dbo.usp_Inventory_ApproveAdjustment", command =>
        {
            AddParameter(command, "@StockAdjustmentId", stockAdjustmentId);
            AddParameter(command, "@Notes", Normalize(dto.Notes));
        });

        var result = await FindRequiredAsync(GetAdjustmentsAsync, x => x.StockAdjustmentId == stockAdjustmentId, "Stock adjustment not found after approval.");
        await WriteStockAdjustmentAuditAsync("Approved", result.StockAdjustmentId, result.AdjustmentNumber, $"Stock adjustment {result.AdjustmentNumber} was approved.");
        return result;
    }

    private Task WriteInventoryAuditAsync(string action, string entityName, long entityId, string targetDisplayName, string summary)
    {
        return _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.Inventory,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            TargetDisplayName = targetDisplayName,
            Summary = summary
        });
    }

    private Task WriteStockAdjustmentAuditAsync(string action, long entityId, string targetDisplayName, string summary)
    {
        return _audit.WriteAsync(new AuditLogRequest
        {
            Category = AuditLogCategories.StockAdjustment,
            Action = action,
            EntityName = "Stock Adjustment",
            EntityId = entityId,
            TargetDisplayName = targetDisplayName,
            Summary = summary
        });
    }

    private async Task<IReadOnlyList<THeader>> ReadHeaderWithLinesAsync<THeader>(
        SqlDataReader reader,
        string idColumn,
        Func<SqlDataReader, THeader> mapHeader) where THeader : class
    {
        var orderedHeaders = new List<THeader>();
        var index = new Dictionary<long, THeader>();

        while (await reader.ReadAsync())
        {
            var header = mapHeader(reader);
            var id = GetLong(reader, idColumn);
            orderedHeaders.Add(header);
            index[id] = header;
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                var parentId = GetLong(reader, "ParentId");
                if (!index.TryGetValue(parentId, out var header))
                {
                    continue;
                }

                var lines = GetHeaderLines(header);
                lines.Add(MapInventoryLine(reader));
            }
        }

        return orderedHeaders;
    }

    private static List<InventoryLineDto> GetHeaderLines<THeader>(THeader header) where THeader : class
    {
        return header switch
        {
            PurchaseOrderDto order => EnsureLines(order.Lines, value => order.Lines = value),
            PurchaseInvoiceDto invoice => EnsureLines(invoice.Lines, value => invoice.Lines = value),
            PurchaseReturnDto purchaseReturn => EnsureLines(purchaseReturn.Lines, value => purchaseReturn.Lines = value),
            StockTransferDto transfer => EnsureLines(transfer.Lines, value => transfer.Lines = value),
            StockAdjustmentDto adjustment => EnsureLines(adjustment.Lines, value => adjustment.Lines = value),
            _ => throw new InvalidOperationException("Unsupported header type.")
        };
    }

    private static List<InventoryLineDto> EnsureLines(
        IEnumerable<InventoryLineDto> source,
        Action<List<InventoryLineDto>> assign)
    {
        if (source is List<InventoryLineDto> list)
        {
            return list;
        }

        var newList = source.ToList();
        assign(newList);
        return newList;
    }

    private async Task<IReadOnlyList<T>> QueryListAsync<T>(
        string procedureName,
        Action<SqlCommand>? configure,
        Func<SqlDataReader, T> map)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = CreateCommand(connection, procedureName);
        configure?.Invoke(command);
        await using var reader = await command.ExecuteReaderAsync();
        var items = new List<T>();
        while (await reader.ReadAsync())
        {
            items.Add(map(reader));
        }

        return items;
    }

    private async Task<long> ExecuteSaveAsync(string procedureName, string outputParameterName, Action<SqlCommand> configure)
    {
        try
        {
            await using var connection = await OpenConnectionAsync();
            await using var command = CreateCommand(connection, procedureName);
            configure(command);
            await command.ExecuteNonQueryAsync();
            return Convert.ToInt64(command.Parameters[outputParameterName].Value);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(ex.Message, ex);
        }
    }

    private async Task ExecuteNonQueryAsync(string procedureName, Action<SqlCommand> configure)
    {
        try
        {
            await using var connection = await OpenConnectionAsync();
            await using var command = CreateCommand(connection, procedureName);
            configure(command);
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(ex.Message, ex);
        }
    }

    private async Task<T> FindRequiredAsync<T>(
        Func<Task<IReadOnlyList<T>>> loader,
        Func<T, bool> predicate,
        string message)
    {
        var item = (await loader()).FirstOrDefault(predicate);
        return item is not null ? item : throw new InvalidOperationException(message);
    }

    private async Task<SqlConnection> OpenConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string procedureName)
    {
        return new SqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };
    }

    private static void AddInputOutputId(SqlCommand command, string parameterName, long? value)
    {
        var parameter = new SqlParameter(parameterName, SqlDbType.BigInt)
        {
            Direction = ParameterDirection.InputOutput,
            Value = value ?? 0L
        };

        command.Parameters.Add(parameter);
    }

    private static void AddParameter(SqlCommand command, string parameterName, object? value)
    {
        command.Parameters.AddWithValue(parameterName, value ?? DBNull.Value);
    }

    private static SqlParameter CreateLineTableParameter(IEnumerable<SaveInventoryLineDto> lines)
    {
        var table = new DataTable();
        table.Columns.Add("ReferenceLineId", typeof(long));
        table.Columns.Add("StockBatchId", typeof(long));
        table.Columns.Add("MedicineMasterId", typeof(long));
        table.Columns.Add("StockItemMasterId", typeof(long));
        table.Columns.Add("ItemName", typeof(string));
        table.Columns.Add("UnitName", typeof(string));
        table.Columns.Add("BatchNumber", typeof(string));
        table.Columns.Add("ExpiryDate", typeof(DateTime));
        table.Columns.Add("Quantity", typeof(decimal));
        table.Columns.Add("UnitCost", typeof(decimal));
        table.Columns.Add("Reason", typeof(string));
        table.Columns.Add("Notes", typeof(string));

        foreach (var line in lines ?? Array.Empty<SaveInventoryLineDto>())
        {
            if (line.Quantity == 0m && !line.StockBatchId.HasValue && !line.MedicineMasterId.HasValue && !line.StockItemMasterId.HasValue)
            {
                continue;
            }

            table.Rows.Add(
                ToDbValue(line.ReferenceLineId),
                ToDbValue(line.StockBatchId),
                ToDbValue(line.MedicineMasterId),
                ToDbValue(line.StockItemMasterId),
                ToDbValue(Normalize(line.ItemName)),
                ToDbValue(Normalize(line.UnitName)),
                ToDbValue(Normalize(line.BatchNumber)),
                ToDbValue(line.ExpiryDate?.Date),
                line.Quantity,
                line.UnitCost,
                ToDbValue(Normalize(line.Reason)),
                ToDbValue(Normalize(line.Notes)));
        }

        return new SqlParameter("@Lines", SqlDbType.Structured)
        {
            TypeName = "dbo.InventoryLineInputType",
            Value = table
        };
    }

    private static InventoryDashboardDto MapDashboardHeader(SqlDataReader reader)
    {
        return new InventoryDashboardDto
        {
            ActiveLocations = GetInt(reader, "ActiveLocations"),
            ActiveSuppliers = GetInt(reader, "ActiveSuppliers"),
            OpenPurchaseOrders = GetInt(reader, "OpenPurchaseOrders"),
            PendingTransfers = GetInt(reader, "PendingTransfers"),
            PendingAdjustments = GetInt(reader, "PendingAdjustments"),
            LowStockCount = GetInt(reader, "LowStockCount"),
            NearExpiryCount = GetInt(reader, "NearExpiryCount"),
            ExpiredCount = GetInt(reader, "ExpiredCount"),
            SupplierDueAmount = GetDecimal(reader, "SupplierDueAmount")
        };
    }

    private static InventoryAlertDto MapInventoryAlert(SqlDataReader reader)
    {
        return new InventoryAlertDto
        {
            StockBatchId = GetLong(reader, "StockBatchId"),
            ItemType = GetString(reader, "ItemType"),
            ItemName = GetString(reader, "ItemName"),
            UnitName = GetString(reader, "UnitName"),
            LocationName = GetString(reader, "LocationName"),
            BatchNumber = GetNullableString(reader, "BatchNumber"),
            ExpiryDate = GetNullableDate(reader, "ExpiryDate"),
            QuantityOnHand = GetDecimal(reader, "QuantityOnHand"),
            MinimumStock = GetDecimal(reader, "MinimumStock"),
            IsExpiredLocked = GetBool(reader, "IsExpiredLocked")
        };
    }

    private static InventoryUnitDto MapInventoryUnit(SqlDataReader reader)
    {
        return new InventoryUnitDto
        {
            InventoryUnitId = GetLong(reader, "InventoryUnitId"),
            Name = GetString(reader, "Name"),
            ShortName = GetNullableString(reader, "ShortName"),
            Description = GetNullableString(reader, "Description"),
            IsActive = GetBool(reader, "IsActive")
        };
    }

    private static InventoryCategoryDto MapInventoryCategory(SqlDataReader reader)
    {
        return new InventoryCategoryDto
        {
            InventoryCategoryId = GetLong(reader, "InventoryCategoryId"),
            CategoryType = GetString(reader, "CategoryType"),
            Name = GetString(reader, "Name"),
            Description = GetNullableString(reader, "Description"),
            IsActive = GetBool(reader, "IsActive")
        };
    }

    private static MedicineMasterDto MapMedicine(SqlDataReader reader)
    {
        return new MedicineMasterDto
        {
            MedicineMasterId = GetLong(reader, "MedicineMasterId"),
            MedicineName = GetString(reader, "MedicineName"),
            GenericName = GetNullableString(reader, "GenericName"),
            Brand = GetNullableString(reader, "Brand"),
            UnitName = GetString(reader, "UnitName"),
            InventoryUnitId = GetLong(reader, "InventoryUnitId"),
            CategoryName = GetNullableString(reader, "CategoryName"),
            InventoryCategoryId = GetNullableLong(reader, "InventoryCategoryId"),
            Strength = GetNullableString(reader, "Strength"),
            BatchRequired = GetBool(reader, "BatchRequired"),
            MinimumStock = GetDecimal(reader, "MinimumStock"),
            MaximumStock = GetDecimal(reader, "MaximumStock"),
            IsActive = GetBool(reader, "IsActive")
        };
    }

    private static StockItemMasterDto MapItem(SqlDataReader reader)
    {
        return new StockItemMasterDto
        {
            StockItemMasterId = GetLong(reader, "StockItemMasterId"),
            ItemType = GetString(reader, "ItemType"),
            ItemName = GetString(reader, "ItemName"),
            Specification = GetNullableString(reader, "Specification"),
            UnitName = GetString(reader, "UnitName"),
            InventoryUnitId = GetLong(reader, "InventoryUnitId"),
            CategoryName = GetNullableString(reader, "CategoryName"),
            InventoryCategoryId = GetNullableLong(reader, "InventoryCategoryId"),
            MinimumStock = GetDecimal(reader, "MinimumStock"),
            MaximumStock = GetDecimal(reader, "MaximumStock"),
            IsActive = GetBool(reader, "IsActive")
        };
    }

    private static SupplierDto MapSupplier(SqlDataReader reader)
    {
        return new SupplierDto
        {
            SupplierId = GetLong(reader, "SupplierId"),
            SupplierName = GetString(reader, "SupplierName"),
            SupplierCode = GetString(reader, "SupplierCode"),
            ContactPerson = GetNullableString(reader, "ContactPerson"),
            ContactPhone = GetNullableString(reader, "ContactPhone"),
            ContactEmail = GetNullableString(reader, "ContactEmail"),
            Address = GetNullableString(reader, "Address"),
            PaymentTermsDays = GetInt(reader, "PaymentTermsDays"),
            Notes = GetNullableString(reader, "Notes"),
            IsActive = GetBool(reader, "IsActive"),
            PurchaseOrderCount = GetInt(reader, "PurchaseOrderCount"),
            PurchaseInvoiceCount = GetInt(reader, "PurchaseInvoiceCount"),
            TotalPurchasedAmount = GetDecimal(reader, "TotalPurchasedAmount"),
            DueAmount = GetDecimal(reader, "DueAmount"),
            LastPurchaseDate = GetNullableDate(reader, "LastPurchaseDate")
        };
    }

    private static StockLocationDto MapLocation(SqlDataReader reader)
    {
        return new StockLocationDto
        {
            StockLocationId = GetLong(reader, "StockLocationId"),
            BranchId = GetNullableLong(reader, "BranchId"),
            BranchName = GetString(reader, "BranchName"),
            Name = GetString(reader, "Name"),
            Code = GetString(reader, "Code"),
            LocationType = GetString(reader, "LocationType"),
            Description = GetNullableString(reader, "Description"),
            IsActive = GetBool(reader, "IsActive")
        };
    }

    private static StockBatchDto MapBatch(SqlDataReader reader)
    {
        return new StockBatchDto
        {
            StockBatchId = GetLong(reader, "StockBatchId"),
            StockLocationId = GetLong(reader, "StockLocationId"),
            LocationName = GetString(reader, "LocationName"),
            ItemType = GetString(reader, "ItemType"),
            MedicineMasterId = GetNullableLong(reader, "MedicineMasterId"),
            StockItemMasterId = GetNullableLong(reader, "StockItemMasterId"),
            ItemName = GetString(reader, "ItemName"),
            UnitName = GetString(reader, "UnitName"),
            CategoryName = GetNullableString(reader, "CategoryName"),
            BatchNumber = GetNullableString(reader, "BatchNumber"),
            ExpiryDate = GetNullableDate(reader, "ExpiryDate"),
            QuantityOnHand = GetDecimal(reader, "QuantityOnHand"),
            UnitCost = GetDecimal(reader, "UnitCost"),
            MinimumStock = GetDecimal(reader, "MinimumStock"),
            MaximumStock = GetDecimal(reader, "MaximumStock"),
            IsNearExpiry = GetBool(reader, "IsNearExpiry"),
            IsExpiredLocked = GetBool(reader, "IsExpiredLocked")
        };
    }

    private static InventoryLineDto MapInventoryLine(SqlDataReader reader)
    {
        return new InventoryLineDto
        {
            ReferenceLineId = GetLong(reader, "ReferenceLineId"),
            StockBatchId = GetNullableLong(reader, "StockBatchId"),
            MedicineMasterId = GetNullableLong(reader, "MedicineMasterId"),
            StockItemMasterId = GetNullableLong(reader, "StockItemMasterId"),
            ItemType = GetString(reader, "ItemType"),
            ItemName = GetString(reader, "ItemName"),
            UnitName = GetString(reader, "UnitName"),
            BatchNumber = GetNullableString(reader, "BatchNumber"),
            ExpiryDate = GetNullableDate(reader, "ExpiryDate"),
            Quantity = GetDecimal(reader, "Quantity"),
            ReceivedQuantity = HasColumn(reader, "ReceivedQuantity") ? GetDecimal(reader, "ReceivedQuantity") : 0m,
            UnitCost = GetDecimal(reader, "UnitCost"),
            LineTotal = GetDecimal(reader, "LineTotal"),
            Reason = GetNullableString(reader, "Reason"),
            Notes = GetNullableString(reader, "Notes")
        };
    }

    private static PurchaseOrderDto MapPurchaseOrderHeader(SqlDataReader reader)
    {
        return new PurchaseOrderDto
        {
            PurchaseOrderId = GetLong(reader, "PurchaseOrderId"),
            OrderNumber = GetString(reader, "OrderNumber"),
            SupplierId = GetLong(reader, "SupplierId"),
            SupplierName = GetString(reader, "SupplierName"),
            StockLocationId = GetLong(reader, "StockLocationId"),
            StockLocationName = GetString(reader, "StockLocationName"),
            OrderDate = GetDate(reader, "OrderDate"),
            ExpectedDeliveryDate = GetNullableDate(reader, "ExpectedDeliveryDate"),
            Status = GetString(reader, "Status"),
            Notes = GetNullableString(reader, "Notes"),
            TotalAmount = GetDecimal(reader, "TotalAmount"),
            OrderedQuantity = GetDecimal(reader, "OrderedQuantity"),
            ReceivedQuantity = GetDecimal(reader, "ReceivedQuantity"),
            Lines = new List<InventoryLineDto>()
        };
    }

    private static PurchaseInvoiceDto MapPurchaseInvoiceHeader(SqlDataReader reader)
    {
        return new PurchaseInvoiceDto
        {
            PurchaseInvoiceId = GetLong(reader, "PurchaseInvoiceId"),
            PurchaseOrderId = GetLong(reader, "PurchaseOrderId"),
            OrderNumber = GetString(reader, "OrderNumber"),
            SupplierId = GetLong(reader, "SupplierId"),
            SupplierName = GetString(reader, "SupplierName"),
            StockLocationId = GetLong(reader, "StockLocationId"),
            StockLocationName = GetString(reader, "StockLocationName"),
            InvoiceNumber = GetString(reader, "InvoiceNumber"),
            InvoiceDate = GetDate(reader, "InvoiceDate"),
            DueDate = GetNullableDate(reader, "DueDate"),
            TotalAmount = GetDecimal(reader, "TotalAmount"),
            PaidAmount = GetDecimal(reader, "PaidAmount"),
            DueAmount = GetDecimal(reader, "DueAmount"),
            Status = GetString(reader, "Status"),
            Notes = GetNullableString(reader, "Notes"),
            Lines = new List<InventoryLineDto>()
        };
    }

    private static PurchaseReturnDto MapPurchaseReturnHeader(SqlDataReader reader)
    {
        return new PurchaseReturnDto
        {
            PurchaseReturnId = GetLong(reader, "PurchaseReturnId"),
            PurchaseInvoiceId = GetLong(reader, "PurchaseInvoiceId"),
            InvoiceNumber = GetString(reader, "InvoiceNumber"),
            SupplierId = GetLong(reader, "SupplierId"),
            SupplierName = GetString(reader, "SupplierName"),
            ReturnNumber = GetString(reader, "ReturnNumber"),
            ReturnDate = GetDate(reader, "ReturnDate"),
            TotalAmount = GetDecimal(reader, "TotalAmount"),
            Notes = GetNullableString(reader, "Notes"),
            Lines = new List<InventoryLineDto>()
        };
    }

    private static StockTransferDto MapTransferHeader(SqlDataReader reader)
    {
        return new StockTransferDto
        {
            StockTransferId = GetLong(reader, "StockTransferId"),
            TransferNumber = GetString(reader, "TransferNumber"),
            FromStockLocationId = GetLong(reader, "FromStockLocationId"),
            FromStockLocationName = GetString(reader, "FromStockLocationName"),
            ToStockLocationId = GetLong(reader, "ToStockLocationId"),
            ToStockLocationName = GetString(reader, "ToStockLocationName"),
            TransferDate = GetDate(reader, "TransferDate"),
            Status = GetString(reader, "Status"),
            Notes = GetNullableString(reader, "Notes"),
            ApprovedAt = GetNullableDateTime(reader, "ApprovedAt"),
            ReceivedAt = GetNullableDateTime(reader, "ReceivedAt"),
            TotalQuantity = GetDecimal(reader, "TotalQuantity"),
            Lines = new List<InventoryLineDto>()
        };
    }

    private static StockAdjustmentDto MapAdjustmentHeader(SqlDataReader reader)
    {
        return new StockAdjustmentDto
        {
            StockAdjustmentId = GetLong(reader, "StockAdjustmentId"),
            AdjustmentNumber = GetString(reader, "AdjustmentNumber"),
            StockLocationId = GetLong(reader, "StockLocationId"),
            StockLocationName = GetString(reader, "StockLocationName"),
            Reason = GetString(reader, "Reason"),
            Status = GetString(reader, "Status"),
            AdjustmentDate = GetDate(reader, "AdjustmentDate"),
            Notes = GetNullableString(reader, "Notes"),
            ApprovedAt = GetNullableDateTime(reader, "ApprovedAt"),
            TotalDelta = GetDecimal(reader, "TotalDelta"),
            Lines = new List<InventoryLineDto>()
        };
    }

    private static SupplierDueSummaryDto MapSupplierDueSummary(SqlDataReader reader)
    {
        return new SupplierDueSummaryDto
        {
            SupplierId = GetLong(reader, "SupplierId"),
            SupplierName = GetString(reader, "SupplierName"),
            SupplierCode = GetString(reader, "SupplierCode"),
            OpenInvoiceCount = GetInt(reader, "OpenInvoiceCount"),
            TotalInvoiceAmount = GetDecimal(reader, "TotalInvoiceAmount"),
            PaidAmount = GetDecimal(reader, "PaidAmount"),
            ReturnAmount = GetDecimal(reader, "ReturnAmount"),
            DueAmount = GetDecimal(reader, "DueAmount")
        };
    }

    private static string NormalizeRequired(string? value)
    {
        var normalized = Normalize(value);
        return !string.IsNullOrWhiteSpace(normalized)
            ? normalized
            : throw new InvalidOperationException("A required field was not provided.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static object ToDbValue(object? value) => value ?? DBNull.Value;

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }

    private static bool HasColumn(SqlDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetString(SqlDataReader reader, string columnName) =>
        reader[columnName] == DBNull.Value ? string.Empty : Convert.ToString(reader[columnName]) ?? string.Empty;

    private static string? GetNullableString(SqlDataReader reader, string columnName) =>
        reader[columnName] == DBNull.Value ? null : Convert.ToString(reader[columnName]);

    private static long GetLong(SqlDataReader reader, string columnName) =>
        reader[columnName] == DBNull.Value ? 0L : Convert.ToInt64(reader[columnName]);

    private static long? GetNullableLong(SqlDataReader reader, string columnName) =>
        reader[columnName] == DBNull.Value ? null : Convert.ToInt64(reader[columnName]);

    private static int GetInt(SqlDataReader reader, string columnName) =>
        reader[columnName] == DBNull.Value ? 0 : Convert.ToInt32(reader[columnName]);

    private static decimal GetDecimal(SqlDataReader reader, string columnName) =>
        reader[columnName] == DBNull.Value ? 0m : Convert.ToDecimal(reader[columnName]);

    private static bool GetBool(SqlDataReader reader, string columnName) =>
        reader[columnName] != DBNull.Value && Convert.ToBoolean(reader[columnName]);

    private static DateTime GetDate(SqlDataReader reader, string columnName) =>
        reader[columnName] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader[columnName]);

    private static DateTime? GetNullableDate(SqlDataReader reader, string columnName) =>
        reader[columnName] == DBNull.Value ? null : Convert.ToDateTime(reader[columnName]);

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName) =>
        reader[columnName] == DBNull.Value ? null : Convert.ToDateTime(reader[columnName]);
}
