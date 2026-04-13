import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, map } from 'rxjs';
import { ApiResponse, AuthResponse, AuthSession, LoginRequest, RegisterRequest } from '../../models/hms/auth.model';

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

    if (role === 'User') {
      return '/user/dashboard';
    }

    return '/auth/login';
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.sessionKey);
    this.sessionSubject.next(null);
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
      localStorage.removeItem(this.sessionKey);
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

    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.sessionKey);
    this.sessionSubject.next(null);
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
}
