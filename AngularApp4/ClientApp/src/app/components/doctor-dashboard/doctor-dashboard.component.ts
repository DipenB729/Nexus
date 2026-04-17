import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { Subscription, filter, interval } from 'rxjs';
import { AppNotification, AuthSession, DoctorAppointment } from '../../core/models/hms/auth.model';
import {
  DiagnosticRequestRecord,
  DoctorAvailabilityExceptionRecord,
  DoctorBlockedSlotRecord,
  DoctorScheduleRecord,
  DoctorWorkspaceAppointmentDetail,
  LabTestSearchResult,
  MedicineSearchResult,
  PrescriptionItemRecord
} from '../../core/models/hms/doctor-workspace.model';
import { DoctorAvailableSlot } from '../../core/models/hms/master-setup.model';
import { AppointmentService } from '../../core/services/appointment.service';
import { AuthApiService } from '../../core/services/hms/auth-api.service';
import { DoctorWorkspaceService } from '../../core/services/hms/doctor-workspace.service';
import { MasterSetupService } from '../../core/services/hms/master-setup.service';
import { NotificationsService } from '../../core/services/notifications.service';

type AppointmentCategory = 'today' | 'upcoming' | 'completed' | 'cancelled' | 'no-show';

interface ScheduleFormState {
  scheduleId: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  breakStartTime: string;
  breakEndTime: string;
  slotDurationMinutes: number;
  maxPatientsPerSlot: number;
  onlineBookingEnabled: boolean;
  isActive: boolean;
}

interface AvailabilityExceptionFormState {
  exceptionType: string;
  startDate: string;
  endDate: string;
  notes: string;
  isActive: boolean;
}

interface BlockedSlotFormState {
  blockDate: string;
  startTime: string;
  endTime: string;
  reason: string;
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

interface RescheduleFormState {
  appointmentDate: string;
  scheduleId: number | null;
  slotStartTime: string;
  slotEndTime: string;
  reason: string;
  adminRemarks: string;
}

const EMPTY_SCHEDULE_FORM: ScheduleFormState = {
  scheduleId: 0,
  dayOfWeek: 1,
  startTime: '09:00',
  endTime: '12:00',
  breakStartTime: '',
  breakEndTime: '',
  slotDurationMinutes: 30,
  maxPatientsPerSlot: 1,
  onlineBookingEnabled: true,
  isActive: true
};

const EMPTY_EXCEPTION_FORM: AvailabilityExceptionFormState = {
  exceptionType: 'Leave',
  startDate: new Date().toISOString().slice(0, 10),
  endDate: new Date().toISOString().slice(0, 10),
  notes: '',
  isActive: true
};

const EMPTY_BLOCKED_SLOT_FORM: BlockedSlotFormState = {
  blockDate: new Date().toISOString().slice(0, 10),
  startTime: '10:00',
  endTime: '10:30',
  reason: '',
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

const EMPTY_RESCHEDULE_FORM: RescheduleFormState = {
  appointmentDate: '',
  scheduleId: null,
  slotStartTime: '',
  slotEndTime: '',
  reason: '',
  adminRemarks: ''
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
  blockedSlots: DoctorBlockedSlotRecord[] = [];
  medicineResults: MedicineSearchResult[] = [];
  labTestResults: LabTestSearchResult[] = [];
  notifications: AppNotification[] = [];
  rescheduleSlots: DoctorAvailableSlot[] = [];

  searchTerm = '';
  appointmentDateFilter = '';
  appointmentStatusFilter = 'All';
  medicineSearchTerm = '';
  labSearchTerm = '';
  activeAppointmentCategory: AppointmentCategory = 'today';
  upcomingActionAppointmentId: number | null = null;
  isLoadingRescheduleSlots = false;
  isLoading = true;
  isLoadingDetail = false;
  isLoadingAvailability = false;
  isLoadingNotifications = false;
  isSavingSchedule = false;
  isSavingConsultation = false;
  isSavingPrescription = false;
  isSavingRequest = false;
  isUpdatingAppointmentStatus = false;
  isManagingUpcomingAppointment = false;
  statusMessage = '';
  errorMessage = '';

  scheduleForm: ScheduleFormState = { ...EMPTY_SCHEDULE_FORM };
  availabilityExceptionForm: AvailabilityExceptionFormState = { ...EMPTY_EXCEPTION_FORM };
  blockedSlotForm: BlockedSlotFormState = { ...EMPTY_BLOCKED_SLOT_FORM };
  consultationForm: ConsultationFormState = { ...EMPTY_CONSULTATION_FORM };
  prescriptionForm: PrescriptionFormState = { ...EMPTY_PRESCRIPTION_FORM };
  diagnosticRequestForm: DiagnosticRequestFormState = { ...EMPTY_REQUEST_FORM };
  rescheduleForm: RescheduleFormState = { ...EMPTY_RESCHEDULE_FORM };
  cancelReason = '';

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
  readonly appointmentStatusFilters = ['All', 'Pending', 'Approved', 'Rescheduled', 'Cancelled', 'Completed', 'NoShow'];
  readonly appointmentCategories: Array<{ key: AppointmentCategory; label: string }> = [
    { key: 'today', label: 'Today appointments' },
    { key: 'upcoming', label: 'Upcoming appointments' },
    { key: 'completed', label: 'Completed appointments' },
    { key: 'cancelled', label: 'Cancelled appointments' },
    { key: 'no-show', label: 'No-show appointments' }
  ];

  private sessionSub?: Subscription;
  private routeSub?: Subscription;
  private notificationsRefreshSub?: Subscription;
  private notificationsPollingSub?: Subscription;

  constructor(
    private readonly appointmentsApi: AppointmentService,
    private readonly auth: AuthApiService,
    private readonly workspaceApi: DoctorWorkspaceService,
    private readonly masterSetupApi: MasterSetupService,
    private readonly notificationsApi: NotificationsService,
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

    this.notificationsRefreshSub = this.notificationsApi.refresh$.subscribe(() => {
      this.loadNotifications();
    });

    this.notificationsPollingSub = interval(30000).subscribe(() => {
      this.loadNotifications();
    });

    this.syncRoute();
    this.loadAppointments();
    this.loadAvailability();
    this.loadNotifications();
  }

  ngOnDestroy(): void {
    this.sessionSub?.unsubscribe();
    this.routeSub?.unsubscribe();
    this.notificationsRefreshSub?.unsubscribe();
    this.notificationsPollingSub?.unsubscribe();
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

  get isAvailabilityRoute(): boolean {
    return this.router.url.startsWith('/doctor/availability');
  }

  get selectedAppointmentId(): number | null {
    const value = this.route.snapshot.paramMap.get('appointmentId');
    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  }

  get todayAppointmentsCount(): number {
    return this.appointmentsByCategory('today').length;
  }

  get upcomingAppointmentsCount(): number {
    return this.appointmentsByCategory('upcoming').length;
  }

  get activeQueueCount(): number {
    return this.appointments.filter((appointment) => ['Pending', 'Approved', 'Rescheduled'].includes(appointment.status)).length;
  }

  get completedAppointmentsCount(): number {
    return this.appointments.filter((appointment) => appointment.status === 'Completed').length;
  }

  get cancelledAppointmentsCount(): number {
    return this.appointments.filter((appointment) => appointment.status === 'Cancelled').length;
  }

  get noShowAppointmentsCount(): number {
    return this.appointments.filter((appointment) => appointment.status === 'NoShow').length;
  }

  get categoryAppointments(): DoctorAppointment[] {
    return this.appointmentsByCategory(this.activeAppointmentCategory);
  }

  get filteredAppointments(): DoctorAppointment[] {
    const term = this.searchTerm.trim().toLowerCase();
    return this.categoryAppointments.filter((appointment) => {
      const matchesSearch = !term || [
        appointment.patientName,
        appointment.medicalRecordNumber,
        appointment.patientGender,
        appointment.reason,
        appointment.serviceName,
        appointment.status,
        appointment.tokenNumber
      ]
        .map((value) => String(value ?? '').toLowerCase())
        .some((value) => value.includes(term));

      const matchesDate = !this.appointmentDateFilter || appointment.appointmentDate.slice(0, 10) === this.appointmentDateFilter;
      const matchesStatus = this.appointmentStatusFilter === 'All' || appointment.status === this.appointmentStatusFilter;
      return matchesSearch && matchesDate && matchesStatus;
    });
  }

  get upcomingDateGroups(): Array<{ date: string; items: DoctorAppointment[] }> {
    if (this.activeAppointmentCategory !== 'upcoming') {
      return [];
    }

    const groups = new Map<string, DoctorAppointment[]>();
    this.filteredAppointments.forEach((appointment) => {
      const key = appointment.appointmentDate.slice(0, 10);
      const bucket = groups.get(key) ?? [];
      bucket.push(appointment);
      groups.set(key, bucket);
    });

    return [...groups.entries()]
      .sort(([left], [right]) => left.localeCompare(right))
      .map(([date, items]) => ({ date, items }));
  }

  get latestPatientHistory(): Array<{ label: string; value: string }> {
    if (!this.selectedDetail) {
      return [];
    }

    return [
      { label: 'Previous diagnosis', value: this.selectedDetail.pastAppointments[0]?.diagnosis || 'No previous diagnosis recorded' },
      { label: 'Lab history', value: this.selectedDetail.diagnosticRequests[0]?.requestedItemName || 'No lab or radiology history recorded' },
      { label: 'Chronic conditions', value: this.selectedDetail.clinicalProfile.chronicConditions || 'No chronic conditions recorded' },
      { label: 'Admission history', value: this.selectedDetail.admissionHistory[0]?.reason || 'No admission history recorded' }
    ];
  }

  openAppointment(appointmentId: number): void {
    void this.router.navigate(['/doctor/appointments', appointmentId]);
  }

  startUpcomingAction(appointment: DoctorAppointment): void {
    this.upcomingActionAppointmentId = appointment.appointmentId;
    this.cancelReason = appointment.adminRemarks || '';
    this.rescheduleSlots = [];
    this.rescheduleForm = {
      appointmentDate: appointment.appointmentDate.slice(0, 10),
      scheduleId: null,
      slotStartTime: this.toTimeInput(appointment.slotStartTime),
      slotEndTime: this.toTimeInput(appointment.slotEndTime),
      reason: appointment.reason || '',
      adminRemarks: appointment.adminRemarks || ''
    };
    this.loadRescheduleSlots(appointment.doctorId, this.rescheduleForm.appointmentDate, appointment);
  }

  closeUpcomingAction(): void {
    this.upcomingActionAppointmentId = null;
    this.cancelReason = '';
    this.rescheduleSlots = [];
    this.rescheduleForm = { ...EMPTY_RESCHEDULE_FORM };
  }

  isManagingAppointment(appointmentId: number): boolean {
    return this.upcomingActionAppointmentId === appointmentId;
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
      breakStartTime: this.toTimeInput(schedule.breakStartTime || ''),
      breakEndTime: this.toTimeInput(schedule.breakEndTime || ''),
      slotDurationMinutes: schedule.slotDurationMinutes,
      maxPatientsPerSlot: schedule.maxPatientsPerSlot,
      onlineBookingEnabled: schedule.onlineBookingEnabled,
      isActive: schedule.isActive
    };
  }

  resetScheduleForm(): void {
    this.scheduleForm = { ...EMPTY_SCHEDULE_FORM };
  }

  saveSchedule(): void {
    const validationError = this.validateScheduleForm();
    if (validationError) {
      this.errorMessage = validationError;
      this.statusMessage = '';
      return;
    }

    this.isSavingSchedule = true;
    this.errorMessage = '';
    this.statusMessage = '';

    const payload = {
      dayOfWeek: this.scheduleForm.dayOfWeek,
      startTime: this.toTimePayload(this.scheduleForm.startTime),
      endTime: this.toTimePayload(this.scheduleForm.endTime),
      breakStartTime: this.scheduleForm.breakStartTime ? this.toTimePayload(this.scheduleForm.breakStartTime) : null,
      breakEndTime: this.scheduleForm.breakEndTime ? this.toTimePayload(this.scheduleForm.breakEndTime) : null,
      slotDurationMinutes: this.scheduleForm.slotDurationMinutes,
      maxPatientsPerSlot: this.scheduleForm.maxPatientsPerSlot,
      onlineBookingEnabled: this.scheduleForm.onlineBookingEnabled,
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
      error: (error: unknown) => {
        this.isSavingSchedule = false;
        this.errorMessage = this.extractApiError(error, 'Unable to save doctor schedule.');
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
      error: (error: unknown) => {
        this.isSavingSchedule = false;
        this.errorMessage = this.extractApiError(error, 'Unable to save leave or unavailable date.');
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
          this.selectedDetail = {
            ...this.selectedDetail,
            consultation,
            status: consultation.status === 'Completed' ? 'Completed' : this.selectedDetail.status
          };
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

  categoryCount(category: AppointmentCategory): number {
    return this.appointmentsByCategory(category).length;
  }

  setAppointmentCategory(category: AppointmentCategory): void {
    this.activeAppointmentCategory = category;
  }

  patientAgeGender(appointment: DoctorAppointment): string {
    const parts = [this.calculateAgeLabel(appointment.patientDateOfBirth), appointment.patientGender || null].filter(Boolean);
    return parts.length > 0 ? parts.join(' / ') : 'Age and gender not recorded';
  }

  detailAgeGender(detail: DoctorWorkspaceAppointmentDetail): string {
    const parts = [this.calculateAgeLabel(detail.dateOfBirth), detail.gender || null].filter(Boolean);
    return parts.length > 0 ? parts.join(' / ') : 'Age and gender not recorded';
  }

  appointmentStatusClass(status: string): string {
    return status.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-');
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

  updateAppointmentStatus(status: 'Completed' | 'NoShow'): void {
    if (!this.selectedAppointmentId) {
      return;
    }

    this.isUpdatingAppointmentStatus = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.appointmentsApi.updateDoctorAppointmentStatus(this.selectedAppointmentId, status).subscribe({
      next: (appointment) => {
        this.isUpdatingAppointmentStatus = false;
        this.statusMessage = `Appointment marked as ${status === 'NoShow' ? 'no-show' : 'completed'}.`;
        this.appointments = this.appointments.map((item) =>
          item.appointmentId === appointment.appointmentId ? appointment : item
        );
        if (this.selectedDetail) {
          this.selectedDetail = { ...this.selectedDetail, status: appointment.status };
          if (status === 'Completed') {
            this.consultationForm.status = 'Completed';
          }
        }
      },
      error: (error: { error?: { message?: string } }) => {
        this.isUpdatingAppointmentStatus = false;
        this.errorMessage = error?.error?.message || 'Unable to update appointment status.';
      }
    });
  }

  approveAppointment(appointment: DoctorAppointment): void {
    this.manageUpcomingAppointment(appointment, {
      status: 'Approved',
      adminRemarks: appointment.adminRemarks || null
    }, 'Appointment approved.');
  }

  approveSelectedAppointment(): void {
    if (!this.selectedDetail) {
      return;
    }

    this.manageSelectedAppointment({
      status: 'Approved',
      adminRemarks: this.normalizeOptional(this.cancelReason)
    }, 'Appointment approved.');
  }

  rejectSelectedAppointment(): void {
    if (!this.selectedDetail) {
      return;
    }

    const reason = this.cancelReason.trim();
    if (!reason) {
      this.errorMessage = 'Add a rejection reason before rejecting the appointment.';
      return;
    }

    this.manageSelectedAppointment({
      status: 'Cancelled',
      adminRemarks: reason
    }, 'Appointment rejected.');
  }

  rejectAppointment(appointment: DoctorAppointment): void {
    const reason = this.cancelReason.trim();
    if (!reason) {
      this.errorMessage = 'Add a rejection reason before rejecting the appointment.';
      return;
    }

    this.manageUpcomingAppointment(appointment, {
      status: 'Cancelled',
      adminRemarks: reason
    }, 'Appointment rejected.');
  }

  cancelAppointment(appointment: DoctorAppointment): void {
    const reason = this.cancelReason.trim();
    if (!reason) {
      this.errorMessage = 'Add a cancellation reason before cancelling the appointment.';
      return;
    }

    this.manageUpcomingAppointment(appointment, {
      status: 'Cancelled',
      adminRemarks: reason
    }, 'Appointment cancelled.');
  }

  onRescheduleDateChange(appointment: DoctorAppointment): void {
    this.rescheduleForm.scheduleId = null;
    this.rescheduleForm.slotStartTime = '';
    this.rescheduleForm.slotEndTime = '';
    this.loadRescheduleSlots(appointment.doctorId, this.rescheduleForm.appointmentDate, appointment);
  }

  applyRescheduleSlot(slot: DoctorAvailableSlot): void {
    this.rescheduleForm.scheduleId = slot.scheduleId ?? null;
    this.rescheduleForm.slotStartTime = this.toTimeInput(slot.startTime);
    this.rescheduleForm.slotEndTime = this.toTimeInput(slot.endTime);
  }

  saveReschedule(appointment: DoctorAppointment): void {
    if (!this.rescheduleForm.appointmentDate || !this.rescheduleForm.slotStartTime || !this.rescheduleForm.slotEndTime) {
      this.errorMessage = 'Choose a valid reschedule date and slot.';
      return;
    }

    this.manageUpcomingAppointment(appointment, {
      status: 'Rescheduled',
      appointmentDate: this.rescheduleForm.appointmentDate,
      scheduleId: this.rescheduleForm.scheduleId,
      slotStartTime: this.toTimePayload(this.rescheduleForm.slotStartTime),
      slotEndTime: this.toTimePayload(this.rescheduleForm.slotEndTime),
      reason: this.normalizeOptional(this.rescheduleForm.reason),
      adminRemarks: this.normalizeOptional(this.rescheduleForm.adminRemarks)
    }, 'Appointment rescheduled.');
  }

  startConsultation(): void {
    this.consultationForm.status = this.consultationForm.status || 'Draft';
    this.scrollToSection('consultation-section');
  }

  jumpToNotes(): void {
    this.scrollToSection('consultation-section');
  }

  jumpToDiagnostics(): void {
    this.scrollToSection('diagnostics-section');
  }

  jumpToPrescription(): void {
    this.scrollToSection('prescription-section');
  }

  saveBlockedSlot(): void {
    this.isSavingSchedule = true;
    this.workspaceApi.createBlockedSlot({
      blockDate: this.blockedSlotForm.blockDate,
      startTime: this.toTimePayload(this.blockedSlotForm.startTime),
      endTime: this.toTimePayload(this.blockedSlotForm.endTime),
      reason: this.normalizeOptional(this.blockedSlotForm.reason),
      isActive: this.blockedSlotForm.isActive
    }).subscribe({
      next: () => {
        this.isSavingSchedule = false;
        this.statusMessage = 'Blocked slot saved.';
        this.blockedSlotForm = { ...EMPTY_BLOCKED_SLOT_FORM };
        this.loadAvailability();
      },
      error: (error: unknown) => {
        this.isSavingSchedule = false;
        this.errorMessage = this.extractApiError(error, 'Unable to save blocked slot.');
      }
    });
  }

  deleteBlockedSlot(blockedSlotId: number): void {
    this.isSavingSchedule = true;
    this.workspaceApi.deleteBlockedSlot(blockedSlotId).subscribe({
      next: () => {
        this.isSavingSchedule = false;
        this.statusMessage = 'Blocked slot deleted.';
        this.loadAvailability();
      },
      error: () => {
        this.isSavingSchedule = false;
        this.errorMessage = 'Unable to delete blocked slot.';
      }
    });
  }

  markNotificationAsRead(notification: AppNotification): void {
    if (notification.isRead) {
      return;
    }

    this.notificationsApi.markAsRead(notification.appNotificationId).subscribe({
      next: () => {
        this.notifications = this.notifications.map((item) =>
          item.appNotificationId === notification.appNotificationId ? { ...item, isRead: true } : item
        );
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
        this.blockedSlots = workspace.blockedSlots;
        this.isLoadingAvailability = false;
      },
      error: (error: { error?: { message?: string } }) => {
        this.isLoadingAvailability = false;
        this.errorMessage = error?.error?.message || 'Unable to load doctor availability.';
      }
    });
  }

  private loadNotifications(): void {
    this.isLoadingNotifications = true;
    this.notificationsApi.getNotifications(8).subscribe({
      next: (items) => {
        this.notifications = items;
        this.isLoadingNotifications = false;
      },
      error: () => {
        this.notifications = [];
        this.isLoadingNotifications = false;
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

  private appointmentsByCategory(category: AppointmentCategory): DoctorAppointment[] {
    const today = new Date().toISOString().slice(0, 10);
    return this.appointments.filter((appointment) => {
      const appointmentDay = appointment.appointmentDate.slice(0, 10);
      switch (category) {
        case 'today':
          return appointmentDay === today;
        case 'upcoming':
          return appointmentDay > today && !['Cancelled', 'Completed', 'NoShow'].includes(appointment.status);
        case 'completed':
          return appointment.status === 'Completed';
        case 'cancelled':
          return appointment.status === 'Cancelled';
        case 'no-show':
          return appointment.status === 'NoShow';
        default:
          return false;
      }
    });
  }

  private calculateAgeLabel(dateOfBirth?: string | null): string | null {
    if (!dateOfBirth) {
      return null;
    }

    const dob = new Date(dateOfBirth);
    if (Number.isNaN(dob.getTime())) {
      return null;
    }

    const today = new Date();
    let age = today.getFullYear() - dob.getFullYear();
    const monthDiff = today.getMonth() - dob.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
      age -= 1;
    }

    return age >= 0 ? `${age}y` : null;
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

  private validateScheduleForm(): string | null {
    if (!this.scheduleForm.startTime || !this.scheduleForm.endTime) {
      return 'Start time and end time are required.';
    }

    if (this.scheduleForm.endTime <= this.scheduleForm.startTime) {
      return 'End time must be after start time.';
    }

    if ((this.scheduleForm.breakStartTime && !this.scheduleForm.breakEndTime) || (!this.scheduleForm.breakStartTime && this.scheduleForm.breakEndTime)) {
      return 'Break start and end time must both be provided.';
    }

    if (this.scheduleForm.breakStartTime && this.scheduleForm.breakEndTime) {
      if (this.scheduleForm.breakEndTime <= this.scheduleForm.breakStartTime) {
        return 'Break end time must be after break start time.';
      }

      if (this.scheduleForm.breakStartTime <= this.scheduleForm.startTime || this.scheduleForm.breakEndTime >= this.scheduleForm.endTime) {
        return 'Break time must fall inside the working schedule.';
      }
    }

    if (this.scheduleForm.slotDurationMinutes < 5) {
      return 'Slot duration must be at least 5 minutes.';
    }

    if (this.scheduleForm.maxPatientsPerSlot < 1) {
      return 'Max patients per slot must be at least 1.';
    }

    return null;
  }

  private extractApiError(error: unknown, fallback: string): string {
    const payload = (error as { error?: { message?: string; errors?: unknown } })?.error;
    const message = payload?.message?.trim();
    if (message) {
      return message;
    }

    const errors = payload?.errors;
    if (Array.isArray(errors)) {
      const first = errors.find((item) => typeof item === 'string' && item.trim().length > 0);
      if (typeof first === 'string') {
        return first;
      }
    }

    if (errors && typeof errors === 'object') {
      const values = Object.values(errors as Record<string, unknown>);
      for (const value of values) {
        if (typeof value === 'string' && value.trim().length > 0) {
          return value;
        }

        if (Array.isArray(value)) {
          const firstString = value.find((item): item is string => typeof item === 'string' && item.trim().length > 0);
          if (firstString) {
            return firstString;
          }
        }
      }
    }

    const topLevelMessage = (error as { message?: string })?.message?.trim();
    return topLevelMessage || fallback;
  }

  private loadRescheduleSlots(doctorId: number, date: string, appointment: DoctorAppointment): void {
    if (!date) {
      this.rescheduleSlots = [];
      return;
    }

    this.isLoadingRescheduleSlots = true;
    this.masterSetupApi.getAvailableSlots(doctorId, date).subscribe({
      next: (slots) => {
        const currentSlot: DoctorAvailableSlot = {
          scheduleId: appointment.scheduleId ?? null,
          startTime: appointment.slotStartTime,
          endTime: appointment.slotEndTime,
          maxPatientsPerSlot: 1,
          bookedPatients: 0,
          remainingPatients: 1
        };

        const hasCurrentSlot = slots.some((slot) => slot.startTime === appointment.slotStartTime && slot.endTime === appointment.slotEndTime);
        this.rescheduleSlots = hasCurrentSlot ? slots : [currentSlot, ...slots];
        this.isLoadingRescheduleSlots = false;
      },
      error: () => {
        this.rescheduleSlots = [];
        this.isLoadingRescheduleSlots = false;
        this.errorMessage = 'Unable to load available slots for rescheduling.';
      }
    });
  }

  private manageUpcomingAppointment(
    appointment: DoctorAppointment,
    payload: {
      status: 'Approved' | 'Rescheduled' | 'Cancelled' | 'Completed' | 'NoShow';
      scheduleId?: number | null;
      appointmentDate?: string | null;
      slotStartTime?: string | null;
      slotEndTime?: string | null;
      reason?: string | null;
      adminRemarks?: string | null;
    },
    successMessage: string
  ): void {
    this.isManagingUpcomingAppointment = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.appointmentsApi.manageDoctorAppointment(appointment.appointmentId, payload).subscribe({
      next: (updated) => {
        this.isManagingUpcomingAppointment = false;
        this.statusMessage = successMessage;
        this.appointments = this.appointments.map((item) => item.appointmentId === updated.appointmentId ? updated : item);
        this.closeUpcomingAction();
        this.loadNotifications();
        this.notificationsApi.requestRefresh();
      },
      error: (error: { error?: { message?: string } }) => {
        this.isManagingUpcomingAppointment = false;
        this.errorMessage = error?.error?.message || 'Unable to update appointment.';
      }
    });
  }

  private scrollToSection(elementId: string): void {
    const target = document.getElementById(elementId);
    target?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  private manageSelectedAppointment(
    payload: {
      status: 'Approved' | 'Rescheduled' | 'Cancelled' | 'Completed' | 'NoShow';
      scheduleId?: number | null;
      appointmentDate?: string | null;
      slotStartTime?: string | null;
      slotEndTime?: string | null;
      reason?: string | null;
      adminRemarks?: string | null;
    },
    successMessage: string
  ): void {
    if (!this.selectedDetail) {
      return;
    }

    this.isManagingUpcomingAppointment = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.appointmentsApi.manageDoctorAppointment(this.selectedDetail.appointmentId, payload).subscribe({
      next: (updated) => {
        this.isManagingUpcomingAppointment = false;
        this.statusMessage = successMessage;
        this.appointments = this.appointments.map((item) => item.appointmentId === updated.appointmentId ? updated : item);
        this.selectedDetail = {
          ...this.selectedDetail!,
          appointmentDate: updated.appointmentDate,
          slotStartTime: updated.slotStartTime,
          slotEndTime: updated.slotEndTime,
          status: updated.status,
          tokenNumber: updated.tokenNumber,
          reason: updated.reason,
          adminRemarks: updated.adminRemarks
        };
        this.loadNotifications();
        this.notificationsApi.requestRefresh();
      },
      error: (error: { error?: { message?: string } }) => {
        this.isManagingUpcomingAppointment = false;
        this.errorMessage = this.extractApiError(error, 'Unable to update appointment.');
      }
    });
  }
}
