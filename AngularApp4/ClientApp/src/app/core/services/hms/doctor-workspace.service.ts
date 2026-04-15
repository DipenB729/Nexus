import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiResponse } from '../../models/hms/auth.model';
import {
  DoctorAvailabilityExceptionRecord,
  DoctorAvailabilityWorkspace,
  DoctorScheduleRecord,
  DoctorWorkspaceAppointmentDetail,
  LabTestSearchResult,
  MedicineSearchResult,
  PrescriptionRecord,
  DiagnosticRequestRecord,
  DoctorConsultationRecord,
  SaveConsultationPayload,
  SaveDiagnosticRequestPayload,
  SaveDoctorAvailabilityExceptionPayload,
  SavePrescriptionPayload
} from '../../models/hms/doctor-workspace.model';

@Injectable({ providedIn: 'root' })
export class DoctorWorkspaceService {
  private readonly api = '/api/doctor/workspace';

  constructor(private readonly http: HttpClient) {}

  getAvailability(): Observable<DoctorAvailabilityWorkspace> {
    return this.http.get<ApiResponse<DoctorAvailabilityWorkspace>>(`${this.api}/availability`).pipe(map((res) => res.data));
  }

  createSchedule(payload: Partial<DoctorScheduleRecord>): Observable<DoctorScheduleRecord> {
    return this.http.post<ApiResponse<DoctorScheduleRecord>>(`${this.api}/schedules`, payload).pipe(map((res) => res.data));
  }

  updateSchedule(scheduleId: number, payload: Partial<DoctorScheduleRecord>): Observable<DoctorScheduleRecord> {
    return this.http.put<ApiResponse<DoctorScheduleRecord>>(`${this.api}/schedules/${scheduleId}`, payload).pipe(map((res) => res.data));
  }

  deleteSchedule(scheduleId: number): Observable<void> {
    return this.http.delete<ApiResponse<unknown>>(`${this.api}/schedules/${scheduleId}`).pipe(map(() => undefined));
  }

  createAvailabilityException(payload: SaveDoctorAvailabilityExceptionPayload): Observable<DoctorAvailabilityExceptionRecord> {
    return this.http.post<ApiResponse<DoctorAvailabilityExceptionRecord>>(`${this.api}/exceptions`, payload).pipe(map((res) => res.data));
  }

  deleteAvailabilityException(exceptionId: number): Observable<void> {
    return this.http.delete<ApiResponse<unknown>>(`${this.api}/exceptions/${exceptionId}`).pipe(map(() => undefined));
  }

  getAppointmentDetail(appointmentId: number): Observable<DoctorWorkspaceAppointmentDetail> {
    return this.http.get<ApiResponse<DoctorWorkspaceAppointmentDetail>>(`${this.api}/appointments/${appointmentId}`).pipe(map((res) => res.data));
  }

  saveConsultation(appointmentId: number, payload: SaveConsultationPayload): Observable<DoctorConsultationRecord> {
    return this.http.put<ApiResponse<DoctorConsultationRecord>>(`${this.api}/appointments/${appointmentId}/consultation`, payload).pipe(map((res) => res.data));
  }

  savePrescription(appointmentId: number, payload: SavePrescriptionPayload): Observable<PrescriptionRecord> {
    return this.http.post<ApiResponse<PrescriptionRecord>>(`${this.api}/appointments/${appointmentId}/prescriptions`, payload).pipe(map((res) => res.data));
  }

  saveDiagnosticRequest(appointmentId: number, payload: SaveDiagnosticRequestPayload): Observable<DiagnosticRequestRecord> {
    return this.http.post<ApiResponse<DiagnosticRequestRecord>>(`${this.api}/appointments/${appointmentId}/requests`, payload).pipe(map((res) => res.data));
  }

  searchMedicines(search = ''): Observable<MedicineSearchResult[]> {
    const query = encodeURIComponent(search.trim());
    return this.http.get<ApiResponse<MedicineSearchResult[]>>(`${this.api}/medicines?search=${query}`).pipe(map((res) => res.data ?? []));
  }

  searchLabTests(search = ''): Observable<LabTestSearchResult[]> {
    const query = encodeURIComponent(search.trim());
    return this.http.get<ApiResponse<LabTestSearchResult[]>>(`${this.api}/lab-tests?search=${query}`).pipe(map((res) => res.data ?? []));
  }
}
