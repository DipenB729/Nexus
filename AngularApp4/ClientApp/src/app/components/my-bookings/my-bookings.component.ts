import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { PatientAppointment } from '../../core/models/hms/auth.model';
import { AppointmentService } from '../../core/services/appointment.service';
import { NotificationsService } from '../../core/services/notifications.service';

type ViewMode = 'index' | 'details';

@Component({
  selector: 'app-my-bookings',
  templateUrl: './my-bookings.component.html',
  styleUrls: ['./my-bookings.component.scss']
})
export class MyBookingsComponent implements OnInit, OnDestroy {
  appointments: PatientAppointment[] = [];
  viewMode: ViewMode = 'index';
  selectedAppointmentId: number | null = null;

  isLoading = true;
  isCancelling = false;
  errorMessage = '';
  statusMessage = '';
  searchTerm = '';

  private appointmentsSub?: Subscription;
  private routeSub?: Subscription;

  constructor(
    private readonly router: Router,
    private readonly appointmentsApi: AppointmentService,
    private readonly notifications: NotificationsService
  ) {}

  ngOnInit(): void {
    this.syncRoute(this.router.url);
    this.routeSub = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.syncRoute(event.urlAfterRedirects));
    this.loadAppointments();
  }

  ngOnDestroy(): void {
    this.appointmentsSub?.unsubscribe();
    this.routeSub?.unsubscribe();
  }

  get pageTitle(): string {
    return this.isDetailsView ? 'Appointment Details' : 'My Appointments';
  }

  get pageSubtitle(): string {
    return this.isDetailsView
      ? 'Review a single appointment, then reschedule or cancel it from a focused details page.'
      : 'Track upcoming visits, completed care, cancelled requests, and open any appointment for full details.';
  }

  get isIndexView(): boolean {
    return this.viewMode === 'index';
  }

  get isDetailsView(): boolean {
    return this.viewMode === 'details';
  }

  get filteredUpcomingAppointments(): PatientAppointment[] {
    return this.filterAppointments(this.appointments.filter((appointment) =>
      appointment.status !== 'Completed' && appointment.status !== 'Cancelled'));
  }

  get filteredAppointments(): PatientAppointment[] {
    return this.filterAppointments(this.appointments);
  }

  get filteredCompletedAppointments(): PatientAppointment[] {
    return this.filterAppointments(this.appointments.filter((appointment) => appointment.status === 'Completed'));
  }

  get filteredCancelledAppointments(): PatientAppointment[] {
    return this.filterAppointments(this.appointments.filter((appointment) => appointment.status === 'Cancelled'));
  }

  get selectedAppointment(): PatientAppointment | null {
    return this.selectedAppointmentId
      ? this.appointments.find((appointment) => appointment.appointmentId === this.selectedAppointmentId) ?? null
      : null;
  }

  openDetails(appointmentId: number): void {
    this.router.navigate(['/patient/appointments', appointmentId]);
  }

  backToIndex(): void {
    this.router.navigate(['/patient/appointments']);
  }

  cancelAppointment(appointment: PatientAppointment): void {
    if (this.isCancelling) {
      return;
    }

    const shouldCancel = window.confirm(`Cancel appointment #${appointment.appointmentId} with ${appointment.doctorName}?`);
    if (!shouldCancel) {
      return;
    }

    this.isCancelling = true;
    this.errorMessage = '';
    this.statusMessage = '';

    this.appointmentsApi.cancelAppointment(appointment.appointmentId).subscribe({
      next: () => {
        this.isCancelling = false;
        this.statusMessage = 'Appointment cancelled successfully.';
        this.loadAppointments();
        this.notifications.requestRefresh();
        if (this.selectedAppointmentId === appointment.appointmentId) {
          this.router.navigate(['/patient/appointments', appointment.appointmentId]);
        }
      },
      error: (error) => {
        this.isCancelling = false;
        this.errorMessage = error?.error?.message ?? 'Unable to cancel the appointment right now.';
      }
    });
  }

  canCancel(appointment: PatientAppointment): boolean {
    return appointment.status !== 'Completed' && appointment.status !== 'Cancelled';
  }

  canReschedule(appointment: PatientAppointment): boolean {
    return appointment.status !== 'Completed' && appointment.status !== 'Cancelled';
  }

  getStatusClass(status: string): string {
    return `status-badge ${status.toLowerCase()}`;
  }

  formatSlot(appointment: PatientAppointment): string {
    return `${this.formatTime(appointment.slotStartTime)} - ${this.formatTime(appointment.slotEndTime)}`;
  }

  private syncRoute(url: string): void {
    const parts = url.split('?')[0].split('/').filter(Boolean);
    const appointmentId = this.parseId(parts[2] ?? null);

    if (appointmentId) {
      this.viewMode = 'details';
      this.selectedAppointmentId = appointmentId;
      return;
    }

    this.viewMode = 'index';
    this.selectedAppointmentId = null;
  }

  private formatTime(value: string): string {
    const [hours, minutes] = value.split(':');
    const date = new Date();
    date.setHours(Number(hours), Number(minutes), 0, 0);
    return date.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });
  }

  private loadAppointments(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.appointmentsSub?.unsubscribe();
    this.appointmentsSub = this.appointmentsApi.getMyAppointments().subscribe({
      next: (appointments) => {
        this.appointments = [...appointments].sort((a, b) =>
          `${b.appointmentDate}${b.slotStartTime}`.localeCompare(`${a.appointmentDate}${a.slotStartTime}`));
        if (this.isDetailsView && this.selectedAppointmentId && !this.selectedAppointment) {
          this.errorMessage = 'The selected appointment could not be found.';
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load your appointments right now.';
      }
    });
  }

  private filterAppointments(appointments: PatientAppointment[]): PatientAppointment[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) {
      return appointments;
    }

    return appointments.filter((appointment) =>
      [
        appointment.appointmentId,
        appointment.doctorName,
        appointment.doctorSpecialization,
        appointment.serviceName,
        appointment.status,
        appointment.tokenNumber
      ]
        .map((value) => String(value ?? '').toLowerCase())
        .some((value) => value.includes(term)));
  }

  private parseId(value: string | null): number | null {
    if (!value) {
      return null;
    }

    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  }
}
