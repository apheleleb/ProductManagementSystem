import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { AppNotification } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly baseUrl = `${environment.apiUrl}/notifications`;

  constructor(private http: HttpClient) {}

  getMine(): Observable<ApiResponse<AppNotification[]>> {
    return this.http.get<ApiResponse<AppNotification[]>>(`${this.baseUrl}/mine`);
  }

  getUnreadCount(): Observable<ApiResponse<{ count: number }>> {
    return this.http.get<ApiResponse<{ count: number }>>(`${this.baseUrl}/unread-count`);
  }

  markAsRead(id: number): Observable<ApiResponse<null>> {
    return this.http.patch<ApiResponse<null>>(`${this.baseUrl}/${id}/read`, {});
  }
}
