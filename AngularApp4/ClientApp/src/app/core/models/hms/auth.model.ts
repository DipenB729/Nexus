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

export interface DoctorProfile {
  doctorId: number;
  departmentId?: number | null;
  fullName: string;
  photoUrl?: string | null;
  email: string;
  phone?: string | null;
  specialization?: string | null;
  licenseNumber?: string | null;
  experienceYears: number;
  qualification?: string | null;
  consultationFee: number;
  branchName?: string | null;
  departmentName?: string | null;
  bio?: string | null;
  address?: string | null;
  opdDays?: string | null;
  opdStartTime?: string | null;
  opdEndTime?: string | null;
}

export interface UpdateDoctorProfileRequest {
  fullName: string;
  email: string;
  phone?: string | null;
  departmentId?: number | null;
  specialization?: string | null;
  licenseNumber?: string | null;
  experienceYears: number;
  qualification?: string | null;
  consultationFee: number;
  bio?: string | null;
  address?: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface DoctorProfileDepartmentOption {
  departmentId: number;
  name: string;
  branchName: string;
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
  scheduleId?: number | null;
  patientName: string;
  medicalRecordNumber: string;
  patientGender?: string | null;
  patientDateOfBirth?: string | null;
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

export interface ManageDoctorAppointmentRequest {
  status: 'Approved' | 'Rescheduled' | 'Cancelled' | 'Completed' | 'NoShow';
  scheduleId?: number | null;
  appointmentDate?: string | null;
  slotStartTime?: string | null;
  slotEndTime?: string | null;
  reason?: string | null;
  adminRemarks?: string | null;
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
