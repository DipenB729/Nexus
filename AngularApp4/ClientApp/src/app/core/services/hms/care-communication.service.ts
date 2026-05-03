import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiResponse } from '../../models/hms/auth.model';
import {
  CareConversationMessage,
  CareThreadDetail,
  CareThreadSummary,
  PatientCaseReport,
  SaveCareMessagePayload,
  SavePatientReportPayload
} from '../../models/hms/care-communication.model';

@Injectable({ providedIn: 'root' })
export class CareCommunicationService {
  private readonly api = '/api/care-communication';

  constructor(private readonly http: HttpClient) {}

  getThreads(): Observable<CareThreadSummary[]> {
    return this.http.get<ApiResponse<CareThreadSummary[]>>(`${this.api}/threads`).pipe(map((res) => res.data ?? []));
  }

  getThread(appointmentId: number): Observable<CareThreadDetail> {
    return this.http.get<ApiResponse<CareThreadDetail>>(`${this.api}/threads/${appointmentId}`).pipe(map((res) => res.data));
  }

  sendMessage(appointmentId: number, payload: SaveCareMessagePayload): Observable<CareConversationMessage> {
    return this.http.post<ApiResponse<CareConversationMessage>>(`${this.api}/threads/${appointmentId}/messages`, payload).pipe(map((res) => res.data));
  }

  submitReport(appointmentId: number, payload: SavePatientReportPayload): Observable<PatientCaseReport> {
    return this.http.post<ApiResponse<PatientCaseReport>>(`${this.api}/threads/${appointmentId}/reports`, payload).pipe(map((res) => res.data));
  }

  submitReportForm(appointmentId: number, formData: FormData): Observable<PatientCaseReport> {
    return this.http.post<ApiResponse<PatientCaseReport>>(`${this.api}/threads/${appointmentId}/reports/upload`, formData).pipe(map((res) => res.data));
  }
}
