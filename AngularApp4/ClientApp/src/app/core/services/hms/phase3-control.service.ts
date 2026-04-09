import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponse } from '../../models/hms/auth.model';
import {
  AdmissionRecord,
  AppointmentAdminRecord,
  AppointmentTokenSettings,
  ApproveDischargePayload,
  CreatePatientPayload,
  CreateAdmissionPayload,
  DoctorScheduleRecord,
  ManageAppointmentPayload,
  MergePatientPayload,
  PatientRecord,
  SavePatientPayload,
  TransferAdmissionPayload
} from '../../models/hms/phase3-control.model';

@Injectable({ providedIn: 'root' })
export class Phase3ControlService {
  constructor(private readonly http: HttpClient) {}

  getPatients(search = '', patientCategoryId?: number | null, includeMerged = false): Observable<PatientRecord[]> {
    let params = new HttpParams().set('includeMerged', includeMerged);
    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    if (patientCategoryId) {
      params = params.set('patientCategoryId', patientCategoryId);
    }

    return this.http
      .get<ApiResponse<PatientRecord[]>>('/api/admin/patients', { params })
      .pipe(map((res) => res.data));
  }

  createPatient(payload: CreatePatientPayload): Observable<PatientRecord> {
    return this.http
      .post<ApiResponse<PatientRecord>>('/api/admin/patients', payload)
      .pipe(map((res) => res.data));
  }

  updatePatient(patientId: number, payload: SavePatientPayload): Observable<PatientRecord> {
    return this.http
      .put<ApiResponse<PatientRecord>>(`/api/admin/patients/${patientId}`, payload)
      .pipe(map((res) => res.data));
  }

  mergePatients(payload: MergePatientPayload): Observable<PatientRecord> {
    return this.http
      .post<ApiResponse<PatientRecord>>('/api/admin/patients/merge', payload)
      .pipe(map((res) => res.data));
  }

  getAppointments(search = '', status?: string | null, doctorId?: number | null): Observable<AppointmentAdminRecord[]> {
    let params = new HttpParams();
    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    if (status) {
      params = params.set('status', status);
    }

    if (doctorId) {
      params = params.set('doctorId', doctorId);
    }

    return this.http
      .get<ApiResponse<AppointmentAdminRecord[]>>('/api/admin/appointments', { params })
      .pipe(map((res) => res.data));
  }

  manageAppointment(appointmentId: number, payload: ManageAppointmentPayload): Observable<AppointmentAdminRecord> {
    return this.http
      .put<ApiResponse<AppointmentAdminRecord>>(`/api/admin/appointments/${appointmentId}/manage`, payload)
      .pipe(map((res) => res.data));
  }

  getTokenSettings(): Observable<AppointmentTokenSettings> {
    return this.http
      .get<ApiResponse<AppointmentTokenSettings>>('/api/admin/appointments/token-settings')
      .pipe(map((res) => res.data));
  }

  updateTokenSettings(payload: AppointmentTokenSettings): Observable<AppointmentTokenSettings> {
    return this.http
      .put<ApiResponse<AppointmentTokenSettings>>('/api/admin/appointments/token-settings', payload)
      .pipe(map((res) => res.data));
  }

  getDoctorSchedules(doctorId: number): Observable<DoctorScheduleRecord[]> {
    return this.http
      .get<ApiResponse<DoctorScheduleRecord[]>>(`/api/doctors/${doctorId}/schedules`)
      .pipe(map((res) => res.data));
  }

  createDoctorSchedule(doctorId: number, payload: Partial<DoctorScheduleRecord>): Observable<DoctorScheduleRecord> {
    return this.http
      .post<ApiResponse<DoctorScheduleRecord>>(`/api/doctors/${doctorId}/schedules`, payload)
      .pipe(map((res) => res.data));
  }

  updateDoctorSchedule(scheduleId: number, payload: Partial<DoctorScheduleRecord>): Observable<DoctorScheduleRecord> {
    return this.http
      .put<ApiResponse<DoctorScheduleRecord>>(`/api/schedules/${scheduleId}`, payload)
      .pipe(map((res) => res.data));
  }

  deleteDoctorSchedule(scheduleId: number): Observable<void> {
    return this.http
      .delete<ApiResponse<null>>(`/api/schedules/${scheduleId}`)
      .pipe(map(() => void 0));
  }

  getAdmissions(search = '', status?: string | null): Observable<AdmissionRecord[]> {
    let params = new HttpParams();
    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    if (status) {
      params = params.set('status', status);
    }

    return this.http
      .get<ApiResponse<AdmissionRecord[]>>('/api/admin/admissions', { params })
      .pipe(map((res) => res.data));
  }

  createAdmission(payload: CreateAdmissionPayload): Observable<AdmissionRecord> {
    return this.http
      .post<ApiResponse<AdmissionRecord>>('/api/admin/admissions', payload)
      .pipe(map((res) => res.data));
  }

  transferAdmission(admissionId: number, payload: TransferAdmissionPayload): Observable<AdmissionRecord> {
    return this.http
      .post<ApiResponse<AdmissionRecord>>(`/api/admin/admissions/${admissionId}/transfer`, payload)
      .pipe(map((res) => res.data));
  }

  approveDischarge(admissionId: number, payload: ApproveDischargePayload): Observable<AdmissionRecord> {
    return this.http
      .post<ApiResponse<AdmissionRecord>>(`/api/admin/admissions/${admissionId}/discharge`, payload)
      .pipe(map((res) => res.data));
  }
}
