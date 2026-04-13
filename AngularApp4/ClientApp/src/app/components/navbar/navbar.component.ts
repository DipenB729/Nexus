import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';

type DashboardRole = 'Admin' | 'User';

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
export class NavbarComponent implements OnInit, OnDestroy {
  @Input() isExpanded = true;
  @Input() role: DashboardRole = 'Admin';
  @Input() displayName = 'Nexus User';
  @Output() toggleEvent = new EventEmitter<void>();
  @Output() logoutEvent = new EventEmitter<void>();
  expandedSections: Record<string, boolean> = {};
  private routeSub?: Subscription;

  private readonly adminSections: NavSection[] = [
    {
      id: 'overview',
      label: 'Overview',
      items: [
        { label: 'Dashboard', icon: 'dashboard', route: '/admin/dashboard', exact: true },
        { label: 'Patients', icon: 'groups', route: '/admin/patients' },
        { label: 'Services', icon: 'medical_services', route: '/admin/services' },
        { label: 'Appointments', icon: 'event_note', route: '/admin/bookings' },
        { label: 'Admissions', icon: 'local_hotel', route: '/admin/admissions' }
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

  private readonly userSections: NavSection[] = [
    {
      id: 'workspace',
      label: 'Workspace',
      items: [
        { label: 'Dashboard', icon: 'space_dashboard', route: '/user/dashboard', exact: true },
        { label: 'Services', icon: 'widgets', route: '/user/services' },
        { label: 'Appointments', icon: 'calendar_month', route: '/user/appointments' }
      ]
    }
  ];

  get navigationSections(): NavSection[] {
    return this.role === 'Admin' ? this.adminSections : this.userSections;
  }

  constructor(private readonly router: Router) {}

  ngOnInit(): void {
    this.syncExpandedSections(this.router.url);
    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.syncExpandedSections(event.urlAfterRedirects));
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  get brandText(): string {
    return this.role === 'Admin' ? 'Nexus Admin' : 'Nexus User';
  }

  get roleSubtitle(): string {
    return this.role === 'Admin' ? 'Control Center' : 'Member Space';
  }

  toggle(): void {
    this.toggleEvent.emit();
  }

  isSectionExpanded(section: NavSection): boolean {
    return section.collapsible ? this.expandedSections[section.id] !== false : true;
  }

  toggleSection(section: NavSection): void {
    if (!section.collapsible || !this.isExpanded) {
      return;
    }

    this.expandedSections = {
      ...this.expandedSections,
      [section.id]: !this.isSectionExpanded(section)
    };
  }

  logout(): void {
    this.logoutEvent.emit();
  }

  private syncExpandedSections(url: string): void {
    const nextState: Record<string, boolean> = { ...this.expandedSections };

    for (const section of this.adminSections) {
      if (!section.collapsible) {
        continue;
      }

      const hasActiveChild = section.items.some((item) => url.startsWith(item.route));
      if (!(section.id in nextState) || hasActiveChild) {
        nextState[section.id] = hasActiveChild || nextState[section.id] !== false;
      }
    }

    this.expandedSections = nextState;
  }
}
