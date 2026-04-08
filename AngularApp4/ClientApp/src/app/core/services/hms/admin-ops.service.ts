import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponse } from '../../models/hms/auth.model';
import { DashboardSummary, OrganizationSettings, RoleDetails } from '../../models/hms/admin-ops.model';

@Injectable({ providedIn: 'root' })
export class AdminOpsService {
  constructor(private readonly http: HttpClient) {}

  getDashboardSummary(): Observable<DashboardSummary> {
    return this.http
      .get<ApiResponse<DashboardSummary>>('/api/dashboard/admin-summary')
      .pipe(map((res) => res.data));
  }

  getRoles(): Observable<RoleDetails[]> {
    return this.http
      .get<ApiResponse<RoleDetails[]>>('/api/roles')
      .pipe(map((res) => res.data));
  }

  createRole(payload: Partial<RoleDetails>): Observable<RoleDetails> {
    return this.http
      .post<ApiResponse<RoleDetails>>('/api/roles', payload)
      .pipe(map((res) => res.data));
  }

  updateRole(roleId: number, payload: Partial<RoleDetails>): Observable<RoleDetails> {
    return this.http
      .put<ApiResponse<RoleDetails>>(`/api/roles/${roleId}`, payload)
      .pipe(map((res) => res.data));
  }

  getOrganizationSettings(): Observable<OrganizationSettings> {
    return this.http
      .get<ApiResponse<OrganizationSettings>>('/api/settings/organization')
      .pipe(map((res) => res.data));
  }

  updateOrganizationSettings(payload: OrganizationSettings): Observable<OrganizationSettings> {
    return this.http
      .put<ApiResponse<OrganizationSettings>>('/api/settings/organization', payload)
      .pipe(map((res) => res.data));
  }
}
