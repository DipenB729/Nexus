import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AppNotification, AuthSession } from './core/models/hms/auth.model';
import { AuthApiService } from './core/services/hms/auth-api.service';
import { NotificationsService } from './core/services/notifications.service';

type DashboardRole = 'Admin' | 'User' | 'Doctor';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  isSidebarExpanded = true;
  isAuthRoute = false;
  isDashboardRoute = false;
  pageTitle = 'Nexus Portal';
  currentRole: DashboardRole | null = null;
  currentSession: AuthSession | null = null;
  notifications: AppNotification[] = [];

  private routeSub?: Subscription;
  private sessionSub?: Subscription;
  private notificationsSub?: Subscription;
  private notificationsRefreshSub?: Subscription;

  constructor(
    private readonly router: Router,
    private readonly auth: AuthApiService,
    private readonly notificationsApi: NotificationsService
  ) {}

  ngOnInit(): void {
    this.currentSession = this.auth.getSession();
    this.updateLayoutState(this.router.url);

    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.updateLayoutState(event.urlAfterRedirects));

    this.sessionSub = this.auth.session$.subscribe((session) => {
      this.currentSession = session;
      if (session) {
        this.loadNotifications();
      } else {
        this.notifications = [];
      }
    });

    this.notificationsRefreshSub = this.notificationsApi.refresh$.subscribe(() => {
      if (this.currentSession) {
        this.loadNotifications();
      }
    });

    if (this.currentSession) {
      this.loadNotifications();
    }
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
    this.sessionSub?.unsubscribe();
    this.notificationsSub?.unsubscribe();
    this.notificationsRefreshSub?.unsubscribe();
  }

  toggleSidebar(): void {
    this.isSidebarExpanded = !this.isSidebarExpanded;
  }

  logout(): void {
    this.auth.logout();
  }

  get userDisplayName(): string {
    if (this.currentSession?.fullName) {
      return this.currentSession.fullName;
    }

    if (this.currentRole === 'Admin') {
      return 'System Admin';
    }

    if (this.currentRole === 'Doctor') {
      return 'Doctor Portal';
    }

    return 'Patient Portal';
  }

  get userInitial(): string {
    return this.userDisplayName.charAt(0).toUpperCase();
  }

  get workspaceLabel(): string {
    if (this.currentRole === 'Admin') {
      return 'Admin Workspace';
    }

    if (this.currentRole === 'Doctor') {
      return 'Doctor Workspace';
    }

    return 'Patient Workspace';
  }

  get primaryMenuRoute(): string {
    if (this.currentRole === 'Admin') {
      return '/admin/dashboard';
    }

    if (this.currentRole === 'Doctor') {
      return '/doctor/dashboard';
    }

    return '/patient/dashboard';
  }

  get secondaryMenuRoute(): string {
    if (this.currentRole === 'Admin') {
      return '/admin/settings';
    }

    if (this.currentRole === 'Doctor') {
      return '/doctor/appointments';
    }

    return '/patient/book';
  }

  get secondaryMenuLabel(): string {
    if (this.currentRole === 'Admin') {
      return 'Settings';
    }

    if (this.currentRole === 'Doctor') {
      return 'Appointments';
    }

    return 'Book Appointment';
  }

  get sessionRoleLabel(): string {
    return this.auth.getDisplayRole(this.currentSession?.role ?? this.currentRole ?? undefined);
  }

  get unreadNotificationsCount(): number {
    return this.notifications.filter((item) => !item.isRead).length;
  }

  loadNotifications(): void {
    this.notificationsSub?.unsubscribe();
    this.notificationsSub = this.notificationsApi.getNotifications().subscribe({
      next: (items) => {
        this.notifications = items;
      },
      error: () => {
        this.notifications = [];
      }
    });
  }

  markNotificationAsRead(notification: AppNotification): void {
    if (notification.isRead) {
      if (notification.actionUrl) {
        void this.router.navigateByUrl(notification.actionUrl);
      }
      return;
    }

    this.notificationsApi.markAsRead(notification.appNotificationId).subscribe({
      next: () => {
        this.notifications = this.notifications.map((item) =>
          item.appNotificationId === notification.appNotificationId ? { ...item, isRead: true } : item);
        if (notification.actionUrl) {
          void this.router.navigateByUrl(notification.actionUrl);
        }
      }
    });
  }

  markAllNotificationsAsRead(): void {
    this.notificationsApi.markAllAsRead().subscribe({
      next: () => {
        this.notifications = this.notifications.map((item) => ({ ...item, isRead: true }));
      }
    });
  }

  private updateLayoutState(url: string): void {
    const currentPath = url.split('?')[0];

    this.isAuthRoute = currentPath.startsWith('/auth');
    this.currentRole = currentPath.startsWith('/admin')
      ? 'Admin'
      : currentPath.startsWith('/doctor')
        ? 'Doctor'
      : currentPath.startsWith('/user') || currentPath.startsWith('/patient')
        ? 'User'
        : null;
    this.isDashboardRoute = this.currentRole !== null;
    this.pageTitle = this.resolvePageTitle(currentPath);
  }

  private resolvePageTitle(path: string): string {
    if (path.startsWith('/admin/masters/departments')) {
      return 'Department Management';
    }

    if (path.startsWith('/admin/masters/doctors')) {
      return 'Doctor Management';
    }

    if (path.startsWith('/admin/masters/staff')) {
      return 'Staff Management';
    }

    if (path.startsWith('/admin/masters/patientCategories')) {
      return 'Patient Category Setup';
    }

    if (path.startsWith('/admin/masters/wards')) {
      return 'Ward Management';
    }

    if (path.startsWith('/admin/masters/beds')) {
      return 'Bed Management';
    }

    if (path.startsWith('/admin/patients')) {
      return 'Patient Registration Oversight';
    }

    if (path.startsWith('/admin/bookings')) {
      return 'Appointment Management';
    }

    if (path.startsWith('/admin/admissions')) {
      return 'Admission & Discharge Control';
    }

    if (path.startsWith('/admin/billing')) {
      const billingSection = path.split('/')[3] ?? 'bills';
      const billingTitles: Record<string, string> = {
        charges: 'Service & Charge Setup',
        bills: 'Billing Management',
        paymentMethods: 'Payment Method Setup',
        insurance: 'Insurance Billing Setup',
        panels: 'Corporate Billing Setup',
        rules: 'Billing Rules & Claims'
      };

      return billingTitles[billingSection] ?? 'Billing Management';
    }

    if (path.startsWith('/admin/inventory')) {
      const inventorySection = path.split('/')[3] ?? 'dashboard';
      const inventoryTitles: Record<string, string> = {
        dashboard: 'Supply Chain Overview',
        units: 'Inventory Unit Setup',
        categories: 'Inventory Category Setup',
        medicines: 'Medicine Master',
        items: 'Item & Stock Master',
        suppliers: 'Supplier Management',
        locations: 'Stock Location Management',
        purchases: 'Purchase Management',
        returns: 'Purchase Return Management',
        batches: 'Batch & Expiry Management',
        transfers: 'Stock Transfer Management',
        adjustments: 'Stock Adjustment Management'
      };

      return inventoryTitles[inventorySection] ?? 'Pharmacy & Stock Admin';
    }

    if (path.startsWith('/admin/laboratory')) {
      const laboratorySection = path.split('/')[3] ?? 'labTests';
      const laboratoryTitles: Record<string, string> = {
        labTests: 'Lab Test Master',
        healthPackages: 'Health Package Management',
        surgeryPackages: 'Surgery Package Management',
        corporatePackages: 'Corporate Package Management',
        discountedBundles: 'Discounted Bundle Management'
      };

      return laboratoryTitles[laboratorySection] ?? 'Lab & Service Admin';
    }

    if (path.startsWith('/admin/monitoring')) {
      const monitoringSection = path.split('/')[3] ?? 'reports';
      const monitoringReport = path.split('/')[4] ?? '';
      const reportTitles: Record<string, string> = {
        patient: 'Patient Report',
        billing: 'Billing Report',
        doctorRevenue: 'Doctor Revenue Report',
        pharmacySales: 'Pharmacy Sales Report',
        purchase: 'Purchase Report',
        stockBalance: 'Stock Balance Report',
        expiry: 'Expiry Report',
        bedOccupancy: 'Bed Occupancy Report'
      };
      const monitoringTitles: Record<string, string> = {
        reports: 'Reports Dashboard',
        audit: 'Audit Log & Monitoring'
      };

      if (monitoringSection === 'reports' && monitoringReport) {
        return reportTitles[monitoringReport] ?? 'Reports Dashboard';
      }

      return monitoringTitles[monitoringSection] ?? 'Reports & Monitoring';
    }

    if (path.startsWith('/admin/settings')) {
      const settingsSection = path.split('/')[3] ?? 'organization';
      const settingsTitles: Record<string, string> = {
        organization: 'Organization Settings',
        notifications: 'Notification Settings',
        system: 'System Settings',
        backup: 'Backup & Restore',
        security: 'Security Settings'
      };

      return settingsTitles[settingsSection] ?? 'Hospital Settings';
    }

    if (path.startsWith('/patient/doctors/')) {
      return 'Doctor Details';
    }

    if (path.startsWith('/doctor/appointments')) {
      return 'Doctor Appointments';
    }

    const titles: Record<string, string> = {
      '/admin/dashboard': 'Admin Dashboard',
      '/admin/services': 'Service Management',
      '/admin/roles': 'Roles & Permissions',
      '/admin/users': 'Roles & Permissions',
      '/patient/dashboard': 'Patient Dashboard',
      '/patient/book': 'Book Appointment',
      '/patient/doctors': 'Doctor Listing',
      '/patient/services': 'Doctor Listing',
      '/patient/appointments': 'My Appointments',
      '/patient/profile': 'Patient Profile',
      '/doctor/dashboard': 'Doctor Dashboard'
    };

    return titles[path] ?? 'Nexus Portal';
  }
}
