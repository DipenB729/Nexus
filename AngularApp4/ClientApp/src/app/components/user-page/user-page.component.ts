import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { Appointment } from '../../core/models/admin.model';
import { AuthSession } from '../../core/models/hms/auth.model';
import { AdminStateService } from '../../core/services/admin-state.service';
import { AuthApiService } from '../../core/services/hms/auth-api.service';

@Component({
  selector: 'app-user-page',
  templateUrl: './user-page.component.html',
  styleUrls: ['./user-page.component.scss']
})
export class UserPageComponent implements OnInit, OnDestroy {
  session: AuthSession | null = null;
  appointments: Appointment[] = [];

  private sessionSub?: Subscription;
  private bookingsSub?: Subscription;

  constructor(
    private readonly state: AdminStateService,
    private readonly auth: AuthApiService
  ) {}

  ngOnInit(): void {
    this.session = this.auth.getSession();

    this.sessionSub = this.auth.session$.subscribe((session) => {
      this.session = session;
    });

    this.bookingsSub = this.state.bookings$.subscribe((bookings) => {
      this.appointments = [...bookings].sort(
        (a, b) => +new Date(a.appointmentDate) - +new Date(b.appointmentDate)
      );
    });
  }

  ngOnDestroy(): void {
    this.sessionSub?.unsubscribe();
    this.bookingsSub?.unsubscribe();
  }

  get displayName(): string {
    return this.session?.fullName ?? 'Nexus User';
  }

  get email(): string {
    return this.session?.email ?? 'user@nexus.local';
  }

  get role(): string {
    return this.session?.role ?? 'User';
  }

  get upcomingAppointments(): Appointment[] {
    const today = new Date();
    return this.appointments
      .filter((item) => new Date(item.appointmentDate) >= today)
      .slice(0, 3);
  }

  get recentAppointments(): Appointment[] {
    return [...this.appointments]
      .sort((a, b) => +new Date(b.appointmentDate) - +new Date(a.appointmentDate))
      .slice(0, 5);
  }

  get totalSpent(): number {
    return this.appointments
      .filter((item) => item.status === 'Completed')
      .reduce((sum, item) => sum + item.price, 0);
  }

  get loyaltyTier(): 'Silver' | 'Gold' | 'Platinum' {
    if (this.totalSpent >= 250) return 'Platinum';
    if (this.totalSpent >= 100) return 'Gold';
    return 'Silver';
  }
}
