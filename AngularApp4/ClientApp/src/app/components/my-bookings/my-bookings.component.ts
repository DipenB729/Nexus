import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { Appointment } from '../../core/models/admin.model';
import { AdminStateService } from '../../core/services/admin-state.service';
import { AuthApiService } from '../../core/services/hms/auth-api.service';

@Component({
  selector: 'app-my-bookings',
  templateUrl: './my-bookings.component.html',
  styleUrls: ['./my-bookings.component.scss']
})
export class MyBookingsComponent implements OnInit, OnDestroy {
  displayedColumns: string[] = [];
  dataSource = new MatTableDataSource<Appointment>([]);
  isAdmin = false;

  private sub?: Subscription;

  constructor(
    private readonly state: AdminStateService,
    private readonly auth: AuthApiService
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.auth.getRole() === 'Admin';
    this.displayedColumns = this.isAdmin
      ? ['id', 'customer', 'service', 'date', 'status', 'price']
      : ['id', 'service', 'date', 'status', 'price'];

    this.sub = this.state.bookings$.subscribe((data) => {
      this.dataSource.data = data;
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  get pageTitle(): string {
    return this.isAdmin ? 'All Appointments' : 'My Appointments';
  }

  get pageSubtitle(): string {
    return this.isAdmin
      ? 'Monitor and review appointments across the platform.'
      : 'Track your booking history and upcoming sessions.';
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  getStatusClass(status: string): string {
    return `status-badge ${status.toLowerCase()}`;
  }

  getStatusText(status: string): string {
    const normalized = status.toLowerCase();
    if (normalized === 'confirmed') return 'Confirmed';
    if (normalized === 'pending') return 'Pending Approval';
    if (normalized === 'cancelled') return 'Cancelled';
    if (normalized === 'completed') return 'Completed';
    return status;
  }
}
