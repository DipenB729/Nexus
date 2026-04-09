import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthSession } from './core/models/hms/auth.model';
import { AuthApiService } from './core/services/hms/auth-api.service';

type DashboardRole = 'Admin' | 'User';

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

  private routeSub?: Subscription;
  private sessionSub?: Subscription;

  constructor(private readonly router: Router, private readonly auth: AuthApiService) {}

  ngOnInit(): void {
    this.currentSession = this.auth.getSession();
    this.updateLayoutState(this.router.url);

    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.updateLayoutState(event.urlAfterRedirects));

    this.sessionSub = this.auth.session$.subscribe((session) => {
      this.currentSession = session;
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
    this.sessionSub?.unsubscribe();
  }

  toggleSidebar(): void {
    this.isSidebarExpanded = !this.isSidebarExpanded;
  }

  logout(): void {
    this.auth.logout();
  }

  get userDisplayName(): string {
    return this.currentSession?.fullName ?? (this.currentRole === 'Admin' ? 'System Admin' : 'Portal User');
  }

  get userInitial(): string {
    return this.userDisplayName.charAt(0).toUpperCase();
  }

  get workspaceLabel(): string {
    return this.currentRole === 'Admin' ? 'Admin Workspace' : 'User Workspace';
  }

  get primaryMenuRoute(): string {
    return this.currentRole === 'Admin' ? '/admin/dashboard' : '/user/dashboard';
  }

  get secondaryMenuRoute(): string {
    return this.currentRole === 'Admin' ? '/admin/settings' : '/user/services';
  }

  get secondaryMenuLabel(): string {
    return this.currentRole === 'Admin' ? 'Settings' : 'Services';
  }

  private updateLayoutState(url: string): void {
    const currentPath = url.split('?')[0];

    this.isAuthRoute = currentPath.startsWith('/auth');
    this.currentRole = currentPath.startsWith('/admin')
      ? 'Admin'
      : currentPath.startsWith('/user')
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

    const titles: Record<string, string> = {
      '/admin/dashboard': 'Admin Dashboard',
      '/admin/services': 'Service Management',
      '/admin/roles': 'Roles & Permissions',
      '/admin/users': 'Roles & Permissions',
      '/admin/settings': 'Hospital Settings',
      '/user/dashboard': 'User Dashboard',
      '/user/services': 'Service Catalog',
      '/user/appointments': 'My Appointments'
    };

    return titles[path] ?? 'Nexus Portal';
  }
}
