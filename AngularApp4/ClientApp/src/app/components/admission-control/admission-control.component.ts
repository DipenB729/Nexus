import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter, forkJoin } from 'rxjs';
import { BedMaster, DoctorMaster, WardMaster } from '../../core/models/hms/master-setup.model';
import {
  AdmissionRecord,
  ApproveDischargePayload,
  CreateAdmissionPayload,
  PatientRecord,
  TransferAdmissionPayload
} from '../../core/models/hms/phase3-control.model';
import { MasterSetupService } from '../../core/services/hms/master-setup.service';
import { Phase3ControlService } from '../../core/services/hms/phase3-control.service';

type ViewMode = 'index' | 'details' | 'create' | 'edit';

interface AdmissionCreateFormState {
  patientId: number | null;
  doctorId: number | null;
  wardId: number | null;
  bedId: number | null;
  admissionDate: string;
  expectedDischargeDate: string;
  reason: string;
  notes: string;
}

interface TransferFormState {
  wardId: number | null;
  bedId: number | null;
  transferDate: string;
  notes: string;
}

interface DischargeFormState {
  dischargeDate: string;
  dischargeSummary: string;
  notes: string;
}

const EMPTY_CREATE_FORM: AdmissionCreateFormState = {
  patientId: null,
  doctorId: null,
  wardId: null,
  bedId: null,
  admissionDate: new Date().toISOString().slice(0, 10),
  expectedDischargeDate: '',
  reason: '',
  notes: ''
};

const EMPTY_TRANSFER_FORM: TransferFormState = {
  wardId: null,
  bedId: null,
  transferDate: new Date().toISOString().slice(0, 10),
  notes: ''
};

const EMPTY_DISCHARGE_FORM: DischargeFormState = {
  dischargeDate: new Date().toISOString().slice(0, 10),
  dischargeSummary: '',
  notes: ''
};

@Component({
  selector: 'app-admission-control',
  templateUrl: './admission-control.component.html',
  styleUrls: ['./admission-control.component.scss']
})
export class AdmissionControlComponent implements OnInit, OnDestroy {
  admissions: AdmissionRecord[] = [];
  patients: PatientRecord[] = [];
  doctors: DoctorMaster[] = [];
  wards: WardMaster[] = [];
  beds: BedMaster[] = [];

  viewMode: ViewMode = 'index';
  selectedAdmissionId: number | null = null;
  admissionForm: AdmissionCreateFormState = { ...EMPTY_CREATE_FORM };
  transferForm: TransferFormState = { ...EMPTY_TRANSFER_FORM };
  dischargeForm: DischargeFormState = { ...EMPTY_DISCHARGE_FORM };

  searchTerm = '';
  statusFilter: 'All' | 'Active' | 'DischargePending' | 'Discharged' = 'All';

  isLoading = true;
  isSavingAdmission = false;
  isSavingTransfer = false;
  isSavingDischarge = false;
  errorMessage = '';
  statusMessage = '';

  private routeSub?: Subscription;

  constructor(
    private readonly router: Router,
    private readonly phase3: Phase3ControlService,
    private readonly masterSetup: MasterSetupService
  ) {}

  ngOnInit(): void {
    this.syncRoute(this.router.url);
    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.syncRoute(event.urlAfterRedirects));
    this.loadData(this.selectedAdmissionId);
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
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

  get activeAdmissions(): number {
    return this.admissions.filter((admission) => admission.status === 'Active').length;
  }

  get dischargedAdmissions(): number {
    return this.admissions.filter((admission) => admission.status === 'Discharged').length;
  }

  get occupiedBeds(): number {
    return this.beds.filter((bed) => bed.isActive && bed.isOccupied).length;
  }

  get filteredAdmissions(): AdmissionRecord[] {
    const term = this.searchTerm.trim().toLowerCase();

    return this.admissions.filter((admission) => {
      if (this.statusFilter !== 'All' && admission.status !== this.statusFilter) {
        return false;
      }

      if (!term) {
        return true;
      }

      return [
        admission.patientName,
        admission.medicalRecordNumber,
        admission.admissionNumber,
        admission.doctorName,
        admission.wardName,
        admission.bedNumber
      ].some((value) => String(value ?? '').toLowerCase().includes(term));
    });
  }

  get availablePatients(): PatientRecord[] {
    return this.patients.filter((patient) => patient.isActive && !patient.isMerged);
  }

  get availableDoctors(): DoctorMaster[] {
    return this.doctors.filter((doctor) => doctor.isActive);
  }

  get availableWards(): WardMaster[] {
    return this.wards.filter((ward) => ward.isActive);
  }

  get availableAdmissionBeds(): BedMaster[] {
    return this.beds.filter((bed) =>
      bed.isActive &&
      !bed.isOccupied &&
      (!this.admissionForm.wardId || bed.wardId === this.admissionForm.wardId));
  }

  get availableTransferBeds(): BedMaster[] {
    return this.beds.filter((bed) =>
      bed.isActive &&
      !bed.isOccupied &&
      (!this.transferForm.wardId || bed.wardId === this.transferForm.wardId));
  }

  get selectedAdmission(): AdmissionRecord | null {
    return this.selectedAdmissionId
      ? this.admissions.find((admission) => admission.patientAdmissionId === this.selectedAdmissionId) ?? null
      : null;
  }

  backToIndex(): void {
    this.router.navigate(['/admin/admissions']);
  }

  goToCreate(): void {
    this.router.navigate(['/admin/admissions/create']);
  }

  goToDetails(admissionId: number): void {
    this.router.navigate(['/admin/admissions', admissionId]);
  }

  goToEdit(admissionId: number): void {
    this.router.navigate(['/admin/admissions', admissionId, 'edit']);
  }

  onAdmissionWardChange(): void {
    if (!this.availableAdmissionBeds.some((bed) => bed.bedId === this.admissionForm.bedId)) {
      this.admissionForm.bedId = null;
    }
  }

  onTransferWardChange(): void {
    if (!this.availableTransferBeds.some((bed) => bed.bedId === this.transferForm.bedId)) {
      this.transferForm.bedId = null;
    }
  }

  createAdmission(): void {
    if (!this.admissionForm.patientId || !this.admissionForm.wardId || !this.admissionForm.bedId || !this.admissionForm.admissionDate) {
      this.errorMessage = 'Patient, ward, bed, and admission date are required.';
      return;
    }

    this.isSavingAdmission = true;
    this.errorMessage = '';
    this.statusMessage = '';

    const payload: CreateAdmissionPayload = {
      patientId: this.admissionForm.patientId,
      doctorId: this.admissionForm.doctorId,
      wardId: this.admissionForm.wardId,
      bedId: this.admissionForm.bedId,
      admissionDate: this.admissionForm.admissionDate,
      expectedDischargeDate: this.normalizeOptional(this.admissionForm.expectedDischargeDate),
      reason: this.normalizeOptional(this.admissionForm.reason),
      notes: this.normalizeOptional(this.admissionForm.notes)
    };

    this.phase3.createAdmission(payload).subscribe({
      next: (admission) => {
        this.isSavingAdmission = false;
        this.statusMessage = 'Patient admitted successfully.';
        this.admissionForm = { ...EMPTY_CREATE_FORM };
        this.router.navigate(['/admin/admissions', admission.patientAdmissionId]);
        this.loadData(admission.patientAdmissionId);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingAdmission = false;
        this.errorMessage = error?.error?.message || 'Unable to admit patient.';
      }
    });
  }

  transferPatient(): void {
    const selected = this.selectedAdmission;
    if (!selected || !this.transferForm.wardId || !this.transferForm.bedId || !this.transferForm.transferDate) {
      this.errorMessage = 'Select a ward, bed, and transfer date.';
      return;
    }

    this.isSavingTransfer = true;
    this.errorMessage = '';
    this.statusMessage = '';

    const payload: TransferAdmissionPayload = {
      wardId: this.transferForm.wardId,
      bedId: this.transferForm.bedId,
      transferDate: this.transferForm.transferDate,
      notes: this.normalizeOptional(this.transferForm.notes)
    };

    this.phase3.transferAdmission(selected.patientAdmissionId, payload).subscribe({
      next: (admission) => {
        this.isSavingTransfer = false;
        this.statusMessage = 'Patient transfer recorded.';
        this.router.navigate(['/admin/admissions', admission.patientAdmissionId]);
        this.loadData(admission.patientAdmissionId);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingTransfer = false;
        this.errorMessage = error?.error?.message || 'Unable to transfer patient.';
      }
    });
  }

  approveDischarge(): void {
    const selected = this.selectedAdmission;
    if (!selected || !this.dischargeForm.dischargeDate || !this.dischargeForm.dischargeSummary.trim()) {
      this.errorMessage = 'Discharge date and summary are required.';
      return;
    }

    this.isSavingDischarge = true;
    this.errorMessage = '';
    this.statusMessage = '';

    const payload: ApproveDischargePayload = {
      dischargeDate: this.dischargeForm.dischargeDate,
      dischargeSummary: this.dischargeForm.dischargeSummary.trim(),
      notes: this.normalizeOptional(this.dischargeForm.notes)
    };

    this.phase3.approveDischarge(selected.patientAdmissionId, payload).subscribe({
      next: (admission) => {
        this.isSavingDischarge = false;
        this.statusMessage = 'Discharge summary approved.';
        this.router.navigate(['/admin/admissions', admission.patientAdmissionId]);
        this.loadData(admission.patientAdmissionId);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingDischarge = false;
        this.errorMessage = error?.error?.message || 'Unable to approve discharge.';
      }
    });
  }

  refresh(): void {
    this.loadData(this.selectedAdmissionId);
  }

  private syncRoute(url: string): void {
    const parts = url.split('?')[0].split('/').filter(Boolean);
    const routeSegment = parts[2] ?? null;
    const nextAdmissionId = this.parseId(routeSegment);

    if (routeSegment === 'create') {
      this.viewMode = 'create';
      this.selectedAdmissionId = null;
      this.admissionForm = { ...EMPTY_CREATE_FORM };
    } else if (nextAdmissionId && parts[3] === 'edit') {
      this.viewMode = 'edit';
      this.selectedAdmissionId = nextAdmissionId;
    } else if (nextAdmissionId) {
      this.viewMode = 'details';
      this.selectedAdmissionId = nextAdmissionId;
    } else {
      this.viewMode = 'index';
      this.selectedAdmissionId = null;
    }

    this.syncSelectedAdmissionForms();
  }

  private loadData(preferredAdmissionId?: number | null): void {
    this.isLoading = true;
    this.errorMessage = '';

    forkJoin({
      admissions: this.phase3.getAdmissions(),
      patients: this.phase3.getPatients('', null, false),
      doctors: this.masterSetup.getDoctors(),
      wards: this.masterSetup.getWards(),
      beds: this.masterSetup.getBeds()
    }).subscribe({
      next: ({ admissions, patients, doctors, wards, beds }) => {
        this.admissions = admissions;
        this.patients = patients;
        this.doctors = doctors;
        this.wards = wards;
        this.beds = beds;
        this.selectedAdmissionId = this.resolveSelectedAdmissionId(admissions, preferredAdmissionId);
        this.syncSelectedAdmissionForms();
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load admission controls right now.';
      }
    });
  }

  private resolveSelectedAdmissionId(admissions: AdmissionRecord[], preferredAdmissionId?: number | null): number | null {
    if (this.isIndexView || this.isCreateView) {
      return null;
    }

    if (preferredAdmissionId && admissions.some((admission) => admission.patientAdmissionId === preferredAdmissionId)) {
      return preferredAdmissionId;
    }

    return admissions[0]?.patientAdmissionId ?? null;
  }

  private syncSelectedAdmissionForms(): void {
    const admission = this.selectedAdmission;
    this.transferForm = {
      ...EMPTY_TRANSFER_FORM,
      wardId: admission?.wardId ?? null,
      transferDate: new Date().toISOString().slice(0, 10)
    };
    this.dischargeForm = {
      ...EMPTY_DISCHARGE_FORM,
      dischargeDate: new Date().toISOString().slice(0, 10),
      dischargeSummary: admission?.dischargeSummary ?? '',
      notes: ''
    };
  }

  private parseId(value: string | null): number | null {
    if (!value) {
      return null;
    }

    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  }

  private normalizeOptional(value: string): string | null {
    const normalized = value.trim();
    return normalized ? normalized : null;
  }
}
