import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponse } from '../../models/hms/auth.model';
import { BedMaster, DepartmentMaster, DoctorAvailableSlot, DoctorMaster, PatientCategoryMaster, StaffMaster, WardMaster } from '../../models/hms/master-setup.model';

@Injectable({ providedIn: 'root' })
export class MasterSetupService {
  constructor(private readonly http: HttpClient) {}

  getDepartments(): Observable<DepartmentMaster[]> {
    return this.http.get<ApiResponse<DepartmentMaster[]>>('/api/departments').pipe(map((res) => res.data));
  }

  createDepartment(payload: Partial<DepartmentMaster>): Observable<DepartmentMaster> {
    return this.http.post<ApiResponse<DepartmentMaster>>('/api/departments', payload).pipe(map((res) => res.data));
  }

  updateDepartment(departmentId: number, payload: Partial<DepartmentMaster>): Observable<DepartmentMaster> {
    return this.http.put<ApiResponse<DepartmentMaster>>(`/api/departments/${departmentId}`, payload).pipe(map((res) => res.data));
  }

  setDepartmentStatus(departmentId: number, isActive: boolean): Observable<void> {
    return this.http.put<ApiResponse<null>>(`/api/departments/${departmentId}/status`, { isActive }).pipe(map(() => void 0));
  }

  getDoctors(isActive?: boolean): Observable<DoctorMaster[]> {
    let params = new HttpParams();
    if (typeof isActive === 'boolean') {
      params = params.set('isActive', isActive);
    }

    return this.http.get<ApiResponse<DoctorMaster[]>>('/api/doctors', { params }).pipe(map((res) => res.data));
  }

  getDoctor(doctorId: number): Observable<DoctorMaster> {
    return this.http.get<ApiResponse<DoctorMaster>>(`/api/doctors/${doctorId}`).pipe(map((res) => res.data));
  }

  getAvailableSlots(doctorId: number, date: string): Observable<DoctorAvailableSlot[]> {
    const params = new HttpParams().set('date', date);
    return this.http
      .get<ApiResponse<DoctorAvailableSlot[]>>(`/api/doctors/${doctorId}/available-slots`, { params })
      .pipe(map((res) => res.data));
  }

  createDoctor(payload: Partial<DoctorMaster>): Observable<DoctorMaster> {
    return this.http.post<ApiResponse<DoctorMaster>>('/api/doctors', payload).pipe(map((res) => res.data));
  }

  updateDoctor(doctorId: number, payload: Partial<DoctorMaster>): Observable<DoctorMaster> {
    return this.http.put<ApiResponse<DoctorMaster>>(`/api/doctors/${doctorId}`, payload).pipe(map((res) => res.data));
  }

  setDoctorStatus(doctorId: number, isActive: boolean): Observable<void> {
    return this.http.put<ApiResponse<null>>(`/api/doctors/${doctorId}/status`, { isActive }).pipe(map(() => void 0));
  }

  getStaff(): Observable<StaffMaster[]> {
    return this.http.get<ApiResponse<StaffMaster[]>>('/api/staff').pipe(map((res) => res.data));
  }

  createStaff(payload: Partial<StaffMaster>): Observable<StaffMaster> {
    return this.http.post<ApiResponse<StaffMaster>>('/api/staff', payload).pipe(map((res) => res.data));
  }

  updateStaff(staffId: number, payload: Partial<StaffMaster>): Observable<StaffMaster> {
    return this.http.put<ApiResponse<StaffMaster>>(`/api/staff/${staffId}`, payload).pipe(map((res) => res.data));
  }

  setStaffStatus(staffId: number, isActive: boolean): Observable<void> {
    return this.http.put<ApiResponse<null>>(`/api/staff/${staffId}/status`, { isActive }).pipe(map(() => void 0));
  }

  getPatientCategories(): Observable<PatientCategoryMaster[]> {
    return this.http.get<ApiResponse<PatientCategoryMaster[]>>('/api/patient-categories').pipe(map((res) => res.data));
  }

  createPatientCategory(payload: Partial<PatientCategoryMaster>): Observable<PatientCategoryMaster> {
    return this.http.post<ApiResponse<PatientCategoryMaster>>('/api/patient-categories', payload).pipe(map((res) => res.data));
  }

  updatePatientCategory(patientCategoryId: number, payload: Partial<PatientCategoryMaster>): Observable<PatientCategoryMaster> {
    return this.http.put<ApiResponse<PatientCategoryMaster>>(`/api/patient-categories/${patientCategoryId}`, payload).pipe(map((res) => res.data));
  }

  setPatientCategoryStatus(patientCategoryId: number, isActive: boolean): Observable<void> {
    return this.http.put<ApiResponse<null>>(`/api/patient-categories/${patientCategoryId}/status`, { isActive }).pipe(map(() => void 0));
  }

  getWards(): Observable<WardMaster[]> {
    return this.http.get<ApiResponse<WardMaster[]>>('/api/wards').pipe(map((res) => res.data));
  }

  createWard(payload: Partial<WardMaster>): Observable<WardMaster> {
    return this.http.post<ApiResponse<WardMaster>>('/api/wards', payload).pipe(map((res) => res.data));
  }

  updateWard(wardId: number, payload: Partial<WardMaster>): Observable<WardMaster> {
    return this.http.put<ApiResponse<WardMaster>>(`/api/wards/${wardId}`, payload).pipe(map((res) => res.data));
  }

  setWardStatus(wardId: number, isActive: boolean): Observable<void> {
    return this.http.put<ApiResponse<null>>(`/api/wards/${wardId}/status`, { isActive }).pipe(map(() => void 0));
  }

  getBeds(): Observable<BedMaster[]> {
    return this.http.get<ApiResponse<BedMaster[]>>('/api/beds').pipe(map((res) => res.data));
  }

  createBed(payload: Partial<BedMaster>): Observable<BedMaster> {
    return this.http.post<ApiResponse<BedMaster>>('/api/beds', payload).pipe(map((res) => res.data));
  }

  updateBed(bedId: number, payload: Partial<BedMaster>): Observable<BedMaster> {
    return this.http.put<ApiResponse<BedMaster>>(`/api/beds/${bedId}`, payload).pipe(map((res) => res.data));
  }

  setBedStatus(bedId: number, isActive: boolean): Observable<void> {
    return this.http.put<ApiResponse<null>>(`/api/beds/${bedId}/status`, { isActive }).pipe(map(() => void 0));
  }
}
