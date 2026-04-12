using AngularApp4.Dtos.Hms;
using AngularApp4.Services.Hms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AngularApp4.Controllers;

[ApiController]
[Route("api/admin/inventory")]
[Authorize(Policy = "AdminOnly")]
public class AdminInventoryController : ControllerBase
{
    private readonly IInventoryAdminService _inventory;

    public AdminInventoryController(IInventoryAdminService inventory)
    {
        _inventory = inventory;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<InventoryDashboardDto>>> GetDashboard() =>
        Ok(ApiResponse<InventoryDashboardDto>.Ok(await _inventory.GetDashboardAsync()));

    [HttpGet("units")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryUnitDto>>>> GetUnits() =>
        Ok(ApiResponse<IEnumerable<InventoryUnitDto>>.Ok(await _inventory.GetUnitsAsync()));

    [HttpPost("units")]
    public async Task<ActionResult<ApiResponse<InventoryUnitDto>>> CreateUnit([FromBody] SaveInventoryUnitDto dto) =>
        await Execute(async () => ApiResponse<InventoryUnitDto>.Ok(await _inventory.SaveUnitAsync(null, dto), "Inventory unit created"));

    [HttpPut("units/{inventoryUnitId:long}")]
    public async Task<ActionResult<ApiResponse<InventoryUnitDto>>> UpdateUnit(long inventoryUnitId, [FromBody] SaveInventoryUnitDto dto) =>
        await Execute(async () => ApiResponse<InventoryUnitDto>.Ok(await _inventory.SaveUnitAsync(inventoryUnitId, dto), "Inventory unit updated"));

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryCategoryDto>>>> GetCategories() =>
        Ok(ApiResponse<IEnumerable<InventoryCategoryDto>>.Ok(await _inventory.GetCategoriesAsync()));

    [HttpPost("categories")]
    public async Task<ActionResult<ApiResponse<InventoryCategoryDto>>> CreateCategory([FromBody] SaveInventoryCategoryDto dto) =>
        await Execute(async () => ApiResponse<InventoryCategoryDto>.Ok(await _inventory.SaveCategoryAsync(null, dto), "Inventory category created"));

    [HttpPut("categories/{inventoryCategoryId:long}")]
    public async Task<ActionResult<ApiResponse<InventoryCategoryDto>>> UpdateCategory(long inventoryCategoryId, [FromBody] SaveInventoryCategoryDto dto) =>
        await Execute(async () => ApiResponse<InventoryCategoryDto>.Ok(await _inventory.SaveCategoryAsync(inventoryCategoryId, dto), "Inventory category updated"));

    [HttpGet("medicines")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MedicineMasterDto>>>> GetMedicines() =>
        Ok(ApiResponse<IEnumerable<MedicineMasterDto>>.Ok(await _inventory.GetMedicinesAsync()));

    [HttpPost("medicines")]
    public async Task<ActionResult<ApiResponse<MedicineMasterDto>>> CreateMedicine([FromBody] SaveMedicineMasterDto dto) =>
        await Execute(async () => ApiResponse<MedicineMasterDto>.Ok(await _inventory.SaveMedicineAsync(null, dto), "Medicine master created"));

    [HttpPut("medicines/{medicineMasterId:long}")]
    public async Task<ActionResult<ApiResponse<MedicineMasterDto>>> UpdateMedicine(long medicineMasterId, [FromBody] SaveMedicineMasterDto dto) =>
        await Execute(async () => ApiResponse<MedicineMasterDto>.Ok(await _inventory.SaveMedicineAsync(medicineMasterId, dto), "Medicine master updated"));

    [HttpGet("items")]
    public async Task<ActionResult<ApiResponse<IEnumerable<StockItemMasterDto>>>> GetItems() =>
        Ok(ApiResponse<IEnumerable<StockItemMasterDto>>.Ok(await _inventory.GetItemsAsync()));

    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<StockItemMasterDto>>> CreateItem([FromBody] SaveStockItemMasterDto dto) =>
        await Execute(async () => ApiResponse<StockItemMasterDto>.Ok(await _inventory.SaveItemAsync(null, dto), "Item master created"));

    [HttpPut("items/{stockItemMasterId:long}")]
    public async Task<ActionResult<ApiResponse<StockItemMasterDto>>> UpdateItem(long stockItemMasterId, [FromBody] SaveStockItemMasterDto dto) =>
        await Execute(async () => ApiResponse<StockItemMasterDto>.Ok(await _inventory.SaveItemAsync(stockItemMasterId, dto), "Item master updated"));

    [HttpGet("suppliers")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SupplierDto>>>> GetSuppliers() =>
        Ok(ApiResponse<IEnumerable<SupplierDto>>.Ok(await _inventory.GetSuppliersAsync()));

    [HttpPost("suppliers")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> CreateSupplier([FromBody] SaveSupplierDto dto) =>
        await Execute(async () => ApiResponse<SupplierDto>.Ok(await _inventory.SaveSupplierAsync(null, dto), "Supplier created"));

    [HttpPut("suppliers/{supplierId:long}")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> UpdateSupplier(long supplierId, [FromBody] SaveSupplierDto dto) =>
        await Execute(async () => ApiResponse<SupplierDto>.Ok(await _inventory.SaveSupplierAsync(supplierId, dto), "Supplier updated"));

    [HttpGet("locations")]
    public async Task<ActionResult<ApiResponse<IEnumerable<StockLocationDto>>>> GetLocations() =>
        Ok(ApiResponse<IEnumerable<StockLocationDto>>.Ok(await _inventory.GetLocationsAsync()));

    [HttpPost("locations")]
    public async Task<ActionResult<ApiResponse<StockLocationDto>>> CreateLocation([FromBody] SaveStockLocationDto dto) =>
        await Execute(async () => ApiResponse<StockLocationDto>.Ok(await _inventory.SaveLocationAsync(null, dto), "Stock location created"));

    [HttpPut("locations/{stockLocationId:long}")]
    public async Task<ActionResult<ApiResponse<StockLocationDto>>> UpdateLocation(long stockLocationId, [FromBody] SaveStockLocationDto dto) =>
        await Execute(async () => ApiResponse<StockLocationDto>.Ok(await _inventory.SaveLocationAsync(stockLocationId, dto), "Stock location updated"));

    [HttpGet("batches")]
    public async Task<ActionResult<ApiResponse<IEnumerable<StockBatchDto>>>> GetBatches() =>
        Ok(ApiResponse<IEnumerable<StockBatchDto>>.Ok(await _inventory.GetBatchesAsync()));

    [HttpGet("purchase-orders")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PurchaseOrderDto>>>> GetPurchaseOrders() =>
        Ok(ApiResponse<IEnumerable<PurchaseOrderDto>>.Ok(await _inventory.GetPurchaseOrdersAsync()));

    [HttpPost("purchase-orders")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> CreatePurchaseOrder([FromBody] SavePurchaseOrderDto dto) =>
        await Execute(async () => ApiResponse<PurchaseOrderDto>.Ok(await _inventory.SavePurchaseOrderAsync(null, dto), "Purchase order created"));

    [HttpPut("purchase-orders/{purchaseOrderId:long}")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> UpdatePurchaseOrder(long purchaseOrderId, [FromBody] SavePurchaseOrderDto dto) =>
        await Execute(async () => ApiResponse<PurchaseOrderDto>.Ok(await _inventory.SavePurchaseOrderAsync(purchaseOrderId, dto), "Purchase order updated"));

    [HttpPost("purchase-orders/{purchaseOrderId:long}/receive")]
    public async Task<ActionResult<ApiResponse<PurchaseInvoiceDto>>> ReceivePurchaseOrder(long purchaseOrderId, [FromBody] ReceivePurchaseOrderDto dto) =>
        await Execute(async () => ApiResponse<PurchaseInvoiceDto>.Ok(await _inventory.ReceivePurchaseOrderAsync(purchaseOrderId, dto), "Stock received successfully"));

    [HttpGet("purchase-invoices")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PurchaseInvoiceDto>>>> GetPurchaseInvoices() =>
        Ok(ApiResponse<IEnumerable<PurchaseInvoiceDto>>.Ok(await _inventory.GetPurchaseInvoicesAsync()));

    [HttpGet("purchase-returns")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PurchaseReturnDto>>>> GetPurchaseReturns() =>
        Ok(ApiResponse<IEnumerable<PurchaseReturnDto>>.Ok(await _inventory.GetPurchaseReturnsAsync()));

    [HttpPost("purchase-returns")]
    public async Task<ActionResult<ApiResponse<PurchaseReturnDto>>> CreatePurchaseReturn([FromBody] SavePurchaseReturnDto dto) =>
        await Execute(async () => ApiResponse<PurchaseReturnDto>.Ok(await _inventory.SavePurchaseReturnAsync(dto), "Purchase return recorded"));

    [HttpGet("supplier-dues")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SupplierDueSummaryDto>>>> GetSupplierDueSummary() =>
        Ok(ApiResponse<IEnumerable<SupplierDueSummaryDto>>.Ok(await _inventory.GetSupplierDueSummaryAsync()));

    [HttpGet("transfers")]
    public async Task<ActionResult<ApiResponse<IEnumerable<StockTransferDto>>>> GetTransfers() =>
        Ok(ApiResponse<IEnumerable<StockTransferDto>>.Ok(await _inventory.GetTransfersAsync()));

    [HttpPost("transfers")]
    public async Task<ActionResult<ApiResponse<StockTransferDto>>> CreateTransfer([FromBody] SaveStockTransferDto dto) =>
        await Execute(async () => ApiResponse<StockTransferDto>.Ok(await _inventory.SaveTransferAsync(null, dto), "Stock transfer created"));

    [HttpPut("transfers/{stockTransferId:long}")]
    public async Task<ActionResult<ApiResponse<StockTransferDto>>> UpdateTransfer(long stockTransferId, [FromBody] SaveStockTransferDto dto) =>
        await Execute(async () => ApiResponse<StockTransferDto>.Ok(await _inventory.SaveTransferAsync(stockTransferId, dto), "Stock transfer updated"));

    [HttpPost("transfers/{stockTransferId:long}/approve")]
    public async Task<ActionResult<ApiResponse<StockTransferDto>>> ApproveTransfer(long stockTransferId, [FromBody] ApproveStockTransferDto dto) =>
        await Execute(async () => ApiResponse<StockTransferDto>.Ok(await _inventory.ApproveTransferAsync(stockTransferId, dto), "Transfer approved"));

    [HttpPost("transfers/{stockTransferId:long}/receive")]
    public async Task<ActionResult<ApiResponse<StockTransferDto>>> ReceiveTransfer(long stockTransferId, [FromBody] ReceiveStockTransferDto dto) =>
        await Execute(async () => ApiResponse<StockTransferDto>.Ok(await _inventory.ReceiveTransferAsync(stockTransferId, dto), "Transfer received"));

    [HttpGet("adjustments")]
    public async Task<ActionResult<ApiResponse<IEnumerable<StockAdjustmentDto>>>> GetAdjustments() =>
        Ok(ApiResponse<IEnumerable<StockAdjustmentDto>>.Ok(await _inventory.GetAdjustmentsAsync()));

    [HttpPost("adjustments")]
    public async Task<ActionResult<ApiResponse<StockAdjustmentDto>>> CreateAdjustment([FromBody] SaveStockAdjustmentDto dto) =>
        await Execute(async () => ApiResponse<StockAdjustmentDto>.Ok(await _inventory.SaveAdjustmentAsync(null, dto), "Stock adjustment created"));

    [HttpPut("adjustments/{stockAdjustmentId:long}")]
    public async Task<ActionResult<ApiResponse<StockAdjustmentDto>>> UpdateAdjustment(long stockAdjustmentId, [FromBody] SaveStockAdjustmentDto dto) =>
        await Execute(async () => ApiResponse<StockAdjustmentDto>.Ok(await _inventory.SaveAdjustmentAsync(stockAdjustmentId, dto), "Stock adjustment updated"));

    [HttpPost("adjustments/{stockAdjustmentId:long}/approve")]
    public async Task<ActionResult<ApiResponse<StockAdjustmentDto>>> ApproveAdjustment(long stockAdjustmentId, [FromBody] ApproveStockAdjustmentDto dto) =>
        await Execute(async () => ApiResponse<StockAdjustmentDto>.Ok(await _inventory.ApproveAdjustmentAsync(stockAdjustmentId, dto), "Stock adjustment approved"));

    private async Task<ActionResult<ApiResponse<T>>> Execute<T>(Func<Task<ApiResponse<T>>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<T>.Fail(ex.Message));
        }
    }
}
