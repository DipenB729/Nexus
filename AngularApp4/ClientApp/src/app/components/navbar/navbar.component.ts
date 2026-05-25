import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { RolePermission } from '../../core/models/hms/admin-ops.model';
import { AdminOpsService } from '../../core/services/hms/admin-ops.service';

type DashboardRole = 'SuperAdmin' | 'Admin' | 'User' | 'Doctor';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  exact?: boolean;
}

interface NavSection {
  id: string;
  label: string;
  collapsible?: boolean;
  items: NavItem[];
}

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnChanges, OnInit, OnDestroy {
  @Input() isExpanded = true;
  @Input() role: DashboardRole = 'Admin';
  @Input() displayName = 'Nexus User';
  @Output() toggleEvent = new EventEmitter<void>();
  @Output() logoutEvent = new EventEmitter<void>();
  expandedSections: Record<string, boolean> = {};
  accessPermissions: RolePermission[] = [];
  private routeSub?: Subscription;
  private accessSub?: Subscription;

  private readonly adminSections: NavSection[] = [
    {
      id: 'overview',
      label: 'Overview',
      items: [
        { label: 'Dashboard', icon: 'dashboard', route: '/admin/dashboard', exact: true },
        { label: 'Patients', icon: 'groups', route: '/admin/patients' },
        { label: 'Services', icon: 'medical_services', route: '/admin/services' },
        { label: 'Appointments', icon: 'event_note', route: '/admin/bookings' },
        { label: 'Admissions', icon: 'local_hotel', route: '/admin/admissions' },
        { label: 'Notifications', icon: 'notifications', route: '/admin/notifications' }
      ]
    },
    {
      id: 'masters',
      label: 'Masters',
      collapsible: true,
      items: [
        { label: 'Departments', icon: 'domain', route: '/admin/masters/departments' },
        { label: 'Doctors', icon: 'local_hospital', route: '/admin/masters/doctors' },
        { label: 'Staff', icon: 'badge', route: '/admin/masters/staff' },
        { label: 'Categories', icon: 'sell', route: '/admin/masters/patientCategories' },
        { label: 'Wards', icon: 'meeting_room', route: '/admin/masters/wards' },
        { label: 'Beds', icon: 'hotel', route: '/admin/masters/beds' }
      ]
    },
    {
      id: 'billing',
      label: 'Billing',
      collapsible: true,
      items: [
        { label: 'Bills', icon: 'receipt_long', route: '/admin/billing/bills' },
        { label: 'Charges', icon: 'sell', route: '/admin/billing/charges' },
        { label: 'Payment Methods', icon: 'payments', route: '/admin/billing/paymentMethods' },
        { label: 'Insurance', icon: 'shield', route: '/admin/billing/insurance' },
        { label: 'Corporate Panels', icon: 'apartment', route: '/admin/billing/panels' },
        { label: 'Billing Rules', icon: 'rule', route: '/admin/billing/rules' }
      ]
    },
    {
      id: 'supply-chain',
      label: 'Supply Chain',
      collapsible: true,
      items: [
        { label: 'Overview', icon: 'monitoring', route: '/admin/inventory/dashboard', exact: true },
        { label: 'Units', icon: 'straighten', route: '/admin/inventory/units' },
        { label: 'Categories', icon: 'category', route: '/admin/inventory/categories' },
        { label: 'Medicines', icon: 'medication', route: '/admin/inventory/medicines' },
        { label: 'Items', icon: 'inventory_2', route: '/admin/inventory/items' },
        { label: 'Suppliers', icon: 'local_shipping', route: '/admin/inventory/suppliers' },
        { label: 'Locations', icon: 'warehouse', route: '/admin/inventory/locations' },
        { label: 'Purchases', icon: 'receipt_long', route: '/admin/inventory/purchases' },
        { label: 'Returns', icon: 'assignment_return', route: '/admin/inventory/returns' },
        { label: 'Batches', icon: 'sell', route: '/admin/inventory/batches' },
        { label: 'Transfers', icon: 'swap_horiz', route: '/admin/inventory/transfers' },
        { label: 'Adjustments', icon: 'rule', route: '/admin/inventory/adjustments' }
      ]
    },
    {
      id: 'laboratory',
      label: 'Lab & Packages',
      collapsible: true,
      items: [
        { label: 'Lab Tests', icon: 'biotech', route: '/admin/laboratory/labTests' },
        { label: 'Health Packages', icon: 'health_and_safety', route: '/admin/laboratory/healthPackages' },
        { label: 'Surgery Packages', icon: 'surgical', route: '/admin/laboratory/surgeryPackages' },
        { label: 'Corporate Packages', icon: 'business_center', route: '/admin/laboratory/corporatePackages' },
        { label: 'Discounted Bundles', icon: 'sell', route: '/admin/laboratory/discountedBundles' }
      ]
    },
    {
      id: 'monitoring',
      label: 'Monitoring',
      collapsible: true,
      items: [
        { label: 'Reports Dashboard', icon: 'insights', route: '/admin/monitoring/reports' },
        { label: 'Audit Log', icon: 'history', route: '/admin/monitoring/audit' }
      ]
    },
    {
      id: 'administration',
      label: 'Administration',
      items: [
        { label: 'Roles & Access', icon: 'admin_panel_settings', route: '/admin/roles' },
        { label: 'Hospital Settings', icon: 'settings', route: '/admin/settings' }
      ]
    }
  ];
  private permittedAdminSections: NavSection[] = this.adminSections;

  private readonly superAdminSections: NavSection[] = [
    {
      id: 'superadmin-overview',
      label: 'Overview',
      items: [
        { label: 'Dashboard', icon: 'space_dashboard', route: '/superadmin/dashboard', exact: true }
      ]
    },
    {
      id: 'superadmin-hospital',
      label: 'Hospital',
      items: [
        { label: 'Hospitals', icon: 'domain', route: '/superadmin/hospitals' }
      ]
    },
    {
      id: 'superadmin-access',
      label: 'Access',
      items: [
        { label: 'Admins', icon: 'admin_panel_settings', route: '/superadmin/admins' }
      ]
    }
  ];

  private readonly userSections: NavSection[] = [
    {
      id: 'patient-care',
      label: 'Care',
      items: [
        { label: 'Dashboard', icon: 'space_dashboard', route: '/patient/dashboard', exact: true },
        { label: 'Doctors', icon: 'medical_services', route: '/patient/doctors' },
        { label: 'Book Appointment', icon: 'event_available', route: '/patient/book' },
        { label: 'Appointments', icon: 'calendar_month', route: '/patient/appointments' }
      ]
    },
    {
      id: 'patient-records',
      label: 'Records',
      items: [
        { label: 'Reports', icon: 'assignment', route: '/patient/reports' },
        { label: 'Notifications', icon: 'notifications', route: '/patient/notifications' },
        { label: 'Profile', icon: 'person', route: '/patient/profile' }
      ]
    }
  ];

  private readonly doctorSections: NavSection[] = [
    {
      id: 'doctor-care',
      label: 'Care',
      items: [
        { label: 'Dashboard', icon: 'space_dashboard', route: '/doctor/dashboard', exact: true },
        { label: 'Appointments', icon: 'calendar_month', route: '/doctor/appointments' },
        { label: 'Reports', icon: 'assignment', route: '/doctor/reports' }
      ]
    },
    {
      id: 'doctor-workspace',
      label: 'Workspace',
      items: [
        { label: 'Notifications', icon: 'notifications', route: '/doctor/notifications' },
        { label: 'Availability', icon: 'schedule', route: '/doctor/availability' },
        { label: 'Profile', icon: 'badge', route: '/doctor/profile' }
      ]
    }
  ];

  get navigationSections(): NavSection[] {
    if (this.role === 'SuperAdmin') {
      return this.superAdminSections;
    }

    if (this.role === 'Admin') {
      return this.permittedAdminSections;
    }

    if (this.role === 'Doctor') {
      return this.doctorSections;
    }

    return this.userSections;
  }

  constructor(
    private readonly router: Router,
    private readonly adminOps: AdminOpsService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['role'] && !changes['role'].firstChange) {
      this.syncExpandedSections(this.router.url);
    }
  }

  ngOnInit(): void {
    this.loadAccessProfile();
    this.syncExpandedSections(this.router.url);
    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.syncExpandedSections(event.urlAfterRedirects));
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
    this.accessSub?.unsubscribe();
  }

  get brandText(): string {
    if (this.role === 'SuperAdmin') {
      return 'Nexus Superadmin';
    }

    if (this.role === 'Admin') {
      return 'Nexus Admin';
    }

    if (this.role === 'Doctor') {
      return 'Nexus Doctor';
    }

    return 'Nexus Patient';
  }

  get roleSubtitle(): string {
    if (this.role === 'SuperAdmin') {
      return 'Platform Control';
    }

    if (this.role === 'Admin') {
      return 'Control Center';
    }

    if (this.role === 'Doctor') {
      return 'Clinical Workspace';
    }

    return 'Patient Portal';
  }

  toggle(): void {
    this.toggleEvent.emit();
  }

  isSectionExpanded(section: NavSection): boolean {
    return section.collapsible ? this.expandedSections[this.getSectionKey(section)] === true : true;
  }

  toggleSection(section: NavSection): void {
    if (!section.collapsible || !this.isExpanded) {
      return;
    }

    const sectionKey = this.getSectionKey(section);
    const isOpening = !this.isSectionExpanded(section);
    const nextState = { ...this.expandedSections };

    for (const candidate of this.navigationSections) {
      if (candidate.collapsible) {
        nextState[this.getSectionKey(candidate)] = false;
      }
    }

    this.expandedSections = {
      ...nextState,
      [sectionKey]: isOpening
    };
  }

  logout(): void {
    this.logoutEvent.emit();
  }

  private syncExpandedSections(url: string): void {
    const nextState: Record<string, boolean> = { ...this.expandedSections };

    for (const section of this.navigationSections) {
      if (!section.collapsible) {
        continue;
      }

      const sectionKey = this.getSectionKey(section);
      const hasActiveChild = section.items.some((item) => url.startsWith(item.route));
      if (hasActiveChild) {
        for (const candidate of this.navigationSections) {
          if (candidate.collapsible) {
            nextState[this.getSectionKey(candidate)] = false;
          }
        }
        nextState[sectionKey] = true;
      } else if (!(sectionKey in nextState)) {
        nextState[sectionKey] = false;
      }
    }

    this.expandedSections = nextState;
  }

  private loadAccessProfile(): void {
    this.accessSub?.unsubscribe();
    this.accessSub = this.adminOps.getAccessProfile().subscribe({
      next: (profile) => {
        this.accessPermissions = profile.permissions ?? [];
        this.permittedAdminSections = this.buildPermittedAdminSections();
        this.syncExpandedSections(this.router.url);
      },
      error: () => {
        this.accessPermissions = [];
        this.permittedAdminSections = this.adminSections;
      }
    });
  }

  private buildPermittedAdminSections(): NavSection[] {
    if (!this.accessPermissions.length) {
      return this.adminSections;
    }

    return this.adminSections
      .map((section) => ({
        ...section,
        items: section.items.filter((item) => this.canUseAdminItem(section.id, item.route))
      }))
      .filter((section) => section.items.length > 0);
  }

  private canUseAdminItem(sectionId: string, route: string): boolean {
    return this.accessPermissions.some((permission) =>
      permission.canAccessMenu &&
      permission.canAccessPage &&
      (permission.menuKey === sectionId || route.startsWith(permission.pageRoute) || permission.pageRoute.startsWith(route)));
  }

  private getSectionKey(section: NavSection): string {
    return `${this.role}:${section.id}`;
  }
}
