export type MonitoringSection = 'reports' | 'audit';
export type AuditPanel = 'changeTrail' | 'loginHistory' | 'stockAdjustmentHistory' | 'billingChanges' | 'refundApprovals';

export interface ReportCountRow {
  label: string;
  count: number;
}

export interface PatientBranchActivity {
  branchId: number;
  branchName: string;
  distinctPatients: number;
  activeAdmissions: number;
}

export interface PatientReport {
  totalPatients: number;
  activePatients: number;
  newPatientsLast30Days: number;
  mergedPatients: number;
  categoryRows: ReportCountRow[];
  branchRows: PatientBranchActivity[];
}

export interface BillingStatusRow {
  status: string;
  invoiceCount: number;
  amount: number;
}

export interface BillingInvoiceReportRow {
  billingInvoiceId: number;
  invoiceNumber: string;
  patientName: string;
  branchName: string;
  totalAmount: number;
  dueAmount: number;
  status: string;
  invoiceDate: string;
}

export interface BillingReport {
  totalInvoices: number;
  totalBilled: number;
  totalCollected: number;
  totalDue: number;
  totalRefunded: number;
  statusRows: BillingStatusRow[];
  recentInvoices: BillingInvoiceReportRow[];
}

export interface DoctorRevenueRow {
  doctorId: number;
  doctorName: string;
  specialization: string;
  appointmentCount: number;
  linkedInvoiceCount: number;
  recognizedRevenue: number;
  consultationEstimate: number;
  reportedRevenue: number;
}

export interface DoctorRevenueReport {
  activeDoctors: number;
  doctorsWithRevenue: number;
  totalRecognizedRevenue: number;
  totalConsultationEstimate: number;
  rows: DoctorRevenueRow[];
}

export interface PharmacyMovementRow {
  stockLocationId: number;
  locationName: string;
  branchName: string;
  receivedValue: number;
  returnedValue: number;
  transferOutValue: number;
  adjustmentOutValue: number;
  currentStockValue: number;
}

export interface PharmacySalesReport {
  basisNote: string;
  pharmacyLocations: number;
  receivedValue: number;
  returnedValue: number;
  transferOutValue: number;
  adjustmentOutValue: number;
  currentStockValue: number;
  rows: PharmacyMovementRow[];
}

export interface PurchaseSupplierReportRow {
  supplierId: number;
  supplierName: string;
  invoiceCount: number;
  totalPurchased: number;
  dueAmount: number;
  returnAmount: number;
}

export interface PurchaseInvoiceReportRow {
  purchaseInvoiceId: number;
  invoiceNumber: string;
  supplierName: string;
  locationName: string;
  totalAmount: number;
  dueAmount: number;
  status: string;
  invoiceDate: string;
}

export interface PurchaseReport {
  purchaseOrders: number;
  purchaseInvoices: number;
  totalPurchased: number;
  totalPaid: number;
  totalDue: number;
  totalReturned: number;
  supplierRows: PurchaseSupplierReportRow[];
  recentInvoices: PurchaseInvoiceReportRow[];
}

export interface StockBalanceRow {
  stockLocationId: number;
  locationName: string;
  branchName: string;
  batchCount: number;
  itemCount: number;
  quantityOnHand: number;
  stockValue: number;
  lowStockBatches: number;
}

export interface StockBalanceReport {
  activeLocations: number;
  activeBatches: number;
  quantityOnHand: number;
  stockValue: number;
  lowStockBatches: number;
  locationRows: StockBalanceRow[];
}

export interface ExpiryBatchRow {
  stockBatchId: number;
  itemName: string;
  locationName: string;
  batchNumber: string;
  expiryDate: string;
  quantityOnHand: number;
  stockValue: number;
  status: string;
}

export interface ExpiryReport {
  nearExpiryCount: number;
  expiredCount: number;
  nearExpiryValue: number;
  expiredValue: number;
  rows: ExpiryBatchRow[];
}

export interface BranchOccupancyRow {
  branchId: number;
  branchName: string;
  totalBeds: number;
  occupiedBeds: number;
  occupancyRate: number;
}

export interface WardOccupancyRow {
  wardId: number;
  wardName: string;
  branchName: string;
  totalBeds: number;
  occupiedBeds: number;
  occupancyRate: number;
}

export interface BedOccupancyReport {
  totalBeds: number;
  occupiedBeds: number;
  availableBeds: number;
  occupancyRate: number;
  branchRows: BranchOccupancyRow[];
  wardRows: WardOccupancyRow[];
}

export interface MonitoringReports {
  patientReport: PatientReport;
  billingReport: BillingReport;
  doctorRevenueReport: DoctorRevenueReport;
  pharmacySalesReport: PharmacySalesReport;
  purchaseReport: PurchaseReport;
  stockBalanceReport: StockBalanceReport;
  expiryReport: ExpiryReport;
  bedOccupancyReport: BedOccupancyReport;
}

export interface AuditLogEntry {
  auditLogEntryId: number;
  category: string;
  action: string;
  entityName?: string | null;
  entityId?: number | null;
  targetDisplayName?: string | null;
  summary: string;
  metadataJson?: string | null;
  performedByUserId?: number | null;
  performedByName?: string | null;
  performedByRole?: string | null;
  actorEmail?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  createdAt: string;
}

export interface AuditOverview {
  totalEntries: number;
  last24Hours: number;
  uniqueActors: number;
  loginEvents: number;
  billingEvents: number;
  stockAdjustmentEvents: number;
  refundEvents: number;
}

export interface AuditMonitoring {
  overview: AuditOverview;
  changeTrail: AuditLogEntry[];
  loginHistory: AuditLogEntry[];
  stockAdjustmentHistory: AuditLogEntry[];
  billingChanges: AuditLogEntry[];
  refundApprovals: AuditLogEntry[];
}
