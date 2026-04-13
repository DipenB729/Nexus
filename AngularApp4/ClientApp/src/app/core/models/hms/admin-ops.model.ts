export type SettingsSection = 'organization' | 'notifications' | 'system' | 'backup' | 'security';

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

export interface AppointmentReminderPreview {
  appointmentId: number;
  patientName: string;
  doctorName: string;
  appointmentDate: string;
  timeSlot: string;
  status: string;
}

export interface NotificationPreview {
  lowStockCount: number;
  nearExpiryCount: number;
  appointmentReminderCount: number;
  paymentDueCount: number;
  lowStockAlerts: StockAlert[];
  expiryAlerts: StockAlert[];
  appointmentReminders: AppointmentReminderPreview[];
  paymentDueAlerts: PendingPayment[];
}

export interface NotificationSettings {
  lowStockAlertsEnabled: boolean;
  lowStockAlertChannels: string;
  lowStockReminderFrequencyHours: number;
  expiryAlertsEnabled: boolean;
  expiryAlertDays: number;
  expiryAlertChannels: string;
  appointmentRemindersEnabled: boolean;
  appointmentReminderHoursBefore: number;
  appointmentReminderChannels: string;
  paymentDueAlertsEnabled: boolean;
  paymentDueReminderDaysBefore: number;
  paymentDueAlertChannels: string;
  recipientEmails: string;
  updatedAt: string;
  preview: NotificationPreview;
}

export interface SystemSettings {
  defaultCurrencyCode: string;
  timeZoneId: string;
  invoicePrefix: string;
  nextInvoiceNumber: number;
  smsProviderName: string;
  smsApiUrl: string;
  smsApiKey: string;
  smsSenderId: string;
  emailProviderName: string;
  emailApiUrl: string;
  emailApiKey: string;
  emailFromAddress: string;
  autoBackupEnabled: boolean;
  autoBackupTime: string;
  backupRetentionCount: number;
  backupStoragePath: string;
  updatedAt: string;
}

export interface BackupLogEntry {
  backupLogId: number;
  backupName: string;
  backupType: string;
  status: string;
  summary: string;
  triggeredByName?: string | null;
  createdAt: string;
  restoredAt?: string | null;
  restoredByName?: string | null;
  canRestore: boolean;
}

export interface BackupCenter {
  lastBackupAt?: string | null;
  lastRestoreAt?: string | null;
  logs: BackupLogEntry[];
}

export interface SecuritySettings {
  sessionTimeoutMinutes: number;
  minPasswordLength: number;
  requireUppercase: boolean;
  requireLowercase: boolean;
  requireDigit: boolean;
  requireSpecialCharacter: boolean;
  passwordExpiryDays: number;
  permissionReviewIntervalDays: number;
  lastPermissionReviewAt?: string | null;
  lastPermissionReviewedByName?: string | null;
  updatedAt: string;
}

export interface PermissionReviewRole {
  roleId: number;
  roleName: string;
  isActive: boolean;
  userCount: number;
  modulesWithView: number;
  modulesWithEdit: number;
  modulesWithDelete: number;
}

export interface PermissionReviewSummary {
  totalRoles: number;
  activeRoles: number;
  totalUsers: number;
  lastReviewedAt?: string | null;
  lastReviewedByName?: string | null;
  nextReviewDueAt?: string | null;
  roles: PermissionReviewRole[];
}

export interface SecurityDeviceLog {
  auditLogEntryId: number;
  action: string;
  actorName: string;
  actorEmail?: string | null;
  actorRole?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  createdAt: string;
}

export interface SecurityCenter {
  settings: SecuritySettings;
  permissionReview: PermissionReviewSummary;
  deviceLogs: SecurityDeviceLog[];
}

export interface ControlCenterSettings {
  organization: OrganizationSettings;
  notifications: NotificationSettings;
  system: SystemSettings;
  backups: BackupCenter;
  security: SecurityCenter;
}
