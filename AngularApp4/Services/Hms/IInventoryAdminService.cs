using AngularApp4.Dtos.Hms;

namespace AngularApp4.Services.Hms;

public interface IInventoryAdminService
{
    Task<InventoryDashboardDto> GetDashboardAsync();
    Task<IReadOnlyList<InventoryUnitDto>> GetUnitsAsync();
    Task<InventoryUnitDto> SaveUnitAsync(long? inventoryUnitId, SaveInventoryUnitDto dto);
    Task<IReadOnlyList<InventoryCategoryDto>> GetCategoriesAsync();
    Task<InventoryCategoryDto> SaveCategoryAsync(long? inventoryCategoryId, SaveInventoryCategoryDto dto);
    Task<IReadOnlyList<MedicineMasterDto>> GetMedicinesAsync();
    Task<MedicineMasterDto> SaveMedicineAsync(long? medicineMasterId, SaveMedicineMasterDto dto);
    Task<IReadOnlyList<StockItemMasterDto>> GetItemsAsync();
    Task<StockItemMasterDto> SaveItemAsync(long? stockItemMasterId, SaveStockItemMasterDto dto);
    Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync();
    Task<SupplierDto> SaveSupplierAsync(long? supplierId, SaveSupplierDto dto);
    Task<IReadOnlyList<StockLocationDto>> GetLocationsAsync();
    Task<StockLocationDto> SaveLocationAsync(long? stockLocationId, SaveStockLocationDto dto);
    Task<IReadOnlyList<StockBatchDto>> GetBatchesAsync();
    Task<IReadOnlyList<PurchaseOrderDto>> GetPurchaseOrdersAsync();
    Task<PurchaseOrderDto> SavePurchaseOrderAsync(long? purchaseOrderId, SavePurchaseOrderDto dto);
    Task<PurchaseInvoiceDto> ReceivePurchaseOrderAsync(long purchaseOrderId, ReceivePurchaseOrderDto dto);
    Task<IReadOnlyList<PurchaseInvoiceDto>> GetPurchaseInvoicesAsync();
    Task<IReadOnlyList<PurchaseReturnDto>> GetPurchaseReturnsAsync();
    Task<PurchaseReturnDto> SavePurchaseReturnAsync(SavePurchaseReturnDto dto);
    Task<IReadOnlyList<SupplierDueSummaryDto>> GetSupplierDueSummaryAsync();
    Task<IReadOnlyList<StockTransferDto>> GetTransfersAsync();
    Task<StockTransferDto> SaveTransferAsync(long? stockTransferId, SaveStockTransferDto dto);
    Task<StockTransferDto> ApproveTransferAsync(long stockTransferId, ApproveStockTransferDto dto);
    Task<StockTransferDto> ReceiveTransferAsync(long stockTransferId, ReceiveStockTransferDto dto);
    Task<IReadOnlyList<StockAdjustmentDto>> GetAdjustmentsAsync();
    Task<StockAdjustmentDto> SaveAdjustmentAsync(long? stockAdjustmentId, SaveStockAdjustmentDto dto);
    Task<StockAdjustmentDto> ApproveAdjustmentAsync(long stockAdjustmentId, ApproveStockAdjustmentDto dto);
}
