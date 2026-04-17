export interface PatientClinicalProfile {
  medicalHistory?: string | null;
  allergies?: string | null;
  chronicConditions?: string | null;
  currentMedications?: string | null;
}

export interface DoctorConsultationRecord {
  doctorConsultationId?: number | null;
  symptoms?: string | null;
  diagnosis?: string | null;
  notes?: string | null;
  vitalObservations?: string | null;
  advice?: string | null;
  followUpDate?: string | null;
  status: string;
  updatedAt?: string | null;
}

export interface PrescriptionItemRecord {
  medicineMasterId?: number | null;
  medicineName: string;
  dosage?: string | null;
  frequency?: string | null;
  duration?: string | null;
  instructions?: string | null;
}

export interface PrescriptionRecord {
  doctorPrescriptionId: number;
  appointmentId: number;
  createdAt: string;
  notes?: string | null;
  items: PrescriptionItemRecord[];
}

export interface DiagnosticRequestRecord {
  diagnosticRequestId: number;
  appointmentId: number;
  requestType: string;
  labTestMasterId?: number | null;
  requestedItemName: string;
  remarks?: string | null;
  resultSummary?: string | null;
  status: string;
  createdAt: string;
}

export interface AppointmentHistoryRecord {
  appointmentId: number;
  appointmentDate: string;
  doctorName: string;
  diagnosis?: string | null;
  serviceName?: string | null;
  status: string;
}

export interface AdmissionHistoryRecord {
  patientAdmissionId: number;
  admissionNumber: string;
  admissionDate: string;
  dischargeDate?: string | null;
  status: string;
  wardName?: string | null;
  bedNumber?: string | null;
  reason?: string | null;
}

export interface PatientDocumentRecord {
  patientDocumentId: number;
  category: string;
  title: string;
  fileUrl: string;
  notes?: string | null;
  uploadedByRole: string;
  uploadedAt: string;
}

export interface DoctorWorkspaceAppointmentDetail {
  appointmentId: number;
  appointmentDate: string;
  slotStartTime: string;
  slotEndTime: string;
  status: string;
  tokenNumber?: string | null;
  reason?: string | null;
  adminRemarks?: string | null;
  doctorId: number;
  doctorName: string;
  patientId: number;
  patientName: string;
  medicalRecordNumber: string;
  patientEmail?: string | null;
  patientPhone?: string | null;
  gender?: string | null;
  dateOfBirth?: string | null;
  bloodGroup?: string | null;
  emergencyContact?: string | null;
  address?: string | null;
  serviceName?: string | null;
  departmentName?: string | null;
  clinicalProfile: PatientClinicalProfile;
  consultation: DoctorConsultationRecord;
  previousPrescriptions: PrescriptionRecord[];
  diagnosticRequests: DiagnosticRequestRecord[];
  pastAppointments: AppointmentHistoryRecord[];
  admissionHistory: AdmissionHistoryRecord[];
  uploadedReports: PatientDocumentRecord[];
}

export interface DoctorAvailabilityExceptionRecord {
  doctorAvailabilityExceptionId: number;
  exceptionType: string;
  startDate: string;
  endDate: string;
  notes?: string | null;
  isActive: boolean;
}

export interface DoctorAvailabilityWorkspace {
  schedules: DoctorScheduleRecord[];
  exceptions: DoctorAvailabilityExceptionRecord[];
  blockedSlots: DoctorBlockedSlotRecord[];
}

export interface DoctorScheduleRecord {
  scheduleId: number;
  doctorId: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  breakStartTime?: string | null;
  breakEndTime?: string | null;
  slotDurationMinutes: number;
  maxPatientsPerSlot: number;
  onlineBookingEnabled: boolean;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface SaveConsultationPayload {
  symptoms?: string | null;
  diagnosis?: string | null;
  notes?: string | null;
  vitalObservations?: string | null;
  advice?: string | null;
  followUpDate?: string | null;
  status?: string | null;
}

export interface SavePrescriptionPayload {
  notes?: string | null;
  items: PrescriptionItemRecord[];
}

export interface SaveDiagnosticRequestPayload {
  requestType: string;
  labTestMasterId?: number | null;
  requestedItemName: string;
  remarks?: string | null;
}

export interface SaveDoctorAvailabilityExceptionPayload {
  exceptionType: string;
  startDate: string;
  endDate: string;
  notes?: string | null;
  isActive: boolean;
}

export interface DoctorBlockedSlotRecord {
  doctorBlockedSlotId: number;
  blockDate: string;
  startTime: string;
  endTime: string;
  reason?: string | null;
  isActive: boolean;
}

export interface SaveDoctorBlockedSlotPayload {
  blockDate: string;
  startTime: string;
  endTime: string;
  reason?: string | null;
  isActive: boolean;
}

export interface MedicineSearchResult {
  medicineMasterId: number;
  medicineName: string;
  brand?: string | null;
  strength?: string | null;
  dosageForm?: string | null;
}

export interface LabTestSearchResult {
  labTestMasterId: number;
  testName: string;
  departmentName: string;
  price: number;
  sampleType: string;
  reportFormat: string;
  isActive: boolean;
}
