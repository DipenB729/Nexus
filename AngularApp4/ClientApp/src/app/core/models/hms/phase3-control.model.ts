export interface PatientRecord {
  patientId: number;
  userId: number;
  medicalRecordNumber: string;
  fullName: string;
  email: string;
  phone?: string | null;
  patientCategoryId?: number | null;
  patientCategoryName: string;
  gender?: string | null;
  dateOfBirth?: string | null;
  address?: string | null;
  bloodGroup?: string | null;
  emergencyContact?: string | null;
  notes?: string | null;
  isActive: boolean;
  mergedIntoPatientId?: number | null;
  isMerged: boolean;
  appointmentCount: number;
  activeAdmissions: number;
  duplicateGroupSize: number;
}

export interface SavePatientPayload {
  fullName: string;
  email: string;
  phone?: string | null;
  patientCategoryId?: number | null;
  gender?: string | null;
  dateOfBirth?: string | null;
  address?: string | null;
  bloodGroup?: string | null;
  emergencyContact?: string | null;
  notes?: string | null;
  isActive: boolean;
}

export interface CreatePatientPayload extends SavePatientPayload {
  password: string;
}

export interface MergePatientPayload {
  sourcePatientId: number;
  targetPatientId: number;
  notes?: string | null;
}

export type AppointmentLifecycleStatus = 'Pending' | 'Approved' | 'Rescheduled' | 'Cancelled' | 'Completed';

export interface AppointmentAdminRecord {
  appointmentId: number;
  patientId: number;
  patientName: string;
  medicalRecordNumber: string;
  doctorId: number;
  doctorName: string;
  departmentName: string;
  branchName: string;
  scheduleId?: number | null;
  serviceId?: number | null;
  serviceName: string;
  appointmentDate: string;
  slotStartTime: string;
  slotEndTime: string;
  status: AppointmentLifecycleStatus;
  tokenNumber?: string | null;
  reason?: string | null;
  adminRemarks?: string | null;
}

export interface ManageAppointmentPayload {
  status: AppointmentLifecycleStatus;
  doctorId?: number | null;
  scheduleId?: number | null;
  appointmentDate?: string | null;
  slotStartTime?: string | null;
  slotEndTime?: string | null;
  adminRemarks?: string | null;
}

export interface AppointmentTokenSettings {
  appointmentTokenSettingId: number;
  prefix: string;
  startingNumber: number;
  numberPadding: number;
  resetDaily: boolean;
}

export interface DoctorScheduleRecord {
  scheduleId: number;
  doctorId: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
  maxPatientsPerSlot: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string | null;
}

export type AdmissionLifecycleStatus = 'Active' | 'DischargePending' | 'Discharged';

export interface AdmissionTransferRecord {
  admissionTransferId: number;
  fromWardName?: string | null;
  fromBedNumber?: string | null;
  toWardName: string;
  toBedNumber: string;
  transferDate: string;
  notes?: string | null;
}

export interface AdmissionRecord {
  patientAdmissionId: number;
  admissionNumber: string;
  patientId: number;
  patientName: string;
  medicalRecordNumber: string;
  appointmentId?: number | null;
  doctorId?: number | null;
  doctorName: string;
  branchId: number;
  branchName: string;
  wardId: number;
  wardName: string;
  bedId: number;
  bedNumber: string;
  admissionDate: string;
  expectedDischargeDate?: string | null;
  dischargeDate?: string | null;
  status: AdmissionLifecycleStatus;
  reason?: string | null;
  notes?: string | null;
  dischargeSummary?: string | null;
  dischargeApprovedAt?: string | null;
  transfers: AdmissionTransferRecord[];
}

export interface CreateAdmissionPayload {
  patientId: number;
  appointmentId?: number | null;
  doctorId?: number | null;
  wardId: number;
  bedId: number;
  admissionDate: string;
  expectedDischargeDate?: string | null;
  reason?: string | null;
  notes?: string | null;
}

export interface TransferAdmissionPayload {
  wardId: number;
  bedId: number;
  transferDate: string;
  notes?: string | null;
}

export interface ApproveDischargePayload {
  dischargeDate: string;
  dischargeSummary: string;
  notes?: string | null;
}
