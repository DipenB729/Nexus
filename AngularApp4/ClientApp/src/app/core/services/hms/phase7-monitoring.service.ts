import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponse } from '../../models/hms/auth.model';
import { AuditMonitoring, MonitoringReports } from '../../models/hms/phase7-monitoring.model';

@Injectable({ providedIn: 'root' })
export class Phase7MonitoringService {
  constructor(private readonly http: HttpClient) {}

  getReports(): Observable<MonitoringReports> {
    return this.http
      .get<ApiResponse<MonitoringReports>>('/api/admin/monitoring/reports')
      .pipe(map((res) => res.data));
  }

  getAudit(): Observable<AuditMonitoring> {
    return this.http
      .get<ApiResponse<AuditMonitoring>>('/api/admin/monitoring/audit')
      .pipe(map((res) => res.data));
  }
}
