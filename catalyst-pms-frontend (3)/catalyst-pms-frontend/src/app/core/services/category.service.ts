import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { Category, CreateCategoryRequest, UpdateCategoryRequest } from '../models/category.model';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly baseUrl = `${environment.apiUrl}/categories`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<Category[]>> {
    return this.http.get<ApiResponse<Category[]>>(this.baseUrl);
  }

  create(request: CreateCategoryRequest): Observable<ApiResponse<{ categoryId: number }>> {
    return this.http.post<ApiResponse<{ categoryId: number }>>(this.baseUrl, request);
  }

  update(id: number, request: UpdateCategoryRequest): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.baseUrl}/${id}`, request);
  }

  deactivate(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`);
  }
}
