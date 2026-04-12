export type InventoryCategoryType = 'Medicine' | 'Item';
export type InventoryItemType = 'Consumable' | 'SurgicalItem' | 'LabReagent' | 'NonMedicalSupply' | 'EquipmentSpare';
export type StockLocationType = 'MainStore' | 'PharmacyStore' | 'LabStore' | 'OtStore' | 'WardStock';
export type PurchaseOrderStatus = 'Draft' | 'Ordered' | 'PartiallyReceived' | 'Received' | 'Cancelled';
export type PurchaseInvoiceStatus = 'Open' | 'Partial' | 'Paid' | 'Cancelled';
export type TransferStatus = 'Pending' | 'Approved' | 'Received' | 'Rejected';
export type StockAdjustmentReason = 'Damage' | 'Loss' | 'ExpiryRemoval' | 'ManualCorrection';
export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected';

export interface InventoryDashboard {
  activeLocations: number;
  activeSuppliers: number;
  openPurchaseOrders: number;
  pendingTransfers: number;
  pendingAdjustments: number;
  lowStockCount: number;
  nearExpiryCount: number;
  expiredCount: number;
  supplierDueAmount: number;
  lowStockAlerts: InventoryAlert[];
  nearExpiryAlerts: InventoryAlert[];
  supplierDues: SupplierDueSummary[];
}

export interface InventoryAlert {
  stockBatchId: number;
  itemType: string;
  itemName: string;
  unitName: string;
  locationName: string;
  batchNumber?: string | null;
  expiryDate?: string | null;
  quantityOnHand: number;
  minimumStock: number;
  isExpiredLocked: boolean;
}

export interface InventoryUnit {
  inventoryUnitId: number;
  name: string;
  shortName?: string | null;
  description?: string | null;
  isActive: boolean;
}

export interface SaveInventoryUnitPayload {
  name: string;
  shortName?: string | null;
  description?: string | null;
  isActive: boolean;
}

export interface InventoryCategory {
  inventoryCategoryId: number;
  categoryType: InventoryCategoryType;
  name: string;
  description?: string | null;
  isActive: boolean;
}

export interface SaveInventoryCategoryPayload {
  categoryType: InventoryCategoryType;
  name: string;
  description?: string | null;
  isActive: boolean;
}

export interface MedicineMaster {
  medicineMasterId: number;
  medicineName: string;
  genericName?: string | null;
  brand?: string | null;
  unitName: string;
  inventoryUnitId: number;
  categoryName?: string | null;
  inventoryCategoryId?: number | null;
  strength?: string | null;
  batchRequired: boolean;
  minimumStock: number;
  maximumStock: number;
  isActive: boolean;
}

export interface SaveMedicineMasterPayload {
  medicineName: string;
  genericName?: string | null;
  brand?: string | null;
  inventoryUnitId: number;
  inventoryCategoryId?: number | null;
  strength?: string | null;
  batchRequired: boolean;
  minimumStock: number;
  maximumStock: number;
  isActive: boolean;
}

export interface StockItemMaster {
  stockItemMasterId: number;
  itemType: InventoryItemType;
  itemName: string;
  specification?: string | null;
  unitName: string;
  inventoryUnitId: number;
  categoryName?: string | null;
  inventoryCategoryId?: number | null;
  minimumStock: number;
  maximumStock: number;
  isActive: boolean;
}

export interface SaveStockItemMasterPayload {
  itemType: InventoryItemType;
  itemName: string;
  specification?: string | null;
  inventoryUnitId: number;
  inventoryCategoryId?: number | null;
  minimumStock: number;
  maximumStock: number;
  isActive: boolean;
}

export interface Supplier {
  supplierId: number;
  supplierName: string;
  supplierCode: string;
  contactPerson?: string | null;
  contactPhone?: string | null;
  contactEmail?: string | null;
  address?: string | null;
  paymentTermsDays: number;
  notes?: string | null;
  isActive: boolean;
  purchaseOrderCount: number;
  purchaseInvoiceCount: number;
  totalPurchasedAmount: number;
  dueAmount: number;
  lastPurchaseDate?: string | null;
}

export interface SaveSupplierPayload {
  supplierName: string;
  supplierCode: string;
  contactPerson?: string | null;
  contactPhone?: string | null;
  contactEmail?: string | null;
  address?: string | null;
  paymentTermsDays: number;
  notes?: string | null;
  isActive: boolean;
}

export interface StockLocation {
  stockLocationId: number;
  branchId?: number | null;
  branchName: string;
  name: string;
  code: string;
  locationType: StockLocationType;
  description?: string | null;
  isActive: boolean;
}

export interface SaveStockLocationPayload {
  branchId?: number | null;
  name: string;
  code: string;
  locationType: StockLocationType;
  description?: string | null;
  isActive: boolean;
}

export interface StockBatch {
  stockBatchId: number;
  stockLocationId: number;
  locationName: string;
  itemType: string;
  medicineMasterId?: number | null;
  stockItemMasterId?: number | null;
  itemName: string;
  unitName: string;
  categoryName?: string | null;
  batchNumber?: string | null;
  expiryDate?: string | null;
  quantityOnHand: number;
  unitCost: number;
  minimumStock: number;
  maximumStock: number;
  isNearExpiry: boolean;
  isExpiredLocked: boolean;
}

export interface InventoryLine {
  referenceLineId: number;
  stockBatchId?: number | null;
  medicineMasterId?: number | null;
  stockItemMasterId?: number | null;
  itemType: string;
  itemName: string;
  unitName: string;
  batchNumber?: string | null;
  expiryDate?: string | null;
  quantity: number;
  receivedQuantity: number;
  unitCost: number;
  lineTotal: number;
  reason?: string | null;
  notes?: string | null;
}

export interface SaveInventoryLinePayload {
  referenceLineId?: number | null;
  stockBatchId?: number | null;
  medicineMasterId?: number | null;
  stockItemMasterId?: number | null;
  itemName?: string | null;
  unitName?: string | null;
  batchNumber?: string | null;
  expiryDate?: string | null;
  quantity: number;
  unitCost: number;
  reason?: string | null;
  notes?: string | null;
}

export interface PurchaseOrder {
  purchaseOrderId: number;
  orderNumber: string;
  supplierId: number;
  supplierName: string;
  stockLocationId: number;
  stockLocationName: string;
  orderDate: string;
  expectedDeliveryDate?: string | null;
  status: PurchaseOrderStatus;
  notes?: string | null;
  totalAmount: number;
  orderedQuantity: number;
  receivedQuantity: number;
  lines: InventoryLine[];
}

export interface SavePurchaseOrderPayload {
  supplierId: number;
  stockLocationId: number;
  orderDate: string;
  expectedDeliveryDate?: string | null;
  status: PurchaseOrderStatus;
  notes?: string | null;
  lines: SaveInventoryLinePayload[];
}

export interface ReceivePurchaseOrderPayload {
  invoiceNumber: string;
  invoiceDate: string;
  dueDate?: string | null;
  paidAmount: number;
  notes?: string | null;
  lines: SaveInventoryLinePayload[];
}

export interface PurchaseInvoice {
  purchaseInvoiceId: number;
  purchaseOrderId: number;
  orderNumber: string;
  supplierId: number;
  supplierName: string;
  stockLocationId: number;
  stockLocationName: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate?: string | null;
  totalAmount: number;
  paidAmount: number;
  dueAmount: number;
  status: PurchaseInvoiceStatus;
  notes?: string | null;
  lines: InventoryLine[];
}

export interface PurchaseReturn {
  purchaseReturnId: number;
  purchaseInvoiceId: number;
  invoiceNumber: string;
  supplierId: number;
  supplierName: string;
  returnNumber: string;
  returnDate: string;
  totalAmount: number;
  notes?: string | null;
  lines: InventoryLine[];
}

export interface SavePurchaseReturnPayload {
  purchaseInvoiceId: number;
  returnDate: string;
  notes?: string | null;
  lines: SaveInventoryLinePayload[];
}

export interface SupplierDueSummary {
  supplierId: number;
  supplierName: string;
  supplierCode: string;
  openInvoiceCount: number;
  totalInvoiceAmount: number;
  paidAmount: number;
  returnAmount: number;
  dueAmount: number;
}

export interface StockTransfer {
  stockTransferId: number;
  transferNumber: string;
  fromStockLocationId: number;
  fromStockLocationName: string;
  toStockLocationId: number;
  toStockLocationName: string;
  transferDate: string;
  status: TransferStatus;
  notes?: string | null;
  approvedAt?: string | null;
  receivedAt?: string | null;
  totalQuantity: number;
  lines: InventoryLine[];
}

export interface SaveStockTransferPayload {
  fromStockLocationId: number;
  toStockLocationId: number;
  transferDate: string;
  notes?: string | null;
  lines: SaveInventoryLinePayload[];
}

export interface StockAdjustment {
  stockAdjustmentId: number;
  adjustmentNumber: string;
  stockLocationId: number;
  stockLocationName: string;
  reason: StockAdjustmentReason;
  status: ApprovalStatus;
  adjustmentDate: string;
  notes?: string | null;
  approvedAt?: string | null;
  totalDelta: number;
  lines: InventoryLine[];
}

export interface SaveStockAdjustmentPayload {
  stockLocationId: number;
  reason: StockAdjustmentReason;
  adjustmentDate: string;
  notes?: string | null;
  lines: SaveInventoryLinePayload[];
}
