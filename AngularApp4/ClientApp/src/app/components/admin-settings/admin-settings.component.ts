import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import {
  BackupCenter,
  BackupLogEntry,
  BranchSettings,
  ControlCenterSettings,
  HospitalProfile,
  NotificationSettings,
  OrganizationSettings,
  SecurityCenter,
  SettingsSection,
  SystemSettings
} from '../../core/models/hms/admin-ops.model';
import { AdminOpsService } from '../../core/services/hms/admin-ops.service';

interface SectionOption {
  key: SettingsSection;
  label: string;
  icon: string;
  description: string;
}

const SECTION_OPTIONS: ReadonlyArray<SectionOption> = [
  { key: 'organization', label: 'Organization', icon: 'apartment', description: 'Hospital profile, branch settings, and operating identity.' },
  { key: 'notifications', label: 'Notifications', icon: 'notifications_active', description: 'Alert rules for stock, expiry, appointments, and payments.' },
  { key: 'system', label: 'System', icon: 'tune', description: 'Currency, time zone, numbering, API, and backup configuration.' },
  { key: 'backup', label: 'Backup & Restore', icon: 'save_history', description: 'Manual backups, automatic backup history, and restore actions.' },
  { key: 'security', label: 'Security', icon: 'shield', description: 'Session timeout, password rules, device logs, and permission review.' }
];

const EMPTY_PROFILE: HospitalProfile = {
  hospitalProfileId: 0,
  hospitalName: '',
  logoUrl: '',
  addressLine1: '',
  city: '',
  stateOrProvince: '',
  postalCode: '',
  country: '',
  contactEmail: '',
  contactPhone: '',
  taxLabel: 'VAT',
  taxRegistrationNumber: '',
  taxPercentage: 13,
  currencyCode: 'NPR',
  invoicePrefix: 'NEX',
  invoiceStartingNumber: 5001,
  invoiceFooterNote: '',
  multiBranchEnabled: true
};

const EMPTY_NOTIFICATIONS: NotificationSettings = {
  lowStockAlertsEnabled: true,
  lowStockAlertChannels: 'Dashboard,Email',
  lowStockReminderFrequencyHours: 12,
  expiryAlertsEnabled: true,
  expiryAlertDays: 30,
  expiryAlertChannels: 'Dashboard,Email',
  appointmentRemindersEnabled: true,
  appointmentReminderHoursBefore: 24,
  appointmentReminderChannels: 'SMS,Email',
  paymentDueAlertsEnabled: true,
  paymentDueReminderDaysBefore: 2,
  paymentDueAlertChannels: 'Dashboard,Email',
  recipientEmails: '',
  updatedAt: '',
  preview: {
    lowStockCount: 0,
    nearExpiryCount: 0,
    appointmentReminderCount: 0,
    paymentDueCount: 0,
    lowStockAlerts: [],
    expiryAlerts: [],
    appointmentReminders: [],
    paymentDueAlerts: []
  }
};

const EMPTY_SYSTEM: SystemSettings = {
  defaultCurrencyCode: 'NPR',
  timeZoneId: 'Asia/Kathmandu',
  invoicePrefix: 'NEX',
  nextInvoiceNumber: 5001,
  smsProviderName: '',
  smsApiUrl: '',
  smsApiKey: '',
  smsSenderId: '',
  emailProviderName: '',
  emailApiUrl: '',
  emailSmtpPort: 587,
  emailSmtpUsername: '',
  emailApiKey: '',
  emailFromAddress: '',
  emailUseSsl: true,
  doctorPortalBaseUrl: '',
  autoBackupEnabled: true,
  autoBackupTime: '02:00',
  backupRetentionCount: 10,
  backupStoragePath: 'App_Data/Backups',
  updatedAt: ''
};

const EMPTY_SECURITY: SecurityCenter = {
  settings: {
    sessionTimeoutMinutes: 120,
    minPasswordLength: 8,
    requireUppercase: true,
    requireLowercase: true,
    requireDigit: true,
    requireSpecialCharacter: true,
    passwordExpiryDays: 90,
    permissionReviewIntervalDays: 30,
    updatedAt: ''
  },
  permissionReview: {
    totalRoles: 0,
    activeRoles: 0,
    totalUsers: 0,
    roles: []
  },
  deviceLogs: []
};

@Component({
  selector: 'app-admin-settings',
  templateUrl: './admin-settings.component.html',
  styleUrls: ['./admin-settings.component.scss']
})
export class AdminSettingsComponent implements OnInit, OnDestroy {
  readonly sections = SECTION_OPTIONS;

  activeSection: SettingsSection = 'organization';
  profile: HospitalProfile = { ...EMPTY_PROFILE };
  branches: BranchSettings[] = [];
  notifications: NotificationSettings = structuredClone(EMPTY_NOTIFICATIONS);
  system: SystemSettings = { ...EMPTY_SYSTEM };
  backups: BackupCenter = { logs: [] };
  security: SecurityCenter = structuredClone(EMPTY_SECURITY);
  backupName = '';
  isLoading = true;
  errorMessage = '';
  successMessage = '';
  savingSection: SettingsSection | '' = '';
  restoringBackupId: number | null = null;

  private routeSub?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly adminOps: AdminOpsService
  ) {}

  ngOnInit(): void {
    this.routeSub = this.route.paramMap.subscribe((params) => this.syncSection(params));
    this.loadControlCenter();
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  get currentSection(): SectionOption {
    return this.sections.find((section) => section.key === this.activeSection) ?? this.sections[0];
  }

  navigateSection(section: SettingsSection): void {
    void this.router.navigate(['/admin/settings', section]);
  }

  addBranch(): void {
    this.branches = [
      ...this.branches,
      {
        branchId: 0,
        name: '',
        code: '',
        address: '',
        contactPhone: '',
        contactEmail: '',
        isPrimary: this.branches.length === 0,
        isActive: true,
        totalBeds: 0,
        occupiedBeds: 0
      }
    ];
  }

  removeBranch(index: number): void {
    const branch = this.branches[index];
    if (!branch) {
      return;
    }

    if (branch.branchId > 0) {
      this.branches[index] = { ...branch, isActive: false, isPrimary: false };
    } else {
      this.branches = this.branches.filter((_, currentIndex) => currentIndex !== index);
    }

    if (!this.branches.some((item) => item.isPrimary) && this.branches.length) {
      this.branches[0].isPrimary = true;
    }
  }

  setPrimary(selectedIndex: number): void {
    this.branches = this.branches.map((branch, index) => ({
      ...branch,
      isPrimary: index === selectedIndex
    }));
  }

  isSectionSaving(section: SettingsSection): boolean {
    return this.savingSection === section;
  }

  saveOrganization(): void {
    this.savingSection = 'organization';
    this.clearMessages();

    const payload: OrganizationSettings = {
      profile: { ...this.profile },
      branches: this.branches.map((branch) => ({
        ...branch,
        occupiedBeds: Math.min(branch.occupiedBeds, branch.totalBeds)
      }))
    };

    this.adminOps.updateOrganizationSettings(payload).subscribe({
      next: () => this.finishSectionSave('organization', 'Organization settings saved.'),
      error: () => this.failSectionSave('Unable to save organization settings.')
    });
  }

  saveNotifications(): void {
    this.savingSection = 'notifications';
    this.clearMessages();

    this.adminOps.updateNotificationSettings({ ...this.notifications }).subscribe({
      next: () => this.finishSectionSave('notifications', 'Notification settings saved.'),
      error: () => this.failSectionSave('Unable to save notification settings.')
    });
  }

  saveSystem(): void {
    this.savingSection = 'system';
    this.clearMessages();

    this.adminOps.updateSystemSettings({ ...this.system }).subscribe({
      next: () => this.finishSectionSave('system', 'System settings saved.'),
      error: () => this.failSectionSave('Unable to save system settings.')
    });
  }

  saveSecurity(): void {
    this.savingSection = 'security';
    this.clearMessages();

    this.adminOps.updateSecuritySettings({ ...this.security.settings }).subscribe({
      next: () => this.finishSectionSave('security', 'Security settings saved.'),
      error: () => this.failSectionSave('Unable to save security settings.')
    });
  }

  completePermissionReview(): void {
    this.savingSection = 'security';
    this.clearMessages();

    this.adminOps.markPermissionReview().subscribe({
      next: () => this.finishSectionSave('security', 'Permission review marked as completed.'),
      error: () => this.failSectionSave('Unable to update permission review.')
    });
  }

  createManualBackup(): void {
    this.savingSection = 'backup';
    this.clearMessages();

    this.adminOps.createManualBackup(this.backupName.trim()).subscribe({
      next: (backups) => {
        this.backups = { ...backups, logs: backups.logs.map((log) => ({ ...log })) };
        this.savingSection = '';
        this.backupName = '';
        this.successMessage = 'Manual backup created.';
      },
      error: () => this.failSectionSave('Unable to create manual backup.')
    });
  }

  restoreBackup(log: BackupLogEntry): void {
    if (!log.canRestore || !window.confirm(`Restore settings from backup "${log.backupName}"?`)) {
      return;
    }

    this.restoringBackupId = log.backupLogId;
    this.clearMessages();

    this.adminOps.restoreBackup(log.backupLogId).subscribe({
      next: (backups) => {
        this.backups = { ...backups, logs: backups.logs.map((entry) => ({ ...entry })) };
        this.restoringBackupId = null;
        this.successMessage = `Backup "${log.backupName}" restored.`;
        this.loadControlCenter(false);
      },
      error: () => {
        this.restoringBackupId = null;
        this.errorMessage = 'Unable to restore the selected backup.';
      }
    });
  }

  formatCurrency(value: number): string {
    const currency = this.system.defaultCurrencyCode || 'NPR';
    return `${currency} ${value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }

  formatDateTime(value?: string | null): string {
    return value ? new Date(value).toLocaleString() : 'Not available';
  }

  private syncSection(params: ParamMap): void {
    const section = params.get('section');
    if (!this.isSettingsSection(section)) {
      void this.router.navigate(['/admin/settings', 'organization'], { replaceUrl: true });
      return;
    }

    this.activeSection = section;
    this.clearMessages();
  }

  private loadControlCenter(showLoader = true): void {
    if (showLoader) {
      this.isLoading = true;
    }

    this.adminOps.getControlCenter().subscribe({
      next: (settings) => {
        this.applyControlCenter(settings);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load control center settings.';
      }
    });
  }

  private applyControlCenter(settings: ControlCenterSettings): void {
    this.profile = { ...EMPTY_PROFILE, ...settings.organization.profile };
    this.branches = settings.organization.branches.map((branch) => ({ ...branch }));
    this.notifications = {
      ...structuredClone(EMPTY_NOTIFICATIONS),
      ...settings.notifications,
      preview: {
        ...structuredClone(EMPTY_NOTIFICATIONS.preview),
        ...settings.notifications.preview,
        lowStockAlerts: settings.notifications.preview.lowStockAlerts.map((item) => ({ ...item })),
        expiryAlerts: settings.notifications.preview.expiryAlerts.map((item) => ({ ...item })),
        appointmentReminders: settings.notifications.preview.appointmentReminders.map((item) => ({ ...item })),
        paymentDueAlerts: settings.notifications.preview.paymentDueAlerts.map((item) => ({ ...item }))
      }
    };
    this.system = { ...EMPTY_SYSTEM, ...settings.system };
    this.backups = { ...settings.backups, logs: settings.backups.logs.map((log) => ({ ...log })) };
    this.security = {
      settings: { ...EMPTY_SECURITY.settings, ...settings.security.settings },
      permissionReview: {
        ...EMPTY_SECURITY.permissionReview,
        ...settings.security.permissionReview,
        roles: settings.security.permissionReview.roles.map((role) => ({ ...role }))
      },
      deviceLogs: settings.security.deviceLogs.map((log) => ({ ...log }))
    };
  }

  private finishSectionSave(section: SettingsSection, message: string): void {
    this.savingSection = '';
    this.successMessage = message;
    this.loadControlCenter(false);
    this.activeSection = section;
  }

  private failSectionSave(message: string): void {
    this.savingSection = '';
    this.errorMessage = message;
  }

  private clearMessages(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }

  private isSettingsSection(value: string | null): value is SettingsSection {
    return !!value && this.sections.some((section) => section.key === value);
  }
}
