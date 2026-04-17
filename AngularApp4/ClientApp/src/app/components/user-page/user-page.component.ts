import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription, interval } from 'rxjs';
import { AppNotification, AuthSession, PatientAppointment } from '../../core/models/hms/auth.model';
import { AppointmentService } from '../../core/services/appointment.service';
import { AuthApiService } from '../../core/services/hms/auth-api.service';
import { NotificationsService } from '../../core/services/notifications.service';

@Component({
  selector: 'app-user-page',
  templateUrl: './user-page.component.html',
  styleUrls: ['./user-page.component.scss']
})
export class UserPageComponent implements OnInit, OnDestroy {
  session: AuthSession | null = null;
  appointments: PatientAppointment[] = [];
  notifications: AppNotification[] = [];
  isLoading = true;
  errorMessage = '';

  private sessionSub?: Subscription;
  private appointmentsSub?: Subscription;
  private notificationsSub?: Subscription;
  private notificationsRefreshSub?: Subscription;
  private notificationsPollingSub?: Subscription;

  constructor(
    private readonly appointmentsApi: AppointmentService,
    private readonly auth: AuthApiService,
    private readonly notificationsApi: NotificationsService
  ) {}

  ngOnInit(): void {
    this.session = this.auth.getSession();

    this.sessionSub = this.auth.session$.subscribe((session) => {
      this.session = session;
    });

    this.notificationsRefreshSub = this.notificationsApi.refresh$.subscribe(() => {
      this.loadNotifications();
    });

    this.notificationsPollingSub = interval(30000).subscribe(() => {
      this.loadNotifications();
    });

    this.loadAppointments();
    this.loadNotifications();
  }

  ngOnDestroy(): void {
    this.sessionSub?.unsubscribe();
    this.appointmentsSub?.unsubscribe();
    this.notificationsSub?.unsubscribe();
    this.notificationsRefreshSub?.unsubscribe();
    this.notificationsPollingSub?.unsubscribe();
  }

  get displayName(): string {
    return this.session?.fullName ?? 'Patient';
  }

  get email(): string {
    return this.session?.email ?? 'patient@nexus.local';
  }

  get role(): string {
    return this.auth.getDisplayRole(this.session?.role);
  }

  get upcomingAppointments(): PatientAppointment[] {
    return [...this.appointments]
      .filter((appointment) => this.isUpcoming(appointment))
      .sort((a, b) => this.toDateTime(a).getTime() - this.toDateTime(b).getTime())
      .slice(0, 3);
  }

  get recentAppointments(): PatientAppointment[] {
    return [...this.appointments]
      .sort((a, b) => this.toDateTime(b).getTime() - this.toDateTime(a).getTime())
      .slice(0, 5);
  }

  get totalSpent(): number {
    return this.appointments
      .filter((appointment) => appointment.status === 'Completed')
      .reduce((sum, appointment) => sum + (appointment.servicePrice ?? 0), 0);
  }

  get completedAppointmentsCount(): number {
    return this.appointments.filter((appointment) => appointment.status === 'Completed').length;
  }

  get pendingAppointmentsCount(): number {
    return this.appointments.filter((appointment) => ['Pending', 'Approved', 'Rescheduled'].includes(appointment.status)).length;
  }

  get recentNotifications(): AppNotification[] {
    return this.notifications.slice(0, 5);
  }

  formatSlot(appointment: PatientAppointment): string {
    return `${this.formatTime(appointment.slotStartTime)} - ${this.formatTime(appointment.slotEndTime)}`;
  }

  statusClass(status: string): string {
    return status.toLowerCase();
  }

  markNotificationAsRead(notification: AppNotification): void {
    if (notification.isRead) {
      return;
    }

    this.notificationsApi.markAsRead(notification.appNotificationId).subscribe({
      next: () => {
        this.notifications = this.notifications.map((item) =>
          item.appNotificationId === notification.appNotificationId ? { ...item, isRead: true } : item);
      }
    });
  }

  private loadAppointments(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.appointmentsSub = this.appointmentsApi.getMyAppointments().subscribe({
      next: (appointments) => {
        this.appointments = appointments;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Unable to load your appointments right now.';
      }
    });
  }

  private loadNotifications(): void {
    this.notificationsSub?.unsubscribe();
    this.notificationsSub = this.notificationsApi.getNotifications(8).subscribe({
      next: (items) => {
        this.notifications = items;
      },
      error: () => {
        this.notifications = [];
      }
    });
  }

  private isUpcoming(appointment: PatientAppointment): boolean {
    if (['Cancelled', 'Completed'].includes(appointment.status)) {
      return false;
    }

    return this.toDateTime(appointment).getTime() >= Date.now();
  }

  private toDateTime(appointment: PatientAppointment): Date {
    return new Date(`${appointment.appointmentDate.slice(0, 10)}T${appointment.slotStartTime}`);
  }

  private formatTime(value: string): string {
    const [hours, minutes] = value.split(':');
    const date = new Date();
    date.setHours(Number(hours), Number(minutes), 0, 0);
    return date.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });
  }
}
