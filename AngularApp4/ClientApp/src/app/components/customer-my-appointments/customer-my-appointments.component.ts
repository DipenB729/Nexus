import { Component, OnInit } from '@angular/core';
import { BookingRecord } from '../../core/models/api.model';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-customer-my-appointments',
  templateUrl: './customer-my-appointments.component.html',
  styleUrls: ['./customer-my-appointments.component.scss']
})
export class CustomerMyAppointmentsComponent implements OnInit {
  bookings: BookingRecord[] = [];

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getMyBookings().subscribe(data => this.bookings = data);
  }
}
