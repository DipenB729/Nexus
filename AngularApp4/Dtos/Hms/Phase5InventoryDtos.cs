using AngularApp4.Model.Hms;

namespace AngularApp4.Dtos.Hms;

public class InventoryDashboardDto
{
    public int ActiveLocations { get; set; }
    public int ActiveSuppliers { get; set; }
    public int OpenPurchaseOrders { get; set; }
    public int PendingTransfers { get; set; }
    public int PendingAdjustments { get; set; }
    public int LowStockCount { get; set; }
    public int NearExpiryCount { get; set; }
    public int ExpiredCount { get; set; }
    public decimal SupplierDueAmount { get; set; }
    public IEnumerable<InventoryAlertDto> LowStockAlerts { get; set; } = Array.Empty<InventoryAlertDto>();
    public IEnumerable<InventoryAlertDto> NearExpiryAlerts { get; set; } = Array.Empty<InventoryAlertDto>();
    public IEnumerable<SupplierDueSummaryDto> SupplierDues { get; set; } = Array.Empty<SupplierDueSummaryDto>();
}

public class InventoryAlertDto
{
    public long StockBatchId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal MinimumStock { get; set; }
    public bool IsExpiredLocked { get; set; }
}

public class InventoryUnitDto
{
    public long InventoryUnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class SaveInventoryUnitDto
{
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class InventoryCategoryDto
{
    public long InventoryCategoryId { get; set; }
    public string CategoryType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class SaveInventoryCategoryDto
{
    public string CategoryType { get; set; } = InventoryCategoryType.Item.ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MedicineMasterDto
{
    public long MedicineMasterId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Brand { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public long InventoryUnitId { get; set; }
    public string? CategoryName { get; set; }
    public long? InventoryCategoryId { get; set; }
    public string? Strength { get; set; }
    public bool BatchRequired { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public bool IsActive { get; set; }
}

public class SaveMedicineMasterDto
{
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Brand { get; set; }
    public long InventoryUnitId { get; set; }
    public long? InventoryCategoryId { get; set; }
    public string? Strength { get; set; }
    public bool BatchRequired { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StockItemMasterDto
{
    public long StockItemMasterId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Specification { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public long InventoryUnitId { get; set; }
    public string? CategoryName { get; set; }
    public long? InventoryCategoryId { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public bool IsActive { get; set; }
}

public class SaveStockItemMasterDto
{
    public string ItemType { get; set; } = InventoryItemType.Consumable.ToString();
    public string ItemName { get; set; } = string.Empty;
    public string? Specification { get; set; }
    public long InventoryUnitId { get; set; }
    public long? InventoryCategoryId { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SupplierDto
{
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Address { get; set; }
    public int PaymentTermsDays { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public int PurchaseOrderCount { get; set; }
    public int PurchaseInvoiceCount { get; set; }
    public decimal TotalPurchasedAmount { get; set; }
    public decimal DueAmount { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
}

public class SaveSupplierDto
{
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Address { get; set; }
    public int PaymentTermsDays { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StockLocationDto
{
    public long StockLocationId { get; set; }
    public long? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class SaveStockLocationDto
{
    public long? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string LocationType { get; set; } = StockLocationType.MainStore.ToString();
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StockBatchDto
{
    public long StockBatchId { get; set; }
    public long StockLocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public long? MedicineMasterId { get; set; }
    public long? StockItemMasterId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal UnitCost { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public bool IsNearExpiry { get; set; }
    public bool IsExpiredLocked { get; set; }
}

public class InventoryLineDto
{
    public long ReferenceLineId { get; set; }
    public long? StockBatchId { get; set; }
    public long? MedicineMasterId { get; set; }
    public long? StockItemMasterId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

public class SaveInventoryLineDto
{
    public long? ReferenceLineId { get; set; }
    public long? StockBatchId { get; set; }
    public long? MedicineMasterId { get; set; }
    public long? StockItemMasterId { get; set; }
    public string? ItemName { get; set; }
    public string? UnitName { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseOrderDto
{
    public long PurchaseOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public long StockLocationId { get; set; }
    public string StockLocationName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public IEnumerable<InventoryLineDto> Lines { get; set; } = Array.Empty<InventoryLineDto>();
}

public class SavePurchaseOrderDto
{
    public long SupplierId { get; set; }
    public long StockLocationId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Status { get; set; } = PurchaseOrderStatus.Ordered.ToString();
    public string? Notes { get; set; }
    public IEnumerable<SaveInventoryLineDto> Lines { get; set; } = Array.Empty<SaveInventoryLineDto>();
}

public class ReceivePurchaseOrderDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Notes { get; set; }
    public IEnumerable<SaveInventoryLineDto> Lines { get; set; } = Array.Empty<SaveInventoryLineDto>();
}

public class PurchaseInvoiceDto
{
    public long PurchaseInvoiceId { get; set; }
    public long PurchaseOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public long StockLocationId { get; set; }
    public string StockLocationName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public IEnumerable<InventoryLineDto> Lines { get; set; } = Array.Empty<InventoryLineDto>();
}

public class PurchaseReturnDto
{
    public long PurchaseReturnId { get; set; }
    public long PurchaseInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public IEnumerable<InventoryLineDto> Lines { get; set; } = Array.Empty<InventoryLineDto>();
}

public class SavePurchaseReturnDto
{
    public long PurchaseInvoiceId { get; set; }
    public DateTime ReturnDate { get; set; }
    public string? Notes { get; set; }
    public IEnumerable<SaveInventoryLineDto> Lines { get; set; } = Array.Empty<SaveInventoryLineDto>();
}

public class StockTransferDto
{
    public long StockTransferId { get; set; }
    public string TransferNumber { get; set; } = string.Empty;
    public long FromStockLocationId { get; set; }
    public string FromStockLocationName { get; set; } = string.Empty;
    public long ToStockLocationId { get; set; }
    public string ToStockLocationName { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public decimal TotalQuantity { get; set; }
    public IEnumerable<InventoryLineDto> Lines { get; set; } = Array.Empty<InventoryLineDto>();
}

public class SaveStockTransferDto
{
    public long FromStockLocationId { get; set; }
    public long ToStockLocationId { get; set; }
    public DateTime TransferDate { get; set; }
    public string? Notes { get; set; }
    public IEnumerable<SaveInventoryLineDto> Lines { get; set; } = Array.Empty<SaveInventoryLineDto>();
}

public class ApproveStockTransferDto
{
    public string? Notes { get; set; }
}

public class ReceiveStockTransferDto
{
    public string? Notes { get; set; }
}

public class StockAdjustmentDto
{
    public long StockAdjustmentId { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public long StockLocationId { get; set; }
    public string StockLocationName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; }
    public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public decimal TotalDelta { get; set; }
    public IEnumerable<InventoryLineDto> Lines { get; set; } = Array.Empty<InventoryLineDto>();
}

public class SaveStockAdjustmentDto
{
    public long StockLocationId { get; set; }
    public string Reason { get; set; } = StockAdjustmentReason.ManualCorrection.ToString();
    public DateTime AdjustmentDate { get; set; }
    public string? Notes { get; set; }
    public IEnumerable<SaveInventoryLineDto> Lines { get; set; } = Array.Empty<SaveInventoryLineDto>();
}

public class ApproveStockAdjustmentDto
{
    public string? Notes { get; set; }
}

public class SupplierDueSummaryDto
{
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public int OpenInvoiceCount { get; set; }
    public decimal TotalInvoiceAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ReturnAmount { get; set; }
    public decimal DueAmount { get; set; }
}
