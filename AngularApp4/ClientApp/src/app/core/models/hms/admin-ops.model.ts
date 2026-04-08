export interface StockAlert {
  itemId: number;
  name: string;
  branchName: string;
  quantityInStock: number;
  reorderLevel: number;
  expiryDate: string;
}

export interface PendingPayment {
  invoiceId: number;
  invoiceNumber: string;
  patientName: string;
  dueAmount: number;
  dueDate?: string | null;
  status: string;
}

export interface BranchOccupancy {
  branchId: number;
  branchName: string;
  totalBeds: number;
  occupiedBeds: number;
  occupancyRate: number;
}

export interface DashboardSummary {
  totalPatients: number;
  todayAppointments: number;
  todaySales: number;
  lowStockItems: number;
  expiredMedicines: number;
  pendingPayments: number;
  pendingPaymentAmount: number;
  totalBeds: number;
  occupiedBeds: number;
  bedOccupancyRate: number;
  lowStockAlerts: StockAlert[];
  expiredMedicineAlerts: StockAlert[];
  pendingPaymentDetails: PendingPayment[];
  bedOccupancyByBranch: BranchOccupancy[];
}

export interface RolePermission {
  moduleKey: string;
  moduleName: string;
  canView: boolean;
  canAdd: boolean;
  canEdit: boolean;
  canDelete: boolean;
}

export interface RoleDetails {
  roleId: number;
  name: string;
  description?: string | null;
  isActive: boolean;
  isSystemRole: boolean;
  permissions: RolePermission[];
}

export interface HospitalProfile {
  hospitalProfileId: number;
  hospitalName: string;
  logoUrl?: string | null;
  addressLine1: string;
  city: string;
  stateOrProvince: string;
  postalCode: string;
  country: string;
  contactEmail: string;
  contactPhone: string;
  taxLabel: string;
  taxRegistrationNumber: string;
  taxPercentage: number;
  currencyCode: string;
  invoicePrefix: string;
  invoiceStartingNumber: number;
  invoiceFooterNote: string;
  multiBranchEnabled: boolean;
}

export interface BranchSettings {
  branchId: number;
  name: string;
  code: string;
  address: string;
  contactPhone: string;
  contactEmail: string;
  isPrimary: boolean;
  isActive: boolean;
  totalBeds: number;
  occupiedBeds: number;
}

export interface OrganizationSettings {
  profile: HospitalProfile;
  branches: BranchSettings[];
}
