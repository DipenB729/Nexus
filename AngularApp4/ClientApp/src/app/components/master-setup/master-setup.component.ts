import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Observable, Subscription, filter, forkJoin } from 'rxjs';
import { BranchSettings } from '../../core/models/hms/admin-ops.model';
import { BedMaster, DepartmentMaster, DoctorMaster, PatientCategoryMaster, StaffMaster, WardMaster } from '../../core/models/hms/master-setup.model';
import { DoctorScheduleRecord } from '../../core/models/hms/phase3-control.model';
import { AdminOpsService } from '../../core/services/hms/admin-ops.service';
import { MasterSetupService } from '../../core/services/hms/master-setup.service';
import { Phase3ControlService } from '../../core/services/hms/phase3-control.service';

type MasterSection = 'departments' | 'doctors' | 'staff' | 'patientCategories' | 'wards' | 'beds';
type StatusFilter = 'all' | 'active' | 'inactive';
type ViewMode = 'index' | 'details' | 'create' | 'edit';

interface MasterSectionOption {
  key: MasterSection;
  label: string;
  icon: string;
  description: string;
}

interface DepartmentFormState {
  departmentId: number;
  branchId: number | null;
  name: string;
  code: string;
  description: string;
  isActive: boolean;
}

interface DoctorFormState {
  doctorId: number;
  branchId: number | null;
  departmentId: number | null;
  fullName: string;
  specialization: string;
  email: string;
  phone: string;
  experienceYears: number;
  qualification: string;
  opdDays: string;
  opdStartTime: string;
  opdEndTime: string;
  consultationFee: number;
  isActive: boolean;
}

interface ScheduleFormState {
  scheduleId: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
  maxPatientsPerSlot: number;
  isActive: boolean;
}

interface StaffFormState {
  staffId: number;
  branchId: number | null;
  departmentId: number | null;
  fullName: string;
  employeeCode: string;
  designation: string;
  shift: string;
  email: string;
  phone: string;
  joinDate: string;
  isActive: boolean;
}

interface PatientCategoryFormState {
  patientCategoryId: number;
  name: string;
  description: string;
  priorityOrder: number;
  isActive: boolean;
}

interface WardFormState {
  wardId: number;
  branchId: number | null;
  departmentId: number | null;
  name: string;
  wardType: string;
  roomType: string;
  chargePerDay: number;
  isActive: boolean;
}

interface BedFormState {
  bedId: number;
  wardId: number | null;
  branchId: number | null;
  departmentId: number | null;
  bedNumber: string;
  chargePerDay: number;
  isOccupied: boolean;
  isActive: boolean;
}

const SECTION_OPTIONS: ReadonlyArray<MasterSectionOption> = [
  { key: 'departments', label: 'Departments', icon: 'domain', description: 'OPD, IPD, Pharmacy, Lab, OT, ICU and branch-level departments.' },
  { key: 'doctors', label: 'Doctors', icon: 'local_hospital', description: 'Specialization, OPD schedule, consultation fee and department mapping.' },
  { key: 'staff', label: 'Staff', icon: 'badge', description: 'Designation, shift, contact details and department assignment.' },
  { key: 'patientCategories', label: 'Patient Categories', icon: 'sell', description: 'General, Emergency, Corporate, Insurance and VIP categories.' },
  { key: 'wards', label: 'Wards', icon: 'meeting_room', description: 'Ward type, room type, charges and branch ownership.' },
  { key: 'beds', label: 'Beds', icon: 'hotel', description: 'Bed numbers, occupancy, charges and ward mapping.' }
];

const EMPTY_DEPARTMENT_FORM: DepartmentFormState = {
  departmentId: 0,
  branchId: null,
  name: '',
  code: '',
  description: '',
  isActive: true
};

const EMPTY_DOCTOR_FORM: DoctorFormState = {
  doctorId: 0,
  branchId: null,
  departmentId: null,
  fullName: '',
  specialization: '',
  email: '',
  phone: '',
  experienceYears: 0,
  qualification: '',
  opdDays: '',
  opdStartTime: '',
  opdEndTime: '',
  consultationFee: 0,
  isActive: true
};

const EMPTY_SCHEDULE_FORM: ScheduleFormState = {
  scheduleId: 0,
  dayOfWeek: 1,
  startTime: '09:00',
  endTime: '12:00',
  slotDurationMinutes: 30,
  maxPatientsPerSlot: 1,
  isActive: true
};

const EMPTY_STAFF_FORM: StaffFormState = {
  staffId: 0,
  branchId: null,
  departmentId: null,
  fullName: '',
  employeeCode: '',
  designation: '',
  shift: '',
  email: '',
  phone: '',
  joinDate: new Date().toISOString().slice(0, 10),
  isActive: true
};

const EMPTY_PATIENT_CATEGORY_FORM: PatientCategoryFormState = {
  patientCategoryId: 0,
  name: '',
  description: '',
  priorityOrder: 1,
  isActive: true
};

const EMPTY_WARD_FORM: WardFormState = {
  wardId: 0,
  branchId: null,
  departmentId: null,
  name: '',
  wardType: '',
  roomType: '',
  chargePerDay: 0,
  isActive: true
};

const EMPTY_BED_FORM: BedFormState = {
  bedId: 0,
  wardId: null,
  branchId: null,
  departmentId: null,
  bedNumber: '',
  chargePerDay: 0,
  isOccupied: false,
  isActive: true
};

@Component({
  selector: 'app-master-setup',
  templateUrl: './master-setup.component.html',
  styleUrls: ['./master-setup.component.scss']
})
export class MasterSetupComponent implements OnInit, OnDestroy {
  readonly sections = SECTION_OPTIONS;

  activeSection: MasterSection = 'departments';
  statusFilter: StatusFilter = 'all';
  viewMode: ViewMode = 'index';
  selectedRecordId: number | null = null;
  searchTerm = '';

  isLoading = true;
  isSaving = false;
  errorMessage = '';
  successMessage = '';

  branches: BranchSettings[] = [];
  departments: DepartmentMaster[] = [];
  doctors: DoctorMaster[] = [];
  doctorSchedules: DoctorScheduleRecord[] = [];
  staffMembers: StaffMaster[] = [];
  patientCategories: PatientCategoryMaster[] = [];
  wards: WardMaster[] = [];
  beds: BedMaster[] = [];

  departmentForm: DepartmentFormState = { ...EMPTY_DEPARTMENT_FORM };
  doctorForm: DoctorFormState = { ...EMPTY_DOCTOR_FORM };
  scheduleForm: ScheduleFormState = { ...EMPTY_SCHEDULE_FORM };
  staffForm: StaffFormState = { ...EMPTY_STAFF_FORM };
  patientCategoryForm: PatientCategoryFormState = { ...EMPTY_PATIENT_CATEGORY_FORM };
  wardForm: WardFormState = { ...EMPTY_WARD_FORM };
  bedForm: BedFormState = { ...EMPTY_BED_FORM };
  private routeSub?: Subscription;
  isSavingSchedule = false;
  readonly dayOptions = [
    { value: 1, label: 'Sunday' },
    { value: 2, label: 'Monday' },
    { value: 3, label: 'Tuesday' },
    { value: 4, label: 'Wednesday' },
    { value: 5, label: 'Thursday' },
    { value: 6, label: 'Friday' },
    { value: 7, label: 'Saturday' }
  ];

  constructor(
    private readonly router: Router,
    private readonly adminOps: AdminOpsService,
    private readonly masterSetup: MasterSetupService,
    private readonly phase3: Phase3ControlService
  ) {}

  ngOnInit(): void {
    this.syncRoute(this.router.url);
    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.syncRoute(event.urlAfterRedirects));
    this.loadMasterData();
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  get currentSection(): MasterSectionOption {
    return this.sections.find((section) => section.key === this.activeSection) ?? this.sections[0];
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

  get currentRouteBase(): string[] {
    return ['/admin/masters', this.activeSection];
  }

  get currentRecordLabel(): string {
    const singularLabels: Record<MasterSection, string> = {
      departments: 'Department',
      doctors: 'Doctor',
      staff: 'Staff',
      patientCategories: 'Patient Category',
      wards: 'Ward',
      beds: 'Bed'
    };

    return singularLabels[this.activeSection];
  }

  get selectedDepartment(): DepartmentMaster | null {
    return this.selectedRecordId ? this.departments.find((item) => item.departmentId === this.selectedRecordId) ?? null : null;
  }

  get selectedDoctor(): DoctorMaster | null {
    return this.selectedRecordId ? this.doctors.find((item) => item.doctorId === this.selectedRecordId) ?? null : null;
  }

  get selectedStaff(): StaffMaster | null {
    return this.selectedRecordId ? this.staffMembers.find((item) => item.staffId === this.selectedRecordId) ?? null : null;
  }

  get selectedPatientCategory(): PatientCategoryMaster | null {
    return this.selectedRecordId ? this.patientCategories.find((item) => item.patientCategoryId === this.selectedRecordId) ?? null : null;
  }

  get selectedWard(): WardMaster | null {
    return this.selectedRecordId ? this.wards.find((item) => item.wardId === this.selectedRecordId) ?? null : null;
  }

  get selectedBed(): BedMaster | null {
    return this.selectedRecordId ? this.beds.find((item) => item.bedId === this.selectedRecordId) ?? null : null;
  }

  get hasDetailsRecord(): boolean {
    return !!(this.selectedDepartment || this.selectedDoctor || this.selectedStaff || this.selectedPatientCategory || this.selectedWard || this.selectedBed);
  }

  get totalMasterRecords(): number {
    return this.departments.length + this.doctors.length + this.staffMembers.length + this.patientCategories.length + this.wards.length + this.beds.length;
  }

  get activeMasterRecords(): number {
    return this.departments.filter((item) => item.isActive).length +
      this.doctors.filter((item) => item.isActive).length +
      this.staffMembers.filter((item) => item.isActive).length +
      this.patientCategories.filter((item) => item.isActive).length +
      this.wards.filter((item) => item.isActive).length +
      this.beds.filter((item) => item.isActive).length;
  }

  get branchCount(): number {
    return this.branches.filter((branch) => branch.isActive).length;
  }

  get occupiedBedCount(): number {
    return this.beds.filter((bed) => bed.isOccupied && bed.isActive).length;
  }

  get filteredDepartments(): DepartmentMaster[] {
    return this.departments.filter((item) =>
      this.matchesStatus(item.isActive) &&
      this.matchesSearch(item.name, item.code, item.branchName, item.description));
  }

  get filteredDoctors(): DoctorMaster[] {
    return this.doctors.filter((item) =>
      this.matchesStatus(item.isActive) &&
      this.matchesSearch(item.fullName, item.specialization, item.branchName, item.departmentName, item.email));
  }

  get filteredStaffMembers(): StaffMaster[] {
    return this.staffMembers.filter((item) =>
      this.matchesStatus(item.isActive) &&
      this.matchesSearch(item.fullName, item.designation, item.employeeCode, item.branchName, item.departmentName, item.email));
  }

  get filteredPatientCategories(): PatientCategoryMaster[] {
    return this.patientCategories.filter((item) =>
      this.matchesStatus(item.isActive) &&
      this.matchesSearch(item.name, item.description, item.priorityOrder));
  }

  get filteredWards(): WardMaster[] {
    return this.wards.filter((item) =>
      this.matchesStatus(item.isActive) &&
      this.matchesSearch(item.name, item.wardType, item.roomType, item.branchName, item.departmentName));
  }

  get filteredBeds(): BedMaster[] {
    return this.beds.filter((item) =>
      this.matchesStatus(item.isActive) &&
      this.matchesSearch(item.bedNumber, item.wardName, item.branchName, item.departmentName, item.isOccupied ? 'occupied' : 'available'));
  }

  get availableBranches(): BranchSettings[] {
    return this.branches.filter((branch) => branch.isActive);
  }

  get availableDepartments(): DepartmentMaster[] {
    return this.departments.filter((department) => department.isActive);
  }

  get availableWards(): WardMaster[] {
    return this.wards.filter((ward) => ward.isActive);
  }

  get doctorDepartmentOptions(): DepartmentMaster[] {
    return this.filterDepartmentsByBranch(this.doctorForm.branchId);
  }

  get staffDepartmentOptions(): DepartmentMaster[] {
    return this.filterDepartmentsByBranch(this.staffForm.branchId);
  }

  get wardDepartmentOptions(): DepartmentMaster[] {
    return this.filterDepartmentsByBranch(this.wardForm.branchId);
  }

  get bedDepartmentOptions(): DepartmentMaster[] {
    return this.filterDepartmentsByBranch(this.bedForm.branchId);
  }

  get bedWardOptions(): WardMaster[] {
    return this.availableWards.filter((ward) => !this.bedForm.branchId || ward.branchId === this.bedForm.branchId);
  }

  get sectionRecordCount(): number {
    switch (this.activeSection) {
      case 'departments':
        return this.filteredDepartments.length;
      case 'doctors':
        return this.filteredDoctors.length;
      case 'staff':
        return this.filteredStaffMembers.length;
      case 'patientCategories':
        return this.filteredPatientCategories.length;
      case 'wards':
        return this.filteredWards.length;
      case 'beds':
        return this.filteredBeds.length;
      default:
        return 0;
    }
  }

  setSection(section: MasterSection): void {
    this.router.navigate(['/admin/masters', section]);
  }

  setStatusFilter(filter: StatusFilter): void {
    this.statusFilter = filter;
  }

  goToCreate(): void {
    this.router.navigate(['/admin/masters', this.activeSection, 'create']);
  }

  backToIndex(): void {
    this.router.navigate(['/admin/masters', this.activeSection]);
  }

  goToDetails(recordId: number): void {
    this.router.navigate(['/admin/masters', this.activeSection, recordId]);
  }

  goToEdit(recordId: number): void {
    this.router.navigate(['/admin/masters', this.activeSection, recordId, 'edit']);
  }

  resetCurrentForm(): void {
    this.resetFormForSection(this.activeSection);
  }

  onDoctorBranchChange(): void {
    if (!this.doctorDepartmentOptions.some((item) => item.departmentId === this.doctorForm.departmentId)) {
      this.doctorForm.departmentId = null;
    }
  }

  onStaffBranchChange(): void {
    if (!this.staffDepartmentOptions.some((item) => item.departmentId === this.staffForm.departmentId)) {
      this.staffForm.departmentId = null;
    }
  }

  onWardBranchChange(): void {
    if (!this.wardDepartmentOptions.some((item) => item.departmentId === this.wardForm.departmentId)) {
      this.wardForm.departmentId = null;
    }
  }

  onBedBranchChange(): void {
    if (!this.bedWardOptions.some((item) => item.wardId === this.bedForm.wardId)) {
      this.bedForm.wardId = null;
    }

    if (!this.bedDepartmentOptions.some((item) => item.departmentId === this.bedForm.departmentId)) {
      this.bedForm.departmentId = null;
    }
  }

  onBedWardChange(): void {
    const ward = this.wards.find((item) => item.wardId === this.bedForm.wardId);
    if (!ward) {
      return;
    }

    this.bedForm.branchId = ward.branchId;
    this.bedForm.departmentId = ward.departmentId ?? null;
    if (!this.bedForm.chargePerDay) {
      this.bedForm.chargePerDay = ward.chargePerDay;
    }
  }

  editDepartment(item: DepartmentMaster): void {
    this.goToEdit(item.departmentId);
  }

  editDoctor(item: DoctorMaster): void {
    this.goToEdit(item.doctorId);
  }

  editStaff(item: StaffMaster): void {
    this.goToEdit(item.staffId);
  }

  editPatientCategory(item: PatientCategoryMaster): void {
    this.goToEdit(item.patientCategoryId);
  }

  editWard(item: WardMaster): void {
    this.goToEdit(item.wardId);
  }

  editBed(item: BedMaster): void {
    this.goToEdit(item.bedId);
  }

  saveDepartment(): void {
    if (!this.departmentForm.branchId || !this.departmentForm.name.trim() || !this.departmentForm.code.trim()) {
      this.errorMessage = 'Branch, department name, and code are required.';
      return;
    }

    this.runSave(
      this.departmentForm.departmentId
        ? this.masterSetup.updateDepartment(this.departmentForm.departmentId, {
            branchId: this.departmentForm.branchId,
            name: this.departmentForm.name.trim(),
            code: this.departmentForm.code.trim(),
            description: this.normalizeOptional(this.departmentForm.description),
            isActive: this.departmentForm.isActive
          })
        : this.masterSetup.createDepartment({
            branchId: this.departmentForm.branchId,
            name: this.departmentForm.name.trim(),
            code: this.departmentForm.code.trim(),
            description: this.normalizeOptional(this.departmentForm.description),
            isActive: this.departmentForm.isActive
          }),
      (item) => {
        this.departments = this.sortDepartments(this.replaceOrAppend(this.departments, item, 'departmentId'));
        this.successMessage = this.departmentForm.departmentId ? 'Department updated successfully.' : 'Department created successfully.';
        this.goToDetails(item.departmentId);
      }
    );
  }

  saveDoctor(): void {
    if (!this.doctorForm.fullName.trim() || !this.doctorForm.specialization.trim() || !this.doctorForm.email.trim()) {
      this.errorMessage = 'Doctor name, specialization, and email are required.';
      return;
    }

    this.runSave(
      this.doctorForm.doctorId
        ? this.masterSetup.updateDoctor(this.doctorForm.doctorId, this.buildDoctorPayload())
        : this.masterSetup.createDoctor(this.buildDoctorPayload()),
      (item) => {
        this.doctors = this.sortDoctors(this.replaceOrAppend(this.doctors, item, 'doctorId'));
        this.successMessage = item.portalProvisioningNote || (this.doctorForm.doctorId ? 'Doctor updated successfully.' : 'Doctor created successfully.');
        this.goToDetails(item.doctorId);
      }
    );
  }

  saveStaff(): void {
    if (!this.staffForm.fullName.trim() || !this.staffForm.designation.trim() || !this.staffForm.email.trim() || !this.staffForm.joinDate) {
      this.errorMessage = 'Staff name, designation, email, and join date are required.';
      return;
    }

    this.runSave(
      this.staffForm.staffId
        ? this.masterSetup.updateStaff(this.staffForm.staffId, this.buildStaffPayload())
        : this.masterSetup.createStaff(this.buildStaffPayload()),
      (item) => {
        this.staffMembers = this.sortStaff(this.replaceOrAppend(this.staffMembers, item, 'staffId'));
        this.successMessage = this.staffForm.staffId ? 'Staff updated successfully.' : 'Staff created successfully.';
        this.goToDetails(item.staffId);
      }
    );
  }

  savePatientCategory(): void {
    if (!this.patientCategoryForm.name.trim()) {
      this.errorMessage = 'Category name is required.';
      return;
    }

    this.runSave(
      this.patientCategoryForm.patientCategoryId
        ? this.masterSetup.updatePatientCategory(this.patientCategoryForm.patientCategoryId, {
            name: this.patientCategoryForm.name.trim(),
            description: this.normalizeOptional(this.patientCategoryForm.description),
            priorityOrder: this.patientCategoryForm.priorityOrder,
            isActive: this.patientCategoryForm.isActive
          })
        : this.masterSetup.createPatientCategory({
            name: this.patientCategoryForm.name.trim(),
            description: this.normalizeOptional(this.patientCategoryForm.description),
            priorityOrder: this.patientCategoryForm.priorityOrder,
            isActive: this.patientCategoryForm.isActive
          }),
      (item) => {
        this.patientCategories = this.sortPatientCategories(this.replaceOrAppend(this.patientCategories, item, 'patientCategoryId'));
        this.successMessage = this.patientCategoryForm.patientCategoryId ? 'Patient category updated successfully.' : 'Patient category created successfully.';
        this.goToDetails(item.patientCategoryId);
      }
    );
  }

  saveWard(): void {
    if (!this.wardForm.branchId || !this.wardForm.name.trim() || !this.wardForm.wardType.trim() || !this.wardForm.roomType.trim()) {
      this.errorMessage = 'Branch, ward name, ward type, and room type are required.';
      return;
    }

    this.runSave(
      this.wardForm.wardId
        ? this.masterSetup.updateWard(this.wardForm.wardId, {
            branchId: this.wardForm.branchId,
            departmentId: this.wardForm.departmentId,
            name: this.wardForm.name.trim(),
            wardType: this.wardForm.wardType.trim(),
            roomType: this.wardForm.roomType.trim(),
            chargePerDay: this.wardForm.chargePerDay,
            isActive: this.wardForm.isActive
          })
        : this.masterSetup.createWard({
            branchId: this.wardForm.branchId,
            departmentId: this.wardForm.departmentId,
            name: this.wardForm.name.trim(),
            wardType: this.wardForm.wardType.trim(),
            roomType: this.wardForm.roomType.trim(),
            chargePerDay: this.wardForm.chargePerDay,
            isActive: this.wardForm.isActive
          }),
      (item) => {
        this.wards = this.sortWards(this.replaceOrAppend(this.wards, item, 'wardId'));
        this.successMessage = this.wardForm.wardId ? 'Ward updated successfully.' : 'Ward created successfully.';
        this.goToDetails(item.wardId);
      }
    );
  }

  saveBed(): void {
    if (!this.bedForm.branchId || !this.bedForm.wardId || !this.bedForm.bedNumber.trim()) {
      this.errorMessage = 'Branch, ward, and bed number are required.';
      return;
    }

    this.runSave(
      this.bedForm.bedId
        ? this.masterSetup.updateBed(this.bedForm.bedId, {
            wardId: this.bedForm.wardId,
            branchId: this.bedForm.branchId,
            departmentId: this.bedForm.departmentId,
            bedNumber: this.bedForm.bedNumber.trim(),
            chargePerDay: this.bedForm.chargePerDay,
            isOccupied: this.bedForm.isOccupied,
            isActive: this.bedForm.isActive
          })
        : this.masterSetup.createBed({
            wardId: this.bedForm.wardId,
            branchId: this.bedForm.branchId,
            departmentId: this.bedForm.departmentId,
            bedNumber: this.bedForm.bedNumber.trim(),
            chargePerDay: this.bedForm.chargePerDay,
            isOccupied: this.bedForm.isOccupied,
            isActive: this.bedForm.isActive
          }),
      (item) => {
        this.beds = this.sortBeds(this.replaceOrAppend(this.beds, item, 'bedId'));
        this.successMessage = this.bedForm.bedId ? 'Bed updated successfully.' : 'Bed created successfully.';
        this.goToDetails(item.bedId);
      }
    );
  }

  updateDepartmentStatus(item: DepartmentMaster): void {
    this.runStatusUpdate(
      this.masterSetup.setDepartmentStatus(item.departmentId, !item.isActive),
      () => {
        this.departments = this.sortDepartments(this.departments.map((entry) =>
          entry.departmentId === item.departmentId ? { ...entry, isActive: !entry.isActive } : entry));
        if (this.departmentForm.departmentId === item.departmentId) {
          this.departmentForm = { ...this.departmentForm, isActive: !item.isActive };
        }
      },
      `Department ${item.isActive ? 'deactivated' : 'activated'} successfully.`
    );
  }

  updateDoctorStatus(item: DoctorMaster): void {
    this.runStatusUpdate(
      this.masterSetup.setDoctorStatus(item.doctorId, !item.isActive),
      () => {
        this.doctors = this.sortDoctors(this.doctors.map((entry) =>
          entry.doctorId === item.doctorId ? { ...entry, isActive: !entry.isActive } : entry));
        if (this.doctorForm.doctorId === item.doctorId) {
          this.doctorForm = { ...this.doctorForm, isActive: !item.isActive };
        }
      },
      `Doctor ${item.isActive ? 'deactivated' : 'activated'} successfully.`
    );
  }

  updateStaffStatus(item: StaffMaster): void {
    this.runStatusUpdate(
      this.masterSetup.setStaffStatus(item.staffId, !item.isActive),
      () => {
        this.staffMembers = this.sortStaff(this.staffMembers.map((entry) =>
          entry.staffId === item.staffId ? { ...entry, isActive: !entry.isActive } : entry));
        if (this.staffForm.staffId === item.staffId) {
          this.staffForm = { ...this.staffForm, isActive: !item.isActive };
        }
      },
      `Staff member ${item.isActive ? 'deactivated' : 'activated'} successfully.`
    );
  }

  updatePatientCategoryStatus(item: PatientCategoryMaster): void {
    this.runStatusUpdate(
      this.masterSetup.setPatientCategoryStatus(item.patientCategoryId, !item.isActive),
      () => {
        this.patientCategories = this.sortPatientCategories(this.patientCategories.map((entry) =>
          entry.patientCategoryId === item.patientCategoryId ? { ...entry, isActive: !entry.isActive } : entry));
        if (this.patientCategoryForm.patientCategoryId === item.patientCategoryId) {
          this.patientCategoryForm = { ...this.patientCategoryForm, isActive: !item.isActive };
        }
      },
      `Patient category ${item.isActive ? 'deactivated' : 'activated'} successfully.`
    );
  }

  updateWardStatus(item: WardMaster): void {
    this.runStatusUpdate(
      this.masterSetup.setWardStatus(item.wardId, !item.isActive),
      () => {
        this.wards = this.sortWards(this.wards.map((entry) =>
          entry.wardId === item.wardId ? { ...entry, isActive: !entry.isActive } : entry));
        if (this.wardForm.wardId === item.wardId) {
          this.wardForm = { ...this.wardForm, isActive: !item.isActive };
        }
      },
      `Ward ${item.isActive ? 'deactivated' : 'activated'} successfully.`
    );
  }

  updateBedStatus(item: BedMaster): void {
    this.runStatusUpdate(
      this.masterSetup.setBedStatus(item.bedId, !item.isActive),
      () => {
        this.beds = this.sortBeds(this.beds.map((entry) =>
          entry.bedId === item.bedId ? { ...entry, isActive: !entry.isActive } : entry));
        if (this.bedForm.bedId === item.bedId) {
          this.bedForm = { ...this.bedForm, isActive: !item.isActive };
        }
      },
      `Bed ${item.isActive ? 'deactivated' : 'activated'} successfully.`
    );
  }

  getDepartmentBadge(item: DepartmentMaster): string {
    return `${item.branchName} | ${item.code}`;
  }

  getDoctorMeta(item: DoctorMaster): string {
    const location = [item.branchName, item.departmentName].filter(Boolean).join(' | ');
    return location || 'No branch mapping';
  }

  getDoctorSchedule(item: DoctorMaster): string {
    if (!item.opdDays && !item.opdStartTime && !item.opdEndTime) {
      return 'Schedule pending';
    }

    return `${item.opdDays || 'Days pending'}${item.opdStartTime ? ` | ${this.toTimeInput(item.opdStartTime)}` : ''}${item.opdEndTime ? ` - ${this.toTimeInput(item.opdEndTime)}` : ''}`;
  }

  scheduleDayLabel(day: number): string {
    return this.dayOptions.find((option) => option.value === day)?.label ?? 'Unknown';
  }

  editDoctorSchedule(schedule: DoctorScheduleRecord): void {
    this.scheduleForm = {
      scheduleId: schedule.scheduleId,
      dayOfWeek: schedule.dayOfWeek,
      startTime: this.toTimeInput(schedule.startTime),
      endTime: this.toTimeInput(schedule.endTime),
      slotDurationMinutes: schedule.slotDurationMinutes,
      maxPatientsPerSlot: schedule.maxPatientsPerSlot,
      isActive: schedule.isActive
    };
  }

  resetDoctorScheduleForm(): void {
    this.scheduleForm = { ...EMPTY_SCHEDULE_FORM };
  }

  saveDoctorSchedule(): void {
    const doctor = this.selectedDoctor;
    if (!doctor) {
      this.errorMessage = 'Select a doctor before saving schedules.';
      return;
    }

    if (!this.scheduleForm.startTime || !this.scheduleForm.endTime) {
      this.errorMessage = 'Start and end time are required for doctor availability.';
      return;
    }

    if (this.scheduleForm.endTime <= this.scheduleForm.startTime) {
      this.errorMessage = 'End time must be later than start time.';
      return;
    }

    if (this.scheduleForm.slotDurationMinutes < 5) {
      this.errorMessage = 'Slot duration must be at least 5 minutes.';
      return;
    }

    if (this.scheduleForm.maxPatientsPerSlot < 1) {
      this.errorMessage = 'Max patients per slot must be at least 1.';
      return;
    }

    this.isSavingSchedule = true;
    this.errorMessage = '';
    this.successMessage = '';

    const payload = {
      doctorId: doctor.doctorId,
      dayOfWeek: this.scheduleForm.dayOfWeek,
      startTime: this.toTimePayload(this.scheduleForm.startTime),
      endTime: this.toTimePayload(this.scheduleForm.endTime),
      slotDurationMinutes: this.scheduleForm.slotDurationMinutes,
      maxPatientsPerSlot: this.scheduleForm.maxPatientsPerSlot,
      isActive: this.scheduleForm.isActive
    };

    const request = this.scheduleForm.scheduleId
      ? this.phase3.updateDoctorSchedule(this.scheduleForm.scheduleId, payload)
      : this.phase3.createDoctorSchedule(doctor.doctorId, payload);

    request.subscribe({
      next: () => {
        this.isSavingSchedule = false;
        this.successMessage = 'Doctor schedule saved.';
        this.resetDoctorScheduleForm();
        this.loadDoctorSchedules(doctor.doctorId);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingSchedule = false;
        this.errorMessage = error?.error?.message || 'Unable to save doctor schedule.';
      }
    });
  }

  deleteDoctorSchedule(scheduleId: number): void {
    const doctor = this.selectedDoctor;
    if (!doctor) {
      return;
    }

    this.isSavingSchedule = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.phase3.deleteDoctorSchedule(scheduleId).subscribe({
      next: () => {
        this.isSavingSchedule = false;
        this.successMessage = 'Doctor schedule deleted.';
        this.resetDoctorScheduleForm();
        this.loadDoctorSchedules(doctor.doctorId);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingSchedule = false;
        this.errorMessage = error?.error?.message || 'Unable to delete doctor schedule.';
      }
    });
  }

  getStaffMeta(item: StaffMaster): string {
    const parts = [item.branchName, item.departmentName, item.shift].filter(Boolean);
    return parts.length ? parts.join(' | ') : 'No mapping';
  }

  getWardMeta(item: WardMaster): string {
    const parts = [item.branchName, item.departmentName].filter(Boolean);
    return parts.length ? parts.join(' | ') : 'No department mapping';
  }

  getBedMeta(item: BedMaster): string {
    const parts = [item.branchName, item.departmentName, item.wardName].filter(Boolean);
    return parts.join(' | ');
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

    if (this.activeSection === 'doctors' && this.viewMode === 'details' && this.selectedRecordId) {
      this.resetDoctorScheduleForm();
      this.loadDoctorSchedules(this.selectedRecordId);
    } else {
      this.doctorSchedules = [];
      this.resetDoctorScheduleForm();
    }
  }

  private resetFormForSection(section: MasterSection): void {
    this.errorMessage = '';
    this.successMessage = '';

    switch (section) {
      case 'departments':
        this.departmentForm = { ...EMPTY_DEPARTMENT_FORM };
        break;
      case 'doctors':
        this.doctorForm = { ...EMPTY_DOCTOR_FORM };
        break;
      case 'staff':
        this.staffForm = { ...EMPTY_STAFF_FORM };
        break;
      case 'patientCategories':
        this.patientCategoryForm = { ...EMPTY_PATIENT_CATEGORY_FORM };
        break;
      case 'wards':
        this.wardForm = { ...EMPTY_WARD_FORM };
        break;
      case 'beds':
        this.bedForm = { ...EMPTY_BED_FORM };
        break;
    }
  }

  private loadSelectedRecordIntoForm(): void {
    if (this.viewMode !== 'edit') {
      return;
    }

    switch (this.activeSection) {
      case 'departments':
        if (this.selectedDepartment) {
          this.departmentForm = {
            departmentId: this.selectedDepartment.departmentId,
            branchId: this.selectedDepartment.branchId,
            name: this.selectedDepartment.name,
            code: this.selectedDepartment.code,
            description: this.selectedDepartment.description ?? '',
            isActive: this.selectedDepartment.isActive
          };
        }
        break;
      case 'doctors':
        if (this.selectedDoctor) {
          this.doctorForm = {
            doctorId: this.selectedDoctor.doctorId,
            branchId: this.selectedDoctor.branchId ?? null,
            departmentId: this.selectedDoctor.departmentId ?? null,
            fullName: this.selectedDoctor.fullName,
            specialization: this.selectedDoctor.specialization,
            email: this.selectedDoctor.email,
            phone: this.selectedDoctor.phone ?? '',
            experienceYears: this.selectedDoctor.experienceYears,
            qualification: this.selectedDoctor.qualification ?? '',
            opdDays: this.selectedDoctor.opdDays ?? '',
            opdStartTime: this.toTimeInput(this.selectedDoctor.opdStartTime),
            opdEndTime: this.toTimeInput(this.selectedDoctor.opdEndTime),
            consultationFee: this.selectedDoctor.consultationFee,
            isActive: this.selectedDoctor.isActive
          };
        }
        break;
      case 'staff':
        if (this.selectedStaff) {
          this.staffForm = {
            staffId: this.selectedStaff.staffId,
            branchId: this.selectedStaff.branchId ?? null,
            departmentId: this.selectedStaff.departmentId ?? null,
            fullName: this.selectedStaff.fullName,
            employeeCode: this.selectedStaff.employeeCode ?? '',
            designation: this.selectedStaff.designation,
            shift: this.selectedStaff.shift ?? '',
            email: this.selectedStaff.email,
            phone: this.selectedStaff.phone ?? '',
            joinDate: this.toDateInput(this.selectedStaff.joinDate),
            isActive: this.selectedStaff.isActive
          };
        }
        break;
      case 'patientCategories':
        if (this.selectedPatientCategory) {
          this.patientCategoryForm = {
            patientCategoryId: this.selectedPatientCategory.patientCategoryId,
            name: this.selectedPatientCategory.name,
            description: this.selectedPatientCategory.description ?? '',
            priorityOrder: this.selectedPatientCategory.priorityOrder,
            isActive: this.selectedPatientCategory.isActive
          };
        }
        break;
      case 'wards':
        if (this.selectedWard) {
          this.wardForm = {
            wardId: this.selectedWard.wardId,
            branchId: this.selectedWard.branchId,
            departmentId: this.selectedWard.departmentId ?? null,
            name: this.selectedWard.name,
            wardType: this.selectedWard.wardType,
            roomType: this.selectedWard.roomType,
            chargePerDay: this.selectedWard.chargePerDay,
            isActive: this.selectedWard.isActive
          };
        }
        break;
      case 'beds':
        if (this.selectedBed) {
          this.bedForm = {
            bedId: this.selectedBed.bedId,
            wardId: this.selectedBed.wardId,
            branchId: this.selectedBed.branchId,
            departmentId: this.selectedBed.departmentId ?? null,
            bedNumber: this.selectedBed.bedNumber,
            chargePerDay: this.selectedBed.chargePerDay,
            isOccupied: this.selectedBed.isOccupied,
            isActive: this.selectedBed.isActive
          };
        }
        break;
    }
  }

  private loadMasterData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    forkJoin({
      settings: this.adminOps.getOrganizationSettings(),
      departments: this.masterSetup.getDepartments(),
      doctors: this.masterSetup.getDoctors(),
      staff: this.masterSetup.getStaff(),
      patientCategories: this.masterSetup.getPatientCategories(),
      wards: this.masterSetup.getWards(),
      beds: this.masterSetup.getBeds()
    }).subscribe({
      next: ({ settings, departments, doctors, staff, patientCategories, wards, beds }) => {
        this.branches = settings.branches.map((branch) => ({ ...branch }));
        this.departments = this.sortDepartments(departments);
        this.doctors = this.sortDoctors(doctors);
        this.staffMembers = this.sortStaff(staff);
        this.patientCategories = this.sortPatientCategories(patientCategories);
        this.wards = this.sortWards(wards);
        this.beds = this.sortBeds(beds);
        this.loadSelectedRecordIntoForm();
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load master setup data right now.';
      }
    });
  }

  private buildDoctorPayload(): Partial<DoctorMaster> {
    return {
      branchId: this.doctorForm.branchId,
      departmentId: this.doctorForm.departmentId,
      fullName: this.doctorForm.fullName.trim(),
      specialization: this.doctorForm.specialization.trim(),
      email: this.doctorForm.email.trim(),
      phone: this.normalizeOptional(this.doctorForm.phone),
      experienceYears: this.doctorForm.experienceYears,
      qualification: this.normalizeOptional(this.doctorForm.qualification),
      opdDays: this.normalizeOptional(this.doctorForm.opdDays),
      opdStartTime: this.normalizeOptional(this.doctorForm.opdStartTime),
      opdEndTime: this.normalizeOptional(this.doctorForm.opdEndTime),
      consultationFee: this.doctorForm.consultationFee,
      isActive: this.doctorForm.isActive
    };
  }

  private buildStaffPayload(): Partial<StaffMaster> {
    return {
      branchId: this.staffForm.branchId,
      departmentId: this.staffForm.departmentId,
      fullName: this.staffForm.fullName.trim(),
      employeeCode: this.normalizeOptional(this.staffForm.employeeCode),
      designation: this.staffForm.designation.trim(),
      shift: this.normalizeOptional(this.staffForm.shift),
      email: this.staffForm.email.trim(),
      phone: this.normalizeOptional(this.staffForm.phone),
      joinDate: this.staffForm.joinDate,
      isActive: this.staffForm.isActive
    };
  }

  private runSave<T>(request: Observable<T>, onSuccess: (value: T) => void): void {
    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    request.subscribe({
      next: (value) => {
        onSuccess(value);
        this.isSaving = false;
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSaving = false;
        this.errorMessage = error?.error?.message || 'Unable to save master record.';
      }
    });
  }

  private runStatusUpdate(request: Observable<void>, onSuccess: () => void, message: string): void {
    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    request.subscribe({
      next: () => {
        onSuccess();
        this.isSaving = false;
        this.successMessage = message;
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSaving = false;
        this.errorMessage = error?.error?.message || 'Unable to update record status.';
      }
    });
  }

  private filterDepartmentsByBranch(branchId: number | null): DepartmentMaster[] {
    return this.availableDepartments.filter((department) => !branchId || department.branchId === branchId);
  }

  private matchesStatus(isActive: boolean): boolean {
    if (this.statusFilter === 'active') {
      return isActive;
    }

    if (this.statusFilter === 'inactive') {
      return !isActive;
    }

    return true;
  }

  private matchesSearch(...values: Array<string | number | null | undefined>): boolean {
    const normalizedTerm = this.searchTerm.trim().toLowerCase();
    if (!normalizedTerm) {
      return true;
    }

    return values.some((value) => String(value ?? '').toLowerCase().includes(normalizedTerm));
  }

  private normalizeOptional(value: string): string | null {
    const normalized = value.trim();
    return normalized ? normalized : null;
  }

  private toDateInput(value?: string | null): string {
    return value ? value.slice(0, 10) : '';
  }

  private toTimeInput(value?: string | null): string {
    return value ? value.slice(0, 5) : '';
  }

  private toTimePayload(value: string): string {
    return value.length === 5 ? `${value}:00` : value;
  }

  private parseId(value: string | null): number | null {
    if (!value) {
      return null;
    }

    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  }

  private loadDoctorSchedules(doctorId: number): void {
    this.phase3.getDoctorSchedules(doctorId).subscribe({
      next: (schedules) => {
        this.doctorSchedules = [...schedules].sort((a, b) => a.dayOfWeek - b.dayOfWeek || a.startTime.localeCompare(b.startTime));
      },
      error: () => {
        this.doctorSchedules = [];
        this.errorMessage = 'Unable to load doctor schedules.';
      }
    });
  }

  private toSection(value: string | null): MasterSection {
    return this.sections.some((section) => section.key === value)
      ? (value as MasterSection)
      : 'departments';
  }

  private replaceOrAppend<T, K extends keyof T>(items: T[], item: T, key: K): T[] {
    const existingIndex = items.findIndex((entry) => entry[key] === item[key]);
    if (existingIndex === -1) {
      return [...items, item];
    }

    return items.map((entry) => entry[key] === item[key] ? item : entry);
  }

  private sortDepartments(items: DepartmentMaster[]): DepartmentMaster[] {
    return [...items].sort((a, b) => `${a.branchName} ${a.name}`.localeCompare(`${b.branchName} ${b.name}`));
  }

  private sortDoctors(items: DoctorMaster[]): DoctorMaster[] {
    return [...items].sort((a, b) => a.fullName.localeCompare(b.fullName));
  }

  private sortStaff(items: StaffMaster[]): StaffMaster[] {
    return [...items].sort((a, b) => a.fullName.localeCompare(b.fullName));
  }

  private sortPatientCategories(items: PatientCategoryMaster[]): PatientCategoryMaster[] {
    return [...items].sort((a, b) => a.priorityOrder - b.priorityOrder || a.name.localeCompare(b.name));
  }

  private sortWards(items: WardMaster[]): WardMaster[] {
    return [...items].sort((a, b) => `${a.branchName} ${a.name}`.localeCompare(`${b.branchName} ${b.name}`));
  }

  private sortBeds(items: BedMaster[]): BedMaster[] {
    return [...items].sort((a, b) => `${a.branchName} ${a.wardName} ${a.bedNumber}`.localeCompare(`${b.branchName} ${b.wardName} ${b.bedNumber}`));
  }
}
