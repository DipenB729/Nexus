import { Component, OnInit } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';

interface Appointment {
  id: string;
  customerName: string;
  serviceName: string;
  appointmentDate: Date;
  time: string;
  status: 'Confirmed' | 'Pending' | 'Cancelled' | 'Completed';
  price: number;
}

@Component({
  selector: 'app-my-bookings',
  templateUrl: './my-bookings.component.html',
  styleUrls: ['./my-bookings.component.scss']
})
export class MyBookingsComponent implements OnInit {
  displayedColumns: string[] = ['id', 'customer', 'service', 'date', 'status', 'actions'];

  dataSource = new MatTableDataSource<Appointment>([
    { id: 'BK-9921', customerName: 'John Doe', serviceName: 'Initial Consultation', appointmentDate: new Date('2026-01-10'), time: '09:00 AM', status: 'Confirmed', price: 75 },
    { id: 'BK-9925', customerName: 'Sarah Smith', serviceName: 'Deep Tissue Massage', appointmentDate: new Date('2026-01-12'), time: '02:30 PM', status: 'Pending', price: 120 },
    { id: 'BK-9930', customerName: 'Mike Ross', serviceName: 'Wellness Coaching', appointmentDate: new Date('2026-01-15'), time: '11:00 AM', status: 'Cancelled', price: 90 },
    { id: 'BK-9935', customerName: 'Rachel Zane', serviceName: 'Express Checkup', appointmentDate: new Date('2025-12-28'), time: '04:00 PM', status: 'Completed', price: 30 }
  ]);

  constructor() { }

  ngOnInit(): void { }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  getStatusClass(status: string): string {
    return `status-badge ${status.toLowerCase()}`;
  }
}
