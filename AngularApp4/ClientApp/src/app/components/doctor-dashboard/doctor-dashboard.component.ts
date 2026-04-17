import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthSession, DoctorAppointment } from '../../core/models/hms/auth.model';
import {
  DiagnosticRequestRecord,
  DoctorAvailabilityExceptionRecord,
  DoctorScheduleRecord,
  DoctorWorkspaceAppointmentDetail,
  LabTestSearchResult,
  MedicineSearchResult,
  PrescriptionItemRecord
} from '../../core/models/hms/doctor-workspace.model';
import { AppointmentService } from '../../core/services/appointment.service';
import { AuthApiService } from '../../core/services/hms/auth-api.service';
import { DoctorWorkspaceService } from '../../core/services/hms/doctor-workspace.service';

interface ScheduleFormState {
  scheduleId: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  slotDurationMinutes: number;
  maxPatientsPerSlot: number;
  isActive: boolean;
}

interface AvailabilityExceptionFormState {
  exceptionType: string;
  startDate: string;
  endDate: string;
  notes: string;
  isActive: boolean;
}

interface ConsultationFormState {
  symptoms: string;
  diagnosis: string;
  notes: string;
  vitalObservations: string;
  advice: string;
  followUpDate: string;
  status: string;
}

interface PrescriptionFormState {
  notes: string;
  items: PrescriptionItemRecord[];
}

interface DiagnosticRequestFormState {
  requestType: string;
  labTestMasterId: number | null;
  requestedItemName: string;
  remarks: string;
}

type DoctorAppointmentStatusView = Pick<DoctorAppointment, 'appointmentId' | 'status'>;

const EMPTY_SCHEDULE_FORM: ScheduleFormState = {
  scheduleId: 0,
  dayOfWeek: 1,
  startTime: '09:00',
  endTime: '12:00',
  slotDurationMinutes: 30,
  maxPatientsPerSlot: 1,
  isActive: true
};

const EMPTY_EXCEPTION_FORM: AvailabilityExceptionFormState = {
  exceptionType: 'Leave',
  startDate: new Date().toISOString().slice(0, 10),
  endDate: new Date().toISOString().slice(0, 10),
  notes: '',
  isActive: true
};

const EMPTY_CONSULTATION_FORM: ConsultationFormState = {
  symptoms: '',
  diagnosis: '',
  notes: '',
  vitalObservations: '',
  advice: '',
  followUpDate: '',
  status: 'Draft'
};

const EMPTY_PRESCRIPTION_ITEM: PrescriptionItemRecord = {
  medicineName: '',
  dosage: '',
  frequency: '',
  duration: '',
  instructions: ''
};

const EMPTY_PRESCRIPTION_FORM: PrescriptionFormState = {
  notes: '',
  items: [{ ...EMPTY_PRESCRIPTION_ITEM }]
};

const EMPTY_REQUEST_FORM: DiagnosticRequestFormState = {
  requestType: 'Lab',
  labTestMasterId: null,
  requestedItemName: '',
  remarks: ''
};

@Component({
  selector: 'app-doctor-dashboard',
  templateUrl: './doctor-dashboard.component.html',
  styleUrls: ['./doctor-dashboard.component.scss']
})
export class DoctorDashboardComponent implements OnInit, OnDestroy {
  session: AuthSession | null = null;
  appointments: DoctorAppointment[] = [];
  selectedDetail: DoctorWorkspaceAppointmentDetail | null = null;
  schedules: DoctorScheduleRecord[] = [];
  availabilityExceptions: DoctorAvailabilityExceptionRecord[] = [];
  medicineResults: MedicineSearchResult[] = [];
  labTestResults: LabTestSearchResult[] = [];

  searchTerm = '';
  medicineSearchTerm = '';
  labSearchTerm = '';
  isLoading = true;
  isLoadingDetail = false;
  isLoadingAvailability = false;
  isSavingSchedule = false;
  isSavingConsultation = false;
  isSavingPrescription = false;
  isSavingRequest = false;
  statusMessage = '';
  errorMessage = '';

  scheduleForm: ScheduleFormState = { ...EMPTY_SCHEDULE_FORM };
  availabilityExceptionForm: AvailabilityExceptionFormState = { ...EMPTY_EXCEPTION_FORM };
  consultationForm: ConsultationFormState = { ...EMPTY_CONSULTATION_FORM };
  prescriptionForm: PrescriptionFormState = { ...EMPTY_PRESCRIPTION_FORM };
  diagnosticRequestForm: DiagnosticRequestFormState = { ...EMPTY_REQUEST_FORM };

  readonly dayOptions = [
    { value: 1, label: 'Sunday' },
    { value: 2, label: 'Monday' },
    { value: 3, label: 'Tuesday' },
    { value: 4, label: 'Wednesday' },
    { value: 5, label: 'Thursday' },
    { value: 6, label: 'Friday' },
    { value: 7, label: 'Saturday' }
  ];
  readonly consultationStatuses = ['Draft', 'Completed', 'FollowUpPlanned'];
  readonly requestTypes = ['Lab', 'Radiology'];

  private sessionSub?: Subscription;
  private routeSub?: Subscription;

  constructor(
    private readonly appointmentsApi: AppointmentService,
    private readonly auth: AuthApiService,
    private readonly workspaceApi: DoctorWorkspaceService,
    private readonly router: Router,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.session = this.auth.getSession();
    this.sessionSub = this.auth.session$.subscribe((session) => {
      this.session = session;
    });

    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.syncRoute());

    this.syncRoute();
    this.loadAppointments();
    this.loadAvailability();
  }

  ngOnDestroy(): void {
    this.sessionSub?.unsubscribe();
    this.routeSub?.unsubscribe();
  }

  get displayName(): string {
    return this.session?.fullName ?? 'Doctor';
  }

  get email(): string {
    return this.session?.email ?? 'doctor@nexus.local';
  }

  get role(): string {
    return this.auth.getDisplayRole(this.session?.role);
  }

  get isDashboardRoute(): boolean {
    return this.router.url.startsWith('/doctor/dashboard');
  }

  get isAppointmentsRoute(): boolean {
    return this.router.url.startsWith('/doctor/appointments') && !this.selectedAppointmentId;
  }

  get selectedAppointmentId(): number | null {
    const value = this.route.snapshot.paramMap.get('appointmentId');
    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  }

  get todayAppointmentsCount(): number {
    const today = new Date().toISOString().slice(0, 10);
    return this.appointments.filter((appointment) => appointment.appointmentDate.slice(0, 10) === today).length;
  }

  get activeQueueCount(): number {
    return this.appointments.filter((appointment) => ['Pending', 'Approved', 'Rescheduled'].includes(appointment.status)).length;
  }

  get completedAppointmentsCount(): number {
    return this.appointments.filter((appointment) => appointment.status === 'Completed').length;
  }

  get filteredAppointments(): DoctorAppointment[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) {
      return this.appointments;
    }

    return this.appointments.filter((appointment) =>
      [
        appointment.patientName,
        appointment.medicalRecordNumber,
        appointment.serviceName,
        appointment.status,
        appointment.tokenNumber
      ]
        .map((value) => String(value ?? '').toLowerCase())
        .some((value) => value.includes(term)));
  }

  openAppointment(appointmentId: number): void {
    void this.router.navigate(['/doctor/appointments', appointmentId]);
  }

  backToAppointments(): void {
    this.selectedDetail = null;
    this.resetClinicalForms();
    void this.router.navigate(['/doctor/appointments']);
  }

  editSchedule(schedule: DoctorScheduleRecord): void {
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

  resetScheduleForm(): void {
    this.scheduleForm = { ...EMPTY_SCHEDULE_FORM };
  }

  saveSchedule(): void {
    this.isSavingSchedule = true;
    this.errorMessage = '';
    this.statusMessage = '';

    const payload = {
      dayOfWeek: this.scheduleForm.dayOfWeek,
      startTime: this.toTimePayload(this.scheduleForm.startTime),
      endTime: this.toTimePayload(this.scheduleForm.endTime),
      slotDurationMinutes: this.scheduleForm.slotDurationMinutes,
      maxPatientsPerSlot: this.scheduleForm.maxPatientsPerSlot,
      isActive: this.scheduleForm.isActive
    };

    const request = this.scheduleForm.scheduleId
      ? this.workspaceApi.updateSchedule(this.scheduleForm.scheduleId, payload)
      : this.workspaceApi.createSchedule(payload);

    request.subscribe({
      next: () => {
        this.isSavingSchedule = false;
        this.statusMessage = 'Availability schedule saved.';
        this.resetScheduleForm();
        this.loadAvailability();
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingSchedule = false;
        this.errorMessage = error?.error?.message || 'Unable to save doctor schedule.';
      }
    });
  }

  deleteSchedule(scheduleId: number): void {
    this.isSavingSchedule = true;
    this.workspaceApi.deleteSchedule(scheduleId).subscribe({
      next: () => {
        this.isSavingSchedule = false;
        this.statusMessage = 'Availability schedule deleted.';
        this.resetScheduleForm();
        this.loadAvailability();
      },
      error: () => {
        this.isSavingSchedule = false;
        this.errorMessage = 'Unable to delete doctor schedule.';
      }
    });
  }

  saveAvailabilityException(): void {
    this.isSavingSchedule = true;
    this.workspaceApi.createAvailabilityException({
      exceptionType: this.availabilityExceptionForm.exceptionType,
      startDate: this.availabilityExceptionForm.startDate,
      endDate: this.availabilityExceptionForm.endDate,
      notes: this.normalizeOptional(this.availabilityExceptionForm.notes),
      isActive: this.availabilityExceptionForm.isActive
    }).subscribe({
      next: () => {
        this.isSavingSchedule = false;
        this.statusMessage = 'Availability exception saved.';
        this.availabilityExceptionForm = { ...EMPTY_EXCEPTION_FORM };
        this.loadAvailability();
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingSchedule = false;
        this.errorMessage = error?.error?.message || 'Unable to save leave or unavailable date.';
      }
    });
  }

  deleteAvailabilityException(exceptionId: number): void {
    this.isSavingSchedule = true;
    this.workspaceApi.deleteAvailabilityException(exceptionId).subscribe({
      next: () => {
        this.isSavingSchedule = false;
        this.statusMessage = 'Availability exception deleted.';
        this.loadAvailability();
      },
      error: () => {
        this.isSavingSchedule = false;
        this.errorMessage = 'Unable to delete availability exception.';
      }
    });
  }

  saveConsultation(): void {
    if (!this.selectedAppointmentId) {
      return;
    }

    this.isSavingConsultation = true;
    this.workspaceApi.saveConsultation(this.selectedAppointmentId, {
      symptoms: this.normalizeOptional(this.consultationForm.symptoms),
      diagnosis: this.normalizeOptional(this.consultationForm.diagnosis),
      notes: this.normalizeOptional(this.consultationForm.notes),
      vitalObservations: this.normalizeOptional(this.consultationForm.vitalObservations),
      advice: this.normalizeOptional(this.consultationForm.advice),
      followUpDate: this.consultationForm.followUpDate || null,
      status: this.consultationForm.status
    }).subscribe({
      next: (consultation) => {
        this.isSavingConsultation = false;
        this.statusMessage = 'Consultation saved.';
        if (this.selectedDetail) {
          this.selectedDetail = { ...this.selectedDetail, consultation };
        }
        this.loadAppointments();
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingConsultation = false;
        this.errorMessage = error?.error?.message || 'Unable to save consultation.';
      }
    });
  }

  addPrescriptionItem(): void {
    this.prescriptionForm.items = [...this.prescriptionForm.items, { ...EMPTY_PRESCRIPTION_ITEM }];
  }

  removePrescriptionItem(index: number): void {
    this.prescriptionForm.items = this.prescriptionForm.items.length > 1
      ? this.prescriptionForm.items.filter((_, itemIndex) => itemIndex !== index)
      : [{ ...EMPTY_PRESCRIPTION_ITEM }];
  }

  savePrescription(): void {
    if (!this.selectedAppointmentId) {
      return;
    }

    this.isSavingPrescription = true;
    this.workspaceApi.savePrescription(this.selectedAppointmentId, {
      notes: this.normalizeOptional(this.prescriptionForm.notes),
      items: this.prescriptionForm.items
    }).subscribe({
      next: (prescription) => {
        this.isSavingPrescription = false;
        this.statusMessage = 'Prescription saved.';
        if (this.selectedDetail) {
          this.selectedDetail = {
            ...this.selectedDetail,
            previousPrescriptions: [prescription, ...this.selectedDetail.previousPrescriptions]
          };
        }
        this.prescriptionForm = { ...EMPTY_PRESCRIPTION_FORM, items: [{ ...EMPTY_PRESCRIPTION_ITEM }] };
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingPrescription = false;
        this.errorMessage = error?.error?.message || 'Unable to save prescription.';
      }
    });
  }

  saveDiagnosticRequest(): void {
    if (!this.selectedAppointmentId) {
      return;
    }

    this.isSavingRequest = true;
    this.workspaceApi.saveDiagnosticRequest(this.selectedAppointmentId, {
      requestType: this.diagnosticRequestForm.requestType,
      labTestMasterId: this.diagnosticRequestForm.requestType === 'Lab' ? this.diagnosticRequestForm.labTestMasterId : null,
      requestedItemName: this.diagnosticRequestForm.requestedItemName,
      remarks: this.normalizeOptional(this.diagnosticRequestForm.remarks)
    }).subscribe({
      next: (request) => {
        this.isSavingRequest = false;
        this.statusMessage = `${request.requestType} request saved.`;
        if (this.selectedDetail) {
          this.selectedDetail = {
            ...this.selectedDetail,
            diagnosticRequests: [request, ...this.selectedDetail.diagnosticRequests]
          };
        }
        this.diagnosticRequestForm = { ...EMPTY_REQUEST_FORM };
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingRequest = false;
        this.errorMessage = error?.error?.message || 'Unable to save diagnostic request.';
      }
    });
  }

  searchMedicines(): void {
    this.workspaceApi.searchMedicines(this.medicineSearchTerm).subscribe({
      next: (items) => {
        this.medicineResults = items;
      }
    });
  }

  applyMedicine(index: number, medicine: MedicineSearchResult): void {
    this.prescriptionForm.items = this.prescriptionForm.items.map((item, itemIndex) =>
      itemIndex === index
        ? {
            ...item,
            medicineMasterId: medicine.medicineMasterId,
            medicineName: medicine.medicineName,
            dosage: item.dosage || medicine.strength || ''
          }
        : item);
  }

  searchLabTests(): void {
    this.workspaceApi.searchLabTests(this.labSearchTerm).subscribe({
      next: (items) => {
        this.labTestResults = items;
      }
    });
  }

  applyLabTest(test: LabTestSearchResult): void {
    this.diagnosticRequestForm = {
      ...this.diagnosticRequestForm,
      requestType: 'Lab',
      labTestMasterId: test.labTestMasterId,
      requestedItemName: test.testName
    };
  }

  printPrescription(): void {
    window.print();
  }

  downloadPrescription(): void {
    const source = this.selectedDetail?.previousPrescriptions[0];
    const lines = [
      `Prescription for ${this.selectedDetail?.patientName ?? 'Patient'}`,
      `Appointment: ${this.selectedDetail?.appointmentDate ?? ''}`,
      `Doctor: ${this.displayName}`,
      ''
    ];

    (source?.items ?? this.prescriptionForm.items).forEach((item, index) => {
      lines.push(`${index + 1}. ${item.medicineName} | ${item.dosage || ''} | ${item.frequency || ''} | ${item.duration || ''}`);
      lines.push(`   Instructions: ${item.instructions || '-'}`);
    });

    const blob = new Blob([lines.join('\n')], { type: 'text/plain;charset=utf-8' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `prescription-${this.selectedAppointmentId ?? 'draft'}.txt`;
    link.click();
    URL.revokeObjectURL(link.href);
  }

  scheduleDayLabel(day: number): string {
    return this.dayOptions.find((option) => option.value === day)?.label ?? 'Unknown';
  }

  prescriptionSummary(items: PrescriptionItemRecord[]): string {
    return items.map((item) => item.medicineName).filter(Boolean).join(', ');
  }

  formatSlot(appointment: DoctorAppointment): string {
    return `${this.toTimeInput(appointment.slotStartTime)} - ${this.toTimeInput(appointment.slotEndTime)}`;
  }

  formatTime(value?: string | null): string {
    return value ? value.slice(0, 5) : '';
  }

  canApproveAppointment(appointment: DoctorAppointmentStatusView): boolean {
    return appointment.status === 'Pending' || appointment.status === 'Rescheduled';
  }

  canCancelAppointment(appointment: DoctorAppointmentStatusView): boolean {
    return appointment.status !== 'Cancelled' && appointment.status !== 'Completed';
  }

  canCompleteAppointment(appointment: DoctorAppointmentStatusView): boolean {
    return appointment.status === 'Approved' || appointment.status === 'Rescheduled';
  }

  updateAppointmentStatus(appointment: DoctorAppointmentStatusView, status: 'Approved' | 'Cancelled' | 'Completed' | 'NoShow'): void {
    this.errorMessage = '';
    this.statusMessage = '';

    this.appointmentsApi.updateDoctorAppointmentStatus(appointment.appointmentId, { status }).subscribe({
      next: (updated) => {
        this.appointments = this.appointments.map((item) =>
          item.appointmentId === updated.appointmentId ? updated : item);

        if (this.selectedDetail?.appointmentId === updated.appointmentId) {
          this.selectedDetail = {
            ...this.selectedDetail,
            status: updated.status,
            tokenNumber: updated.tokenNumber ?? this.selectedDetail.tokenNumber,
            adminRemarks: updated.adminRemarks ?? this.selectedDetail.adminRemarks
          };
        }

        this.statusMessage = `Appointment ${status.toLowerCase()} successfully.`;
      },
      error: (error: { error?: { message?: string } }) => {
        this.errorMessage = error?.error?.message || 'Unable to update appointment status.';
      }
    });
  }

  private syncRoute(): void {
    const appointmentId = this.selectedAppointmentId;
    if (appointmentId) {
      this.loadAppointmentDetail(appointmentId);
      return;
    }

    this.selectedDetail = null;
    this.resetClinicalForms();
  }

  private loadAppointments(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.appointmentsApi.getDoctorAppointments().subscribe({
      next: (appointments) => {
        this.appointments = [...appointments].sort((a, b) =>
          `${a.appointmentDate}${a.slotStartTime}`.localeCompare(`${b.appointmentDate}${b.slotStartTime}`));
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load doctor appointments right now.';
      }
    });
  }

  private loadAvailability(): void {
    this.isLoadingAvailability = true;
    this.workspaceApi.getAvailability().subscribe({
      next: (workspace) => {
        this.schedules = workspace.schedules;
        this.availabilityExceptions = workspace.exceptions;
        this.isLoadingAvailability = false;
      },
      error: () => {
        this.isLoadingAvailability = false;
        this.errorMessage = 'Unable to load doctor availability.';
      }
    });
  }

  private loadAppointmentDetail(appointmentId: number): void {
    this.isLoadingDetail = true;
    this.errorMessage = '';
    this.workspaceApi.getAppointmentDetail(appointmentId).subscribe({
      next: (detail) => {
        this.selectedDetail = detail;
        this.isLoadingDetail = false;
        this.consultationForm = {
          symptoms: detail.consultation.symptoms || '',
          diagnosis: detail.consultation.diagnosis || '',
          notes: detail.consultation.notes || '',
          vitalObservations: detail.consultation.vitalObservations || '',
          advice: detail.consultation.advice || '',
          followUpDate: detail.consultation.followUpDate?.slice(0, 10) || '',
          status: detail.consultation.status || 'Draft'
        };
      },
      error: () => {
        this.selectedDetail = null;
        this.isLoadingDetail = false;
        this.errorMessage = 'Unable to load appointment detail.';
      }
    });
  }

  private resetClinicalForms(): void {
    this.consultationForm = { ...EMPTY_CONSULTATION_FORM };
    this.prescriptionForm = { ...EMPTY_PRESCRIPTION_FORM, items: [{ ...EMPTY_PRESCRIPTION_ITEM }] };
    this.diagnosticRequestForm = { ...EMPTY_REQUEST_FORM };
    this.medicineResults = [];
    this.labTestResults = [];
  }

  private toTimePayload(value: string): string {
    return value.length === 5 ? `${value}:00` : value;
  }

  private toTimeInput(value: string): string {
    return value ? value.slice(0, 5) : '';
  }

  private normalizeOptional(value: string): string | null {
    const normalized = value.trim();
    return normalized ? normalized : null;
  }
}
