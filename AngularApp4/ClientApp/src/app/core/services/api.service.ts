import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BookingRecord, Doctor, LoginRequest, RegisterRequest, ServiceItem } from '../models/api.model';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private http: HttpClient) {}

  getDoctors(): Observable<Doctor[]> {
    return this.http.get<Doctor[]>('/api/doctors');
  }

  createDoctor(payload: Doctor): Observable<Doctor> {
    return this.http.post<Doctor>('/api/doctors', payload);
  }

  deleteDoctor(id: number): Observable<void> {
    return this.http.delete<void>(`/api/doctors/${id}`);
  }

  getServices(): Observable<ServiceItem[]> {
    return this.http.get<ServiceItem[]>('/api/services');
  }

  createService(payload: ServiceItem): Observable<ServiceItem> {
    return this.http.post<ServiceItem>('/api/services', payload);
  }

  deleteService(id: number): Observable<void> {
    return this.http.delete<void>(`/api/services/${id}`);
  }

  getBookings(): Observable<BookingRecord[]> {
    return this.http.get<BookingRecord[]>('/api/bookings');
  }

  getMyBookings(): Observable<BookingRecord[]> {
    return this.http.get<BookingRecord[]>('/api/bookings/my');
  }

  createBooking(payload: BookingRecord): Observable<BookingRecord> {
    return this.http.post<BookingRecord>('/api/bookings', payload);
  }

  deleteBooking(id: number): Observable<void> {
    return this.http.delete<void>(`/api/bookings/${id}`);
  }

  register(payload: RegisterRequest): Observable<any> {
    return this.http.post('/api/auth/register', payload);
  }

  login(payload: LoginRequest): Observable<any> {
    return this.http.post('/api/auth/login', payload);
  }

  me(): Observable<any> {
    return this.http.get('/api/auth/me');
  }

  logout(): Observable<any> {
    return this.http.post('/api/auth/logout', {});
  }
}
