import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Observable, Subscription, catchError, filter, forkJoin, map, of } from 'rxjs';
import { BranchSettings, OrganizationSettings } from '../../core/models/hms/admin-ops.model';
import {
  InventoryCategory,
  InventoryCategoryType,
  InventoryDashboard,
  InventoryLine,
  InventoryItemType,
  InventoryUnit,
  MedicineMaster,
  PurchaseInvoice,
  PurchaseOrder,
  PurchaseOrderStatus,
  PurchaseReturn,
  SaveInventoryCategoryPayload,
  SaveInventoryLinePayload,
  SaveInventoryUnitPayload,
  SaveMedicineMasterPayload,
  SavePurchaseOrderPayload,
  SavePurchaseReturnPayload,
  SaveStockAdjustmentPayload,
  SaveStockItemMasterPayload,
  SaveStockLocationPayload,
  SaveStockTransferPayload,
  SaveSupplierPayload,
  StockAdjustment,
  StockAdjustmentReason,
  StockBatch,
  StockItemMaster,
  StockLocation,
  StockLocationType,
  StockTransfer,
  Supplier,
  SupplierDueSummary
} from '../../core/models/hms/phase5-inventory.model';
import { AdminOpsService } from '../../core/services/hms/admin-ops.service';
import { Phase5InventoryService } from '../../core/services/hms/phase5-inventory.service';

type InventorySection = 'dashboard' | 'units' | 'categories' | 'medicines' | 'items' | 'suppliers' | 'locations' | 'purchases' | 'returns' | 'batches' | 'transfers' | 'adjustments';
type PurchaseLineEntryType = 'Medicine' | 'Item';
type ViewMode = 'index' | 'details' | 'create' | 'edit';

interface SectionOption { key: InventorySection; label: string; icon: string; description: string; }
interface PurchaseLineFormState extends SaveInventoryLinePayload { entryType: PurchaseLineEntryType; }
interface PurchaseOrderFormState { supplierId: number | null; stockLocationId: number | null; orderDate: string; expectedDeliveryDate: string; status: PurchaseOrderStatus; notes: string; lines: PurchaseLineFormState[]; }
interface ReceiveLineFormState extends SaveInventoryLinePayload { itemName: string; unitName: string; batchRequired: boolean; pendingQuantity: number; }
interface ReceivePurchaseFormState { invoiceNumber: string; invoiceDate: string; dueDate: string; paidAmount: number; notes: string; lines: ReceiveLineFormState[]; }
interface ReturnLineFormState extends SaveInventoryLinePayload { itemName: string; maxQuantity: number; }
interface PurchaseReturnFormState { purchaseInvoiceId: number | null; returnDate: string; notes: string; lines: ReturnLineFormState[]; }
interface TransferLineFormState extends SaveInventoryLinePayload { availableQuantity: number; itemName: string; }
interface TransferFormState { fromStockLocationId: number | null; toStockLocationId: number | null; transferDate: string; notes: string; lines: TransferLineFormState[]; }
interface AdjustmentLineFormState extends SaveInventoryLinePayload { availableQuantity: number; itemName: string; }
interface AdjustmentFormState { stockLocationId: number | null; reason: StockAdjustmentReason; adjustmentDate: string; notes: string; lines: AdjustmentLineFormState[]; }
interface InventoryDetailField { label: string; value: string; fullWidth?: boolean; }
interface InventoryDetailSummary {
  tone: 'amber' | 'blue' | 'teal' | 'violet' | 'rose' | 'slate';
  kicker: string;
  title: string;
  description: string;
  badges: string[];
  statusLabel?: string;
  statusTone?: 'success' | 'warning' | 'danger' | 'info' | 'inactive';
  fields: InventoryDetailField[];
}

const SECTION_OPTIONS: ReadonlyArray<SectionOption> = [
  { key: 'dashboard', label: 'Overview', icon: 'monitoring', description: 'Low stock, near-expiry, due suppliers, and inventory pressure.' },
  { key: 'units', label: 'Units', icon: 'straighten', description: 'Box, piece, vial, strip, and other inventory measurement units.' },
  { key: 'categories', label: 'Categories', icon: 'category', description: 'Medicine categories and item categories used across stock masters.' },
  { key: 'medicines', label: 'Medicine Master', icon: 'medication', description: 'Generic, brand, unit, category, batch rule, and reorder limits.' },
  { key: 'items', label: 'Item Master', icon: 'inventory_2', description: 'Consumables, surgical, lab, non-medical, and spare items.' },
  { key: 'suppliers', label: 'Suppliers', icon: 'local_shipping', description: 'Contacts, payment terms, history, and due summary.' },
  { key: 'locations', label: 'Stock Locations', icon: 'warehouse', description: 'Main store, pharmacy, lab, OT, and ward stock locations.' },
  { key: 'purchases', label: 'Purchases', icon: 'receipt_long', description: 'Purchase orders, stock receipt, invoices, and supplier dues.' },
  { key: 'returns', label: 'Purchase Returns', icon: 'assignment_return', description: 'Return stock to suppliers against purchase invoices.' },
  { key: 'batches', label: 'Batches & Expiry', icon: 'sell', description: 'Batch tracking, expiry dates, near-expiry alert, and expiry lock.' },
  { key: 'transfers', label: 'Transfers', icon: 'swap_horiz', description: 'Move stock between stores with approval and receipt tracking.' },
  { key: 'adjustments', label: 'Adjustments', icon: 'rule', description: 'Damage, loss, expiry removal, and manual correction with approval.' }
];

function currentDateInput(): string { return new Date().toISOString().slice(0, 10); }
function createEmptyUnitForm(): SaveInventoryUnitPayload { return { name: '', shortName: '', description: '', isActive: true }; }
function createEmptyCategoryForm(): SaveInventoryCategoryPayload { return { categoryType: 'Medicine', name: '', description: '', isActive: true }; }
function createEmptyMedicineForm(): SaveMedicineMasterPayload { return { medicineName: '', genericName: '', brand: '', inventoryUnitId: 0, inventoryCategoryId: null, strength: '', batchRequired: true, minimumStock: 0, maximumStock: 0, isActive: true }; }
function createEmptyItemForm(): SaveStockItemMasterPayload { return { itemType: 'Consumable', itemName: '', specification: '', inventoryUnitId: 0, inventoryCategoryId: null, minimumStock: 0, maximumStock: 0, isActive: true }; }
function createEmptySupplierForm(): SaveSupplierPayload { return { supplierName: '', supplierCode: '', contactPerson: '', contactPhone: '', contactEmail: '', address: '', paymentTermsDays: 0, notes: '', isActive: true }; }
function createEmptyLocationForm(): SaveStockLocationPayload { return { branchId: null, name: '', code: '', locationType: 'MainStore', description: '', isActive: true }; }
function createEmptyPurchaseLine(): PurchaseLineFormState { return { entryType: 'Medicine', referenceLineId: null, medicineMasterId: null, stockItemMasterId: null, quantity: 1, unitCost: 0, notes: '' }; }
function createEmptyPurchaseOrderForm(): PurchaseOrderFormState { return { supplierId: null, stockLocationId: null, orderDate: currentDateInput(), expectedDeliveryDate: '', status: 'Ordered', notes: '', lines: [createEmptyPurchaseLine()] }; }
function createEmptyReceiveForm(): ReceivePurchaseFormState { return { invoiceNumber: '', invoiceDate: currentDateInput(), dueDate: '', paidAmount: 0, notes: '', lines: [] }; }
function createEmptyReturnForm(): PurchaseReturnFormState { return { purchaseInvoiceId: null, returnDate: currentDateInput(), notes: '', lines: [] }; }
function createEmptyTransferLine(): TransferLineFormState { return { stockBatchId: null, quantity: 0, unitCost: 0, notes: '', availableQuantity: 0, itemName: '' }; }
function createEmptyTransferForm(): TransferFormState { return { fromStockLocationId: null, toStockLocationId: null, transferDate: currentDateInput(), notes: '', lines: [createEmptyTransferLine()] }; }
function createEmptyAdjustmentLine(): AdjustmentLineFormState { return { stockBatchId: null, quantity: 0, unitCost: 0, notes: '', availableQuantity: 0, itemName: '' }; }
function createEmptyAdjustmentForm(): AdjustmentFormState { return { stockLocationId: null, reason: 'ManualCorrection', adjustmentDate: currentDateInput(), notes: '', lines: [createEmptyAdjustmentLine()] }; }

@Component({
  selector: 'app-inventory-admin',
  templateUrl: './inventory-admin.component.html',
  styleUrls: ['./inventory-admin.component.scss']
})
export class InventoryAdminComponent implements OnInit, OnDestroy {
  readonly sections = SECTION_OPTIONS;
  readonly categoryTypeOptions: InventoryCategoryType[] = ['Medicine', 'Item'];
  readonly itemTypeOptions: InventoryItemType[] = ['Consumable', 'SurgicalItem', 'LabReagent', 'NonMedicalSupply', 'EquipmentSpare'];
  readonly locationTypeOptions: StockLocationType[] = ['MainStore', 'PharmacyStore', 'LabStore', 'OtStore', 'WardStock'];
  readonly purchaseStatusOptions: PurchaseOrderStatus[] = ['Draft', 'Ordered', 'Cancelled'];
  readonly adjustmentReasonOptions: StockAdjustmentReason[] = ['Damage', 'Loss', 'ExpiryRemoval', 'ManualCorrection'];

  activeSection: InventorySection = 'dashboard';
  viewMode: ViewMode = 'index';
  selectedRecordId: number | null = null;
  searchTerm = '';
  dashboard: InventoryDashboard = { activeLocations: 0, activeSuppliers: 0, openPurchaseOrders: 0, pendingTransfers: 0, pendingAdjustments: 0, lowStockCount: 0, nearExpiryCount: 0, expiredCount: 0, supplierDueAmount: 0, lowStockAlerts: [], nearExpiryAlerts: [], supplierDues: [] };
  branches: BranchSettings[] = [];
  units: InventoryUnit[] = [];
  categories: InventoryCategory[] = [];
  medicines: MedicineMaster[] = [];
  items: StockItemMaster[] = [];
  suppliers: Supplier[] = [];
  locations: StockLocation[] = [];
  batches: StockBatch[] = [];
  purchaseOrders: PurchaseOrder[] = [];
  purchaseInvoices: PurchaseInvoice[] = [];
  purchaseReturns: PurchaseReturn[] = [];
  supplierDues: SupplierDueSummary[] = [];
  transfers: StockTransfer[] = [];
  adjustments: StockAdjustment[] = [];

  unitForm = createEmptyUnitForm();
  categoryForm = createEmptyCategoryForm();
  medicineForm = createEmptyMedicineForm();
  itemForm = createEmptyItemForm();
  supplierForm = createEmptySupplierForm();
  locationForm = createEmptyLocationForm();
  purchaseForm = createEmptyPurchaseOrderForm();
  receiveForm = createEmptyReceiveForm();
  returnForm = createEmptyReturnForm();
  transferForm = createEmptyTransferForm();
  adjustmentForm = createEmptyAdjustmentForm();

  editingUnitId: number | null = null;
  editingCategoryId: number | null = null;
  editingMedicineId: number | null = null;
  editingItemId: number | null = null;
  editingSupplierId: number | null = null;
  editingLocationId: number | null = null;
  editingPurchaseOrderId: number | null = null;
  receivingPurchaseOrderId: number | null = null;
  editingTransferId: number | null = null;
  editingAdjustmentId: number | null = null;
  isLoading = true;
  isSaving = false;
  errorMessage = '';
  successMessage = '';
  private routeSub?: Subscription;

  constructor(
    private readonly router: Router,
    private readonly adminOps: AdminOpsService,
    private readonly inventory: Phase5InventoryService
  ) {}

  ngOnInit(): void {
    this.syncRoute(this.router.url);
    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.syncRoute(event.urlAfterRedirects));
    this.loadData();
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  get currentSection(): SectionOption { return this.sections.find((section) => section.key === this.activeSection) ?? this.sections[0]; }
  get currentRouteBase(): string[] { return ['/admin/inventory', this.activeSection]; }
  get isIndexView(): boolean { return this.viewMode === 'index'; }
  get isDetailsView(): boolean { return this.viewMode === 'details'; }
  get isCreateView(): boolean { return this.viewMode === 'create'; }
  get isEditView(): boolean { return this.viewMode === 'edit'; }
  get isFormView(): boolean { return this.isCreateView || this.isEditView; }
  get currentRecordLabel(): string {
    const labels: Record<InventorySection, string> = {
      dashboard: 'Overview',
      units: 'Unit',
      categories: 'Category',
      medicines: 'Medicine',
      items: 'Item',
      suppliers: 'Supplier',
      locations: 'Location',
      purchases: 'Purchase Order',
      returns: 'Purchase Return',
      batches: 'Stock Batch',
      transfers: 'Stock Transfer',
      adjustments: 'Stock Adjustment'
    };

    return labels[this.activeSection];
  }
  get activeBranches(): BranchSettings[] { return this.branches.filter((branch) => branch.isActive); }
  get activeUnits(): InventoryUnit[] { return this.units.filter((unit) => unit.isActive); }
  get medicineCategories(): InventoryCategory[] { return this.categories.filter((category) => category.categoryType === 'Medicine'); }
  get itemCategories(): InventoryCategory[] { return this.categories.filter((category) => category.categoryType === 'Item'); }
  get activeMedicineCategories(): InventoryCategory[] { return this.medicineCategories.filter((category) => category.isActive); }
  get activeItemCategories(): InventoryCategory[] { return this.itemCategories.filter((category) => category.isActive); }
  get activeSuppliers(): Supplier[] { return this.suppliers.filter((supplier) => supplier.isActive); }
  get activeLocations(): StockLocation[] { return this.locations.filter((location) => location.isActive); }
  get activeMedicines(): MedicineMaster[] { return this.medicines.filter((medicine) => medicine.isActive); }
  get activeItems(): StockItemMaster[] { return this.items.filter((item) => item.isActive); }
  get filteredUnits(): InventoryUnit[] { return this.units.filter((x) => this.matchesSearch(x.name, x.shortName, x.description, x.isActive ? 'active' : 'inactive')); }
  get filteredCategories(): InventoryCategory[] { return this.categories.filter((x) => this.matchesSearch(x.name, x.categoryType, x.description, x.isActive ? 'active' : 'inactive')); }
  get filteredMedicines(): MedicineMaster[] { return this.medicines.filter((x) => this.matchesSearch(x.medicineName, x.genericName, x.brand, x.unitName, x.categoryName, x.strength, x.batchRequired ? 'batch' : 'no batch', x.isActive ? 'active' : 'inactive')); }
  get filteredItems(): StockItemMaster[] { return this.items.filter((x) => this.matchesSearch(x.itemName, x.itemType, x.specification, x.unitName, x.categoryName, x.isActive ? 'active' : 'inactive')); }
  get filteredSuppliers(): Supplier[] { return this.suppliers.filter((x) => this.matchesSearch(x.supplierName, x.supplierCode, x.contactPerson, x.contactPhone, x.contactEmail, x.address, x.isActive ? 'active' : 'inactive')); }
  get filteredLocations(): StockLocation[] { return this.locations.filter((x) => this.matchesSearch(x.name, x.code, x.locationType, x.branchName, x.description, x.isActive ? 'active' : 'inactive')); }
  get filteredPurchaseOrders(): PurchaseOrder[] { return this.purchaseOrders.filter((x) => this.matchesSearch(x.orderNumber, x.supplierName, x.stockLocationName, x.status, x.notes)); }
  get filteredPurchaseInvoices(): PurchaseInvoice[] { return this.purchaseInvoices.filter((x) => this.matchesSearch(x.invoiceNumber, x.orderNumber, x.supplierName, x.stockLocationName, x.status, x.notes)); }
  get filteredPurchaseReturns(): PurchaseReturn[] { return this.purchaseReturns.filter((x) => this.matchesSearch(x.returnNumber, x.invoiceNumber, x.supplierName, x.notes)); }
  get filteredSupplierDues(): SupplierDueSummary[] { return this.supplierDues.filter((x) => this.matchesSearch(x.supplierName, x.supplierCode)); }
  get filteredBatches(): StockBatch[] { return this.batches.filter((x) => this.matchesSearch(x.itemName, x.itemType, x.locationName, x.categoryName, x.batchNumber, x.isNearExpiry ? 'near expiry' : '', x.isExpiredLocked ? 'expired locked' : '')); }
  get filteredTransfers(): StockTransfer[] { return this.transfers.filter((x) => this.matchesSearch(x.transferNumber, x.fromStockLocationName, x.toStockLocationName, x.status, x.notes)); }
  get filteredAdjustments(): StockAdjustment[] { return this.adjustments.filter((x) => this.matchesSearch(x.adjustmentNumber, x.stockLocationName, x.reason, x.status, x.notes)); }
  get activeReceiveOrder(): PurchaseOrder | null { return this.receivingPurchaseOrderId ? this.purchaseOrders.find((x) => x.purchaseOrderId === this.receivingPurchaseOrderId) ?? null : null; }
  get selectedReturnInvoice(): PurchaseInvoice | null { return this.returnForm.purchaseInvoiceId ? this.purchaseInvoices.find((x) => x.purchaseInvoiceId === this.returnForm.purchaseInvoiceId) ?? null : null; }
  get transferSourceBatches(): StockBatch[] { return this.transferForm.fromStockLocationId ? this.batches.filter((x) => x.stockLocationId === this.transferForm.fromStockLocationId && x.quantityOnHand > 0 && !x.isExpiredLocked).sort((a, b) => `${a.itemName} ${a.batchNumber ?? ''}`.localeCompare(`${b.itemName} ${b.batchNumber ?? ''}`)) : []; }
  get adjustmentLocationBatches(): StockBatch[] { return this.adjustmentForm.stockLocationId ? this.batches.filter((x) => x.stockLocationId === this.adjustmentForm.stockLocationId).sort((a, b) => `${a.itemName} ${a.batchNumber ?? ''}`.localeCompare(`${b.itemName} ${b.batchNumber ?? ''}`)) : []; }
  get selectedUnit(): InventoryUnit | null { return this.selectedRecordId ? this.units.find((x) => x.inventoryUnitId === this.selectedRecordId) ?? null : null; }
  get selectedCategory(): InventoryCategory | null { return this.selectedRecordId ? this.categories.find((x) => x.inventoryCategoryId === this.selectedRecordId) ?? null : null; }
  get selectedMedicine(): MedicineMaster | null { return this.selectedRecordId ? this.medicines.find((x) => x.medicineMasterId === this.selectedRecordId) ?? null : null; }
  get selectedItem(): StockItemMaster | null { return this.selectedRecordId ? this.items.find((x) => x.stockItemMasterId === this.selectedRecordId) ?? null : null; }
  get selectedSupplier(): Supplier | null { return this.selectedRecordId ? this.suppliers.find((x) => x.supplierId === this.selectedRecordId) ?? null : null; }
  get selectedLocation(): StockLocation | null { return this.selectedRecordId ? this.locations.find((x) => x.stockLocationId === this.selectedRecordId) ?? null : null; }
  get selectedPurchaseOrder(): PurchaseOrder | null { return this.selectedRecordId ? this.purchaseOrders.find((x) => x.purchaseOrderId === this.selectedRecordId) ?? null : null; }
  get selectedPurchaseReturn(): PurchaseReturn | null { return this.selectedRecordId ? this.purchaseReturns.find((x) => x.purchaseReturnId === this.selectedRecordId) ?? null : null; }
  get selectedBatch(): StockBatch | null { return this.selectedRecordId ? this.batches.find((x) => x.stockBatchId === this.selectedRecordId) ?? null : null; }
  get selectedTransfer(): StockTransfer | null { return this.selectedRecordId ? this.transfers.find((x) => x.stockTransferId === this.selectedRecordId) ?? null : null; }
  get selectedAdjustment(): StockAdjustment | null { return this.selectedRecordId ? this.adjustments.find((x) => x.stockAdjustmentId === this.selectedRecordId) ?? null : null; }
  get relatedPurchaseInvoices(): PurchaseInvoice[] {
    return this.selectedPurchaseOrder
      ? this.purchaseInvoices.filter((x) => x.purchaseOrderId === this.selectedPurchaseOrder?.purchaseOrderId)
      : [];
  }
  get hasDetailsRecord(): boolean {
    return !!(
      this.selectedUnit ||
      this.selectedCategory ||
      this.selectedMedicine ||
      this.selectedItem ||
      this.selectedSupplier ||
      this.selectedLocation ||
      this.selectedPurchaseOrder ||
      this.selectedPurchaseReturn ||
      this.selectedBatch ||
      this.selectedTransfer ||
      this.selectedAdjustment
    );
  }
  get canCreateCurrentSection(): boolean {
    return !['dashboard', 'batches'].includes(this.activeSection);
  }
  get sectionRecordCount(): number { return this.getSectionCount(this.activeSection); }
  get canEditSelectedRecord(): boolean {
    switch (this.activeSection) {
      case 'dashboard':
      case 'returns':
      case 'batches':
        return false;
      case 'purchases':
        return !!this.selectedPurchaseOrder && this.canEditPurchaseOrder(this.selectedPurchaseOrder);
      case 'transfers':
        return !!this.selectedTransfer && this.canEditTransfer(this.selectedTransfer);
      case 'adjustments':
        return !!this.selectedAdjustment && this.canEditAdjustment(this.selectedAdjustment);
      default:
        return this.selectedRecordId !== null;
    }
  }
  get detailSummary(): InventoryDetailSummary | null {
    if (this.selectedUnit) {
      return {
        tone: 'amber',
        kicker: 'Inventory unit',
        title: this.selectedUnit.name,
        description: this.selectedUnit.description || 'Measurement unit used across medicines, item masters, and stock movement workflows.',
        badges: [this.selectedUnit.shortName || 'No short name'],
        statusLabel: this.selectedUnit.isActive ? 'Active' : 'Inactive',
        statusTone: this.selectedUnit.isActive ? 'success' : 'inactive',
        fields: [
          { label: 'Unit name', value: this.selectedUnit.name },
          { label: 'Short name', value: this.selectedUnit.shortName || 'Not set' },
          { label: 'Status', value: this.selectedUnit.isActive ? 'Active' : 'Inactive' },
          { label: 'Description', value: this.selectedUnit.description || 'No description provided.', fullWidth: true }
        ]
      };
    }

    if (this.selectedCategory) {
      return {
        tone: 'violet',
        kicker: 'Inventory category',
        title: this.selectedCategory.name,
        description: this.selectedCategory.description || 'Classification used to group medicines and stock items for reporting and controls.',
        badges: [this.formatEnum(this.selectedCategory.categoryType)],
        statusLabel: this.selectedCategory.isActive ? 'Active' : 'Inactive',
        statusTone: this.selectedCategory.isActive ? 'success' : 'inactive',
        fields: [
          { label: 'Category name', value: this.selectedCategory.name },
          { label: 'Category type', value: this.formatEnum(this.selectedCategory.categoryType) },
          { label: 'Status', value: this.selectedCategory.isActive ? 'Active' : 'Inactive' },
          { label: 'Description', value: this.selectedCategory.description || 'No description provided.', fullWidth: true }
        ]
      };
    }

    if (this.selectedMedicine) {
      return {
        tone: 'blue',
        kicker: 'Medicine master',
        title: this.selectedMedicine.medicineName,
        description: [this.selectedMedicine.genericName || this.selectedMedicine.medicineName, this.selectedMedicine.brand ? `Brand ${this.selectedMedicine.brand}` : null, this.selectedMedicine.strength].filter(Boolean).join(' | '),
        badges: [this.selectedMedicine.unitName, this.selectedMedicine.categoryName || 'No category'],
        statusLabel: this.selectedMedicine.isActive ? 'Active' : 'Inactive',
        statusTone: this.selectedMedicine.isActive ? 'success' : 'inactive',
        fields: [
          { label: 'Medicine name', value: this.selectedMedicine.medicineName },
          { label: 'Generic name', value: this.selectedMedicine.genericName || 'Not set' },
          { label: 'Brand', value: this.selectedMedicine.brand || 'Not set' },
          { label: 'Strength', value: this.selectedMedicine.strength || 'Not set' },
          { label: 'Unit', value: this.selectedMedicine.unitName },
          { label: 'Category', value: this.selectedMedicine.categoryName || 'Not set' },
          { label: 'Minimum stock', value: this.formatNumber(this.selectedMedicine.minimumStock) },
          { label: 'Maximum stock', value: this.formatNumber(this.selectedMedicine.maximumStock) },
          { label: 'Batch rule', value: this.selectedMedicine.batchRequired ? 'Batch required' : 'Batch optional' },
          { label: 'Status', value: this.selectedMedicine.isActive ? 'Active' : 'Inactive' }
        ]
      };
    }

    if (this.selectedItem) {
      return {
        tone: 'teal',
        kicker: 'Stock item master',
        title: this.selectedItem.itemName,
        description: this.selectedItem.specification || 'General stock item definition for procurement, issue, and inventory controls.',
        badges: [this.formatEnum(this.selectedItem.itemType), this.selectedItem.unitName],
        statusLabel: this.selectedItem.isActive ? 'Active' : 'Inactive',
        statusTone: this.selectedItem.isActive ? 'success' : 'inactive',
        fields: [
          { label: 'Item name', value: this.selectedItem.itemName },
          { label: 'Item type', value: this.formatEnum(this.selectedItem.itemType) },
          { label: 'Unit', value: this.selectedItem.unitName },
          { label: 'Category', value: this.selectedItem.categoryName || 'Not set' },
          { label: 'Minimum stock', value: this.formatNumber(this.selectedItem.minimumStock) },
          { label: 'Maximum stock', value: this.formatNumber(this.selectedItem.maximumStock) },
          { label: 'Status', value: this.selectedItem.isActive ? 'Active' : 'Inactive' },
          { label: 'Specification', value: this.selectedItem.specification || 'No specification provided.', fullWidth: true }
        ]
      };
    }

    if (this.selectedSupplier) {
      return {
        tone: 'rose',
        kicker: 'Supplier profile',
        title: this.selectedSupplier.supplierName,
        description: [this.selectedSupplier.contactPerson || 'Primary contact pending', this.selectedSupplier.contactPhone].filter(Boolean).join(' | ') || 'Supplier contact details are not fully configured yet.',
        badges: [this.selectedSupplier.supplierCode, `${this.selectedSupplier.paymentTermsDays} day terms`],
        statusLabel: this.selectedSupplier.isActive ? 'Active' : 'Inactive',
        statusTone: this.selectedSupplier.isActive ? 'success' : 'inactive',
        fields: [
          { label: 'Supplier name', value: this.selectedSupplier.supplierName },
          { label: 'Supplier code', value: this.selectedSupplier.supplierCode },
          { label: 'Contact person', value: this.selectedSupplier.contactPerson || 'Not set' },
          { label: 'Contact phone', value: this.selectedSupplier.contactPhone || 'Not set' },
          { label: 'Contact email', value: this.selectedSupplier.contactEmail || 'Not set' },
          { label: 'Payment terms', value: `${this.selectedSupplier.paymentTermsDays} day(s)` },
          { label: 'Purchase orders', value: this.selectedSupplier.purchaseOrderCount.toString() },
          { label: 'Purchase invoices', value: this.selectedSupplier.purchaseInvoiceCount.toString() },
          { label: 'Total purchased', value: this.formatCurrency(this.selectedSupplier.totalPurchasedAmount) },
          { label: 'Due amount', value: this.formatCurrency(this.selectedSupplier.dueAmount) },
          { label: 'Last purchase', value: this.formatDate(this.selectedSupplier.lastPurchaseDate) },
          { label: 'Status', value: this.selectedSupplier.isActive ? 'Active' : 'Inactive' },
          { label: 'Address', value: this.selectedSupplier.address || 'No address added.', fullWidth: true },
          { label: 'Notes', value: this.selectedSupplier.notes || 'No notes added.', fullWidth: true }
        ]
      };
    }

    if (this.selectedLocation) {
      return {
        tone: 'slate',
        kicker: 'Stock location',
        title: this.selectedLocation.name,
        description: `${this.formatEnum(this.selectedLocation.locationType)} stock point${this.selectedLocation.branchName ? ' mapped to ' + this.selectedLocation.branchName : ''}.`,
        badges: [this.selectedLocation.code, this.formatEnum(this.selectedLocation.locationType)],
        statusLabel: this.selectedLocation.isActive ? 'Active' : 'Inactive',
        statusTone: this.selectedLocation.isActive ? 'success' : 'inactive',
        fields: [
          { label: 'Location name', value: this.selectedLocation.name },
          { label: 'Location code', value: this.selectedLocation.code },
          { label: 'Location type', value: this.formatEnum(this.selectedLocation.locationType) },
          { label: 'Branch', value: this.selectedLocation.branchName || 'Not mapped' },
          { label: 'Status', value: this.selectedLocation.isActive ? 'Active' : 'Inactive' },
          { label: 'Description', value: this.selectedLocation.description || 'No description provided.', fullWidth: true }
        ]
      };
    }

    if (this.selectedPurchaseOrder) {
      return {
        tone: 'amber',
        kicker: 'Purchase order',
        title: this.selectedPurchaseOrder.orderNumber,
        description: `${this.selectedPurchaseOrder.supplierName} supply order for ${this.selectedPurchaseOrder.stockLocationName} with ${this.formatNumber(this.selectedPurchaseOrder.receivedQuantity)} of ${this.formatNumber(this.selectedPurchaseOrder.orderedQuantity)} received.`,
        badges: [this.selectedPurchaseOrder.supplierName, this.selectedPurchaseOrder.stockLocationName],
        statusLabel: this.formatEnum(this.selectedPurchaseOrder.status),
        statusTone: this.getStatusTone(this.selectedPurchaseOrder.status),
        fields: [
          { label: 'Order number', value: this.selectedPurchaseOrder.orderNumber },
          { label: 'Supplier', value: this.selectedPurchaseOrder.supplierName },
          { label: 'Stock location', value: this.selectedPurchaseOrder.stockLocationName },
          { label: 'Order date', value: this.formatDate(this.selectedPurchaseOrder.orderDate) },
          { label: 'Expected delivery', value: this.formatDate(this.selectedPurchaseOrder.expectedDeliveryDate) },
          { label: 'Status', value: this.formatEnum(this.selectedPurchaseOrder.status) },
          { label: 'Ordered quantity', value: this.formatNumber(this.selectedPurchaseOrder.orderedQuantity) },
          { label: 'Received quantity', value: this.formatNumber(this.selectedPurchaseOrder.receivedQuantity) },
          { label: 'Total amount', value: this.formatCurrency(this.selectedPurchaseOrder.totalAmount) },
          { label: 'Notes', value: this.selectedPurchaseOrder.notes || 'No notes added.', fullWidth: true }
        ]
      };
    }

    if (this.selectedPurchaseReturn) {
      return {
        tone: 'rose',
        kicker: 'Purchase return',
        title: this.selectedPurchaseReturn.returnNumber,
        description: `Supplier return against invoice ${this.selectedPurchaseReturn.invoiceNumber} with total amount ${this.formatCurrency(this.selectedPurchaseReturn.totalAmount)}.`,
        badges: [this.selectedPurchaseReturn.supplierName, this.selectedPurchaseReturn.invoiceNumber],
        fields: [
          { label: 'Return number', value: this.selectedPurchaseReturn.returnNumber },
          { label: 'Supplier', value: this.selectedPurchaseReturn.supplierName },
          { label: 'Invoice', value: this.selectedPurchaseReturn.invoiceNumber },
          { label: 'Return date', value: this.formatDate(this.selectedPurchaseReturn.returnDate) },
          { label: 'Total amount', value: this.formatCurrency(this.selectedPurchaseReturn.totalAmount) },
          { label: 'Notes', value: this.selectedPurchaseReturn.notes || 'No notes added.', fullWidth: true }
        ]
      };
    }

    if (this.selectedBatch) {
      return {
        tone: 'blue',
        kicker: 'Stock batch',
        title: this.selectedBatch.itemName,
        description: `${this.selectedBatch.batchNumber ? `Batch ${this.selectedBatch.batchNumber}` : 'Non-batched stock'} stored at ${this.selectedBatch.locationName} with on-hand quantity of ${this.formatNumber(this.selectedBatch.quantityOnHand)} ${this.selectedBatch.unitName}.`,
        badges: [this.formatEnum(this.selectedBatch.itemType), this.selectedBatch.locationName],
        statusLabel: this.hasLowStock(this.selectedBatch) ? 'Reorder' : 'Normal',
        statusTone: this.hasLowStock(this.selectedBatch) ? 'warning' : 'success',
        fields: [
          { label: 'Item', value: this.selectedBatch.itemName },
          { label: 'Item type', value: this.formatEnum(this.selectedBatch.itemType) },
          { label: 'Location', value: this.selectedBatch.locationName },
          { label: 'Category', value: this.selectedBatch.categoryName || 'Not set' },
          { label: 'Batch number', value: this.selectedBatch.batchNumber || 'N/A' },
          { label: 'Expiry date', value: this.formatDate(this.selectedBatch.expiryDate) },
          { label: 'Quantity on hand', value: this.formatNumber(this.selectedBatch.quantityOnHand) },
          { label: 'Unit cost', value: this.formatCurrency(this.selectedBatch.unitCost) },
          { label: 'Minimum stock', value: this.formatNumber(this.selectedBatch.minimumStock) },
          { label: 'Maximum stock', value: this.formatNumber(this.selectedBatch.maximumStock) },
          { label: 'Near expiry', value: this.selectedBatch.isNearExpiry ? 'Yes' : 'No' },
          { label: 'Expired lock', value: this.selectedBatch.isExpiredLocked ? 'Locked' : 'Available' }
        ]
      };
    }

    if (this.selectedTransfer) {
      return {
        tone: 'teal',
        kicker: 'Stock transfer',
        title: this.selectedTransfer.transferNumber,
        description: `${this.selectedTransfer.fromStockLocationName} to ${this.selectedTransfer.toStockLocationName} transfer with total quantity of ${this.formatNumber(this.selectedTransfer.totalQuantity)}.`,
        badges: [this.selectedTransfer.fromStockLocationName, this.selectedTransfer.toStockLocationName],
        statusLabel: this.formatEnum(this.selectedTransfer.status),
        statusTone: this.getStatusTone(this.selectedTransfer.status),
        fields: [
          { label: 'Transfer number', value: this.selectedTransfer.transferNumber },
          { label: 'Transfer date', value: this.formatDate(this.selectedTransfer.transferDate) },
          { label: 'From location', value: this.selectedTransfer.fromStockLocationName },
          { label: 'To location', value: this.selectedTransfer.toStockLocationName },
          { label: 'Status', value: this.formatEnum(this.selectedTransfer.status) },
          { label: 'Total quantity', value: this.formatNumber(this.selectedTransfer.totalQuantity) },
          { label: 'Approved at', value: this.formatDateTime(this.selectedTransfer.approvedAt) },
          { label: 'Received at', value: this.formatDateTime(this.selectedTransfer.receivedAt) },
          { label: 'Notes', value: this.selectedTransfer.notes || 'No notes added.', fullWidth: true }
        ]
      };
    }

    if (this.selectedAdjustment) {
      return {
        tone: 'slate',
        kicker: 'Stock adjustment',
        title: this.selectedAdjustment.adjustmentNumber,
        description: `${this.formatEnum(this.selectedAdjustment.reason)} adjustment for ${this.selectedAdjustment.stockLocationName} with total delta of ${this.formatNumber(this.selectedAdjustment.totalDelta)}.`,
        badges: [this.selectedAdjustment.stockLocationName, this.formatEnum(this.selectedAdjustment.reason)],
        statusLabel: this.formatEnum(this.selectedAdjustment.status),
        statusTone: this.getStatusTone(this.selectedAdjustment.status),
        fields: [
          { label: 'Adjustment number', value: this.selectedAdjustment.adjustmentNumber },
          { label: 'Location', value: this.selectedAdjustment.stockLocationName },
          { label: 'Reason', value: this.formatEnum(this.selectedAdjustment.reason) },
          { label: 'Status', value: this.formatEnum(this.selectedAdjustment.status) },
          { label: 'Adjustment date', value: this.formatDate(this.selectedAdjustment.adjustmentDate) },
          { label: 'Total delta', value: this.formatNumber(this.selectedAdjustment.totalDelta) },
          { label: 'Approved at', value: this.formatDateTime(this.selectedAdjustment.approvedAt) },
          { label: 'Notes', value: this.selectedAdjustment.notes || 'No notes added.', fullWidth: true }
        ]
      };
    }

    return null;
  }
  get detailLines(): InventoryLine[] {
    switch (this.activeSection) {
      case 'purchases':
        return this.selectedPurchaseOrder?.lines ?? [];
      case 'returns':
        return this.selectedPurchaseReturn?.lines ?? [];
      case 'transfers':
        return this.selectedTransfer?.lines ?? [];
      case 'adjustments':
        return this.selectedAdjustment?.lines ?? [];
      default:
        return [];
    }
  }
  get detailLineHeading(): string {
    switch (this.activeSection) {
      case 'purchases':
        return 'Order lines';
      case 'returns':
        return 'Return lines';
      case 'transfers':
        return 'Transfer lines';
      case 'adjustments':
        return 'Adjustment lines';
      default:
        return 'Inventory lines';
    }
  }
  get detailLineSubheading(): string {
    switch (this.activeSection) {
      case 'purchases':
        return 'Line items';
      case 'returns':
        return 'Returned items';
      case 'transfers':
        return 'Moved batches';
      case 'adjustments':
        return 'Adjusted batches';
      default:
        return 'Inventory lines';
    }
  }
  get showDetailLines(): boolean {
    return ['purchases', 'returns', 'transfers', 'adjustments'].includes(this.activeSection);
  }
  get showDetailReceivedQuantity(): boolean { return this.activeSection === 'purchases'; }
  get showDetailUnitCost(): boolean { return ['purchases', 'returns'].includes(this.activeSection); }
  get showDetailLineTotal(): boolean { return ['purchases', 'returns'].includes(this.activeSection); }
  get showDetailReason(): boolean { return this.activeSection === 'returns'; }
  get showDetailNotes(): boolean { return this.activeSection === 'adjustments'; }
  get detailRelatedInvoices(): PurchaseInvoice[] {
    return this.activeSection === 'purchases' ? this.relatedPurchaseInvoices : [];
  }

  setSection(section: InventorySection): void { this.router.navigate(['/admin/inventory', section]); }
  goToCreate(): void { this.router.navigate([...this.currentRouteBase, 'create']); }
  backToIndex(): void { this.router.navigate(this.currentRouteBase); }
  goToDetails(recordId: number): void { this.router.navigate([...this.currentRouteBase, recordId]); }
  goToEdit(recordId: number): void { this.router.navigate([...this.currentRouteBase, recordId, 'edit']); }
  refresh(): void { this.loadData(); }
  resetCurrentForm(): void { this.resetFormForSection(this.activeSection); }
  resetUnitForm(): void { this.editingUnitId = null; this.unitForm = createEmptyUnitForm(); }
  editUnit(item: InventoryUnit): void { this.goToEdit(item.inventoryUnitId); }
  saveUnit(): void {
    if (!this.unitForm.name.trim()) { this.errorMessage = 'Unit name is required.'; return; }
    const payload: SaveInventoryUnitPayload = { name: this.unitForm.name.trim(), shortName: this.normalizeOptional(this.unitForm.shortName ?? ''), description: this.normalizeOptional(this.unitForm.description ?? ''), isActive: this.unitForm.isActive };
    this.runSave(this.editingUnitId ? this.inventory.updateUnit(this.editingUnitId, payload) : this.inventory.createUnit(payload), (item) => this.goToDetails(item.inventoryUnitId), this.editingUnitId ? 'Inventory unit updated successfully.' : 'Inventory unit created successfully.', 'Unable to save inventory unit.');
  }
  resetCategoryForm(): void { this.editingCategoryId = null; this.categoryForm = createEmptyCategoryForm(); }
  editCategory(item: InventoryCategory): void { this.goToEdit(item.inventoryCategoryId); }
  saveCategory(): void {
    if (!this.categoryForm.name.trim()) { this.errorMessage = 'Category name is required.'; return; }
    const payload: SaveInventoryCategoryPayload = { categoryType: this.categoryForm.categoryType, name: this.categoryForm.name.trim(), description: this.normalizeOptional(this.categoryForm.description ?? ''), isActive: this.categoryForm.isActive };
    this.runSave(this.editingCategoryId ? this.inventory.updateCategory(this.editingCategoryId, payload) : this.inventory.createCategory(payload), (item) => this.goToDetails(item.inventoryCategoryId), this.editingCategoryId ? 'Inventory category updated successfully.' : 'Inventory category created successfully.', 'Unable to save inventory category.');
  }
  resetMedicineForm(): void { this.editingMedicineId = null; this.medicineForm = createEmptyMedicineForm(); }
  editMedicine(item: MedicineMaster): void {
    this.goToEdit(item.medicineMasterId);
  }
  saveMedicine(): void {
    if (!this.medicineForm.medicineName.trim() || !this.medicineForm.inventoryUnitId) { this.errorMessage = 'Medicine name and unit are required.'; return; }
    const payload: SaveMedicineMasterPayload = { medicineName: this.medicineForm.medicineName.trim(), genericName: this.normalizeOptional(this.medicineForm.genericName ?? ''), brand: this.normalizeOptional(this.medicineForm.brand ?? ''), inventoryUnitId: this.medicineForm.inventoryUnitId, inventoryCategoryId: this.medicineForm.inventoryCategoryId ?? null, strength: this.normalizeOptional(this.medicineForm.strength ?? ''), batchRequired: this.medicineForm.batchRequired, minimumStock: this.medicineForm.minimumStock, maximumStock: this.medicineForm.maximumStock, isActive: this.medicineForm.isActive };
    this.runSave(this.editingMedicineId ? this.inventory.updateMedicine(this.editingMedicineId, payload) : this.inventory.createMedicine(payload), (item) => this.goToDetails(item.medicineMasterId), this.editingMedicineId ? 'Medicine master updated successfully.' : 'Medicine master created successfully.', 'Unable to save medicine master.');
  }
  resetItemForm(): void { this.editingItemId = null; this.itemForm = createEmptyItemForm(); }
  editItem(item: StockItemMaster): void {
    this.goToEdit(item.stockItemMasterId);
  }
  saveItem(): void {
    if (!this.itemForm.itemName.trim() || !this.itemForm.inventoryUnitId) { this.errorMessage = 'Item name and unit are required.'; return; }
    const payload: SaveStockItemMasterPayload = { itemType: this.itemForm.itemType, itemName: this.itemForm.itemName.trim(), specification: this.normalizeOptional(this.itemForm.specification ?? ''), inventoryUnitId: this.itemForm.inventoryUnitId, inventoryCategoryId: this.itemForm.inventoryCategoryId ?? null, minimumStock: this.itemForm.minimumStock, maximumStock: this.itemForm.maximumStock, isActive: this.itemForm.isActive };
    this.runSave(this.editingItemId ? this.inventory.updateItem(this.editingItemId, payload) : this.inventory.createItem(payload), (item) => this.goToDetails(item.stockItemMasterId), this.editingItemId ? 'Item master updated successfully.' : 'Item master created successfully.', 'Unable to save stock item master.');
  }
  resetSupplierForm(): void { this.editingSupplierId = null; this.supplierForm = createEmptySupplierForm(); }
  editSupplier(item: Supplier): void {
    this.goToEdit(item.supplierId);
  }
  saveSupplier(): void {
    if (!this.supplierForm.supplierName.trim() || !this.supplierForm.supplierCode.trim()) { this.errorMessage = 'Supplier name and supplier code are required.'; return; }
    const payload: SaveSupplierPayload = { supplierName: this.supplierForm.supplierName.trim(), supplierCode: this.supplierForm.supplierCode.trim(), contactPerson: this.normalizeOptional(this.supplierForm.contactPerson ?? ''), contactPhone: this.normalizeOptional(this.supplierForm.contactPhone ?? ''), contactEmail: this.normalizeOptional(this.supplierForm.contactEmail ?? ''), address: this.normalizeOptional(this.supplierForm.address ?? ''), paymentTermsDays: this.supplierForm.paymentTermsDays, notes: this.normalizeOptional(this.supplierForm.notes ?? ''), isActive: this.supplierForm.isActive };
    this.runSave(this.editingSupplierId ? this.inventory.updateSupplier(this.editingSupplierId, payload) : this.inventory.createSupplier(payload), (item) => this.goToDetails(item.supplierId), this.editingSupplierId ? 'Supplier updated successfully.' : 'Supplier created successfully.', 'Unable to save supplier.');
  }
  resetLocationForm(): void { this.editingLocationId = null; this.locationForm = createEmptyLocationForm(); }
  editLocation(item: StockLocation): void { this.goToEdit(item.stockLocationId); }
  saveLocation(): void {
    if (!this.locationForm.name.trim() || !this.locationForm.code.trim()) { this.errorMessage = 'Location name and location code are required.'; return; }
    const payload: SaveStockLocationPayload = { branchId: this.locationForm.branchId ?? null, name: this.locationForm.name.trim(), code: this.locationForm.code.trim(), locationType: this.locationForm.locationType, description: this.normalizeOptional(this.locationForm.description ?? ''), isActive: this.locationForm.isActive };
    this.runSave(this.editingLocationId ? this.inventory.updateLocation(this.editingLocationId, payload) : this.inventory.createLocation(payload), (item) => this.goToDetails(item.stockLocationId), this.editingLocationId ? 'Stock location updated successfully.' : 'Stock location created successfully.', 'Unable to save stock location.');
  }
  resetPurchaseForm(): void { this.editingPurchaseOrderId = null; this.purchaseForm = createEmptyPurchaseOrderForm(); }
  startCreatePurchaseOrder(): void { this.goToCreate(); }
  editPurchaseOrder(order: PurchaseOrder): void {
    this.goToEdit(order.purchaseOrderId);
  }
  addPurchaseLine(): void { this.purchaseForm.lines = [...this.purchaseForm.lines, createEmptyPurchaseLine()]; }
  removePurchaseLine(index: number): void { this.purchaseForm.lines = this.purchaseForm.lines.filter((_, i) => i !== index); if (!this.purchaseForm.lines.length) { this.purchaseForm.lines = [createEmptyPurchaseLine()]; } }
  onPurchaseLineEntryTypeChange(line: PurchaseLineFormState): void { line.referenceLineId = null; line.medicineMasterId = null; line.stockItemMasterId = null; }
  savePurchaseOrder(): void {
    if (!this.purchaseForm.supplierId || !this.purchaseForm.stockLocationId || !this.purchaseForm.orderDate) { this.errorMessage = 'Supplier, stock location, and order date are required.'; return; }
    const supplierId = this.purchaseForm.supplierId;
    const stockLocationId = this.purchaseForm.stockLocationId;
    const lines = this.purchaseForm.lines.map((line) => this.toPurchasePayloadLine(line)).filter((line) => line.quantity > 0 && (!!line.medicineMasterId || !!line.stockItemMasterId));
    if (!lines.length) { this.errorMessage = 'At least one valid purchase line is required.'; return; }
    const payload: SavePurchaseOrderPayload = { supplierId, stockLocationId, orderDate: this.purchaseForm.orderDate, expectedDeliveryDate: this.normalizeOptional(this.purchaseForm.expectedDeliveryDate) ?? null, status: this.purchaseForm.status, notes: this.normalizeOptional(this.purchaseForm.notes) ?? null, lines };
    this.runSave(this.editingPurchaseOrderId ? this.inventory.updatePurchaseOrder(this.editingPurchaseOrderId, payload) : this.inventory.createPurchaseOrder(payload), (item) => this.goToDetails(item.purchaseOrderId), this.editingPurchaseOrderId ? 'Purchase order updated successfully.' : 'Purchase order created successfully.', 'Unable to save purchase order.');
  }
  startReceivePurchaseOrder(order: PurchaseOrder): void {
    this.receivingPurchaseOrderId = order.purchaseOrderId;
    this.receiveForm = { invoiceNumber: '', invoiceDate: currentDateInput(), dueDate: '', paidAmount: 0, notes: '', lines: order.lines.filter((line) => line.quantity > line.receivedQuantity).map((line) => ({ referenceLineId: line.referenceLineId, medicineMasterId: line.medicineMasterId ?? null, stockItemMasterId: line.stockItemMasterId ?? null, itemName: line.itemName, unitName: line.unitName, batchNumber: line.batchNumber ?? '', expiryDate: this.toDateInput(line.expiryDate), quantity: Math.max(line.quantity - line.receivedQuantity, 0), unitCost: line.unitCost, notes: line.notes ?? '', batchRequired: this.isInventoryLineBatchRequired(line), pendingQuantity: Math.max(line.quantity - line.receivedQuantity, 0) })) };
  }
  cancelReceivePurchaseOrder(): void { this.receivingPurchaseOrderId = null; this.receiveForm = createEmptyReceiveForm(); }
  receivePurchaseOrder(): void {
    if (!this.receivingPurchaseOrderId || !this.receiveForm.invoiceNumber.trim() || !this.receiveForm.invoiceDate) { this.errorMessage = 'Invoice number and invoice date are required to receive stock.'; return; }
    const purchaseOrderId = this.receivingPurchaseOrderId;
    const lines = this.receiveForm.lines.map((line) => ({ referenceLineId: line.referenceLineId ?? null, medicineMasterId: line.medicineMasterId ?? null, stockItemMasterId: line.stockItemMasterId ?? null, itemName: line.itemName, unitName: line.unitName, batchNumber: this.normalizeOptional(line.batchNumber ?? ''), expiryDate: this.normalizeOptional(line.expiryDate ?? '') ?? null, quantity: line.quantity, unitCost: line.unitCost, notes: this.normalizeOptional(line.notes ?? '') })).filter((line) => line.quantity > 0);
    if (!lines.length) { this.errorMessage = 'Enter at least one received quantity.'; return; }
    if (this.receiveForm.lines.some((line) => line.batchRequired && line.quantity > 0 && !this.normalizeOptional(line.batchNumber ?? ''))) { this.errorMessage = 'Batch number is required for one or more medicine lines.'; return; }
    this.runSave(this.inventory.receivePurchaseOrder(purchaseOrderId, { invoiceNumber: this.receiveForm.invoiceNumber.trim(), invoiceDate: this.receiveForm.invoiceDate, dueDate: this.normalizeOptional(this.receiveForm.dueDate) ?? null, paidAmount: this.receiveForm.paidAmount, notes: this.normalizeOptional(this.receiveForm.notes) ?? null, lines }), () => { this.cancelReceivePurchaseOrder(); this.goToDetails(purchaseOrderId); }, 'Purchase receipt and invoice posted successfully.', 'Unable to receive purchase order.');
  }
  resetReturnForm(): void { this.returnForm = createEmptyReturnForm(); }
  startCreateReturn(): void { this.goToCreate(); }
  onReturnInvoiceChange(): void {
    const invoice = this.selectedReturnInvoice;
    this.returnForm.lines = invoice ? invoice.lines.map((line) => ({ referenceLineId: line.referenceLineId, medicineMasterId: line.medicineMasterId ?? null, stockItemMasterId: line.stockItemMasterId ?? null, itemName: line.itemName, batchNumber: line.batchNumber ?? '', expiryDate: this.toDateInput(line.expiryDate), quantity: 0, unitCost: line.unitCost, reason: '', notes: '', maxQuantity: line.quantity })) : [];
  }
  saveReturn(): void {
    if (!this.returnForm.purchaseInvoiceId || !this.returnForm.returnDate) { this.errorMessage = 'Purchase invoice and return date are required.'; return; }
    const purchaseInvoiceId = this.returnForm.purchaseInvoiceId;
    const lines = this.returnForm.lines.map((line) => ({ referenceLineId: line.referenceLineId ?? null, medicineMasterId: line.medicineMasterId ?? null, stockItemMasterId: line.stockItemMasterId ?? null, batchNumber: this.normalizeOptional(line.batchNumber ?? ''), expiryDate: this.normalizeOptional(line.expiryDate ?? '') ?? null, quantity: line.quantity, unitCost: line.unitCost, reason: this.normalizeOptional(line.reason ?? ''), notes: null })).filter((line) => line.quantity > 0);
    if (!lines.length) { this.errorMessage = 'Enter at least one return quantity.'; return; }
    const payload: SavePurchaseReturnPayload = { purchaseInvoiceId, returnDate: this.returnForm.returnDate, notes: this.normalizeOptional(this.returnForm.notes) ?? null, lines };
    this.runSave(this.inventory.createPurchaseReturn(payload), (item) => this.goToDetails(item.purchaseReturnId), 'Purchase return recorded successfully.', 'Unable to save purchase return.');
  }
  resetTransferForm(): void { this.editingTransferId = null; this.transferForm = createEmptyTransferForm(); }
  startCreateTransfer(): void { this.goToCreate(); }
  editTransfer(transfer: StockTransfer): void {
    this.goToEdit(transfer.stockTransferId);
  }
  onTransferSourceChange(): void { this.transferForm.lines = this.transferForm.lines.map((line) => ({ ...createEmptyTransferLine(), notes: line.notes })); }
  addTransferLine(): void { this.transferForm.lines = [...this.transferForm.lines, createEmptyTransferLine()]; }
  removeTransferLine(index: number): void { this.transferForm.lines = this.transferForm.lines.filter((_, i) => i !== index); if (!this.transferForm.lines.length) { this.transferForm.lines = [createEmptyTransferLine()]; } }
  onTransferBatchChange(line: TransferLineFormState): void { const batch = this.lookupBatch(line.stockBatchId ?? null); line.availableQuantity = batch?.quantityOnHand ?? 0; line.itemName = batch?.itemName ?? ''; if (line.quantity > line.availableQuantity) { line.quantity = line.availableQuantity; } }
  saveTransfer(): void {
    if (!this.transferForm.fromStockLocationId || !this.transferForm.toStockLocationId || !this.transferForm.transferDate) { this.errorMessage = 'Source location, destination location, and transfer date are required.'; return; }
    const fromStockLocationId = this.transferForm.fromStockLocationId;
    const toStockLocationId = this.transferForm.toStockLocationId;
    const lines = this.transferForm.lines.map((line) => ({ stockBatchId: line.stockBatchId ?? null, quantity: line.quantity, unitCost: 0, notes: this.normalizeOptional(line.notes ?? '') })).filter((line) => !!line.stockBatchId && line.quantity > 0);
    if (!lines.length) { this.errorMessage = 'At least one transfer batch line is required.'; return; }
    const payload: SaveStockTransferPayload = { fromStockLocationId, toStockLocationId, transferDate: this.transferForm.transferDate, notes: this.normalizeOptional(this.transferForm.notes) ?? null, lines };
    this.runSave(this.editingTransferId ? this.inventory.updateTransfer(this.editingTransferId, payload) : this.inventory.createTransfer(payload), (item) => this.goToDetails(item.stockTransferId), this.editingTransferId ? 'Stock transfer updated successfully.' : 'Stock transfer created successfully.', 'Unable to save stock transfer.');
  }
  approveTransfer(transfer: StockTransfer): void { this.runSave(this.inventory.approveTransfer(transfer.stockTransferId, null), (item) => this.goToDetails(item.stockTransferId), 'Stock transfer approved successfully.', 'Unable to approve stock transfer.'); }
  receiveTransfer(transfer: StockTransfer): void { this.runSave(this.inventory.receiveTransfer(transfer.stockTransferId, null), (item) => this.goToDetails(item.stockTransferId), 'Stock transfer received successfully.', 'Unable to receive stock transfer.'); }
  resetAdjustmentForm(): void { this.editingAdjustmentId = null; this.adjustmentForm = createEmptyAdjustmentForm(); }
  startCreateAdjustment(): void { this.goToCreate(); }
  editAdjustment(adjustment: StockAdjustment): void {
    this.goToEdit(adjustment.stockAdjustmentId);
  }
  onAdjustmentLocationChange(): void { this.adjustmentForm.lines = this.adjustmentForm.lines.map((line) => ({ ...createEmptyAdjustmentLine(), notes: line.notes })); }
  addAdjustmentLine(): void { this.adjustmentForm.lines = [...this.adjustmentForm.lines, createEmptyAdjustmentLine()]; }
  removeAdjustmentLine(index: number): void { this.adjustmentForm.lines = this.adjustmentForm.lines.filter((_, i) => i !== index); if (!this.adjustmentForm.lines.length) { this.adjustmentForm.lines = [createEmptyAdjustmentLine()]; } }
  onAdjustmentBatchChange(line: AdjustmentLineFormState): void { const batch = this.lookupBatch(line.stockBatchId ?? null); line.availableQuantity = batch?.quantityOnHand ?? 0; line.itemName = batch?.itemName ?? ''; }
  saveAdjustment(): void {
    if (!this.adjustmentForm.stockLocationId || !this.adjustmentForm.adjustmentDate) { this.errorMessage = 'Stock location and adjustment date are required.'; return; }
    const stockLocationId = this.adjustmentForm.stockLocationId;
    const lines = this.adjustmentForm.lines.map((line) => ({ stockBatchId: line.stockBatchId ?? null, quantity: this.normalizeAdjustmentQuantity(line.quantity), unitCost: 0, notes: this.normalizeOptional(line.notes ?? '') })).filter((line) => !!line.stockBatchId && line.quantity !== 0);
    if (!lines.length) { this.errorMessage = 'At least one adjustment batch line is required.'; return; }
    const payload: SaveStockAdjustmentPayload = { stockLocationId, reason: this.adjustmentForm.reason, adjustmentDate: this.adjustmentForm.adjustmentDate, notes: this.normalizeOptional(this.adjustmentForm.notes) ?? null, lines };
    this.runSave(this.editingAdjustmentId ? this.inventory.updateAdjustment(this.editingAdjustmentId, payload) : this.inventory.createAdjustment(payload), (item) => this.goToDetails(item.stockAdjustmentId), this.editingAdjustmentId ? 'Stock adjustment updated successfully.' : 'Stock adjustment created successfully.', 'Unable to save stock adjustment.');
  }
  approveAdjustment(adjustment: StockAdjustment): void { this.runSave(this.inventory.approveAdjustment(adjustment.stockAdjustmentId, null), (item) => this.goToDetails(item.stockAdjustmentId), 'Stock adjustment approved successfully.', 'Unable to approve stock adjustment.'); }
  getSectionCount(section: InventorySection): number {
    switch (section) {
      case 'dashboard': return this.dashboard.lowStockCount + this.dashboard.nearExpiryCount;
      case 'units': return this.units.length;
      case 'categories': return this.categories.length;
      case 'medicines': return this.medicines.length;
      case 'items': return this.items.length;
      case 'suppliers': return this.suppliers.length;
      case 'locations': return this.locations.length;
      case 'purchases': return this.purchaseOrders.length;
      case 'returns': return this.purchaseReturns.length;
      case 'batches': return this.batches.length;
      case 'transfers': return this.transfers.length;
      case 'adjustments': return this.adjustments.length;
    }
  }
  purchaseLineUnitName(line: PurchaseLineFormState): string {
    const medicine = line.entryType === 'Medicine' ? this.medicines.find((x) => x.medicineMasterId === line.medicineMasterId) : null;
    const item = line.entryType === 'Item' ? this.items.find((x) => x.stockItemMasterId === line.stockItemMasterId) : null;
    return medicine?.unitName ?? item?.unitName ?? 'Select item';
  }
  pendingQuantity(order: PurchaseOrder): number { return Math.max(order.orderedQuantity - order.receivedQuantity, 0); }
  canEditPurchaseOrder(order: PurchaseOrder): boolean { return order.receivedQuantity <= 0; }
  canReceivePurchaseOrder(order: PurchaseOrder): boolean { return ['Draft', 'Ordered', 'PartiallyReceived'].includes(order.status) && this.pendingQuantity(order) > 0; }
  canEditTransfer(transfer: StockTransfer): boolean { return transfer.status !== 'Received'; }
  canApproveTransfer(transfer: StockTransfer): boolean { return transfer.status === 'Pending'; }
  canReceiveTransfer(transfer: StockTransfer): boolean { return transfer.status !== 'Received'; }
  canEditAdjustment(adjustment: StockAdjustment): boolean { return adjustment.status !== 'Approved'; }
  canApproveAdjustment(adjustment: StockAdjustment): boolean { return adjustment.status === 'Pending'; }
  hasLowStock(batch: StockBatch): boolean { return batch.quantityOnHand <= batch.minimumStock; }
  batchDisplayName(batch: StockBatch): string { return [batch.itemName, batch.batchNumber ? `Batch ${batch.batchNumber}` : null, batch.locationName].filter(Boolean).join(' | '); }
  formatNumber(value: number): string { return new Intl.NumberFormat('en-US', { maximumFractionDigits: 2 }).format(value); }
  formatCurrency(value: number): string { return `NPR ${new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(value)}`; }
  formatDate(value?: string | null): string {
    if (!value) {
      return 'Not set';
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime())
      ? 'Not set'
      : new Intl.DateTimeFormat('en-US', { year: 'numeric', month: 'short', day: 'numeric' }).format(parsed);
  }
  formatDateTime(value?: string | null): string {
    if (!value) {
      return 'Pending';
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime())
      ? 'Pending'
      : new Intl.DateTimeFormat('en-US', {
          year: 'numeric',
          month: 'short',
          day: 'numeric',
          hour: 'numeric',
          minute: '2-digit'
        }).format(parsed);
  }
  private syncRoute(url: string): void {
    const parts = url.split('?')[0].split('/').filter(Boolean);
    const nextSection = this.toSection(parts[2] ?? null);
    const nextId = this.parseId(parts[3] ?? null);
    const previousSection = this.activeSection;

    this.activeSection = nextSection;
    this.selectedRecordId = nextId;
    this.receivingPurchaseOrderId = null;

    if (parts[3] === 'create') {
      this.viewMode = 'create';
      this.selectedRecordId = null;
      this.resetFormForSection(this.activeSection);
    } else if (nextId && parts[4] === 'edit') {
      this.viewMode = 'edit';
      this.loadSelectedRecordIntoForm();
    } else if (nextId) {
      this.viewMode = 'details';
    } else {
      this.viewMode = 'index';
    }

    if (previousSection !== nextSection) {
      this.searchTerm = '';
      this.errorMessage = '';
      this.successMessage = '';
    }
  }

  private resetFormForSection(section: InventorySection): void {
    switch (section) {
      case 'units':
        this.resetUnitForm();
        break;
      case 'categories':
        this.resetCategoryForm();
        break;
      case 'medicines':
        this.resetMedicineForm();
        break;
      case 'items':
        this.resetItemForm();
        break;
      case 'suppliers':
        this.resetSupplierForm();
        break;
      case 'locations':
        this.resetLocationForm();
        break;
      case 'purchases':
        this.resetPurchaseForm();
        break;
      case 'returns':
        this.resetReturnForm();
        break;
      case 'transfers':
        this.resetTransferForm();
        break;
      case 'adjustments':
        this.resetAdjustmentForm();
        break;
    }
  }

  private loadSelectedRecordIntoForm(): void {
    if (!this.isEditView) {
      return;
    }

    switch (this.activeSection) {
      case 'units':
        if (this.selectedUnit) {
          this.editingUnitId = this.selectedUnit.inventoryUnitId;
          this.unitForm = {
            name: this.selectedUnit.name,
            shortName: this.selectedUnit.shortName ?? '',
            description: this.selectedUnit.description ?? '',
            isActive: this.selectedUnit.isActive
          };
        }
        break;
      case 'categories':
        if (this.selectedCategory) {
          this.editingCategoryId = this.selectedCategory.inventoryCategoryId;
          this.categoryForm = {
            categoryType: this.selectedCategory.categoryType,
            name: this.selectedCategory.name,
            description: this.selectedCategory.description ?? '',
            isActive: this.selectedCategory.isActive
          };
        }
        break;
      case 'medicines':
        if (this.selectedMedicine) {
          this.editingMedicineId = this.selectedMedicine.medicineMasterId;
          this.medicineForm = {
            medicineName: this.selectedMedicine.medicineName,
            genericName: this.selectedMedicine.genericName ?? '',
            brand: this.selectedMedicine.brand ?? '',
            inventoryUnitId: this.selectedMedicine.inventoryUnitId,
            inventoryCategoryId: this.selectedMedicine.inventoryCategoryId ?? null,
            strength: this.selectedMedicine.strength ?? '',
            batchRequired: this.selectedMedicine.batchRequired,
            minimumStock: this.selectedMedicine.minimumStock,
            maximumStock: this.selectedMedicine.maximumStock,
            isActive: this.selectedMedicine.isActive
          };
        }
        break;
      case 'items':
        if (this.selectedItem) {
          this.editingItemId = this.selectedItem.stockItemMasterId;
          this.itemForm = {
            itemType: this.selectedItem.itemType,
            itemName: this.selectedItem.itemName,
            specification: this.selectedItem.specification ?? '',
            inventoryUnitId: this.selectedItem.inventoryUnitId,
            inventoryCategoryId: this.selectedItem.inventoryCategoryId ?? null,
            minimumStock: this.selectedItem.minimumStock,
            maximumStock: this.selectedItem.maximumStock,
            isActive: this.selectedItem.isActive
          };
        }
        break;
      case 'suppliers':
        if (this.selectedSupplier) {
          this.editingSupplierId = this.selectedSupplier.supplierId;
          this.supplierForm = {
            supplierName: this.selectedSupplier.supplierName,
            supplierCode: this.selectedSupplier.supplierCode,
            contactPerson: this.selectedSupplier.contactPerson ?? '',
            contactPhone: this.selectedSupplier.contactPhone ?? '',
            contactEmail: this.selectedSupplier.contactEmail ?? '',
            address: this.selectedSupplier.address ?? '',
            paymentTermsDays: this.selectedSupplier.paymentTermsDays,
            notes: this.selectedSupplier.notes ?? '',
            isActive: this.selectedSupplier.isActive
          };
        }
        break;
      case 'locations':
        if (this.selectedLocation) {
          this.editingLocationId = this.selectedLocation.stockLocationId;
          this.locationForm = {
            branchId: this.selectedLocation.branchId ?? null,
            name: this.selectedLocation.name,
            code: this.selectedLocation.code,
            locationType: this.selectedLocation.locationType,
            description: this.selectedLocation.description ?? '',
            isActive: this.selectedLocation.isActive
          };
        }
        break;
      case 'purchases':
        if (this.selectedPurchaseOrder) {
          this.editingPurchaseOrderId = this.selectedPurchaseOrder.purchaseOrderId;
          this.purchaseForm = {
            supplierId: this.selectedPurchaseOrder.supplierId,
            stockLocationId: this.selectedPurchaseOrder.stockLocationId,
            orderDate: this.toDateInput(this.selectedPurchaseOrder.orderDate),
            expectedDeliveryDate: this.toDateInput(this.selectedPurchaseOrder.expectedDeliveryDate),
            status: this.canPersistPurchaseStatus(this.selectedPurchaseOrder.status) ? this.selectedPurchaseOrder.status : 'Ordered',
            notes: this.selectedPurchaseOrder.notes ?? '',
            lines: this.selectedPurchaseOrder.lines.length
              ? this.selectedPurchaseOrder.lines.map((line) => ({
                  entryType: line.medicineMasterId ? 'Medicine' : 'Item',
                  referenceLineId: line.referenceLineId,
                  medicineMasterId: line.medicineMasterId ?? null,
                  stockItemMasterId: line.stockItemMasterId ?? null,
                  quantity: line.quantity,
                  unitCost: line.unitCost,
                  notes: line.notes ?? ''
                }))
              : [createEmptyPurchaseLine()]
          };
        }
        break;
      case 'transfers':
        if (this.selectedTransfer) {
          this.editingTransferId = this.selectedTransfer.stockTransferId;
          this.transferForm = {
            fromStockLocationId: this.selectedTransfer.fromStockLocationId,
            toStockLocationId: this.selectedTransfer.toStockLocationId,
            transferDate: this.toDateInput(this.selectedTransfer.transferDate),
            notes: this.selectedTransfer.notes ?? '',
            lines: this.selectedTransfer.lines.length
              ? this.selectedTransfer.lines.map((line) => ({
                  stockBatchId: line.stockBatchId ?? null,
                  quantity: line.quantity,
                  unitCost: 0,
                  notes: line.notes ?? '',
                  availableQuantity: this.lookupBatch(line.stockBatchId ?? null)?.quantityOnHand ?? 0,
                  itemName: line.itemName
                }))
              : [createEmptyTransferLine()]
          };
        }
        break;
      case 'adjustments':
        if (this.selectedAdjustment) {
          this.editingAdjustmentId = this.selectedAdjustment.stockAdjustmentId;
          this.adjustmentForm = {
            stockLocationId: this.selectedAdjustment.stockLocationId,
            reason: this.selectedAdjustment.reason,
            adjustmentDate: this.toDateInput(this.selectedAdjustment.adjustmentDate),
            notes: this.selectedAdjustment.notes ?? '',
            lines: this.selectedAdjustment.lines.length
              ? this.selectedAdjustment.lines.map((line) => ({
                  stockBatchId: line.stockBatchId ?? null,
                  quantity: line.quantity,
                  unitCost: 0,
                  notes: line.notes ?? '',
                  availableQuantity: this.lookupBatch(line.stockBatchId ?? null)?.quantityOnHand ?? 0,
                  itemName: line.itemName
                }))
              : [createEmptyAdjustmentLine()]
          };
        }
        break;
    }
  }

  formatEnum(value?: string | null): string { return value ? value.replace(/([a-z])([A-Z])/g, '$1 $2') : 'Not set'; }
  getStatusTone(value?: string | null): 'success' | 'warning' | 'danger' | 'info' {
    const normalized = String(value ?? '').toLowerCase();
    if (['active', 'approved', 'paid', 'received'].includes(normalized)) { return 'success'; }
    if (['cancelled', 'inactive', 'rejected', 'expired'].includes(normalized)) { return 'danger'; }
    if (['pending', 'partial', 'open', 'draft', 'ordered', 'partiallyreceived'].includes(normalized)) { return 'warning'; }
    return 'info';
  }

  private loadData(): void {
    this.isLoading = true; this.errorMessage = '';
    const loadFailures = new Set<string>();
    const withFallback = <T>(key: string, request: Observable<T>, fallback: T): Observable<T> =>
      request.pipe(
        catchError((error) => {
          loadFailures.add(key);
          console.error(`Inventory admin load failed for ${key}.`, error);
          return of(fallback);
        })
      );

    forkJoin({
      branches: withFallback('branches', this.adminOps.getOrganizationSettings().pipe(map((settings: OrganizationSettings) => settings.branches)), this.branches),
      dashboard: withFallback('dashboard', this.inventory.getDashboard(), this.dashboard),
      units: withFallback('units', this.inventory.getUnits(), this.units),
      categories: withFallback('categories', this.inventory.getCategories(), this.categories),
      medicines: withFallback('medicines', this.inventory.getMedicines(), this.medicines),
      items: withFallback('items', this.inventory.getItems(), this.items),
      suppliers: withFallback('suppliers', this.inventory.getSuppliers(), this.suppliers),
      locations: withFallback('locations', this.inventory.getLocations(), this.locations),
      batches: withFallback('batches', this.inventory.getBatches(), this.batches),
      purchaseOrders: withFallback('purchaseOrders', this.inventory.getPurchaseOrders(), this.purchaseOrders),
      purchaseInvoices: withFallback('purchaseInvoices', this.inventory.getPurchaseInvoices(), this.purchaseInvoices),
      purchaseReturns: withFallback('purchaseReturns', this.inventory.getPurchaseReturns(), this.purchaseReturns),
      supplierDues: withFallback('supplierDues', this.inventory.getSupplierDues(), this.supplierDues),
      transfers: withFallback('transfers', this.inventory.getTransfers(), this.transfers),
      adjustments: withFallback('adjustments', this.inventory.getAdjustments(), this.adjustments)
    }).subscribe({
      next: ({ branches, dashboard, units, categories, medicines, items, suppliers, locations, batches, purchaseOrders, purchaseInvoices, purchaseReturns, supplierDues, transfers, adjustments }) => {
        this.branches = branches; this.dashboard = dashboard;
        this.units = [...units].sort((a, b) => a.name.localeCompare(b.name));
        this.categories = [...categories].sort((a, b) => `${a.categoryType} ${a.name}`.localeCompare(`${b.categoryType} ${b.name}`));
        this.medicines = [...medicines].sort((a, b) => a.medicineName.localeCompare(b.medicineName));
        this.items = [...items].sort((a, b) => `${a.itemType} ${a.itemName}`.localeCompare(`${b.itemType} ${b.itemName}`));
        this.suppliers = [...suppliers].sort((a, b) => a.supplierName.localeCompare(b.supplierName));
        this.locations = [...locations].sort((a, b) => `${a.branchName} ${a.name}`.localeCompare(`${b.branchName} ${b.name}`));
        this.batches = [...batches].sort((a, b) => `${a.locationName} ${a.itemName}`.localeCompare(`${b.locationName} ${b.itemName}`));
        this.purchaseOrders = [...purchaseOrders].sort((a, b) => `${b.orderDate} ${b.orderNumber}`.localeCompare(`${a.orderDate} ${a.orderNumber}`));
        this.purchaseInvoices = [...purchaseInvoices].sort((a, b) => `${b.invoiceDate} ${b.invoiceNumber}`.localeCompare(`${a.invoiceDate} ${a.invoiceNumber}`));
        this.purchaseReturns = [...purchaseReturns].sort((a, b) => `${b.returnDate} ${b.returnNumber}`.localeCompare(`${a.returnDate} ${a.returnNumber}`));
        this.supplierDues = [...supplierDues].sort((a, b) => b.dueAmount - a.dueAmount || a.supplierName.localeCompare(b.supplierName));
        this.transfers = [...transfers].sort((a, b) => `${b.transferDate} ${b.transferNumber}`.localeCompare(`${a.transferDate} ${a.transferNumber}`));
        this.adjustments = [...adjustments].sort((a, b) => `${b.adjustmentDate} ${b.adjustmentNumber}`.localeCompare(`${a.adjustmentDate} ${a.adjustmentNumber}`));
        this.errorMessage = this.getCriticalLoadKeys(this.activeSection).some((key) => loadFailures.has(key))
          ? 'Some required supply chain data could not be loaded for this page. Refresh and try again.'
          : '';
        this.loadSelectedRecordIntoForm();
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; this.errorMessage = 'Unable to load pharmacy and stock admin data right now.'; }
    });
  }

  private runSave<T>(
    request: Observable<T>,
    onSuccess: (value: T) => void,
    successMessage: string,
    fallbackError = 'Unable to save inventory changes.'
  ): void {
    this.isSaving = true; this.errorMessage = ''; this.successMessage = '';
    request.subscribe({
      next: (value) => { onSuccess(value); this.isSaving = false; this.successMessage = successMessage; this.loadData(); },
      error: (error: { error?: { message?: string } }) => { this.isSaving = false; this.errorMessage = error?.error?.message || fallbackError; }
    });
  }

  private matchesSearch(...values: Array<string | number | null | undefined>): boolean { const term = this.searchTerm.trim().toLowerCase(); return !term || values.some((value) => String(value ?? '').toLowerCase().includes(term)); }
  private normalizeOptional(value: string): string | null { const normalized = value.trim(); return normalized ? normalized : null; }
  private toDateInput(value?: string | null): string { return value ? value.slice(0, 10) : ''; }
  private parseId(value: string | null): number | null {
    if (!value) {
      return null;
    }

    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  }
  private toSection(value: string | null): InventorySection {
    return this.sections.some((section) => section.key === value) ? (value as InventorySection) : 'dashboard';
  }
  private getCriticalLoadKeys(section: InventorySection): string[] {
    switch (section) {
      case 'dashboard':
        return ['dashboard'];
      case 'units':
        return ['units'];
      case 'categories':
        return ['categories'];
      case 'medicines':
        return ['medicines', 'units', 'categories'];
      case 'items':
        return ['items', 'units', 'categories'];
      case 'suppliers':
        return ['suppliers'];
      case 'locations':
        return ['locations', 'branches'];
      case 'purchases':
        return ['purchaseOrders', 'purchaseInvoices', 'suppliers', 'locations', 'medicines', 'items'];
      case 'returns':
        return ['purchaseReturns', 'purchaseInvoices'];
      case 'batches':
        return ['batches'];
      case 'transfers':
        return ['transfers', 'locations', 'batches'];
      case 'adjustments':
        return ['adjustments', 'locations', 'batches'];
      default:
        return [];
    }
  }
  private lookupBatch(stockBatchId: number | null): StockBatch | null { return stockBatchId ? this.batches.find((batch) => batch.stockBatchId === stockBatchId) ?? null : null; }
  private isInventoryLineBatchRequired(line: InventoryLine): boolean { return !!line.medicineMasterId && (this.medicines.find((x) => x.medicineMasterId === line.medicineMasterId)?.batchRequired ?? false); }
  private toPurchasePayloadLine(line: PurchaseLineFormState): SaveInventoryLinePayload { return { referenceLineId: line.referenceLineId ?? null, medicineMasterId: line.entryType === 'Medicine' ? line.medicineMasterId ?? null : null, stockItemMasterId: line.entryType === 'Item' ? line.stockItemMasterId ?? null : null, quantity: line.quantity, unitCost: line.unitCost, notes: this.normalizeOptional(line.notes ?? '') }; }
  private canPersistPurchaseStatus(status: string): status is PurchaseOrderStatus { return ['Draft', 'Ordered', 'Cancelled'].includes(status); }
  private normalizeAdjustmentQuantity(quantity: number): number { return ['Damage', 'Loss', 'ExpiryRemoval'].includes(this.adjustmentForm.reason) && quantity > 0 ? -quantity : quantity; }
}
