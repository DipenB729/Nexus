export type LabServiceSection = 'labTests' | 'healthPackages' | 'surgeryPackages' | 'corporatePackages' | 'discountedBundles';
export type ServicePackageKind = 'HealthPackage' | 'SurgeryPackage' | 'CorporatePackage' | 'DiscountedBundle';

export interface LabTestMaster {
  labTestMasterId: number;
  testName: string;
  departmentName: string;
  price: number;
  sampleType: string;
  reportFormat: string;
  isActive: boolean;
}

export interface SaveLabTestMasterPayload {
  testName: string;
  departmentName: string;
  price: number;
  sampleType: string;
  reportFormat: string;
  isActive: boolean;
}

export interface ServicePackage {
  servicePackageId: number;
  kind: ServicePackageKind;
  packageName: string;
  departmentName?: string | null;
  price: number;
  discountAmount: number;
  netAmount: number;
  description?: string | null;
  isActive: boolean;
}

export interface SaveServicePackagePayload {
  kind: ServicePackageKind;
  packageName: string;
  departmentName?: string | null;
  price: number;
  discountAmount: number;
  description?: string | null;
  isActive: boolean;
}
