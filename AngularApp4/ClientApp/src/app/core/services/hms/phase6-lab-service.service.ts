import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponse } from '../../models/hms/auth.model';
import {
  LabTestMaster,
  SaveLabTestMasterPayload,
  SaveServicePackagePayload,
  ServicePackage
} from '../../models/hms/phase6-lab-service.model';

@Injectable({ providedIn: 'root' })
export class Phase6LabServiceService {
  constructor(private readonly http: HttpClient) {}

  getLabTests(): Observable<LabTestMaster[]> {
    return this.http
      .get<ApiResponse<LabTestMaster[]>>('/api/admin/laboratory/lab-tests')
      .pipe(map((res) => res.data));
  }

  createLabTest(payload: SaveLabTestMasterPayload): Observable<LabTestMaster> {
    return this.http
      .post<ApiResponse<LabTestMaster>>('/api/admin/laboratory/lab-tests', payload)
      .pipe(map((res) => res.data));
  }

  updateLabTest(labTestMasterId: number, payload: SaveLabTestMasterPayload): Observable<LabTestMaster> {
    return this.http
      .put<ApiResponse<LabTestMaster>>(`/api/admin/laboratory/lab-tests/${labTestMasterId}`, payload)
      .pipe(map((res) => res.data));
  }

  getPackages(kind?: string | null): Observable<ServicePackage[]> {
    const query = kind ? `?kind=${encodeURIComponent(kind)}` : '';
    return this.http
      .get<ApiResponse<ServicePackage[]>>(`/api/admin/laboratory/packages${query}`)
      .pipe(map((res) => res.data));
  }

  createPackage(payload: SaveServicePackagePayload): Observable<ServicePackage> {
    return this.http
      .post<ApiResponse<ServicePackage>>('/api/admin/laboratory/packages', payload)
      .pipe(map((res) => res.data));
  }

  updatePackage(servicePackageId: number, payload: SaveServicePackagePayload): Observable<ServicePackage> {
    return this.http
      .put<ApiResponse<ServicePackage>>(`/api/admin/laboratory/packages/${servicePackageId}`, payload)
      .pipe(map((res) => res.data));
  }
}
