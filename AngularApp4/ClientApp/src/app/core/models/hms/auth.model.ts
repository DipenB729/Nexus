export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phone?: string | null;
  password: string;
}

export interface AuthResponse {
  token: string;
  userId: number;
  fullName: string;
  email: string;
  role: string;
}

export type AuthSession = AuthResponse;

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: string[];
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ForgotPasswordResponse {
  resetCodePreview?: string | null;
  expiresAt?: string | null;
}

export interface ResetPasswordRequest {
  email: string;
  resetCode: string;
  newPassword: string;
}

export interface PatientProfile {
  patientId: number;
  fullName: string;
  email: string;
  phone?: string | null;
  medicalRecordNumber?: string | null;
  patientCategoryName?: string | null;
  gender?: string | null;
  dateOfBirth?: string | null;
  address?: string | null;
  bloodGroup?: string | null;
  emergencyContact?: string | null;
}

export interface UpdatePatientProfileRequest {
  fullName: string;
  phone?: string | null;
  gender?: string | null;
  dateOfBirth?: string | null;
  address?: string | null;
  bloodGroup?: string | null;
  emergencyContact?: string | null;
}

export interface PatientAppointment {
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
  doctorSpecialization?: string | null;
  serviceId?: number | null;
  serviceName?: string | null;
  servicePrice?: number | null;
}

export interface DoctorAppointment {
  appointmentId: number;
  patientId: number;
  patientName: string;
  medicalRecordNumber: string;
  appointmentDate: string;
  slotStartTime: string;
  slotEndTime: string;
  status: string;
  tokenNumber?: string | null;
  reason?: string | null;
  adminRemarks?: string | null;
  doctorId: number;
  doctorName: string;
  doctorSpecialization?: string | null;
  serviceId?: number | null;
  serviceName?: string | null;
  servicePrice?: number | null;
}

export interface AppNotification {
  appNotificationId: number;
  category: string;
  notificationType: string;
  title: string;
  message: string;
  actionUrl?: string | null;
  isRead: boolean;
  scheduledForUtc: string;
  createdAt: string;
}

export interface PatientAppointmentPayload {
  doctorId: number;
  scheduleId?: number | null;
  appointmentDate: string;
  slotStartTime: string;
  slotEndTime: string;
  reason?: string | null;
}
