import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, map } from 'rxjs';
import {
  ApiResponse,
  AuthResponse,
  AuthSession,
  ChangePasswordRequest,
  DoctorProfile,
  DoctorProfileDepartmentOption,
  ForgotPasswordRequest,
  ForgotPasswordResponse,
  LoginRequest,
  PatientProfile,
  RegisterRequest,
  ResetPasswordRequest,
  UpdateDoctorProfileRequest,
  UpdatePatientProfileRequest
} from '../../models/hms/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly api = '/api/auth';
  private readonly tokenKey = 'hms_token';
  private readonly roleKey = 'hms_role';
  private readonly sessionKey = 'hms_session';
  private readonly sessionSubject = new BehaviorSubject<AuthSession | null>(this.readSession());

  readonly session$ = this.sessionSubject.asObservable();

  constructor(private readonly http: HttpClient, private readonly router: Router) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.api}/login`, request).pipe(
      map((res) => {
        this.storeSession(res.data);
        return res.data;
      })
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.api}/register`, request).pipe(
      map((res) => {
        this.storeSession(res.data);
        return res.data;
      })
    );
  }

  requestPasswordReset(request: ForgotPasswordRequest): Observable<ForgotPasswordResponse> {
    return this.http.post<ApiResponse<ForgotPasswordResponse>>(`${this.api}/forgot-password`, request).pipe(
      map((res) => res.data ?? {})
    );
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<ApiResponse<unknown>>(`${this.api}/reset-password`, request).pipe(
      map(() => undefined)
    );
  }

  getProfile(): Observable<PatientProfile> {
    return this.http.get<ApiResponse<PatientProfile>>(`${this.api}/profile`).pipe(
      map((res) => res.data)
    );
  }

  updateProfile(request: UpdatePatientProfileRequest): Observable<PatientProfile> {
    return this.http.put<ApiResponse<PatientProfile>>(`${this.api}/profile`, request).pipe(
      map((res) => {
        this.updateSessionFromProfile(res.data);
        return res.data;
      })
    );
  }

  getDoctorProfile(): Observable<DoctorProfile> {
    return this.http.get<ApiResponse<DoctorProfile>>(`${this.api}/doctor-profile`).pipe(
      map((res) => res.data)
    );
  }

  updateDoctorProfile(request: UpdateDoctorProfileRequest): Observable<DoctorProfile> {
    return this.http.put<ApiResponse<DoctorProfile>>(`${this.api}/doctor-profile`, request).pipe(
      map((res) => {
        this.updateSessionIdentity(res.data.fullName, res.data.email);
        return res.data;
      })
    );
  }

  getDoctorProfileOptions(): Observable<DoctorProfileDepartmentOption[]> {
    return this.http.get<ApiResponse<DoctorProfileDepartmentOption[]>>(`${this.api}/doctor-profile/options`).pipe(
      map((res) => res.data ?? [])
    );
  }

  uploadDoctorProfilePhoto(file: File): Observable<string> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ApiResponse<{ photoUrl: string }>>(`${this.api}/doctor-profile/photo`, formData).pipe(
      map((res) => res.data.photoUrl)
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<ApiResponse<unknown>>(`${this.api}/change-password`, request).pipe(
      map(() => undefined)
    );
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getRole(): string | null {
    return localStorage.getItem(this.roleKey);
  }

  getSession(): AuthSession | null {
    this.ensureSessionValidity();
    return this.sessionSubject.value;
  }

  isLoggedIn(): boolean {
    return this.ensureSessionValidity();
  }

  getDashboardRoute(role = this.getRole()): string {
    if (role === 'Admin') {
      return '/admin/dashboard';
    }

    if (role === 'Doctor') {
      return '/doctor/dashboard';
    }

    if (role === 'User') {
      return '/patient/doctors';
    }

    return '/auth/login';
  }

  getDisplayRole(role = this.getRole()): string {
    if (role === 'Admin') {
      return 'Administrator';
    }

    if (role === 'Doctor') {
      return 'Doctor';
    }

    if (role === 'User') {
      return 'Patient';
    }

    return 'Guest';
  }

  logout(): void {
    this.clearSessionStorage();
    this.router.navigate(['/auth/login']);
  }

  private storeSession(data: AuthResponse): void {
    localStorage.setItem(this.tokenKey, data.token);
    localStorage.setItem(this.roleKey, data.role);
    localStorage.setItem(this.sessionKey, JSON.stringify(data));
    this.sessionSubject.next(data);
  }

  private readSession(): AuthSession | null {
    const raw = localStorage.getItem(this.sessionKey);
    if (!raw) {
      return null;
    }

    try {
      const session = JSON.parse(raw) as AuthSession;
      return this.isTokenExpired(session.token) ? null : session;
    } catch {
      this.clearSessionStorage();
      return null;
    }
  }

  private ensureSessionValidity(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    if (!this.isTokenExpired(token)) {
      return true;
    }

    this.clearSessionStorage();
    return false;
  }

  private isTokenExpired(token: string): boolean {
    const payload = this.parseTokenPayload(token);
    if (!payload?.exp) {
      return false;
    }

    return Date.now() >= payload.exp * 1000;
  }

  private parseTokenPayload(token: string): { exp?: number } | null {
    const parts = token.split('.');
    if (parts.length < 2) {
      return null;
    }

    try {
      const normalized = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=');
      return JSON.parse(atob(padded)) as { exp?: number };
    } catch {
      return null;
    }
  }

  private updateSessionFromProfile(profile: PatientProfile): void {
    this.updateSessionIdentity(profile.fullName, profile.email);
  }

  private updateSessionIdentity(fullName: string, email: string): void {
    const currentSession = this.sessionSubject.value;
    if (!currentSession) {
      return;
    }

    const nextSession: AuthSession = {
      ...currentSession,
      fullName,
      email
    };

    localStorage.setItem(this.sessionKey, JSON.stringify(nextSession));
    this.sessionSubject.next(nextSession);
  }

  private clearSessionStorage(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.sessionKey);
    this.sessionSubject.next(null);
  }
}
