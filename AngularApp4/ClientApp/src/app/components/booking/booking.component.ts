import { Component, OnInit } from '@angular/core';
import { BookingRecord, Doctor, ServiceItem } from '../../core/models/api.model';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-booking',
  templateUrl: './booking.component.html',
  styleUrls: ['./booking.component.scss']
})
export class BookingComponent implements OnInit {
  services: ServiceItem[] = [];
  doctors: Doctor[] = [];
  customerName = '';
  doctorName = '';
  serviceName = '';
  appointmentDate = '';
  time = '09:00 AM';
  message = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getServices().subscribe(data => {
      this.services = data;
      if (data.length && !this.serviceName) this.serviceName = data[0].name;
    });

    this.api.getDoctors().subscribe(data => {
      this.doctors = data;
      if (data.length && !this.doctorName) this.doctorName = data[0].fullName;
    });
  }

  confirmBooking(): void {
    const service = this.services.find(s => s.name === this.serviceName);
    const payload: BookingRecord = {
      customerName: this.customerName,
      doctorName: this.doctorName,
      serviceName: this.serviceName,
      appointmentDate: this.appointmentDate || new Date().toISOString(),
      time: this.time,
      status: 'Pending',
      price: service?.price ?? 0
    };

    this.api.createBooking(payload).subscribe(() => {
      this.message = 'Appointment booked successfully.';
      this.customerName = '';
    });
  }
}
