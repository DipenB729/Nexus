import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiResponse } from '../../models/hms/auth.model';
import {
  CreateAdminUser,
  SuperAdminBranch,
  SuperAdminHospital,
  SuperAdminHospitalProfile,
  SuperAdminSummary,
  SuperAdminUser,
  UpsertBranch
} from '../../models/hms/superadmin.model';

@Injectable({ providedIn: 'root' })
export class SuperAdminService {
  private readonly api = '/api/superadmin';

  constructor(private readonly http: HttpClient) {}

  getSummary(): Observable<SuperAdminSummary> {
    return this.http.get<ApiResponse<SuperAdminSummary>>(`${this.api}/summary`).pipe(map((res) => res.data));
  }

  getHospital(): Observable<SuperAdminHospital> {
    return this.http.get<ApiResponse<SuperAdminHospital>>(`${this.api}/hospital`).pipe(map((res) => res.data));
  }

  getHospitals(): Observable<SuperAdminHospitalProfile[]> {
    return this.http.get<ApiResponse<SuperAdminHospitalProfile[]>>(`${this.api}/hospitals`).pipe(map((res) => res.data ?? []));
  }

  updateHospital(profile: SuperAdminHospitalProfile): Observable<SuperAdminHospitalProfile> {
    return this.http.put<ApiResponse<SuperAdminHospitalProfile>>(`${this.api}/hospital`, profile).pipe(map((res) => res.data));
  }

  createBranch(branch: UpsertBranch): Observable<SuperAdminBranch> {
    return this.http.post<ApiResponse<SuperAdminBranch>>(`${this.api}/branches`, branch).pipe(map((res) => res.data));
  }

  updateBranch(branchId: number, branch: UpsertBranch): Observable<SuperAdminBranch> {
    return this.http.put<ApiResponse<SuperAdminBranch>>(`${this.api}/branches/${branchId}`, branch).pipe(map((res) => res.data));
  }

  getAdmins(): Observable<SuperAdminUser[]> {
    return this.http.get<ApiResponse<SuperAdminUser[]>>(`${this.api}/admins`).pipe(map((res) => res.data ?? []));
  }

  createAdmin(payload: CreateAdminUser): Observable<SuperAdminUser> {
    return this.http.post<ApiResponse<SuperAdminUser>>(`${this.api}/admins`, payload).pipe(map((res) => res.data));
  }

  updateAdminStatus(userId: number, isActive: boolean): Observable<SuperAdminUser> {
    return this.http.put<ApiResponse<SuperAdminUser>>(`${this.api}/admins/${userId}/status`, { isActive }).pipe(map((res) => res.data));
  }
}
