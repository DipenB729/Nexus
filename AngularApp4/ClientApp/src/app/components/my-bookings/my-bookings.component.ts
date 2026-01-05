import { Component, OnInit } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';

// 1. Ensure the path to your service is correct
import { AppointmentService } from '../../core/services/appointment.service';

// 2. IMPORT the Appointment interface from your separate model file
import { Appointment } from '../../core/models/appointment.model';

@Component({
  selector: 'app-my-bookings',
  templateUrl: './my-bookings.component.html',
  styleUrls: ['./my-bookings.component.scss']
})
export class MyBookingsComponent implements OnInit {
  // Columns matching the 'matColumnDef' names in your HTML
  displayedColumns: string[] = ['id', 'customer', 'service', 'date', 'status', 'actions'];

  // Now 'Appointment' is recognized because of the import above
  dataSource = new MatTableDataSource<Appointment>([]);

  // Map the .NET Enum values (int) to readable string labels
  statusLabels: { [key: number]: string } = {
    0: 'Pending',
    1: 'Confirmed',
    2: 'Cancelled',
    3: 'Completed',
    4: 'NoShow'
  };

  constructor(private appointmentService: AppointmentService) { }

  ngOnInit(): void {
    this.loadAppointments();
  }

  loadAppointments() {
    this.appointmentService.getAppointments().subscribe({
      next: (data) => {
        console.log('Appointments received from .NET:', data);
        this.dataSource.data = data;
      },
      error: (err) => {
        console.error('Error fetching appointments:', err);
      }
    });
  }

  // Helper to filter the table data via the search input
  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  // Helper to get the text for the status badge
  getStatusText(status: number): string {
    return this.statusLabels[status] || 'Unknown';
  }

  // Helper to assign CSS classes based on status
  getStatusClass(status: number): string {
    const text = (this.statusLabels[status] || 'pending').toLowerCase();
    return `status-badge ${text}`;
  }
}
