import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { AdminUser, Appointment } from '../../core/models/admin.model';
import { AdminStateService } from '../../core/services/admin-state.service';

@Component({
  selector: 'app-user-page',
  templateUrl: './user-page.component.html',
  styleUrls: ['./user-page.component.scss']
})
export class UserPageComponent implements OnInit, OnDestroy {
  user?: AdminUser;
  appointments: Appointment[] = [];

  private usersSub?: Subscription;
  private bookingsSub?: Subscription;

  constructor(private state: AdminStateService) {}

  ngOnInit(): void {
    this.usersSub = this.state.users$.subscribe(users => {
      this.user = users.find(u => u.status === 'Active') ?? users[0];
    });

    this.bookingsSub = this.state.bookings$.subscribe(bookings => {
      this.appointments = [...bookings].sort(
        (a, b) => +new Date(a.appointmentDate) - +new Date(b.appointmentDate)
      );
    });
  }

  ngOnDestroy(): void {
    this.usersSub?.unsubscribe();
    this.bookingsSub?.unsubscribe();
  }

  get upcomingAppointments(): Appointment[] {
    const today = new Date();
    return this.appointments
      .filter(item => new Date(item.appointmentDate) >= today)
      .slice(0, 3);
  }

  get recentAppointments(): Appointment[] {
    return [...this.appointments]
      .sort((a, b) => +new Date(b.appointmentDate) - +new Date(a.appointmentDate))
      .slice(0, 5);
  }

  get totalSpent(): number {
    return this.appointments
      .filter(item => item.status === 'Completed')
      .reduce((sum, item) => sum + item.price, 0);
  }

  get loyaltyTier(): 'Silver' | 'Gold' | 'Platinum' {
    if (this.totalSpent >= 250) return 'Platinum';
    if (this.totalSpent >= 100) return 'Gold';
    return 'Silver';
  }
}
