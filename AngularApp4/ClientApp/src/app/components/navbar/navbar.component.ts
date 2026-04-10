import { Component, EventEmitter, Input, Output } from '@angular/core';

type DashboardRole = 'Admin' | 'User';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  exact?: boolean;
}

interface NavSection {
  label: string;
  items: NavItem[];
}

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent {
  @Input() isExpanded = true;
  @Input() role: DashboardRole = 'Admin';
  @Input() displayName = 'Nexus User';
  @Output() toggleEvent = new EventEmitter<void>();
  @Output() logoutEvent = new EventEmitter<void>();

  private readonly adminSections: NavSection[] = [
    {
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
      label: 'Masters',
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
      label: 'Billing',
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
      label: 'Administration',
      items: [
        { label: 'Roles & Access', icon: 'admin_panel_settings', route: '/admin/roles' },
        { label: 'Hospital Settings', icon: 'settings', route: '/admin/settings' }
      ]
    }
  ];

  private readonly userSections: NavSection[] = [
    {
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

  get brandText(): string {
    return this.role === 'Admin' ? 'Nexus Admin' : 'Nexus User';
  }

  get roleSubtitle(): string {
    return this.role === 'Admin' ? 'Control Center' : 'Member Space';
  }

  get initials(): string {
    return this.displayName
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('') || 'NX';
  }

  toggle(): void {
    this.toggleEvent.emit();
  }

  logout(): void {
    this.logoutEvent.emit();
  }
}
