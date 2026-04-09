import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter, forkJoin } from 'rxjs';
import { DoctorMaster } from '../../core/models/hms/master-setup.model';
import {
  AppointmentAdminRecord,
  AppointmentLifecycleStatus,
  AppointmentTokenSettings,
  DoctorScheduleRecord,
  ManageAppointmentPayload
} from '../../core/models/hms/phase3-control.model';
import { MasterSetupService } from '../../core/services/hms/master-setup.service';
import { Phase3ControlService } from '../../core/services/hms/phase3-control.service';

type ViewMode = 'index' | 'details' | 'edit';

interface AppointmentFormState {
  doctorId: number | null;
  scheduleId: number | null;
  appointmentDate: string;
  slotStartTime: string;
  slotEndTime: string;
  status: AppointmentLifecycleStatus;
  adminRemarks: string;
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

const EMPTY_APPOINTMENT_FORM: AppointmentFormState = {
  doctorId: null,
  scheduleId: null,
  appointmentDate: '',
  slotStartTime: '',
  slotEndTime: '',
  status: 'Pending',
  adminRemarks: ''
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

@Component({
  selector: 'app-appointment-control',
  templateUrl: './appointment-control.component.html',
  styleUrls: ['./appointment-control.component.scss']
})
export class AppointmentControlComponent implements OnInit, OnDestroy {
  appointments: AppointmentAdminRecord[] = [];
  doctors: DoctorMaster[] = [];
  appointmentSchedules: DoctorScheduleRecord[] = [];
  schedules: DoctorScheduleRecord[] = [];
  tokenSettings: AppointmentTokenSettings = {
    appointmentTokenSettingId: 0,
    prefix: 'OPD',
    startingNumber: 1,
    numberPadding: 3,
    resetDaily: true
  };

  viewMode: ViewMode = 'index';
  selectedAppointmentId: number | null = null;
  selectedDoctorId: number | null = null;
  appointmentForm: AppointmentFormState = { ...EMPTY_APPOINTMENT_FORM };
  scheduleForm: ScheduleFormState = { ...EMPTY_SCHEDULE_FORM };

  searchTerm = '';
  statusFilter: AppointmentLifecycleStatus | 'All' = 'All';
  doctorFilter: number | null = null;

  isLoading = true;
  isSavingAppointment = false;
  isSavingTokenSettings = false;
  isSavingSchedule = false;
  errorMessage = '';
  statusMessage = '';

  readonly statusOptions: AppointmentLifecycleStatus[] = ['Pending', 'Approved', 'Rescheduled', 'Cancelled', 'Completed'];
  readonly dayOptions = [
    { value: 1, label: 'Sunday' },
    { value: 2, label: 'Monday' },
    { value: 3, label: 'Tuesday' },
    { value: 4, label: 'Wednesday' },
    { value: 5, label: 'Thursday' },
    { value: 6, label: 'Friday' },
    { value: 7, label: 'Saturday' }
  ];

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
    this.loadData(this.selectedAppointmentId);
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

  get isEditView(): boolean {
    return this.viewMode === 'edit';
  }

  get pendingCount(): number {
    return this.appointments.filter((appointment) => appointment.status === 'Pending').length;
  }

  get approvedCount(): number {
    return this.appointments.filter((appointment) => appointment.status === 'Approved' || appointment.status === 'Completed').length;
  }

  get rescheduledCount(): number {
    return this.appointments.filter((appointment) => appointment.status === 'Rescheduled').length;
  }

  get filteredAppointments(): AppointmentAdminRecord[] {
    const term = this.searchTerm.trim().toLowerCase();

    return this.appointments.filter((appointment) => {
      if (this.statusFilter !== 'All' && appointment.status !== this.statusFilter) {
        return false;
      }

      if (this.doctorFilter && appointment.doctorId !== this.doctorFilter) {
        return false;
      }

      if (!term) {
        return true;
      }

      return [
        appointment.patientName,
        appointment.medicalRecordNumber,
        appointment.doctorName,
        appointment.serviceName,
        appointment.tokenNumber,
        appointment.status
      ].some((value) => String(value ?? '').toLowerCase().includes(term));
    });
  }

  get selectedAppointment(): AppointmentAdminRecord | null {
    return this.selectedAppointmentId
      ? this.appointments.find((appointment) => appointment.appointmentId === this.selectedAppointmentId) ?? null
      : null;
  }

  get scheduleDoctor(): DoctorMaster | null {
    return this.selectedDoctorId
      ? this.doctors.find((doctor) => doctor.doctorId === this.selectedDoctorId) ?? null
      : null;
  }

  backToIndex(): void {
    this.router.navigate(['/admin/bookings']);
  }

  goToDetails(appointmentId: number): void {
    this.router.navigate(['/admin/bookings', appointmentId]);
  }

  goToEdit(appointmentId: number): void {
    this.router.navigate(['/admin/bookings', appointmentId, 'edit']);
  }

  onAppointmentDoctorChange(): void {
    this.loadAppointmentSchedules(this.appointmentForm.doctorId);
  }

  saveAppointment(): void {
    const selected = this.selectedAppointment;
    if (!selected) {
      return;
    }

    if (!this.appointmentForm.doctorId || !this.appointmentForm.appointmentDate || !this.appointmentForm.slotStartTime || !this.appointmentForm.slotEndTime) {
      this.errorMessage = 'Doctor, appointment date, and slot times are required.';
      return;
    }

    this.isSavingAppointment = true;
    this.errorMessage = '';
    this.statusMessage = '';

    const payload: ManageAppointmentPayload = {
      status: this.appointmentForm.status,
      doctorId: this.appointmentForm.doctorId,
      scheduleId: this.appointmentForm.scheduleId,
      appointmentDate: this.appointmentForm.appointmentDate,
      slotStartTime: this.toTimePayload(this.appointmentForm.slotStartTime),
      slotEndTime: this.toTimePayload(this.appointmentForm.slotEndTime),
      adminRemarks: this.normalizeOptional(this.appointmentForm.adminRemarks)
    };

    this.phase3.manageAppointment(selected.appointmentId, payload).subscribe({
      next: (updated) => {
        this.replaceAppointment(updated);
        this.isSavingAppointment = false;
        this.statusMessage = 'Appointment updated successfully.';
        this.loadAppointmentSchedules(updated.doctorId);
        this.router.navigate(['/admin/bookings', updated.appointmentId]);
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingAppointment = false;
        this.errorMessage = error?.error?.message || 'Unable to update appointment.';
      }
    });
  }

  saveTokenSettings(): void {
    if (!this.tokenSettings.prefix.trim()) {
      this.errorMessage = 'Token prefix is required.';
      return;
    }

    this.isSavingTokenSettings = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.phase3.updateTokenSettings({ ...this.tokenSettings, prefix: this.tokenSettings.prefix.trim() }).subscribe({
      next: (settings) => {
        this.tokenSettings = settings;
        this.isSavingTokenSettings = false;
        this.statusMessage = 'Token settings saved.';
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingTokenSettings = false;
        this.errorMessage = error?.error?.message || 'Unable to save token settings.';
      }
    });
  }

  changeScheduleDoctor(): void {
    this.scheduleForm = { ...EMPTY_SCHEDULE_FORM };
    this.loadSchedules();
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
    if (!this.selectedDoctorId) {
      this.errorMessage = 'Select a doctor before saving schedules.';
      return;
    }

    this.isSavingSchedule = true;
    this.errorMessage = '';
    this.statusMessage = '';

    const payload = {
      doctorId: this.selectedDoctorId,
      dayOfWeek: this.scheduleForm.dayOfWeek,
      startTime: this.toTimePayload(this.scheduleForm.startTime),
      endTime: this.toTimePayload(this.scheduleForm.endTime),
      slotDurationMinutes: this.scheduleForm.slotDurationMinutes,
      maxPatientsPerSlot: this.scheduleForm.maxPatientsPerSlot,
      isActive: this.scheduleForm.isActive
    };

    const request = this.scheduleForm.scheduleId
      ? this.phase3.updateDoctorSchedule(this.scheduleForm.scheduleId, payload)
      : this.phase3.createDoctorSchedule(this.selectedDoctorId, payload);

    request.subscribe({
      next: () => {
        this.isSavingSchedule = false;
        this.statusMessage = 'Doctor schedule saved.';
        this.resetScheduleForm();
        this.loadSchedules();
      },
      error: (error: { error?: { message?: string } }) => {
        this.isSavingSchedule = false;
        this.errorMessage = error?.error?.message || 'Unable to save doctor schedule.';
      }
    });
  }

  deleteSchedule(scheduleId: number): void {
    this.isSavingSchedule = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.phase3.deleteDoctorSchedule(scheduleId).subscribe({
      next: () => {
        this.isSavingSchedule = false;
        this.statusMessage = 'Doctor schedule deleted.';
        this.resetScheduleForm();
        this.loadSchedules();
      },
      error: () => {
        this.isSavingSchedule = false;
        this.errorMessage = 'Unable to delete doctor schedule.';
      }
    });
  }

  refresh(): void {
    this.loadData(this.selectedAppointmentId);
  }

  scheduleDayLabel(day: number): string {
    return this.dayOptions.find((option) => option.value === day)?.label ?? 'Unknown';
  }

  formatSlot(appointment: AppointmentAdminRecord): string {
    return `${this.toTimeInput(appointment.slotStartTime)} - ${this.toTimeInput(appointment.slotEndTime)}`;
  }

  private syncRoute(url: string): void {
    const parts = url.split('?')[0].split('/').filter(Boolean);
    const nextAppointmentId = this.parseId(parts[2] ?? null);

    if (nextAppointmentId && parts[3] === 'edit') {
      this.viewMode = 'edit';
      this.selectedAppointmentId = nextAppointmentId;
    } else if (nextAppointmentId) {
      this.viewMode = 'details';
      this.selectedAppointmentId = nextAppointmentId;
    } else {
      this.viewMode = 'index';
      this.selectedAppointmentId = null;
    }

    this.syncAppointmentForm();
  }

  private loadData(preferredAppointmentId?: number | null): void {
    this.isLoading = true;
    this.errorMessage = '';

    forkJoin({
      appointments: this.phase3.getAppointments(),
      doctors: this.masterSetup.getDoctors(),
      tokenSettings: this.phase3.getTokenSettings()
    }).subscribe({
      next: ({ appointments, doctors, tokenSettings }) => {
        this.appointments = appointments;
        this.doctors = doctors.filter((doctor) => doctor.isActive);
        this.tokenSettings = tokenSettings;
        this.selectedAppointmentId = this.resolveSelectedAppointmentId(appointments, preferredAppointmentId);
        this.syncAppointmentForm();
        this.selectedDoctorId = this.selectedDoctorId && this.doctors.some((doctor) => doctor.doctorId === this.selectedDoctorId)
          ? this.selectedDoctorId
          : this.selectedAppointment?.doctorId ?? this.doctors[0]?.doctorId ?? null;
        this.loadAppointmentSchedules(this.appointmentForm.doctorId);
        this.loadSchedules();
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load appointment controls right now.';
      }
    });
  }

  private resolveSelectedAppointmentId(appointments: AppointmentAdminRecord[], preferredAppointmentId?: number | null): number | null {
    if (this.isIndexView) {
      return null;
    }

    if (preferredAppointmentId && appointments.some((appointment) => appointment.appointmentId === preferredAppointmentId)) {
      return preferredAppointmentId;
    }

    return appointments[0]?.appointmentId ?? null;
  }

  private loadAppointmentSchedules(doctorId: number | null): void {
    if (!doctorId) {
      this.appointmentSchedules = [];
      this.appointmentForm.scheduleId = null;
      return;
    }

    this.phase3.getDoctorSchedules(doctorId).subscribe({
      next: (schedules) => {
        this.appointmentSchedules = [...schedules].sort((a, b) => a.dayOfWeek - b.dayOfWeek || a.startTime.localeCompare(b.startTime));
        if (!this.appointmentSchedules.some((schedule) => schedule.scheduleId === this.appointmentForm.scheduleId)) {
          this.appointmentForm.scheduleId = null;
        }
      },
      error: () => {
        this.errorMessage = 'Unable to load appointment schedule options.';
      }
    });
  }

  private loadSchedules(): void {
    if (!this.selectedDoctorId) {
      this.schedules = [];
      return;
    }

    this.phase3.getDoctorSchedules(this.selectedDoctorId).subscribe({
      next: (schedules) => {
        this.schedules = [...schedules].sort((a, b) => a.dayOfWeek - b.dayOfWeek || a.startTime.localeCompare(b.startTime));
      },
      error: () => {
        this.errorMessage = 'Unable to load doctor schedules.';
      }
    });
  }

  private replaceAppointment(updated: AppointmentAdminRecord): void {
    this.appointments = this.appointments.map((appointment) => appointment.appointmentId === updated.appointmentId ? updated : appointment);
  }

  private syncAppointmentForm(): void {
    const appointment = this.selectedAppointment;
    if (!appointment) {
      this.appointmentForm = { ...EMPTY_APPOINTMENT_FORM };
      this.appointmentSchedules = [];
      return;
    }

    this.appointmentForm = {
      doctorId: appointment.doctorId,
      scheduleId: appointment.scheduleId ?? null,
      appointmentDate: appointment.appointmentDate.slice(0, 10),
      slotStartTime: this.toTimeInput(appointment.slotStartTime),
      slotEndTime: this.toTimeInput(appointment.slotEndTime),
      status: appointment.status,
      adminRemarks: appointment.adminRemarks ?? ''
    };
  }

  private parseId(value: string | null): number | null {
    if (!value) {
      return null;
    }

    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  }

  private toTimePayload(value: string): string {
    return value.length === 5 ? `${value}:00` : value;
  }

  private toTimeInput(value: string | null | undefined): string {
    return value ? value.slice(0, 5) : '';
  }

  private normalizeOptional(value: string): string | null {
    const normalized = value.trim();
    return normalized ? normalized : null;
  }
}
