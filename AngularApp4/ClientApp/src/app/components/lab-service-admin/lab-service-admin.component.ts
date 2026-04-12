import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Observable, Subscription, catchError, filter, forkJoin, of } from 'rxjs';
import { DepartmentMaster } from '../../core/models/hms/master-setup.model';
import {
  LabServiceSection,
  LabTestMaster,
  SaveLabTestMasterPayload,
  SaveServicePackagePayload,
  ServicePackage,
  ServicePackageKind
} from '../../core/models/hms/phase6-lab-service.model';
import { MasterSetupService } from '../../core/services/hms/master-setup.service';
import { Phase6LabServiceService } from '../../core/services/hms/phase6-lab-service.service';

type ViewMode = 'index' | 'details' | 'create' | 'edit';
type StatusFilter = 'all' | 'active' | 'inactive';

interface SectionOption {
  key: LabServiceSection;
  label: string;
  icon: string;
  description: string;
}

interface FilterOption {
  key: StatusFilter;
  label: string;
}

interface LabTestFormState {
  testName: string;
  departmentName: string;
  price: number;
  sampleType: string;
  reportFormat: string;
  isActive: boolean;
}

interface PackageFormState {
  packageName: string;
  departmentName: string;
  price: number;
  discountAmount: number;
  description: string;
  isActive: boolean;
}

interface LabServiceDetailField {
  label: string;
  value: string;
  fullWidth?: boolean;
}

interface LabServiceDetailSummary {
  tone: 'amber' | 'blue' | 'teal' | 'violet' | 'rose';
  kicker: string;
  title: string;
  description: string;
  badges: string[];
  statusLabel: string;
  statusTone: 'success' | 'inactive';
  fields: LabServiceDetailField[];
}

const SECTION_OPTIONS: ReadonlyArray<SectionOption> = [
  { key: 'labTests', label: 'Lab Test Master', icon: 'biotech', description: 'Test catalog with department, sample type, pricing, and reporting format.' },
  { key: 'healthPackages', label: 'Health Packages', icon: 'health_and_safety', description: 'Preventive screening and wellness packages for individual patients.' },
  { key: 'surgeryPackages', label: 'Surgery Packages', icon: 'surgical', description: 'Procedure bundles covering OT, consumables, diagnostics, and recovery charges.' },
  { key: 'corporatePackages', label: 'Corporate Packages', icon: 'business_center', description: 'Employer and institutional packages for workforce checkups and partner care plans.' },
  { key: 'discountedBundles', label: 'Discounted Bundles', icon: 'sell', description: 'Bundled service offers with packaged discounts across common clinical journeys.' }
];

const FILTER_OPTIONS: ReadonlyArray<FilterOption> = [
  { key: 'all', label: 'All' },
  { key: 'active', label: 'Active' },
  { key: 'inactive', label: 'Inactive' }
];

function createEmptyLabTestForm(): LabTestFormState {
  return {
    testName: '',
    departmentName: '',
    price: 0,
    sampleType: '',
    reportFormat: '',
    isActive: true
  };
}

function createEmptyPackageForm(): PackageFormState {
  return {
    packageName: '',
    departmentName: '',
    price: 0,
    discountAmount: 0,
    description: '',
    isActive: true
  };
}

@Component({
  selector: 'app-lab-service-admin',
  templateUrl: './lab-service-admin.component.html',
  styleUrls: ['./lab-service-admin.component.scss']
})
export class LabServiceAdminComponent implements OnInit, OnDestroy {
  readonly sections = SECTION_OPTIONS;
  readonly filterOptions = FILTER_OPTIONS;

  activeSection: LabServiceSection = 'labTests';
  viewMode: ViewMode = 'index';
  selectedRecordId: number | null = null;
  statusFilter: StatusFilter = 'all';
  searchTerm = '';

  departments: DepartmentMaster[] = [];
  labTests: LabTestMaster[] = [];
  packages: ServicePackage[] = [];

  labTestForm: LabTestFormState = createEmptyLabTestForm();
  packageForm: PackageFormState = createEmptyPackageForm();

  editingLabTestId: number | null = null;
  editingPackageId: number | null = null;

  isLoading = true;
  isSaving = false;
  errorMessage = '';
  successMessage = '';

  private routeSub?: Subscription;

  constructor(
    private readonly router: Router,
    private readonly masterSetup: MasterSetupService,
    private readonly phase6: Phase6LabServiceService
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

  get currentRouteBase(): string[] {
    return ['/admin/laboratory', this.activeSection];
  }

  get isIndexView(): boolean {
    return this.viewMode === 'index';
  }

  get isDetailsView(): boolean {
    return this.viewMode === 'details';
  }

  get isCreateView(): boolean {
    return this.viewMode === 'create';
  }

  get isEditView(): boolean {
    return this.viewMode === 'edit';
  }

  get isFormView(): boolean {
    return this.isCreateView || this.isEditView;
  }

  get isLabTestSection(): boolean {
    return this.activeSection === 'labTests';
  }

  get currentPackageKind(): ServicePackageKind | null {
    switch (this.activeSection) {
      case 'healthPackages':
        return 'HealthPackage';
      case 'surgeryPackages':
        return 'SurgeryPackage';
      case 'corporatePackages':
        return 'CorporatePackage';
      case 'discountedBundles':
        return 'DiscountedBundle';
      default:
        return null;
    }
  }

  get currentRecordLabel(): string {
    switch (this.activeSection) {
      case 'labTests':
        return 'Lab Test';
      case 'healthPackages':
        return 'Health Package';
      case 'surgeryPackages':
        return 'Surgery Package';
      case 'corporatePackages':
        return 'Corporate Package';
      case 'discountedBundles':
        return 'Discounted Bundle';
    }
  }

  get activeDepartmentNames(): string[] {
    return [...new Set(
      this.departments
        .filter((department) => department.isActive)
        .map((department) => department.name.trim())
        .filter(Boolean)
    )].sort((a, b) => a.localeCompare(b));
  }

  get currentPackages(): ServicePackage[] {
    return this.currentPackageKind
      ? this.packages.filter((item) => item.kind === this.currentPackageKind)
      : [];
  }

  get filteredLabTests(): LabTestMaster[] {
    return this.labTests.filter((item) =>
      this.matchesActiveFilter(item.isActive) &&
      this.matchesSearch(item.testName, item.departmentName, item.sampleType, item.reportFormat));
  }

  get filteredPackages(): ServicePackage[] {
    return this.currentPackages.filter((item) =>
      this.matchesActiveFilter(item.isActive) &&
      this.matchesSearch(item.packageName, item.departmentName, item.description, item.kind));
  }

  get sectionRecordCount(): number {
    return this.isLabTestSection ? this.filteredLabTests.length : this.filteredPackages.length;
  }

  get selectedLabTest(): LabTestMaster | null {
    return this.isLabTestSection && this.selectedRecordId
      ? this.labTests.find((item) => item.labTestMasterId === this.selectedRecordId) ?? null
      : null;
  }

  get selectedPackage(): ServicePackage | null {
    return !this.isLabTestSection && this.selectedRecordId
      ? this.currentPackages.find((item) => item.servicePackageId === this.selectedRecordId) ?? null
      : null;
  }

  get hasDetailsRecord(): boolean {
    return !!(this.selectedLabTest || this.selectedPackage);
  }

  get packageNetAmountPreview(): number {
    return Math.max(this.packageForm.price - this.packageForm.discountAmount, 0);
  }

  get detailSummary(): LabServiceDetailSummary | null {
    if (this.selectedLabTest) {
      return {
        tone: 'blue',
        kicker: 'Laboratory master',
        title: this.selectedLabTest.testName,
        description: `${this.selectedLabTest.sampleType} sample workflow with ${this.selectedLabTest.reportFormat.toLowerCase()} output.`,
        badges: [
          this.selectedLabTest.departmentName,
          this.selectedLabTest.sampleType,
          this.formatCurrency(this.selectedLabTest.price)
        ],
        statusLabel: this.selectedLabTest.isActive ? 'Active' : 'Inactive',
        statusTone: this.selectedLabTest.isActive ? 'success' : 'inactive',
        fields: [
          { label: 'Test name', value: this.selectedLabTest.testName },
          { label: 'Department', value: this.selectedLabTest.departmentName },
          { label: 'Price', value: this.formatCurrency(this.selectedLabTest.price) },
          { label: 'Sample type', value: this.selectedLabTest.sampleType },
          { label: 'Report format', value: this.selectedLabTest.reportFormat },
          { label: 'Status', value: this.selectedLabTest.isActive ? 'Active' : 'Inactive' }
        ]
      };
    }

    if (this.selectedPackage) {
      return {
        tone: this.packageTone(this.selectedPackage.kind),
        kicker: this.formatEnum(this.selectedPackage.kind),
        title: this.selectedPackage.packageName,
        description: this.selectedPackage.description || 'Structured service package for repeatable pricing, bundled offers, and admin control.',
        badges: [
          this.selectedPackage.departmentName || 'All departments',
          this.formatCurrency(this.selectedPackage.price),
          this.selectedPackage.discountAmount > 0 ? `Discount ${this.formatCurrency(this.selectedPackage.discountAmount)}` : 'No discount'
        ],
        statusLabel: this.selectedPackage.isActive ? 'Active' : 'Inactive',
        statusTone: this.selectedPackage.isActive ? 'success' : 'inactive',
        fields: [
          { label: 'Package type', value: this.formatEnum(this.selectedPackage.kind) },
          { label: 'Department', value: this.selectedPackage.departmentName || 'All departments' },
          { label: 'List price', value: this.formatCurrency(this.selectedPackage.price) },
          { label: 'Discount', value: this.formatCurrency(this.selectedPackage.discountAmount) },
          { label: 'Net amount', value: this.formatCurrency(this.selectedPackage.netAmount) },
          { label: 'Status', value: this.selectedPackage.isActive ? 'Active' : 'Inactive' },
          { label: 'Description', value: this.selectedPackage.description || 'No package notes provided.', fullWidth: true }
        ]
      };
    }

    return null;
  }

  setSection(section: LabServiceSection): void {
    this.router.navigate(['/admin/laboratory', section]);
  }

  setStatusFilter(filterKey: StatusFilter): void {
    this.statusFilter = filterKey;
  }

  goToCreate(): void {
    this.router.navigate([...this.currentRouteBase, 'create']);
  }

  backToIndex(): void {
    this.router.navigate(this.currentRouteBase);
  }

  goToDetails(recordId: number): void {
    this.router.navigate([...this.currentRouteBase, recordId]);
  }

  goToEdit(recordId: number): void {
    this.router.navigate([...this.currentRouteBase, recordId, 'edit']);
  }

  refresh(): void {
    this.loadData();
  }

  resetCurrentForm(): void {
    if (this.isEditView && this.selectedRecordId) {
      this.loadSelectedRecordIntoForm();
      return;
    }

    this.resetFormForSection(this.activeSection);
  }

  editLabTest(item: LabTestMaster): void {
    this.goToEdit(item.labTestMasterId);
  }

  editPackage(item: ServicePackage): void {
    this.goToEdit(item.servicePackageId);
  }

  saveLabTest(): void {
    if (!this.labTestForm.testName.trim() ||
        !this.labTestForm.departmentName.trim() ||
        !this.labTestForm.sampleType.trim() ||
        !this.labTestForm.reportFormat.trim()) {
      this.errorMessage = 'Test name, department, sample type, and report format are required.';
      return;
    }

    const payload = this.buildLabTestPayload(this.labTestForm);
    const request = this.editingLabTestId
      ? this.phase6.updateLabTest(this.editingLabTestId, payload)
      : this.phase6.createLabTest(payload);

    this.runSave(
      request,
      (item) => {
        this.labTests = this.sortLabTests(this.replaceOrAppend(this.labTests, item, 'labTestMasterId'));
        this.goToDetails(item.labTestMasterId);
      },
      this.editingLabTestId ? 'Lab test updated successfully.' : 'Lab test created successfully.',
      'Unable to save lab test.'
    );
  }

  savePackage(): void {
    const kind = this.currentPackageKind;
    if (!kind) {
      this.errorMessage = 'Package category could not be resolved.';
      return;
    }

    if (!this.packageForm.packageName.trim()) {
      this.errorMessage = 'Package name is required.';
      return;
    }

    const payload = this.buildPackagePayload(this.packageForm, kind);
    const request = this.editingPackageId
      ? this.phase6.updatePackage(this.editingPackageId, payload)
      : this.phase6.createPackage(payload);

    this.runSave(
      request,
      (item) => {
        this.packages = this.sortPackages(this.replaceOrAppend(this.packages, item, 'servicePackageId'));
        this.goToDetails(item.servicePackageId);
      },
      this.editingPackageId ? `${this.currentRecordLabel} updated successfully.` : `${this.currentRecordLabel} created successfully.`,
      `Unable to save ${this.currentRecordLabel.toLowerCase()}.`
    );
  }

  updateLabTestStatus(item: LabTestMaster): void {
    this.runSave(
      this.phase6.updateLabTest(item.labTestMasterId, this.buildLabTestPayload({
        testName: item.testName,
        departmentName: item.departmentName,
        price: item.price,
        sampleType: item.sampleType,
        reportFormat: item.reportFormat,
        isActive: !item.isActive
      })),
      (updated) => {
        this.labTests = this.sortLabTests(this.replaceOrAppend(this.labTests, updated, 'labTestMasterId'));
        if (this.selectedRecordId === updated.labTestMasterId && this.isEditView) {
          this.populateLabTestForm(updated);
        }
      },
      `Lab test ${item.isActive ? 'deactivated' : 'activated'} successfully.`,
      'Unable to update lab test status.'
    );
  }

  updatePackageStatus(item: ServicePackage): void {
    this.runSave(
      this.phase6.updatePackage(item.servicePackageId, this.buildPackagePayload({
        packageName: item.packageName,
        departmentName: item.departmentName ?? '',
        price: item.price,
        discountAmount: item.discountAmount,
        description: item.description ?? '',
        isActive: !item.isActive
      }, item.kind)),
      (updated) => {
        this.packages = this.sortPackages(this.replaceOrAppend(this.packages, updated, 'servicePackageId'));
        if (this.selectedRecordId === updated.servicePackageId && this.isEditView) {
          this.populatePackageForm(updated);
        }
      },
      `${this.currentRecordLabel} ${item.isActive ? 'deactivated' : 'activated'} successfully.`,
      `Unable to update ${this.currentRecordLabel.toLowerCase()} status.`
    );
  }

  formatCurrency(value: number): string {
    return `NPR ${new Intl.NumberFormat('en-US', { maximumFractionDigits: 2 }).format(value)}`;
  }

  formatEnum(value?: string | null): string {
    return value ? value.replace(/([a-z])([A-Z])/g, '$1 $2') : 'Not set';
  }

  private syncRoute(url: string): void {
    const parts = url.split('?')[0].split('/').filter(Boolean);
    const nextSection = this.toSection(parts[2] ?? null);
    const nextId = this.parseId(parts[3] ?? null);
    const previousSection = this.activeSection;

    this.activeSection = nextSection;
    this.selectedRecordId = nextId;

    if (parts[3] === 'create') {
      this.viewMode = 'create';
      this.selectedRecordId = null;
      this.resetFormForSection(this.activeSection);
    } else if (nextId && parts[4] === 'edit') {
      this.viewMode = 'edit';
      this.loadSelectedRecordIntoForm();
    } else if (nextId) {
      this.viewMode = 'details';
    } else {
      this.viewMode = 'index';
    }

    if (previousSection !== nextSection) {
      this.searchTerm = '';
      this.statusFilter = 'all';
      this.errorMessage = '';
      this.successMessage = '';
    }
  }

  private loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const loadFailures = new Set<string>();
    const withFallback = <T>(key: string, request: Observable<T>, fallback: T): Observable<T> =>
      request.pipe(
        catchError((error) => {
          loadFailures.add(key);
          console.error(`Lab and service admin load failed for ${key}.`, error);
          return of(fallback);
        })
      );

    forkJoin({
      departments: withFallback('departments', this.masterSetup.getDepartments(), this.departments),
      labTests: withFallback('labTests', this.phase6.getLabTests(), this.labTests),
      packages: withFallback('packages', this.phase6.getPackages(), this.packages)
    }).subscribe({
      next: ({ departments, labTests, packages }) => {
        this.departments = departments;
        this.labTests = this.sortLabTests(labTests);
        this.packages = this.sortPackages(packages);
        this.errorMessage = this.getCriticalLoadKeys(this.activeSection).some((key) => loadFailures.has(key))
          ? 'Some required lab and service admin data could not be loaded for this page. Refresh and try again.'
          : '';
        this.loadSelectedRecordIntoForm();
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load lab and service admin data right now.';
      }
    });
  }

  private resetFormForSection(section: LabServiceSection): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (section === 'labTests') {
      this.editingLabTestId = null;
      this.labTestForm = createEmptyLabTestForm();
      return;
    }

    this.editingPackageId = null;
    this.packageForm = createEmptyPackageForm();
  }

  private loadSelectedRecordIntoForm(): void {
    if (!this.isEditView) {
      return;
    }

    if (this.isLabTestSection) {
      if (this.selectedLabTest) {
        this.editingLabTestId = this.selectedLabTest.labTestMasterId;
        this.populateLabTestForm(this.selectedLabTest);
      }
      return;
    }

    if (this.selectedPackage) {
      this.editingPackageId = this.selectedPackage.servicePackageId;
      this.populatePackageForm(this.selectedPackage);
    }
  }

  private populateLabTestForm(item?: LabTestMaster): void {
    this.labTestForm = item
      ? {
          testName: item.testName,
          departmentName: item.departmentName,
          price: item.price,
          sampleType: item.sampleType,
          reportFormat: item.reportFormat,
          isActive: item.isActive
        }
      : createEmptyLabTestForm();
  }

  private populatePackageForm(item?: ServicePackage): void {
    this.packageForm = item
      ? {
          packageName: item.packageName,
          departmentName: item.departmentName ?? '',
          price: item.price,
          discountAmount: item.discountAmount,
          description: item.description ?? '',
          isActive: item.isActive
        }
      : createEmptyPackageForm();
  }

  private buildLabTestPayload(form: LabTestFormState): SaveLabTestMasterPayload {
    return {
      testName: form.testName.trim(),
      departmentName: form.departmentName.trim(),
      price: Math.max(form.price, 0),
      sampleType: form.sampleType.trim(),
      reportFormat: form.reportFormat.trim(),
      isActive: form.isActive
    };
  }

  private buildPackagePayload(form: PackageFormState, kind: ServicePackageKind): SaveServicePackagePayload {
    return {
      kind,
      packageName: form.packageName.trim(),
      departmentName: this.normalizeOptional(form.departmentName),
      price: Math.max(form.price, 0),
      discountAmount: Math.max(Math.min(form.discountAmount, Math.max(form.price, 0)), 0),
      description: this.normalizeOptional(form.description),
      isActive: form.isActive
    };
  }

  private runSave<T>(
    request: Observable<T>,
    onSuccess: (value: T) => void,
    successMessage: string,
    fallbackError: string
  ): void {
    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    request.subscribe({
      next: (value) => {
        onSuccess(value);
        this.isSaving = false;
        this.successMessage = successMessage;
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSaving = false;
        this.errorMessage = error?.error?.message || fallbackError;
      }
    });
  }

  private matchesActiveFilter(isActive: boolean): boolean {
    if (this.statusFilter === 'active') {
      return isActive;
    }

    if (this.statusFilter === 'inactive') {
      return !isActive;
    }

    return true;
  }

  private matchesSearch(...values: Array<string | number | null | undefined>): boolean {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) {
      return true;
    }

    return values.some((value) => String(value ?? '').toLowerCase().includes(term));
  }

  private normalizeOptional(value: string): string | null {
    const normalized = value.trim();
    return normalized ? normalized : null;
  }

  private parseId(value: string | null): number | null {
    if (!value) {
      return null;
    }

    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  }

  private toSection(value: string | null): LabServiceSection {
    return this.sections.some((section) => section.key === value) ? (value as LabServiceSection) : 'labTests';
  }

  private getCriticalLoadKeys(section: LabServiceSection): string[] {
    return section === 'labTests'
      ? ['labTests', 'departments']
      : ['packages'];
  }

  private replaceOrAppend<T, K extends keyof T>(items: T[], item: T, key: K): T[] {
    const index = items.findIndex((entry) => entry[key] === item[key]);
    if (index === -1) {
      return [...items, item];
    }

    return items.map((entry) => entry[key] === item[key] ? item : entry);
  }

  private sortLabTests(items: LabTestMaster[]): LabTestMaster[] {
    return [...items].sort((a, b) => `${a.departmentName} ${a.testName}`.localeCompare(`${b.departmentName} ${b.testName}`));
  }

  private sortPackages(items: ServicePackage[]): ServicePackage[] {
    return [...items].sort((a, b) => `${a.kind} ${a.packageName}`.localeCompare(`${b.kind} ${b.packageName}`));
  }

  private packageTone(kind: ServicePackageKind): 'amber' | 'blue' | 'teal' | 'violet' | 'rose' {
    switch (kind) {
      case 'HealthPackage':
        return 'teal';
      case 'SurgeryPackage':
        return 'rose';
      case 'CorporatePackage':
        return 'violet';
      case 'DiscountedBundle':
        return 'amber';
      default:
        return 'blue';
    }
  }
}
