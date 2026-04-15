import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { PatientAppointment } from '../../core/models/hms/auth.model';
import { DoctorAvailableSlot, DoctorMaster } from '../../core/models/hms/master-setup.model';
import { AppointmentService } from '../../core/services/appointment.service';
import { MasterSetupService } from '../../core/services/hms/master-setup.service';
import { NotificationsService } from '../../core/services/notifications.service';

@Component({
  selector: 'app-booking',
  templateUrl: './booking.component.html',
  styleUrls: ['./booking.component.scss']
})
export class BookingComponent implements OnInit {
  doctors: DoctorMaster[] = [];
  availableSlots: DoctorAvailableSlot[] = [];
  selectedDoctorId: number | null = null;
  selectedSlot: DoctorAvailableSlot | null = null;
  reason = '';
  isLoadingDoctors = true;
  isLoadingAvailability = false;
  isBooking = false;
  isLoadingAppointment = false;
  errorMessage = '';
  successMessage = '';
  readonly today = this.getTodayDate();
  selectedDate = this.today;
  confirmationSummary: {
    mode: 'booked' | 'rescheduled';
    doctorName: string;
    dateLabel: string;
    slotLabel: string;
    reason: string;
  } | null = null;

  private pendingDoctorId: number | null = null;
  private pendingAppointmentId: number | null = null;
  private appointmentToReschedule: PatientAppointment | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly masterSetup: MasterSetupService,
    private readonly appointmentService: AppointmentService,
    private readonly notifications: NotificationsService
  ) { }

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      const doctorId = Number(params.get('doctorId'));
      const appointmentId = Number(params.get('appointmentId'));
      this.pendingDoctorId = Number.isFinite(doctorId) && doctorId > 0 ? doctorId : null;
      this.pendingAppointmentId = Number.isFinite(appointmentId) && appointmentId > 0 ? appointmentId : null;
      this.applyPreselectedDoctor();
      this.loadAppointmentToReschedule();
    });

    this.loadDoctors();
  }

  get selectedDoctor(): DoctorMaster | null {
    return this.selectedDoctorId
      ? this.doctors.find((doctor) => doctor.doctorId === this.selectedDoctorId) ?? null
      : null;
  }

  get selectedDateLabel(): string {
    return this.formatDateLabel(this.selectedDate);
  }

  get isRescheduleMode(): boolean {
    return !!this.appointmentToReschedule;
  }

  get canConfirm(): boolean {
    return !!(this.selectedDoctor && this.selectedSlot && this.reason.trim()) &&
      !this.isLoadingAvailability &&
      !this.isBooking &&
      !this.isLoadingAppointment;
  }

  trackByDoctor(index: number, doctor: DoctorMaster): number {
    return doctor.doctorId;
  }

  trackBySlot(index: number, slot: DoctorAvailableSlot): string {
    return `${slot.scheduleId ?? 'legacy'}-${slot.startTime}-${slot.endTime}`;
  }

  onDoctorChange(): void {
    this.errorMessage = '';
    this.successMessage = '';
    this.confirmationSummary = null;
    this.selectedSlot = null;
    this.availableSlots = [];

    if (this.selectedDoctorId) {
      this.loadAvailableSlots(this.selectedDoctorId);
    }
  }

  onDateChange(): void {
    if (!this.selectedDate) {
      this.selectedDate = this.today;
    }

    this.successMessage = '';
    this.errorMessage = '';
    this.confirmationSummary = null;
    this.selectedSlot = null;

    if (this.selectedDoctorId) {
      this.loadAvailableSlots(this.selectedDoctorId);
    }
  }

  selectSlot(slot: DoctorAvailableSlot): void {
    this.selectedSlot = slot;
    this.successMessage = '';
    this.errorMessage = '';
    this.confirmationSummary = null;
  }

  confirmAppointment(): void {
    if (!this.selectedDoctor) {
      this.errorMessage = 'Choose a doctor before confirming the appointment.';
      return;
    }

    if (!this.selectedSlot) {
      this.errorMessage = 'Choose an available slot before confirming the appointment.';
      return;
    }

    const reason = this.reason.trim();
    if (!reason) {
      this.errorMessage = 'Add the reason for visit before confirming the appointment.';
      return;
    }

    this.isBooking = true;
    this.errorMessage = '';
    this.successMessage = '';

    const payload = {
      doctorId: this.selectedDoctor.doctorId,
      scheduleId: this.selectedSlot.scheduleId ?? null,
      appointmentDate: this.selectedDate,
      slotStartTime: this.selectedSlot.startTime,
      slotEndTime: this.selectedSlot.endTime,
      reason
    };

    const request: Observable<unknown> = this.appointmentToReschedule
      ? this.appointmentService.rescheduleAppointment(this.appointmentToReschedule.appointmentId, payload)
      : this.appointmentService.createAppointment(payload);

    request.subscribe({
      next: () => {
        this.isBooking = false;
        const mode = this.appointmentToReschedule ? 'rescheduled' : 'booked';
        this.successMessage = this.appointmentToReschedule
          ? 'Appointment rescheduled successfully.'
          : 'Appointment confirmed and saved to your account.';
        this.confirmationSummary = {
          mode,
          doctorName: this.selectedDoctor!.fullName,
          dateLabel: this.selectedDateLabel,
          slotLabel: this.formatSlot(this.selectedSlot!),
          reason
        };
        this.reason = '';
        this.selectedSlot = null;
        this.appointmentToReschedule = null;
        this.pendingAppointmentId = null;
        this.loadAvailableSlots(this.selectedDoctor!.doctorId);
        this.notifications.requestRefresh();
      },
      error: (error: { error?: { message?: string } }) => {
        this.isBooking = false;
        this.errorMessage = error?.error?.message ?? 'Unable to confirm the appointment right now.';
      }
    });
  }

  formatDays(opdDays?: string | null): string {
    if (!opdDays?.trim()) {
      return 'Schedule available on request';
    }

    return opdDays
      .split(',')
      .map((token) => token.trim())
      .filter(Boolean)
      .map((token) => this.mapDayToken(token))
      .join(', ');
  }

  formatSlot(slot: DoctorAvailableSlot): string {
    return `${this.formatTime(slot.startTime)} - ${this.formatTime(slot.endTime)}`;
  }

  formatSlotCapacity(slot: DoctorAvailableSlot): string {
    if (slot.remainingPatients <= 1) {
      return '1 spot left';
    }

    return `${slot.remainingPatients} spots left`;
  }

  formatTimeRange(startTime?: string | null, endTime?: string | null): string {
    if (!startTime || !endTime) {
      return 'Schedule shared after doctor selection';
    }

    return `${this.formatTime(startTime)} - ${this.formatTime(endTime)}`;
  }

  private loadDoctors(): void {
    this.isLoadingDoctors = true;
    this.errorMessage = '';

    this.masterSetup.getDoctors(true).subscribe({
      next: (doctors) => {
        this.doctors = [...doctors].sort((a, b) => a.fullName.localeCompare(b.fullName));
        this.isLoadingDoctors = false;
        this.applyPreselectedDoctor();
      },
      error: () => {
        this.isLoadingDoctors = false;
        this.errorMessage = 'Unable to load doctors right now.';
      }
    });
  }

  private applyPreselectedDoctor(): void {
    if (!this.doctors.length) {
      return;
    }

    if (this.pendingDoctorId && this.doctors.some((doctor) => doctor.doctorId === this.pendingDoctorId)) {
      if (this.selectedDoctorId !== this.pendingDoctorId) {
        this.selectedDoctorId = this.pendingDoctorId;
        this.loadAvailableSlots(this.pendingDoctorId);
      }
      return;
    }

    if (this.selectedDoctorId && !this.doctors.some((doctor) => doctor.doctorId === this.selectedDoctorId)) {
      this.selectedDoctorId = null;
      this.availableSlots = [];
      this.selectedSlot = null;
    }
  }

  private loadAppointmentToReschedule(): void {
    if (!this.pendingAppointmentId) {
      this.appointmentToReschedule = null;
      return;
    }

    this.isLoadingAppointment = true;
    this.errorMessage = '';

    this.appointmentService.getMyAppointments().subscribe({
      next: (appointments) => {
        const appointment = appointments.find((item) => item.appointmentId === this.pendingAppointmentId) ?? null;
        if (!appointment) {
          this.errorMessage = 'The selected appointment could not be found for rescheduling.';
          this.appointmentToReschedule = null;
          this.isLoadingAppointment = false;
          return;
        }

        if (appointment.status === 'Cancelled' || appointment.status === 'Completed') {
          this.errorMessage = 'Only active appointments can be rescheduled.';
          this.appointmentToReschedule = null;
          this.isLoadingAppointment = false;
          return;
        }

        this.appointmentToReschedule = appointment;
        this.pendingDoctorId = appointment.doctorId;
        this.selectedDoctorId = appointment.doctorId;
        this.selectedDate = appointment.appointmentDate.slice(0, 10);
        this.reason = appointment.reason ?? '';
        this.successMessage = '';
        this.confirmationSummary = null;
        this.selectedSlot = null;

        if (this.selectedDoctorId) {
          this.loadAvailableSlots(this.selectedDoctorId);
        }

        this.isLoadingAppointment = false;
      },
      error: () => {
        this.appointmentToReschedule = null;
        this.isLoadingAppointment = false;
        this.errorMessage = 'Unable to load the appointment for rescheduling.';
      }
    });
  }

  private loadAvailableSlots(doctorId: number): void {
    this.isLoadingAvailability = true;
    this.errorMessage = '';

    this.masterSetup.getAvailableSlots(doctorId, this.selectedDate).subscribe({
      next: (slots) => {
        this.availableSlots = slots;
        this.isLoadingAvailability = false;
      },
      error: () => {
        this.availableSlots = [];
        this.isLoadingAvailability = false;
        this.errorMessage = 'Unable to load available slots for the selected date.';
      }
    });
  }

  private mapDayToken(token: string): string {
    const normalized = token.toLowerCase();
    const labels: Record<string, string> = {
      sun: 'Sun',
      sunday: 'Sun',
      mon: 'Mon',
      monday: 'Mon',
      tue: 'Tue',
      tues: 'Tue',
      tuesday: 'Tue',
      wed: 'Wed',
      wednesday: 'Wed',
      thu: 'Thu',
      thur: 'Thu',
      thurs: 'Thu',
      thursday: 'Thu',
      fri: 'Fri',
      friday: 'Fri',
      sat: 'Sat',
      saturday: 'Sat'
    };

    return labels[normalized] ?? token.trim();
  }

  private formatDateLabel(date: string): string {
    const [year, month, day] = date.split('-').map((part) => Number(part));
    const parsed = new Date(year, (month || 1) - 1, day || 1);

    return new Intl.DateTimeFormat('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      year: 'numeric'
    }).format(parsed);
  }

  private formatTime(value: string): string {
    return value.length >= 5 ? value.slice(0, 5) : value;
  }

  private getTodayDate(): string {
    const currentDate = new Date();
    const year = currentDate.getFullYear();
    const month = `${currentDate.getMonth() + 1}`.padStart(2, '0');
    const day = `${currentDate.getDate()}`.padStart(2, '0');

    return `${year}-${month}-${day}`;
  }
}
