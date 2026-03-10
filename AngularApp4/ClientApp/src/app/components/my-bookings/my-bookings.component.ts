import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { Appointment } from '../../core/models/admin.model';
import { AdminStateService } from '../../core/services/admin-state.service';

@Component({
  selector: 'app-my-bookings',
  templateUrl: './my-bookings.component.html',
  styleUrls: ['./my-bookings.component.scss']
})
export class MyBookingsComponent implements OnInit, OnDestroy {
  displayedColumns: string[] = ['id', 'customer', 'service', 'date', 'status', 'actions'];
  dataSource = new MatTableDataSource<Appointment>([]);

  private sub?: Subscription;

  constructor(private state: AdminStateService) {}

  ngOnInit(): void {
    this.sub = this.state.bookings$.subscribe(data => {
      this.dataSource.data = data;
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
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

