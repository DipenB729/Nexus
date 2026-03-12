import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, map } from 'rxjs';
import { ApiResponse, AuthResponse, LoginRequest, RegisterRequest } from '../../models/hms/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly api = '/api/auth';
  private readonly tokenKey = 'hms_token';
  private readonly roleKey = 'hms_role';

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

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.roleKey);
    this.router.navigate(['/auth/login']);
  }

  private storeSession(data: AuthResponse): void {
    localStorage.setItem(this.tokenKey, data.token);
    localStorage.setItem(this.roleKey, data.role);
  }
}
