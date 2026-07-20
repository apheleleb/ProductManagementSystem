import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { ApproveProductRequest, RejectProductRequest } from '../models/approval.model';

@Injectable({ providedIn: 'root' })
export class WorkflowService {
  private readonly baseUrl = `${environment.apiUrl}/workflow`;

  constructor(private http: HttpClient) {}

  approve(productId: number, request: ApproveProductRequest): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.baseUrl}/${productId}/approve`, request);
  }

  reject(productId: number, request: RejectProductRequest): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.baseUrl}/${productId}/reject`, request);
  }

  publish(productId: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.baseUrl}/${productId}/publish`, {});
  }

  unpublish(productId: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.baseUrl}/${productId}/unpublish`, {});
  }

  archive(productId: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.baseUrl}/${productId}/archive`, {});
  }

  restore(productId: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.baseUrl}/${productId}/restore`, {});
  }
}
