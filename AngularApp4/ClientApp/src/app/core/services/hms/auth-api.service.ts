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
    return this.sessionSubject.value;
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
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
      return JSON.parse(raw) as AuthSession;
    } catch {
      localStorage.removeItem(this.sessionKey);
      return null;
    }
  }
}
