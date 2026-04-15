export interface DepartmentMaster {
  departmentId: number;
  branchId: number;
  branchName: string;
  name: string;
  code: string;
  description?: string | null;
  isActive: boolean;
}

export interface DoctorMaster {
  doctorId: number;
  branchId?: number | null;
  branchName: string;
  departmentId?: number | null;
  departmentName: string;
  fullName: string;
  specialization: string;
  email: string;
  phone?: string | null;
  experienceYears: number;
  qualification?: string | null;
  opdDays?: string | null;
  opdStartTime?: string | null;
  opdEndTime?: string | null;
  consultationFee: number;
  isActive: boolean;
  portalAccountCreated?: boolean | null;
  portalEmailSent?: boolean | null;
  portalProvisioningNote?: string | null;
}

export interface DoctorAvailableSlot {
  scheduleId?: number | null;
  startTime: string;
  endTime: string;
  maxPatientsPerSlot: number;
  bookedPatients: number;
  remainingPatients: number;
}

export interface StaffMaster {
  staffId: number;
  branchId?: number | null;
  branchName: string;
  departmentId?: number | null;
  departmentName: string;
  fullName: string;
  employeeCode?: string | null;
  designation: string;
  shift?: string | null;
  email: string;
  phone?: string | null;
  joinDate: string;
  isActive: boolean;
}

export interface PatientCategoryMaster {
  patientCategoryId: number;
  name: string;
  description?: string | null;
  priorityOrder: number;
  isActive: boolean;
}

export interface WardMaster {
  wardId: number;
  branchId: number;
  branchName: string;
  departmentId?: number | null;
  departmentName: string;
  name: string;
  wardType: string;
  roomType: string;
  chargePerDay: number;
  isActive: boolean;
}

export interface BedMaster {
  bedId: number;
  wardId: number;
  wardName: string;
  branchId: number;
  branchName: string;
  departmentId?: number | null;
  departmentName: string;
  bedNumber: string;
  chargePerDay: number;
  isOccupied: boolean;
  isActive: boolean;
}
