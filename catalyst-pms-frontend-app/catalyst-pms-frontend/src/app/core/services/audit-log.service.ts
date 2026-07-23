import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { AuditLogEntry } from '../models/audit-log.model';

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private readonly baseUrl = `${environment.apiUrl}/auditlogs`;

  constructor(private http: HttpClient) {}

  getByProduct(productId: number): Observable<ApiResponse<AuditLogEntry[]>> {
    return this.http.get<ApiResponse<AuditLogEntry[]>>(`${this.baseUrl}/product/${productId}`);
  }
}
