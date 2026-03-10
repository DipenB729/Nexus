import { Component, OnInit } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { BookingRecord } from '../../core/models/api.model';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-my-bookings',
  templateUrl: './my-bookings.component.html',
  styleUrls: ['./my-bookings.component.scss']
})
export class MyBookingsComponent implements OnInit {
  displayedColumns: string[] = ['id', 'customer', 'service', 'date', 'status', 'actions'];
  dataSource = new MatTableDataSource<BookingRecord>([]);

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadBookings();
  }

  loadBookings(): void {
    this.api.getBookings().subscribe(data => this.dataSource.data = data);
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  deleteBooking(id?: number): void {
    if (!id) return;
    this.api.deleteBooking(id).subscribe(() => this.loadBookings());
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
