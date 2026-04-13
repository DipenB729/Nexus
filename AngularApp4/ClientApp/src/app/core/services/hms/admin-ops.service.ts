import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponse } from '../../models/hms/auth.model';
import {
  BackupCenter,
  ControlCenterSettings,
  DashboardSummary,
  NotificationSettings,
  OrganizationSettings,
  RoleDetails,
  SecurityCenter,
  SecuritySettings,
  SystemSettings
} from '../../models/hms/admin-ops.model';

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

  getControlCenter(): Observable<ControlCenterSettings> {
    return this.http
      .get<ApiResponse<ControlCenterSettings>>('/api/settings/control-center')
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

  updateNotificationSettings(payload: NotificationSettings): Observable<NotificationSettings> {
    return this.http
      .put<ApiResponse<NotificationSettings>>('/api/settings/notifications', payload)
      .pipe(map((res) => res.data));
  }

  updateSystemSettings(payload: SystemSettings): Observable<SystemSettings> {
    return this.http
      .put<ApiResponse<SystemSettings>>('/api/settings/system', payload)
      .pipe(map((res) => res.data));
  }

  updateSecuritySettings(payload: SecuritySettings): Observable<SecurityCenter> {
    return this.http
      .put<ApiResponse<SecurityCenter>>('/api/settings/security', payload)
      .pipe(map((res) => res.data));
  }

  markPermissionReview(): Observable<SecurityCenter['permissionReview']> {
    return this.http
      .post<ApiResponse<SecurityCenter['permissionReview']>>('/api/settings/security/permission-review', {})
      .pipe(map((res) => res.data));
  }

  createManualBackup(backupName: string): Observable<BackupCenter> {
    return this.http
      .post<ApiResponse<BackupCenter>>('/api/settings/backups/manual', { backupName })
      .pipe(map((res) => res.data));
  }

  restoreBackup(backupLogId: number): Observable<BackupCenter> {
    return this.http
      .post<ApiResponse<BackupCenter>>(`/api/settings/backups/${backupLogId}/restore`, {})
      .pipe(map((res) => res.data));
  }
}
