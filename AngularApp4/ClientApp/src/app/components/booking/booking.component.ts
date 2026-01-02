import { Component, OnInit } from '@angular/core';
import { Service, TimeSlot } from '../../core/models/booking.model';

@Component({
  selector: 'app-booking',
  templateUrl: './booking.component.html',
  styleUrls: ['./booking.component.scss']
})
export class BookingComponent implements OnInit {  

 
  services: Service[] = [
    { id: 1, name: 'Standard Consultation', duration: 30, price: 50, description: 'Basic checkup' }
  ];
  selectedService: Service | null = null;
  selectedDate: Date | null = new Date();
  selectedTime: string | null = null;
  availableSlots: TimeSlot[] = [
    { time: '09:00 AM', available: true }
  ];

  constructor() { }

 
  ngOnInit(): void {
    console.log('Booking component initialized');
  }
 
  selectService(service: Service) {
    this.selectedService = service;
  }

  selectTime(slot: string) {
    this.selectedTime = slot;
  }

  confirmBooking() {
    console.log('Confirmed');
  }
}
