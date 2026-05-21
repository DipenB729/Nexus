using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace AngularApp4.Model.Hms;

public enum InventoryCategoryType
{
    Medicine,
    Item
}

public enum InventoryItemType
{
    Consumable,
    SurgicalItem,
    LabReagent,
    NonMedicalSupply,
    EquipmentSpare
}

public enum StockLocationType
{
    MainStore,
    PharmacyStore,
    LabStore,
    OtStore,
    WardStock
}

public enum PurchaseOrderStatus
{
    Draft,
    Ordered,
    PartiallyReceived,
    Received,
    Cancelled
}

public enum PurchaseInvoiceStatus
{
    Open,
    Partial,
    Paid,
    Cancelled
}

public enum TransferStatus
{
    Pending,
    Approved,
    Received,
    Rejected
}

public enum StockAdjustmentReason
{
    Damage,
    Loss,
    ExpiryRemoval,
    ManualCorrection
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

public class InventoryUnit
{
    [Key, BsonElement("_id")] public long InventoryUnitId { get; set; }
    [Required, MaxLength(80)] public string Name { get; set; } = string.Empty;
    [MaxLength(30)] public string? ShortName { get; set; }
    [MaxLength(250)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class InventoryCategory
{
    [Key, BsonElement("_id")] public long InventoryCategoryId { get; set; }
    public InventoryCategoryType CategoryType { get; set; } = InventoryCategoryType.Item;
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(250)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class MedicineMaster
{
    [Key, BsonElement("_id")] public long MedicineMasterId { get; set; }
    public long InventoryUnitId { get; set; }
    public long? InventoryCategoryId { get; set; }
    [Required, MaxLength(150)] public string MedicineName { get; set; } = string.Empty;
    [MaxLength(150)] public string? GenericName { get; set; }
    [MaxLength(120)] public string? Brand { get; set; }
    [MaxLength(80)] public string? Strength { get; set; }
    public bool BatchRequired { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public InventoryUnit? InventoryUnit { get; set; }
    public InventoryCategory? InventoryCategory { get; set; }
}

public class StockItemMaster
{
    [Key, BsonElement("_id")] public long StockItemMasterId { get; set; }
    public long InventoryUnitId { get; set; }
    public long? InventoryCategoryId { get; set; }
    public InventoryItemType ItemType { get; set; } = InventoryItemType.Consumable;
    [Required, MaxLength(150)] public string ItemName { get; set; } = string.Empty;
    [MaxLength(150)] public string? Specification { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public InventoryUnit? InventoryUnit { get; set; }
    public InventoryCategory? InventoryCategory { get; set; }
}

public class Supplier
{
    [Key, BsonElement("_id")] public long SupplierId { get; set; }
    [Required, MaxLength(150)] public string SupplierName { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string SupplierCode { get; set; } = string.Empty;
    [MaxLength(120)] public string? ContactPerson { get; set; }
    [MaxLength(30)] public string? ContactPhone { get; set; }
    [MaxLength(150)] public string? ContactEmail { get; set; }
    [MaxLength(300)] public string? Address { get; set; }
    public int PaymentTermsDays { get; set; }
    [MaxLength(400)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class StockLocation
{
    [Key, BsonElement("_id")] public long StockLocationId { get; set; }
    public long? BranchId { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string Code { get; set; } = string.Empty;
    public StockLocationType LocationType { get; set; } = StockLocationType.MainStore;
    [MaxLength(250)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Branch? Branch { get; set; }
}

public class PurchaseOrder
{
    [Key, BsonElement("_id")] public long PurchaseOrderId { get; set; }
    [Required, MaxLength(30)] public string OrderNumber { get; set; } = string.Empty;
    public long SupplierId { get; set; }
    public long StockLocationId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? ExpectedDeliveryDate { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    [MaxLength(400)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Supplier? Supplier { get; set; }
    public StockLocation? StockLocation { get; set; }
}

public class PurchaseOrderLine
{
    [Key, BsonElement("_id")] public long PurchaseOrderLineId { get; set; }
    public long PurchaseOrderId { get; set; }
    public long? MedicineMasterId { get; set; }
    public long? StockItemMasterId { get; set; }
    [Required, MaxLength(180)] public string ItemName { get; set; } = string.Empty;
    [MaxLength(40)] public string? UnitName { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    [MaxLength(250)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }
    public MedicineMaster? MedicineMaster { get; set; }
    public StockItemMaster? StockItemMaster { get; set; }
}

public class PurchaseInvoice
{
    [Key, BsonElement("_id")] public long PurchaseInvoiceId { get; set; }
    public long PurchaseOrderId { get; set; }
    public long SupplierId { get; set; }
    public long StockLocationId { get; set; }
    [Required, MaxLength(50)] public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public PurchaseInvoiceStatus Status { get; set; } = PurchaseInvoiceStatus.Open;
    [MaxLength(400)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }
    public Supplier? Supplier { get; set; }
    public StockLocation? StockLocation { get; set; }
}

public class PurchaseInvoiceLine
{
    [Key, BsonElement("_id")] public long PurchaseInvoiceLineId { get; set; }
    public long PurchaseInvoiceId { get; set; }
    public long? PurchaseOrderLineId { get; set; }
    public long? MedicineMasterId { get; set; }
    public long? StockItemMasterId { get; set; }
    [Required, MaxLength(180)] public string ItemName { get; set; } = string.Empty;
    [MaxLength(40)] public string? UnitName { get; set; }
    [MaxLength(60)] public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    [MaxLength(250)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PurchaseInvoice? PurchaseInvoice { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
    public MedicineMaster? MedicineMaster { get; set; }
    public StockItemMaster? StockItemMaster { get; set; }
}

public class PurchaseReturn
{
    [Key, BsonElement("_id")] public long PurchaseReturnId { get; set; }
    public long PurchaseInvoiceId { get; set; }
    public long SupplierId { get; set; }
    [Required, MaxLength(30)] public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow.Date;
    public decimal TotalAmount { get; set; }
    [MaxLength(400)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PurchaseInvoice? PurchaseInvoice { get; set; }
    public Supplier? Supplier { get; set; }
}

public class PurchaseReturnLine
{
    [Key, BsonElement("_id")] public long PurchaseReturnLineId { get; set; }
    public long PurchaseReturnId { get; set; }
    public long? PurchaseInvoiceLineId { get; set; }
    public long? MedicineMasterId { get; set; }
    public long? StockItemMasterId { get; set; }
    [Required, MaxLength(180)] public string ItemName { get; set; } = string.Empty;
    [MaxLength(60)] public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    [MaxLength(200)] public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PurchaseReturn? PurchaseReturn { get; set; }
    public PurchaseInvoiceLine? PurchaseInvoiceLine { get; set; }
    public MedicineMaster? MedicineMaster { get; set; }
    public StockItemMaster? StockItemMaster { get; set; }
}

public class StockBatch
{
    [Key, BsonElement("_id")] public long StockBatchId { get; set; }
    public long StockLocationId { get; set; }
    public long? MedicineMasterId { get; set; }
    public long? StockItemMasterId { get; set; }
    [MaxLength(60)] public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime LastMovementAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public StockLocation? StockLocation { get; set; }
    public MedicineMaster? MedicineMaster { get; set; }
    public StockItemMaster? StockItemMaster { get; set; }
}

public class StockTransfer
{
    [Key, BsonElement("_id")] public long StockTransferId { get; set; }
    [Required, MaxLength(30)] public string TransferNumber { get; set; } = string.Empty;
    public long FromStockLocationId { get; set; }
    public long ToStockLocationId { get; set; }
    public DateTime TransferDate { get; set; } = DateTime.UtcNow.Date;
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    [MaxLength(400)] public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public StockLocation? FromStockLocation { get; set; }
    public StockLocation? ToStockLocation { get; set; }
}

public class StockTransferLine
{
    [Key, BsonElement("_id")] public long StockTransferLineId { get; set; }
    public long StockTransferId { get; set; }
    public long StockBatchId { get; set; }
    [Required, MaxLength(180)] public string ItemName { get; set; } = string.Empty;
    [MaxLength(60)] public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public StockTransfer? StockTransfer { get; set; }
    public StockBatch? StockBatch { get; set; }
}

public class StockAdjustment
{
    [Key, BsonElement("_id")] public long StockAdjustmentId { get; set; }
    [Required, MaxLength(30)] public string AdjustmentNumber { get; set; } = string.Empty;
    public long StockLocationId { get; set; }
    public StockAdjustmentReason Reason { get; set; } = StockAdjustmentReason.ManualCorrection;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow.Date;
    [MaxLength(400)] public string? Notes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public StockLocation? StockLocation { get; set; }
}

public class StockAdjustmentLine
{
    [Key, BsonElement("_id")] public long StockAdjustmentLineId { get; set; }
    public long StockAdjustmentId { get; set; }
    public long StockBatchId { get; set; }
    [Required, MaxLength(180)] public string ItemName { get; set; } = string.Empty;
    [MaxLength(60)] public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal QuantityDelta { get; set; }
    [MaxLength(250)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public StockAdjustment? StockAdjustment { get; set; }
    public StockBatch? StockBatch { get; set; }
}
