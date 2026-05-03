export interface SuperAdminSummary {
  totalHospitals: number;
  activeBranches: number;
  totalAdmins: number;
  activeAdmins: number;
  totalUsers: number;
  lastUpdatedAt: string;
}

export interface SuperAdminHospitalProfile {
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

export interface SuperAdminBranch {
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

export interface SuperAdminHospital {
  profile: SuperAdminHospitalProfile;
  branches: SuperAdminBranch[];
}

export interface SuperAdminUser {
  userId: number;
  hospitalProfileId?: number | null;
  hospitalName: string;
  fullName: string;
  email: string;
  phone?: string | null;
  role: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateAdminUser {
  hospitalProfileId: number;
  fullName: string;
  email: string;
  phone?: string | null;
  password: string;
}

export type UpsertBranch = Omit<SuperAdminBranch, 'branchId'>;
