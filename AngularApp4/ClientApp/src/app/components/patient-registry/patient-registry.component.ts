import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter, forkJoin } from 'rxjs';
import { PatientCategoryMaster } from '../../core/models/hms/master-setup.model';
import { CreatePatientPayload, PatientRecord, SavePatientPayload } from '../../core/models/hms/phase3-control.model';
import { MasterSetupService } from '../../core/services/hms/master-setup.service';
import { Phase3ControlService } from '../../core/services/hms/phase3-control.service';

type ViewMode = 'index' | 'details' | 'create' | 'edit';

interface PatientFormState {
  fullName: string;
  email: string;
  phone: string;
  patientCategoryId: number | null;
  gender: string;
  dateOfBirth: string;
  address: string;
  bloodGroup: string;
  emergencyContact: string;
  notes: string;
  isActive: boolean;
}

const EMPTY_PATIENT_FORM: PatientFormState = {
  fullName: '',
  email: '',
  phone: '',
  patientCategoryId: null,
  gender: '',
  dateOfBirth: '',
  address: '',
  bloodGroup: '',
  emergencyContact: '',
  notes: '',
  isActive: true
};

@Component({
  selector: 'app-patient-registry',
  templateUrl: './patient-registry.component.html',
  styleUrls: ['./patient-registry.component.scss']
})
export class PatientRegistryComponent implements OnInit, OnDestroy {
  patients: PatientRecord[] = [];
  categories: PatientCategoryMaster[] = [];

  viewMode: ViewMode = 'index';
  selectedPatientId: number | null = null;
  patientForm: PatientFormState = { ...EMPTY_PATIENT_FORM };
  temporaryPassword = '';

  searchTerm = '';
  categoryFilter: number | null = null;
  mergeSourceId: number | null = null;
  mergeNotes = '';

  isLoading = true;
  isSaving = false;
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
    this.loadData(this.selectedPatientId);
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

  get totalPatients(): number {
    return this.patients.filter((patient) => !patient.isMerged).length;
  }

  get activePatients(): number {
    return this.patients.filter((patient) => patient.isActive && !patient.isMerged).length;
  }

  get duplicatePatients(): number {
    return this.patients.filter((patient) => !patient.isMerged && patient.duplicateGroupSize > 1).length;
  }

  get filteredPatients(): PatientRecord[] {
    const term = this.searchTerm.trim().toLowerCase();

    return this.patients.filter((patient) => {
      if (this.categoryFilter && patient.patientCategoryId !== this.categoryFilter) {
        return false;
      }

      if (!term) {
        return true;
      }

      return [
        patient.fullName,
        patient.email,
        patient.phone,
        patient.medicalRecordNumber,
        patient.patientCategoryName,
        patient.address
      ].some((value) => String(value ?? '').toLowerCase().includes(term));
    });
  }

  get selectedPatient(): PatientRecord | null {
    return this.selectedPatientId
      ? this.patients.find((patient) => patient.patientId === this.selectedPatientId) ?? null
      : null;
  }

  get canEditSelectedPatient(): boolean {
    return !!this.selectedPatient && !this.selectedPatient.isMerged;
  }

  get duplicateCandidates(): PatientRecord[] {
    const selected = this.selectedPatient;
    if (!selected || selected.duplicateGroupSize <= 1) {
      return [];
    }

    const duplicateKey = this.buildDuplicateKey(selected);
    return this.patients.filter((patient) =>
      patient.patientId !== selected.patientId &&
      !patient.isMerged &&
      this.buildDuplicateKey(patient) === duplicateKey);
  }

  backToIndex(): void {
    this.router.navigate(['/admin/patients']);
  }

  goToCreate(): void {
    this.router.navigate(['/admin/patients/create']);
  }

  goToDetails(patientId: number): void {
    this.router.navigate(['/admin/patients', patientId]);
  }

  goToEdit(patientId: number): void {
    this.router.navigate(['/admin/patients', patientId, 'edit']);
  }

  createPatient(): void {
    if (!this.patientForm.fullName.trim() || !this.patientForm.email.trim()) {
      this.errorMessage = 'Full name and email are required.';
      return;
    }

    if (!this.temporaryPassword || !this.temporaryPassword.trim() || this.temporaryPassword.length < 6) {
      this.errorMessage = 'Temporary password must be at least 6 characters.';
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    this.statusMessage = '';

    const payload: CreatePatientPayload = {
      fullName: this.patientForm.fullName.trim(),
      email: this.patientForm.email.trim(),
      password: this.temporaryPassword,
      phone: this.normalizeOptional(this.patientForm.phone),
      patientCategoryId: this.patientForm.patientCategoryId,
      gender: this.normalizeOptional(this.patientForm.gender),
      dateOfBirth: this.normalizeOptional(this.patientForm.dateOfBirth),
      address: this.normalizeOptional(this.patientForm.address),
      bloodGroup: this.normalizeOptional(this.patientForm.bloodGroup),
      emergencyContact: this.normalizeOptional(this.patientForm.emergencyContact),
      notes: this.normalizeOptional(this.patientForm.notes),
      isActive: this.patientForm.isActive
    };

    this.phase3.createPatient(payload).subscribe({
      next: (created) => {
        this.upsertPatient(created);
        this.isSaving = false;
        this.statusMessage = 'Patient record created.';
        this.temporaryPassword = '';
        this.router.navigate(['/admin/patients', created.patientId]);
        this.loadData(created.patientId);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSaving = false;
        this.errorMessage = error?.error?.message || 'Unable to create patient record.';
      }
    });
  }

  savePatient(): void {
    const selected = this.selectedPatient;
    if (!selected) {
      return;
    }

    if (!this.patientForm.fullName.trim() || !this.patientForm.email.trim()) {
      this.errorMessage = 'Full name and email are required.';
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    this.statusMessage = '';

    const payload: SavePatientPayload = {
      fullName: this.patientForm.fullName.trim(),
      email: this.patientForm.email.trim(),
      phone: this.normalizeOptional(this.patientForm.phone),
      patientCategoryId: this.patientForm.patientCategoryId,
      gender: this.normalizeOptional(this.patientForm.gender),
      dateOfBirth: this.normalizeOptional(this.patientForm.dateOfBirth),
      address: this.normalizeOptional(this.patientForm.address),
      bloodGroup: this.normalizeOptional(this.patientForm.bloodGroup),
      emergencyContact: this.normalizeOptional(this.patientForm.emergencyContact),
      notes: this.normalizeOptional(this.patientForm.notes),
      isActive: this.patientForm.isActive
    };

    this.phase3.updatePatient(selected.patientId, payload).subscribe({
      next: (updated) => {
        this.upsertPatient(updated);
        this.isSaving = false;
        this.statusMessage = 'Patient record updated.';
        this.router.navigate(['/admin/patients', updated.patientId]);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSaving = false;
        this.errorMessage = error?.error?.message || 'Unable to update patient record.';
      }
    });
  }

  mergeDuplicate(): void {
    const selected = this.selectedPatient;
    if (!selected || !this.mergeSourceId) {
      this.errorMessage = 'Select the duplicate record you want to merge.';
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.phase3.mergePatients({
      sourcePatientId: this.mergeSourceId,
      targetPatientId: selected.patientId,
      notes: this.normalizeOptional(this.mergeNotes)
    }).subscribe({
      next: (updated) => {
        this.isSaving = false;
        this.statusMessage = 'Duplicate record merged into the selected patient.';
        this.mergeNotes = '';
        this.router.navigate(['/admin/patients', updated.patientId]);
        this.loadData(updated.patientId);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSaving = false;
        this.errorMessage = error?.error?.message || 'Unable to merge patient records.';
      }
    });
  }

  refresh(): void {
    this.loadData(this.selectedPatientId);
  }

  categoryLabel(patient: PatientRecord): string {
    return patient.patientCategoryName || 'Unassigned';
  }

  private syncRoute(url: string): void {
    const parts = url.split('?')[0].split('/').filter(Boolean);
    const routeSegment = parts[2] ?? null;
    const nextPatientId = this.parseId(routeSegment);

    if (routeSegment === 'create') {
      this.viewMode = 'create';
      this.selectedPatientId = null;
      this.patientForm = { ...EMPTY_PATIENT_FORM };
      this.temporaryPassword = '';
      this.mergeSourceId = null;
    } else if (nextPatientId && parts[3] === 'edit') {
      this.viewMode = 'edit';
      this.selectedPatientId = nextPatientId;
    } else if (nextPatientId) {
      this.viewMode = 'details';
      this.selectedPatientId = nextPatientId;
    } else {
      this.viewMode = 'index';
      this.selectedPatientId = null;
    }

    this.syncFormFromSelection();
  }

  private loadData(preferredPatientId?: number | null): void {
    this.isLoading = true;
    this.errorMessage = '';

    forkJoin({
      patients: this.phase3.getPatients('', null, true),
      categories: this.masterSetup.getPatientCategories()
    }).subscribe({
      next: ({ patients, categories }) => {
        this.patients = patients;
        this.categories = categories.filter((category) => category.isActive);
        this.selectedPatientId = this.resolveSelectedPatientId(patients, preferredPatientId);
        this.syncFormFromSelection();
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load patient registry right now.';
      }
    });
  }

  private resolveSelectedPatientId(patients: PatientRecord[], preferredPatientId?: number | null): number | null {
    if (this.isIndexView || this.isCreateView) {
      return null;
    }

    if (preferredPatientId && patients.some((patient) => patient.patientId === preferredPatientId)) {
      return preferredPatientId;
    }

    return patients.find((patient) => !patient.isMerged)?.patientId ?? patients[0]?.patientId ?? null;
  }

  private upsertPatient(updated: PatientRecord): void {
    const existingIndex = this.patients.findIndex((patient) => patient.patientId === updated.patientId);

    if (existingIndex === -1) {
      this.patients = [updated, ...this.patients];
      return;
    }

    this.patients = this.patients.map((patient) => patient.patientId === updated.patientId ? updated : patient);
  }

  private syncFormFromSelection(): void {
    if (this.isCreateView) {
      this.patientForm = { ...EMPTY_PATIENT_FORM };
      this.temporaryPassword = '';
      this.mergeSourceId = null;
      return;
    }

    const patient = this.selectedPatient;
    if (!patient) {
      this.patientForm = { ...EMPTY_PATIENT_FORM };
      this.temporaryPassword = '';
      this.mergeSourceId = null;
      return;
    }

    this.patientForm = {
      fullName: patient.fullName,
      email: patient.email,
      phone: patient.phone ?? '',
      patientCategoryId: patient.patientCategoryId ?? null,
      gender: patient.gender ?? '',
      dateOfBirth: patient.dateOfBirth ? patient.dateOfBirth.slice(0, 10) : '',
      address: patient.address ?? '',
      bloodGroup: patient.bloodGroup ?? '',
      emergencyContact: patient.emergencyContact ?? '',
      notes: patient.notes ?? '',
      isActive: patient.isActive
    };

    this.mergeSourceId = this.duplicateCandidates[0]?.patientId ?? null;
    this.temporaryPassword = '';
  }

  private buildDuplicateKey(patient: PatientRecord): string {
    const fullName = patient.fullName.trim().toLowerCase();
    const dateOfBirth = patient.dateOfBirth?.slice(0, 10) ?? '';
    return fullName && dateOfBirth ? `${fullName}|${dateOfBirth}` : '';
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
