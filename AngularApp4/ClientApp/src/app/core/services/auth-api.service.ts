import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthUser, LoginRequest, RegisterRequest } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly baseUrl = '/api/auth';

  constructor(private http: HttpClient) {}

  register(payload: RegisterRequest): Observable<AuthUser> {
    return this.http.post<AuthUser>(`${this.baseUrl}/register`, payload);
  }

  login(payload: LoginRequest): Observable<AuthUser> {
    return this.http.post<AuthUser>(`${this.baseUrl}/login`, payload);
  }
}
