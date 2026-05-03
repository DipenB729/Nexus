import {
  DiagnosticRequestRecord,
  DoctorConsultationRecord,
  PatientDocumentRecord,
  PrescriptionRecord
} from './doctor-workspace.model';

export interface CareThreadSummary {
  appointmentId: number;
  patientId: number;
  doctorId: number;
  patientName: string;
  doctorName: string;
  doctorSpecialization?: string | null;
  medicalRecordNumber?: string | null;
  appointmentDate: string;
  slotStartTime: string;
  slotEndTime: string;
  status: string;
  serviceName?: string | null;
  lastMessageAt?: string | null;
  lastMessage?: string | null;
  reportCount: number;
}

export interface CareConversationMessage {
  careConversationMessageId: number;
  appointmentId: number;
  senderRole: string;
  senderName: string;
  message: string;
  createdAt: string;
}

export interface PatientCaseReport {
  patientCaseReportId: number;
  appointmentId: number;
  symptoms: string;
  previousReportSummary?: string | null;
  document?: PatientDocumentRecord | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CareThreadDetail {
  thread: CareThreadSummary;
  messages: CareConversationMessage[];
  reports: PatientCaseReport[];
  consultation: DoctorConsultationRecord;
  prescriptions: PrescriptionRecord[];
  diagnosticRequests: DiagnosticRequestRecord[];
}

export interface SaveCareMessagePayload {
  message: string;
}

export interface SavePatientReportPayload {
  symptoms: string;
  previousReportSummary?: string | null;
  reportTitle?: string | null;
  reportUrl?: string | null;
  reportCategory?: string | null;
  reportNotes?: string | null;
}
