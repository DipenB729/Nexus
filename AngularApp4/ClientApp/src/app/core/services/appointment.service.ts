import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Service } from '../models/booking.model';
import { Appointment } from '../models/appointment.model';

@Injectable({
  providedIn: 'root'
})
export class AppointmentService {
  // .NET API URL (Matches your Program.cs port)
  private apiUrl = 'https://localhost:44432/api/services';
  private baseurl = 'https://localhost:44432/api/appointments';

  constructor(private http: HttpClient) { }

  getServices(): Observable<Service[]> {
    return this.http.get<Service[]>(this.apiUrl);
  }

  addService(service: Service): Observable<Service> {
    return this.http.post<Service>(this.apiUrl, service);
  }


  getAppointments(): Observable<Appointment[]> {
    // This calls your new AppointmentsController in .NET
    return this.http.get<Appointment[]>(`${this.baseurl}`);
  }

  // We will need this later when we build the Booking Wizard
  createAppointment(appointment: any): Observable<any> {
    return this.http.post<any>(`${this.baseurl}`, appointment);
  }
}
