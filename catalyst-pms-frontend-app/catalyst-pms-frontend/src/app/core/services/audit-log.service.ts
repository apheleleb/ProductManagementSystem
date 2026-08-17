import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { AuditLogEntry } from '../models/audit-log.model';

export interface AuditLogFilters {
  productId?: number;
  actionType?: string;
}

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private readonly baseUrl = `${environment.apiUrl}/auditlogs`;

  constructor(private http: HttpClient) {}

  getAll(filters?: AuditLogFilters): Observable<ApiResponse<AuditLogEntry[]>> {
    let params = new HttpParams();
    if (filters?.productId) params = params.set('productId', filters.productId);
    if (filters?.actionType) params = params.set('actionType', filters.actionType);
    return this.http.get<ApiResponse<AuditLogEntry[]>>(this.baseUrl, { params });
  }
}
