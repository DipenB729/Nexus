import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponse } from '../../models/hms/auth.model';
import {
  InventoryCategory,
  InventoryDashboard,
  InventoryUnit,
  MedicineMaster,
  PurchaseInvoice,
  PurchaseOrder,
  PurchaseReturn,
  ReceivePurchaseOrderPayload,
  SaveInventoryCategoryPayload,
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
  StockBatch,
  StockItemMaster,
  StockLocation,
  StockTransfer,
  Supplier,
  SupplierDueSummary
} from '../../models/hms/phase5-inventory.model';

@Injectable({ providedIn: 'root' })
export class Phase5InventoryService {
  constructor(private readonly http: HttpClient) {}

  getDashboard(): Observable<InventoryDashboard> {
    return this.http.get<ApiResponse<InventoryDashboard>>('/api/admin/inventory/dashboard').pipe(map((res) => res.data));
  }

  getUnits(): Observable<InventoryUnit[]> {
    return this.http.get<ApiResponse<InventoryUnit[]>>('/api/admin/inventory/units').pipe(map((res) => res.data));
  }

  createUnit(payload: SaveInventoryUnitPayload): Observable<InventoryUnit> {
    return this.http.post<ApiResponse<InventoryUnit>>('/api/admin/inventory/units', payload).pipe(map((res) => res.data));
  }

  updateUnit(inventoryUnitId: number, payload: SaveInventoryUnitPayload): Observable<InventoryUnit> {
    return this.http.put<ApiResponse<InventoryUnit>>(`/api/admin/inventory/units/${inventoryUnitId}`, payload).pipe(map((res) => res.data));
  }

  getCategories(): Observable<InventoryCategory[]> {
    return this.http.get<ApiResponse<InventoryCategory[]>>('/api/admin/inventory/categories').pipe(map((res) => res.data));
  }

  createCategory(payload: SaveInventoryCategoryPayload): Observable<InventoryCategory> {
    return this.http.post<ApiResponse<InventoryCategory>>('/api/admin/inventory/categories', payload).pipe(map((res) => res.data));
  }

  updateCategory(inventoryCategoryId: number, payload: SaveInventoryCategoryPayload): Observable<InventoryCategory> {
    return this.http.put<ApiResponse<InventoryCategory>>(`/api/admin/inventory/categories/${inventoryCategoryId}`, payload).pipe(map((res) => res.data));
  }

  getMedicines(): Observable<MedicineMaster[]> {
    return this.http.get<ApiResponse<MedicineMaster[]>>('/api/admin/inventory/medicines').pipe(map((res) => res.data));
  }

  createMedicine(payload: SaveMedicineMasterPayload): Observable<MedicineMaster> {
    return this.http.post<ApiResponse<MedicineMaster>>('/api/admin/inventory/medicines', payload).pipe(map((res) => res.data));
  }

  updateMedicine(medicineMasterId: number, payload: SaveMedicineMasterPayload): Observable<MedicineMaster> {
    return this.http.put<ApiResponse<MedicineMaster>>(`/api/admin/inventory/medicines/${medicineMasterId}`, payload).pipe(map((res) => res.data));
  }

  getItems(): Observable<StockItemMaster[]> {
    return this.http.get<ApiResponse<StockItemMaster[]>>('/api/admin/inventory/items').pipe(map((res) => res.data));
  }

  createItem(payload: SaveStockItemMasterPayload): Observable<StockItemMaster> {
    return this.http.post<ApiResponse<StockItemMaster>>('/api/admin/inventory/items', payload).pipe(map((res) => res.data));
  }

  updateItem(stockItemMasterId: number, payload: SaveStockItemMasterPayload): Observable<StockItemMaster> {
    return this.http.put<ApiResponse<StockItemMaster>>(`/api/admin/inventory/items/${stockItemMasterId}`, payload).pipe(map((res) => res.data));
  }

  getSuppliers(): Observable<Supplier[]> {
    return this.http.get<ApiResponse<Supplier[]>>('/api/admin/inventory/suppliers').pipe(map((res) => res.data));
  }

  createSupplier(payload: SaveSupplierPayload): Observable<Supplier> {
    return this.http.post<ApiResponse<Supplier>>('/api/admin/inventory/suppliers', payload).pipe(map((res) => res.data));
  }

  updateSupplier(supplierId: number, payload: SaveSupplierPayload): Observable<Supplier> {
    return this.http.put<ApiResponse<Supplier>>(`/api/admin/inventory/suppliers/${supplierId}`, payload).pipe(map((res) => res.data));
  }

  getLocations(): Observable<StockLocation[]> {
    return this.http.get<ApiResponse<StockLocation[]>>('/api/admin/inventory/locations').pipe(map((res) => res.data));
  }

  createLocation(payload: SaveStockLocationPayload): Observable<StockLocation> {
    return this.http.post<ApiResponse<StockLocation>>('/api/admin/inventory/locations', payload).pipe(map((res) => res.data));
  }

  updateLocation(stockLocationId: number, payload: SaveStockLocationPayload): Observable<StockLocation> {
    return this.http.put<ApiResponse<StockLocation>>(`/api/admin/inventory/locations/${stockLocationId}`, payload).pipe(map((res) => res.data));
  }

  getBatches(): Observable<StockBatch[]> {
    return this.http.get<ApiResponse<StockBatch[]>>('/api/admin/inventory/batches').pipe(map((res) => res.data));
  }

  getPurchaseOrders(): Observable<PurchaseOrder[]> {
    return this.http.get<ApiResponse<PurchaseOrder[]>>('/api/admin/inventory/purchase-orders').pipe(map((res) => res.data));
  }

  createPurchaseOrder(payload: SavePurchaseOrderPayload): Observable<PurchaseOrder> {
    return this.http.post<ApiResponse<PurchaseOrder>>('/api/admin/inventory/purchase-orders', payload).pipe(map((res) => res.data));
  }

  updatePurchaseOrder(purchaseOrderId: number, payload: SavePurchaseOrderPayload): Observable<PurchaseOrder> {
    return this.http.put<ApiResponse<PurchaseOrder>>(`/api/admin/inventory/purchase-orders/${purchaseOrderId}`, payload).pipe(map((res) => res.data));
  }

  receivePurchaseOrder(purchaseOrderId: number, payload: ReceivePurchaseOrderPayload): Observable<PurchaseInvoice> {
    return this.http.post<ApiResponse<PurchaseInvoice>>(`/api/admin/inventory/purchase-orders/${purchaseOrderId}/receive`, payload).pipe(map((res) => res.data));
  }

  getPurchaseInvoices(): Observable<PurchaseInvoice[]> {
    return this.http.get<ApiResponse<PurchaseInvoice[]>>('/api/admin/inventory/purchase-invoices').pipe(map((res) => res.data));
  }

  getPurchaseReturns(): Observable<PurchaseReturn[]> {
    return this.http.get<ApiResponse<PurchaseReturn[]>>('/api/admin/inventory/purchase-returns').pipe(map((res) => res.data));
  }

  createPurchaseReturn(payload: SavePurchaseReturnPayload): Observable<PurchaseReturn> {
    return this.http.post<ApiResponse<PurchaseReturn>>('/api/admin/inventory/purchase-returns', payload).pipe(map((res) => res.data));
  }

  getSupplierDues(): Observable<SupplierDueSummary[]> {
    return this.http.get<ApiResponse<SupplierDueSummary[]>>('/api/admin/inventory/supplier-dues').pipe(map((res) => res.data));
  }

  getTransfers(): Observable<StockTransfer[]> {
    return this.http.get<ApiResponse<StockTransfer[]>>('/api/admin/inventory/transfers').pipe(map((res) => res.data));
  }

  createTransfer(payload: SaveStockTransferPayload): Observable<StockTransfer> {
    return this.http.post<ApiResponse<StockTransfer>>('/api/admin/inventory/transfers', payload).pipe(map((res) => res.data));
  }

  updateTransfer(stockTransferId: number, payload: SaveStockTransferPayload): Observable<StockTransfer> {
    return this.http.put<ApiResponse<StockTransfer>>(`/api/admin/inventory/transfers/${stockTransferId}`, payload).pipe(map((res) => res.data));
  }

  approveTransfer(stockTransferId: number, notes?: string | null): Observable<StockTransfer> {
    return this.http.post<ApiResponse<StockTransfer>>(`/api/admin/inventory/transfers/${stockTransferId}/approve`, { notes: notes ?? null }).pipe(map((res) => res.data));
  }

  receiveTransfer(stockTransferId: number, notes?: string | null): Observable<StockTransfer> {
    return this.http.post<ApiResponse<StockTransfer>>(`/api/admin/inventory/transfers/${stockTransferId}/receive`, { notes: notes ?? null }).pipe(map((res) => res.data));
  }

  getAdjustments(): Observable<StockAdjustment[]> {
    return this.http.get<ApiResponse<StockAdjustment[]>>('/api/admin/inventory/adjustments').pipe(map((res) => res.data));
  }

  createAdjustment(payload: SaveStockAdjustmentPayload): Observable<StockAdjustment> {
    return this.http.post<ApiResponse<StockAdjustment>>('/api/admin/inventory/adjustments', payload).pipe(map((res) => res.data));
  }

  updateAdjustment(stockAdjustmentId: number, payload: SaveStockAdjustmentPayload): Observable<StockAdjustment> {
    return this.http.put<ApiResponse<StockAdjustment>>(`/api/admin/inventory/adjustments/${stockAdjustmentId}`, payload).pipe(map((res) => res.data));
  }

  approveAdjustment(stockAdjustmentId: number, notes?: string | null): Observable<StockAdjustment> {
    return this.http.post<ApiResponse<StockAdjustment>>(`/api/admin/inventory/adjustments/${stockAdjustmentId}/approve`, { notes: notes ?? null }).pipe(map((res) => res.data));
  }
}
