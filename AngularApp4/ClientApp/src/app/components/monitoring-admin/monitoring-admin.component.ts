import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Observable, Subscription, catchError, filter, forkJoin, of } from 'rxjs';
import {
  AuditLogEntry,
  AuditMonitoring,
  AuditPanel,
  MonitoringReports,
  MonitoringSection
} from '../../core/models/hms/phase7-monitoring.model';
import { Phase7MonitoringService } from '../../core/services/hms/phase7-monitoring.service';

type MonitoringReportKey =
  | 'patient'
  | 'billing'
  | 'doctorRevenue'
  | 'pharmacySales'
  | 'purchase'
  | 'stockBalance'
  | 'expiry'
  | 'bedOccupancy';

interface SectionOption {
  key: MonitoringSection;
  label: string;
  icon: string;
  description: string;
}

interface ReportOption {
  key: MonitoringReportKey;
  label: string;
  icon: string;
  description: string;
}

interface AuditPanelOption {
  key: AuditPanel;
  label: string;
  description: string;
}

interface MetricCard {
  label: string;
  value: string;
  helper: string;
  tone: 'blue' | 'teal' | 'amber' | 'rose' | 'violet' | 'slate';
}

const SECTION_OPTIONS: ReadonlyArray<SectionOption> = [
  { key: 'reports', label: 'Reports Dashboard', icon: 'insights', description: 'Choose one report at a time from the monitoring workspace.' },
  { key: 'audit', label: 'Audit Log', icon: 'history', description: 'Who changed what, billing edits, login history, and stock adjustments.' }
];

const REPORT_OPTIONS: ReadonlyArray<ReportOption> = [
  { key: 'patient', label: 'Patient Report', icon: 'groups', description: 'Patient totals, category mix, and branch activity.' },
  { key: 'billing', label: 'Billing Report', icon: 'receipt_long', description: 'Invoice totals, collections, dues, and recent billing activity.' },
  { key: 'doctorRevenue', label: 'Doctor Revenue Report', icon: 'payments', description: 'Doctor-level revenue, appointments, and consultation estimates.' },
  { key: 'pharmacySales', label: 'Pharmacy Sales Report', icon: 'medication', description: 'Pharmacy movement summary based on receipts, transfers, and stock value.' },
  { key: 'purchase', label: 'Purchase Report', icon: 'shopping_cart', description: 'Supplier purchases, returns, due amounts, and recent invoices.' },
  { key: 'stockBalance', label: 'Stock Balance Report', icon: 'inventory_2', description: 'On-hand quantity, stock value, and low-stock positions by location.' },
  { key: 'expiry', label: 'Expiry Report', icon: 'event_busy', description: 'Near-expiry and expired batch monitoring.' },
  { key: 'bedOccupancy', label: 'Bed Occupancy Report', icon: 'local_hotel', description: 'Bed utilization across branches and wards.' }
];

const AUDIT_PANELS: ReadonlyArray<AuditPanelOption> = [
  { key: 'changeTrail', label: 'Change Trail', description: 'All recent administrative changes across the platform.' },
  { key: 'loginHistory', label: 'Login History', description: 'Successful and failed sign-in activity.' },
  { key: 'stockAdjustmentHistory', label: 'Stock Adjustments', description: 'Adjustment creation and approval history.' },
  { key: 'billingChanges', label: 'Billing Changes', description: 'Invoices, discounts, and payment activity.' },
  { key: 'refundApprovals', label: 'Refund Approvals', description: 'Refund requests, approvals, and processing events.' }
];

@Component({
  selector: 'app-monitoring-admin',
  templateUrl: './monitoring-admin.component.html',
  styleUrls: ['./monitoring-admin.component.scss']
})
export class MonitoringAdminComponent implements OnInit, OnDestroy {
  readonly sections = SECTION_OPTIONS;
  readonly reportOptions = REPORT_OPTIONS;
  readonly auditPanels = AUDIT_PANELS;

  activeSection: MonitoringSection = 'reports';
  activeReportKey: MonitoringReportKey | null = null;
  activeAuditPanel: AuditPanel = 'changeTrail';
  reports: MonitoringReports | null = null;
  audit: AuditMonitoring | null = null;
  auditSearchTerm = '';
  isLoading = true;
  errorMessage = '';

  private routeSub?: Subscription;

  constructor(
    private readonly router: Router,
    private readonly monitoring: Phase7MonitoringService
  ) {}

  ngOnInit(): void {
    this.syncRoute(this.router.url);
    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.syncRoute(event.urlAfterRedirects));
    this.loadData();
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  get currentSection(): SectionOption {
    return this.sections.find((section) => section.key === this.activeSection) ?? this.sections[0];
  }

  get activeReport(): ReportOption | null {
    return this.reportOptions.find((report) => report.key === this.activeReportKey) ?? null;
  }

  get currentHeroTitle(): string {
    return this.activeSection === 'reports' && this.activeReport
      ? this.activeReport.label
      : this.currentSection.label;
  }

  get currentHeroDescription(): string {
    return this.activeSection === 'reports' && this.activeReport
      ? this.activeReport.description
      : this.currentSection.description;
  }

  get currentAuditPanel(): AuditPanelOption {
    return this.auditPanels.find((panel) => panel.key === this.activeAuditPanel) ?? this.auditPanels[0];
  }

  get auditOverviewMetrics(): MetricCard[] {
    if (!this.audit) {
      return [];
    }

    return [
      { label: 'Audit Entries', value: this.audit.overview.totalEntries.toLocaleString(), helper: `${this.audit.overview.last24Hours} in the last 24 hours`, tone: 'blue' },
      { label: 'Actors', value: this.audit.overview.uniqueActors.toLocaleString(), helper: 'Unique users captured in the trail', tone: 'teal' },
      { label: 'Login Events', value: this.audit.overview.loginEvents.toLocaleString(), helper: 'Authentication activity', tone: 'slate' },
      { label: 'Billing Events', value: this.audit.overview.billingEvents.toLocaleString(), helper: 'Invoices, discounts, and payments', tone: 'violet' },
      { label: 'Stock Adjustments', value: this.audit.overview.stockAdjustmentEvents.toLocaleString(), helper: 'Inventory correction events', tone: 'amber' },
      { label: 'Refund Events', value: this.audit.overview.refundEvents.toLocaleString(), helper: 'Refund approvals and processing', tone: 'rose' }
    ];
  }

  get currentAuditRows(): AuditLogEntry[] {
    if (!this.audit) {
      return [];
    }

    switch (this.activeAuditPanel) {
      case 'loginHistory':
        return this.audit.loginHistory;
      case 'stockAdjustmentHistory':
        return this.audit.stockAdjustmentHistory;
      case 'billingChanges':
        return this.audit.billingChanges;
      case 'refundApprovals':
        return this.audit.refundApprovals;
      default:
        return this.audit.changeTrail;
    }
  }

  get filteredAuditRows(): AuditLogEntry[] {
    const query = this.auditSearchTerm.trim().toLowerCase();
    if (!query) {
      return this.currentAuditRows;
    }

    return this.currentAuditRows.filter((entry) => this.getAuditSearchValue(entry).includes(query));
  }

  setSection(section: MonitoringSection): void {
    void this.router.navigate(section === 'reports'
      ? ['/admin/monitoring', 'reports']
      : ['/admin/monitoring', 'audit']);
  }

  openReport(reportKey: MonitoringReportKey): void {
    void this.router.navigate(['/admin/monitoring', 'reports', reportKey]);
  }

  clearReportSelection(): void {
    void this.router.navigate(['/admin/monitoring', 'reports']);
  }

  setAuditPanel(panel: AuditPanel): void {
    this.activeAuditPanel = panel;
  }

  formatCurrency(value: number): string {
    return `NPR ${value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }

  formatQuantity(value: number): string {
    return value.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 });
  }

  formatDate(value: string): string {
    return new Date(value).toLocaleDateString();
  }

  formatDateTime(value: string): string {
    return new Date(value).toLocaleString();
  }

  private syncRoute(url: string): void {
    const parts = url.split('?')[0].split('/').filter(Boolean);
    const section = parts[2];
    const reportKey = parts[3];

    this.activeSection = section === 'audit' ? 'audit' : 'reports';

    if (this.activeSection === 'reports') {
      if (reportKey && !this.isReportKey(reportKey)) {
        void this.router.navigate(['/admin/monitoring', 'reports'], { replaceUrl: true });
        return;
      }

      this.activeReportKey = this.isReportKey(reportKey) ? reportKey : null;
    } else {
      this.activeReportKey = null;
    }

    if (!this.isLoading) {
      this.errorMessage = this.getCriticalLoadKeys().some((key) => (key === 'reports' ? !this.reports : !this.audit))
        ? `Unable to load ${this.activeSection === 'reports' ? 'report data' : 'audit log'} right now.`
        : '';
    }
  }

  private loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const loadFailures = new Set<string>();
    const withFallback = <T>(key: string, request: Observable<T>, fallback: T | null): Observable<T | null> =>
      request.pipe(
        catchError((error) => {
          loadFailures.add(key);
          console.error(`Monitoring load failed for ${key}.`, error);
          return of(fallback);
        })
      );

    forkJoin({
      reports: withFallback('reports', this.monitoring.getReports(), this.reports),
      audit: withFallback('audit', this.monitoring.getAudit(), this.audit)
    }).subscribe({
      next: ({ reports, audit }) => {
        this.reports = reports;
        this.audit = audit;
        this.isLoading = false;
        this.errorMessage = this.getCriticalLoadKeys().some((key) => loadFailures.has(key) || (key === 'reports' ? !this.reports : !this.audit))
          ? `Some ${this.activeSection === 'reports' ? 'report data' : 'audit log data'} could not be loaded. Refresh and try again.`
          : '';
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load monitoring data right now.';
      }
    });
  }

  private getCriticalLoadKeys(): string[] {
    return this.activeSection === 'reports' ? ['reports'] : ['audit'];
  }

  private isReportKey(value: string | undefined): value is MonitoringReportKey {
    return !!value && this.reportOptions.some((report) => report.key === value);
  }

  private getAuditSearchValue(entry: AuditLogEntry): string {
    return [
      entry.category,
      entry.action,
      entry.entityName,
      entry.targetDisplayName,
      entry.summary,
      entry.performedByName,
      entry.performedByRole,
      entry.actorEmail,
      entry.metadataJson
    ]
      .filter((value): value is string => !!value)
      .join(' ')
      .toLowerCase();
  }
}
