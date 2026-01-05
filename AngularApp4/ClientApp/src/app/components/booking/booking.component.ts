import { Component, OnInit } from '@angular/core';
import { Service, TimeSlot } from '../../core/models/booking.model';

@Component({
  selector: 'app-booking',
  templateUrl: './booking.component.html',
  styleUrls: ['./booking.component.scss']
})
export class BookingComponent implements OnInit {

  // FIX: Updated property from 'duration' to 'durationMinutes' 
  // and added missing fields (category, icon, color) to match the interface
  services: Service[] = [
    {
      id: 1,
      name: 'Standard Consultation',
      durationMinutes: 30,
      price: 50,
      description: 'Basic checkup',
      category: 'Health',
      icon: 'medical_services',
      color: '#3b82f6'
    }
  ];

  selectedService: Service | null = null;
  selectedDate: Date | null = new Date();
  selectedTime: string | null = null;

  availableSlots: TimeSlot[] = [
    { time: '09:00 AM', available: true },
    { time: '10:00 AM', available: false },
    { time: '11:00 AM', available: true }
  ];

  constructor() { }

  ngOnInit(): void { }

  selectService(service: Service) {
    this.selectedService = service;
  }

  selectTime(slot: string) {
    this.selectedTime = slot;
  }
}
