import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subject, Observable, map } from 'rxjs';
import { ApiResponse, AppNotification } from '../models/hms/auth.model';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly refreshSubject = new Subject<void>();

  readonly refresh$ = this.refreshSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  getNotifications(limit = 12): Observable<AppNotification[]> {
    return this.http
      .get<ApiResponse<AppNotification[]>>(`/api/notifications?limit=${limit}`)
      .pipe(map((res) => res.data ?? []));
  }

  markAsRead(notificationId: number): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`/api/notifications/${notificationId}/read`, {})
      .pipe(map(() => undefined));
  }

  markAllAsRead(): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>('/api/notifications/read-all', {})
      .pipe(map(() => undefined));
  }

  requestRefresh(): void {
    this.refreshSubject.next();
  }
}
