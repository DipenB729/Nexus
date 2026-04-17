import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { Service } from '../models/booking.model';
import { Appointment } from '../models/appointment.model';
import { ApiResponse, DoctorAppointment, ManageDoctorAppointmentRequest, PatientAppointment, PatientAppointmentPayload } from '../models/hms/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AppointmentService {
  private readonly servicesApi = '/api/services';
  private readonly appointmentsApi = '/api/appointments';

  constructor(private http: HttpClient) { }

  getServices(): Observable<Service[]> {
    return this.http.get<Service[]>(this.servicesApi);
  }

  addService(service: Service): Observable<Service> {
    return this.http.post<Service>(this.servicesApi, service);
  }


  getAppointments(): Observable<Appointment[]> {
    return this.http.get<ApiResponse<Appointment[]>>(`${this.appointmentsApi}`).pipe(
      map((res) => res.data ?? [])
    );
  }

  getMyAppointments(): Observable<PatientAppointment[]> {
    return this.http.get<ApiResponse<PatientAppointment[]>>(`${this.appointmentsApi}/my`).pipe(
      map((res) => res.data ?? [])
    );
  }

  getDoctorAppointments(): Observable<DoctorAppointment[]> {
    return this.http.get<ApiResponse<DoctorAppointment[]>>(`${this.appointmentsApi}/doctor/my`).pipe(
      map((res) => res.data ?? [])
    );
  }

  updateDoctorAppointmentStatus(appointmentId: number, status: 'Completed' | 'NoShow'): Observable<DoctorAppointment> {
    return this.http.put<ApiResponse<DoctorAppointment>>(
      `${this.appointmentsApi}/doctor/${appointmentId}/status`,
      { status }
    ).pipe(map((res) => res.data));
  }

  manageDoctorAppointment(appointmentId: number, payload: ManageDoctorAppointmentRequest): Observable<DoctorAppointment> {
    return this.http.put<ApiResponse<DoctorAppointment>>(
      `${this.appointmentsApi}/doctor/${appointmentId}/manage`,
      payload
    ).pipe(map((res) => res.data));
  }

  createAppointment(appointment: PatientAppointmentPayload): Observable<PatientAppointment> {
    return this.http.post<ApiResponse<PatientAppointment>>(`${this.appointmentsApi}`, appointment).pipe(
      map((res) => res.data)
    );
  }

  rescheduleAppointment(appointmentId: number, appointment: PatientAppointmentPayload): Observable<void> {
    return this.http.put<ApiResponse<unknown>>(`${this.appointmentsApi}/${appointmentId}/reschedule`, appointment).pipe(
      map(() => undefined)
    );
  }

  cancelAppointment(appointmentId: number): Observable<void> {
    return this.http.put<ApiResponse<unknown>>(`${this.appointmentsApi}/${appointmentId}/cancel`, {}).pipe(
      map(() => undefined)
    );
  }
}
